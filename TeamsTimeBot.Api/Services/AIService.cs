#pragma warning disable OPENAI001

using Azure.Identity;
using OpenAI.Responses;
using System.ClientModel.Primitives;
using System.Text.Json;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class AIService
{
    private readonly ResponsesClient _client;
    private readonly string _deploymentName;
    private readonly ActionExecutor _actionExecutor;

    public AIService(
        IConfiguration configuration,
        ActionExecutor actionExecutor)
    {
        _actionExecutor = actionExecutor;

        var endpoint =
            configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException(
                "AzureOpenAI:Endpoint is not configured.");

        _deploymentName =
            configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException(
                "AzureOpenAI:DeploymentName is not configured.");

        var tenantId =
            configuration["AzureAd:TenantId"]
            ?? throw new InvalidOperationException(
                "AzureAd:TenantId is not configured.");

        var clientId =
            configuration["AzureAd:ClientId"]
            ?? throw new InvalidOperationException(
                "AzureAd:ClientId is not configured.");

        var clientSecret =
            configuration["AzureAd:ClientSecret"]
            ?? throw new InvalidOperationException(
                "AzureAd:ClientSecret is not configured.");

        var credential =
            new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret);

        var policy =
            new BearerTokenPolicy(
                credential,
                "https://cognitiveservices.azure.com/.default");

        var options =
            new ResponsesClientOptions
            {
                Endpoint = new Uri(endpoint)
            };

        _client =
            new ResponsesClient(
                policy,
                options);
    }

    // =========================================================
    // AKTUALNA DATA I CZAS W POLSCE
    // =========================================================

    private static DateTime GetPolandDateTime()
    {
        TimeZoneInfo timeZone;

        try
        {
            // macOS / Linux
            timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Europe/Warsaw");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows
            timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Central European Standard Time");
        }

        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            timeZone);
    }

    // =========================================================
    // GŁÓWNA METODA AI
    // =========================================================

    public async Task<string?> GetResponseAsync(
        List<ConversationMessage> messages,
        string userAzureId)
    {
        var inputItems =
            new List<ResponseItem>();

        var now =
            GetPolandDateTime();

        var currentDate =
            now.ToString("yyyy-MM-dd");

        var currentDateTime =
            now.ToString("yyyy-MM-dd HH:mm");

        // =========================================================
        // SYSTEM / DEVELOPER INSTRUCTIONS
        // =========================================================

        inputItems.Add(
            ResponseItem.CreateDeveloperMessageItem(
                $"""
                Jesteś firmowym asystentem AI działającym wewnątrz Microsoft Teams.

                Pomagasz pracownikom zarządzać zadaniami i czasem pracy.

                ZASADY:

                - Rozmawiaj naturalnie po polsku.
                - Rozumiej potoczny język, skróty, literówki i brak polskich znaków.
                - Korzystaj z całej historii rozmowy.
                - Backend i baza danych są źródłem prawdy.
                - Nigdy nie wymyślaj danych zadań.
                - Jeżeli potrzebujesz wykonać operację, użyj odpowiedniego narzędzia.
                - Nie informuj użytkownika o sukcesie operacji przed otrzymaniem wyniku narzędzia.
                - Po otrzymaniu wyniku narzędzia przygotuj naturalną odpowiedź dla użytkownika.
                - Nie pokazuj użytkownikowi nazw narzędzi, JSON ani szczegółów technicznych.
                - Jeśli nie jesteś pewien, poproś użytkownika o doprecyzowanie.
          
                ZASADY UPRAWNIEŃ:

                Nigdy samodzielnie nie zakładaj, że użytkownik nie ma uprawnień do wykonania operacji.

                Nie informuj użytkownika o braku uprawnień przed wywołaniem odpowiedniego narzędzia.

                Uprawnienia są sprawdzane wyłącznie przez backend podczas wykonywania narzędzia.

                Jeżeli użytkownik prosi o raport innej osoby lub raport całego zespołu:
                1. Rozpoznaj intencję.
                2. Wywołaj odpowiednie narzędzie.
                3. Poczekaj na wynik narzędzia.
                4. Jeżeli narzędzie zwróci statusCode 403, poinformuj użytkownika o braku uprawnień.
                5. Jeżeli narzędzie zwróci statusCode 200, przedstaw wynik.
                6. Nigdy nie zgaduj ani nie zakładaj statusu uprawnień na podstawie treści rozmowy.

                Przykład:

                Użytkownik: "pokaż raport Maćka"

                NIE WOLNO:
                "Nie masz uprawnień do przeglądania raportów innych osób."

                NALEŻY:
                wywołać narzędzie get_user_work_report.

                Dopiero wynik narzędzia decyduje, czy użytkownik może zobaczyć raport.
 

                =========================================================
                DATA I CZAS
                =========================================================

                Aktualna data i czas użytkownika:
                {currentDateTime}

                Aktualna data:
                {currentDate}

                Strefa czasowa:
                Europe/Warsaw

                Interpretuj określenia względne na podstawie aktualnej daty:

                - "dzisiaj" → aktualna data
                - "wczoraj" → aktualna data minus 1 dzień
                - "przedwczoraj" → aktualna data minus 2 dni
                - "jutro" → aktualna data plus 1 dzień
                - "pojutrze" → aktualna data plus 2 dni

                Jeżeli użytkownik poda konkretną datę, użyj podanej daty.

                Jeżeli użytkownik podaje czas pracy bez określenia daty,
                dla manual_time przyjmij dzisiejszą datę.

                Dla manual_time zawsze przekaż workDate w formacie:
                YYYY-MM-DD.

                Nie zgaduj daty niezależnie od aktualnej daty przekazanej powyżej.

                =========================================================
                WYBÓR ZADANIA
                =========================================================

                - taskId używaj tylko wtedy, gdy użytkownik podał konkretny identyfikator.
                - taskName przekazuj możliwie dokładnie tak, jak podał go użytkownik.
                - Nie zgaduj identyfikatora zadania.
                - Backend sam sprawdzi, czy zadanie istnieje.
                - Jeżeli backend zwróci kilka pasujących zadań, nie wybieraj jednego samodzielnie.
                - W przypadku kilku pasujących zadań wymień ich konkretne nazwy i poproś użytkownika o wybór.

                =========================================================
                CZAS PRACY
                =========================================================

                - start_time rozpoczyna pomiar czasu.
                - stop_time zatrzymuje aktywny pomiar czasu.
                - manual_time dodaje ręcznie podany czas (czas pracy mozna zalgowac tylko sobie)
                - finish_task oznacza zadanie jako zakończone.

                =========================================================
                RAPORTY CZASU PRACY
                =========================================================

                Użytkownik może poprosić o raport swojego czasu pracy.

                Przykłady:
                - "Ile dzisiaj pracowałem?"
                - "Ile pracowałem wczoraj?"
                - "Ile pracowałem w tym tygodniu?"
                - "Pokaż mój czas z tego miesiąca."

                W takich przypadkach użyj get_my_work_report.

                Administrator może dodatkowo otrzymywać:
                - raport konkretnego pracownika,
                - raport całego zespołu,
                - dane dotyczące czasu pracy innych użytkowników.

                Pracownik nie może otrzymywać danych dotyczących czasu pracy innych użytkowników.

                Jeżeli narzędzie zwróci FORBIDDEN:
                - poinformuj użytkownika, że nie ma uprawnień do tych danych,
                - nie próbuj ponownie wywoływać tego samego narzędzia.

                Nigdy nie próbuj omijać ograniczeń uprawnień.

                Nie obliczaj czasu pracy samodzielnie na podstawie rozmowy.
                Raport zawsze pobieraj z backendu.

                Dla raportów:
                - "dzisiaj" = aktualna data
                - "wczoraj" = aktualna data minus 1 dzień
                - "ten tydzień" = bieżący tydzień
                - "zeszły tydzień" = poprzedni tydzień
                - "ten miesiąc" = od pierwszego dnia bieżącego miesiąca do aktualnej daty
                - "zeszły miesiąc" = cały poprzedni miesiąc

                Daty przekazuj do narzędzi w formacie YYYY-MM-DD.

                Jeżeli użytkownik pyta o dane innego pracownika,
                nie wymyślaj jego AzureId.

                =========================================================
                KOMENTARZE
                =========================================================

                - comment_task służy do dodania komentarza do istniejącego zadania.
                - Jeżeli użytkownik chce tylko dodać komentarz, użyj comment_task.
                - Nie używaj edit_task tylko dlatego, że komentarz dotyczy zadania.

                =========================================================
                EDYCJA
                =========================================================

                - edit_task służy do zmiany nazwy lub opisu istniejącego zadania.
                - Jeżeli użytkownik chce zmienić nazwę lub opis, użyj edit_task.

                =========================================================
                TWORZENIE
                =========================================================

                - create_task służy wyłącznie do tworzenia nowego zadania.
                - Nie używaj create_task do edycji istniejącego zadania.

                =========================================================
                PO WYKONANIU NARZĘDZIA
                =========================================================

                - SUCCESS oznacza poprawne wykonanie operacji.
                - NOT_FOUND oznacza brak znalezionego zadania lub użytkownika.
                - AMBIGUOUS oznacza kilka pasujących elementów.
                - ALREADY_ACTIVE oznacza, że pomiar już trwa.
                - NO_ACTIVE_WORK oznacza brak aktywnego pomiaru.
                - FORBIDDEN oznacza brak uprawnień.
                - ERROR oznacza błąd operacji.

                - Jeżeli wynik to SUCCESS, potwierdź użytkownikowi wykonanie operacji.
                - Jeżeli wynik to AMBIGUOUS, wymień konkretne nazwy wszystkich kandydatów.
                - Jeżeli wynik to FORBIDDEN, poinformuj o braku uprawnień.
                - Nie wykonuj ponownie tej samej operacji bez nowej prośby użytkownika.

                """));

        // =========================================================
        // HISTORIA ROZMOWY
        // =========================================================

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            switch (message.Role.ToLowerInvariant())
            {
                case "user":

                    inputItems.Add(
                        ResponseItem.CreateUserMessageItem(
                            message.Content));

                    break;

                case "assistant":

                    inputItems.Add(
                        ResponseItem.CreateAssistantMessageItem(
                            message.Content));

                    break;

                case "system":

                    inputItems.Add(
                        ResponseItem.CreateSystemMessageItem(
                            message.Content));

                    break;
            }
        }

        // =========================================================
        // DEFINICJE TOOLS
        // =========================================================

        var startTimeTool =
            ResponseTool.CreateFunctionTool(
                functionName: "start_time",
                functionDescription:
                    "Rozpoczyna pomiar czasu dla istniejącego zadania.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer",
                              "description": "Identyfikator zadania, jeżeli użytkownik go podał."
                            },
                            "taskName": {
                              "type": "string",
                              "description": "Nazwa istniejącego zadania."
                            }
                          },
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var stopTimeTool =
            ResponseTool.CreateFunctionTool(
                functionName: "stop_time",
                functionDescription:
                    "Zatrzymuje aktywny pomiar czasu dla istniejącego zadania.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer"
                            },
                            "taskName": {
                              "type": "string"
                            }
                          },
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var finishTaskTool =
            ResponseTool.CreateFunctionTool(
                functionName: "finish_task",
                functionDescription:
                    "Oznacza istniejące zadanie jako ukończone.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer"
                            },
                            "taskName": {
                              "type": "string"
                            }
                          },
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var createTaskTool =
            ResponseTool.CreateFunctionTool(
                functionName: "create_task",
                functionDescription:
                    "Tworzy nowe zadanie.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "newTaskName": {
                              "type": "string"
                            },
                            "description": {
                              "type": "string"
                            }
                          },
                          "required": [
                            "newTaskName"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var editTaskTool =
            ResponseTool.CreateFunctionTool(
                functionName: "edit_task",
                functionDescription:
                    "Zmienia nazwę lub opis istniejącego zadania.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer"
                            },
                            "taskName": {
                              "type": "string"
                            },
                            "newTaskName": {
                              "type": "string"
                            },
                            "description": {
                              "type": "string"
                            }
                          },
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var commentTaskTool =
            ResponseTool.CreateFunctionTool(
                functionName: "comment_task",
                functionDescription:
                    "Dodaje komentarz do istniejącego zadania.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer"
                            },
                            "taskName": {
                              "type": "string"
                            },
                            "comment": {
                              "type": "string"
                            }
                          },
                          "required": [
                            "comment"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var manualTimeTool =
            ResponseTool.CreateFunctionTool(
                functionName: "manual_time",
                functionDescription:
                    "Dodaje ręcznie podany czas pracy do istniejącego zadania.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "taskId": {
                              "type": "integer"
                            },
                            "taskName": {
                              "type": "string"
                            },
                            "manualMinutes": {
                              "type": "integer"
                            },
                            "workDate": {
                              "type": "string",
                              "description": "Data, której dotyczy czas pracy. Format YYYY-MM-DD."
                            }
                          },
                          "required": [
                            "manualMinutes"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        // =========================================================
        // RAPORT - MÓJ CZAS
        // =========================================================

        var myWorkReportTool =
            ResponseTool.CreateFunctionTool(
                functionName: "get_my_work_report",
                functionDescription:
                    "Pobiera raport czasu pracy aktualnego użytkownika za podany okres.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "startDate": {
                              "type": "string",
                              "description": "Początek okresu. Format YYYY-MM-DD."
                            },
                            "endDate": {
                              "type": "string",
                              "description": "Koniec okresu. Format YYYY-MM-DD."
                            }
                          },
                          "required": [
                            "startDate",
                            "endDate"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        // =========================================================
        // RAPORT - KONKRETNY UŻYTKOWNIK
        // TYLKO ADMIN
        // =========================================================

        var userWorkReportTool =
            ResponseTool.CreateFunctionTool(
                functionName: "get_user_work_report",
                functionDescription:
                    "Pobiera raport czasu pracy konkretnego użytkownika. Dostępne wyłącznie dla administratora.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "targetUserAzureId": {
                              "type": "string",
                              "description": "AzureId użytkownika, którego raport ma zostać pobrany."
                            },
                            "startDate": {
                              "type": "string",
                              "description": "Początek okresu. Format YYYY-MM-DD."
                            },
                            "endDate": {
                              "type": "string",
                              "description": "Koniec okresu. Format YYYY-MM-DD."
                            }
                          },
                          "required": [
                            "targetUserAzureId",
                            "startDate",
                            "endDate"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        // =========================================================
        // RAPORT - CAŁY ZESPÓŁ
        // TYLKO ADMIN
        // =========================================================

        var teamWorkReportTool =
            ResponseTool.CreateFunctionTool(
                functionName: "get_team_work_report",
                functionDescription:
                    "Pobiera raport czasu pracy wszystkich aktywnych użytkowników. Dostępne wyłącznie dla administratora.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "startDate": {
                              "type": "string",
                              "description": "Początek okresu. Format YYYY-MM-DD."
                            },
                            "endDate": {
                              "type": "string",
                              "description": "Koniec okresu. Format YYYY-MM-DD."
                            }
                          },
                          "required": [
                            "startDate",
                            "endDate"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);
        
            var findUserTool =
                ResponseTool.CreateFunctionTool(
                    functionName: "find_user",
                    functionDescription:
                        "Wyszukuje użytkownika po imieniu, nazwisku, adresie email lub loginie. Dostępne wyłącznie dla administratora.",
                    functionParameters:
                        BinaryData.FromString(
                            """
                            {
                            "type": "object",
                            "properties": {
                                "search": {
                                "type": "string",
                                "description": "Imię, nazwisko, fragment imienia i nazwiska, email lub login użytkownika."
                                }
                            },
                            "required": ["search"],
                            "additionalProperties": false
                            }
                            """),
                    strictModeEnabled: false);


        // =========================================================
        // PĘTLA TOOL CALLING
        // =========================================================

        while (true)
        {
            var options =
                new CreateResponseOptions
                {
                    Model = _deploymentName
                };

            options.Tools.Add(startTimeTool);
            options.Tools.Add(stopTimeTool);
            options.Tools.Add(finishTaskTool);
            options.Tools.Add(createTaskTool);
            options.Tools.Add(editTaskTool);
            options.Tools.Add(commentTaskTool);
            options.Tools.Add(manualTimeTool);

            // Raporty
            options.Tools.Add(myWorkReportTool);
            options.Tools.Add(userWorkReportTool);
            options.Tools.Add(teamWorkReportTool);
            options.Tools.Add(findUserTool);

            foreach (var item in inputItems)
            {
                options.InputItems.Add(item);
            }

            var response =
                await _client.CreateResponseAsync(
                    options);

            Console.WriteLine(
                "========== AI RESPONSE ==========");

            Console.WriteLine(
                response.Value.GetOutputText());

            Console.WriteLine(
                "=================================");

            // =====================================================
            // Dodaj odpowiedź modelu do historii wejściowej.
            // =====================================================

            foreach (var outputItem in response.Value.OutputItems)
            {
                inputItems.Add(outputItem);
            }

            var functionCalls =
                response.Value.OutputItems
                    .OfType<FunctionCallResponseItem>()
                    .ToList();

            // =====================================================
            // Brak tool call = mamy finalną odpowiedź
            // =====================================================

            if (functionCalls.Count == 0)
            {
                var finalMessage =
                    response.Value
                        .GetOutputText()
                        .Trim();

                return string.IsNullOrWhiteSpace(finalMessage)
                    ? null
                    : finalMessage;
            }

            // =====================================================
            // WYKONANIE TOOLI
            // =====================================================

            foreach (var functionCall in functionCalls)
            {
                Console.WriteLine(
                    $"TOOL: {functionCall.FunctionName}");

                Console.WriteLine(
                    $"ARGUMENTS: {functionCall.FunctionArguments}");

                string functionOutput;

                try
                {
                    functionOutput =
                        await ExecuteToolAsync(
                            functionCall,
                            userAzureId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"TOOL ERROR: {ex}");

                    functionOutput =
                        JsonSerializer.Serialize(
                            new
                            {
                                status = "ERROR",
                                message =
                                    "Nie udało się wykonać operacji."
                            });
                }

                Console.WriteLine(
                    $"TOOL RESULT: {functionOutput}");

                // =================================================
                // Wynik toola wraca do tego samego AI
                // =================================================

                inputItems.Add(
                    new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        functionOutput));
            }

            // =====================================================
            // Pętla wykona kolejne wywołanie Responses API.
            // =====================================================
        }
    }

    // =========================================================
    // WYKONYWANIE TOOLI
    // =========================================================

    private async Task<string> ExecuteToolAsync(
        FunctionCallResponseItem functionCall,
        string userAzureId)
    {
        using var arguments =
            JsonDocument.Parse(
                functionCall.FunctionArguments);

        var root =
            arguments.RootElement;

        // =========================================================
        // RAPORT - MÓJ CZAS
        // =========================================================

        if (functionCall.FunctionName ==
            "get_my_work_report")
        {
            var startDate =
                GetDate(
                    root,
                    "startDate");

            var endDate =
                GetDate(
                    root,
                    "endDate");

            if (!startDate.HasValue ||
                !endDate.HasValue)
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        status = "MISSING_DATE"
                    });
            }

            var report =
                await _actionExecutor
                    .GetMyWorkReportAsync(
                        userAzureId,
                        startDate.Value,
                        endDate.Value);

            return JsonSerializer.Serialize(
                report);
        }

        // =========================================================
        // RAPORT - KONKRETNY UŻYTKOWNIK
        // =========================================================

        if (functionCall.FunctionName ==
            "get_user_work_report")
        {
            var targetUserAzureId =
                GetString(
                    root,
                    "targetUserAzureId");

            var startDate =
                GetDate(
                    root,
                    "startDate");

            var endDate =
                GetDate(
                    root,
                    "endDate");

            if (string.IsNullOrWhiteSpace(
                    targetUserAzureId) ||
                !startDate.HasValue ||
                !endDate.HasValue)
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        status = "MISSING_DATA"
                    });
            }

            var report =
                await _actionExecutor
                    .GetUserWorkReportAsync(
                        userAzureId,
                        targetUserAzureId,
                        startDate.Value,
                        endDate.Value);

            return JsonSerializer.Serialize(
                report);
        }

        // =========================================================
        // RAPORT - CAŁY ZESPÓŁ
        // =========================================================

        if (functionCall.FunctionName ==
            "get_team_work_report")
        {
            var startDate =
                GetDate(
                    root,
                    "startDate");

            var endDate =
                GetDate(
                    root,
                    "endDate");

            if (!startDate.HasValue ||
                !endDate.HasValue)
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        status = "MISSING_DATE"
                    });
            }

            var report =
                await _actionExecutor
                    .GetTeamWorkReportAsync(
                        userAzureId,
                        startDate.Value,
                        endDate.Value);

            return JsonSerializer.Serialize(
                report);
        }

        // =========================================================
        // WYSZUKIWANIE UŻYTKOWNIKA
        // =========================================================

        if (functionCall.FunctionName ==
            "find_user")
        {
            var search =
                GetString(
                    root,
                    "search");

            if (string.IsNullOrWhiteSpace(search))
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        status = "MISSING_DATA"
                    });
            }

            var UsersResult =
                await _actionExecutor
                    .FindUserAsync(
                        userAzureId,
                        search);

            return JsonSerializer.Serialize(
                UsersResult);
        }


        // =========================================================
        // STANDARDOWE PARAMETRY ZADAŃ
        // =========================================================

        int? taskId = null;

        if (root.TryGetProperty(
                "taskId",
                out var taskIdElement) &&
            taskIdElement.ValueKind ==
                JsonValueKind.Number)
        {
            taskId =
                taskIdElement.GetInt32();
        }

        string? taskName =
            GetString(
                root,
                "taskName");

        string? newTaskName =
            GetString(
                root,
                "newTaskName");

        string? description =
            GetString(
                root,
                "description");

        string? comment =
            GetString(
                root,
                "comment");

        int? manualMinutes = null;

        if (root.TryGetProperty(
                "manualMinutes",
                out var minutesElement) &&
            minutesElement.ValueKind ==
                JsonValueKind.Number)
        {
            manualMinutes =
                minutesElement.GetInt32();
        }

        // =========================================================
        // DATA CZASU PRACY
        // =========================================================

        DateTime? workDate = null;

        if (root.TryGetProperty(
                "workDate",
                out var workDateElement) &&
            workDateElement.ValueKind ==
                JsonValueKind.String &&
            DateTime.TryParse(
                workDateElement.GetString(),
                out var parsedWorkDate))
        {
            workDate =
                parsedWorkDate.Date;
        }

        // =========================================================
        // LLM RESPONSE
        // =========================================================

        var response =
            new LLMResponse
            {
                TaskId = taskId,
                TaskName = taskName,
                NewTaskName = newTaskName,
                Description = description,
                Comment = comment,
                ManualMinutes = manualMinutes,
                WorkDate = workDate
            };

        // =========================================================
        // WYKONANIE ODPOWIEDNIEGO TOOL'A
        // =========================================================

        ActionExecutionResult result;

        switch (functionCall.FunctionName)
        {
            case "start_time":

                result =
                    await _actionExecutor
                        .StartTimeAsync(
                            userAzureId,
                            response);

                break;

            case "stop_time":

                result =
                    await _actionExecutor
                        .StopTimeAsync(
                            userAzureId,
                            response);

                break;

            case "finish_task":

                result =
                    await _actionExecutor
                        .FinishTaskAsync(
                            userAzureId,
                            response);

                break;

            case "create_task":

                result =
                    await _actionExecutor
                        .CreateTaskAsync(
                            userAzureId,
                            response);

                break;

            case "edit_task":

                result =
                    await _actionExecutor
                        .EditTaskAsync(
                            userAzureId,
                            response);

                break;

            case "comment_task":

                result =
                    await _actionExecutor
                        .AddCommentAsync(
                            userAzureId,
                            response);

                break;

            case "manual_time":

                result =
                    await _actionExecutor
                        .AddManualTimeAsync(
                            userAzureId,
                            response);

                break;

            default:

                result =
                    ActionExecutionResult.Error();

                break;
        }

        return JsonSerializer.Serialize(
            new
            {
                status = result.StatusCode,
                taskId = result.TaskId,
                taskName = result.TaskName,
                candidates = result.Candidates
            });
    }

    // =========================================================
    // POBIERANIE STRINGA Z JSON
    // =========================================================

    private static string? GetString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName,
                out var element))
        {
            return null;
        }

        if (element.ValueKind !=
            JsonValueKind.String)
        {
            return null;
        }

        return element.GetString();
    }

    // =========================================================
    // POBIERANIE DATY Z JSON
    // =========================================================

    private static DateTime? GetDate(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName,
                out var element))
        {
            return null;
        }

        if (element.ValueKind !=
            JsonValueKind.String)
        {
            return null;
        }

        if (DateTime.TryParse(
                element.GetString(),
                out var date))
        {
            return date.Date;
        }

        return null;
    }
}

#pragma warning restore OPENAI001

