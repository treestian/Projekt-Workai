#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.Text.Json;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class AIService
{
    private const int MaxToolRounds = 8;

    private readonly ResponsesClient _client;
    private readonly string _deploymentName;
    private readonly ActionExecutor _actionExecutor;
    private readonly PendingChoice _pendingChoice;
    private readonly ILogger<AIService> _logger;

    public AIService(
        AzureOpenAIClientProvider clientProvider,
        ActionExecutor actionExecutor,
        PendingChoice pendingChoice,
        ILogger<AIService> logger)
    {
        _actionExecutor = actionExecutor;
        _pendingChoice = pendingChoice;
        _logger = logger;
        _client = clientProvider.Client;
        _deploymentName = clientProvider.DeploymentName;
    }

    // =========================================================
    // AKTUALNA DATA I CZAS W POLSCE
    // =========================================================

    private static DateTime GetPolandDateTime()
    {
        return PolandTime.Now;
    }

    // =========================================================
    // GŁÓWNA METODA AI
    // =========================================================

    public async Task<string?> GetResponseAsync(
        List<ConversationMessage> messages,
        string userAzureId,
        CancellationToken cancellationToken = default)
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
                Jesteś asystentem AI w Microsoft Teams.
                Pomagasz pracownikom zarządzać zadaniami i czasem pracy.

                =========================================================
                JAK DZIAŁASZ
                =========================================================

                1. Przeczytaj wiadomość użytkownika.
                2. Jeżeli prosi o operację albo o dane, wywołaj narzędzie.
                3. Poczekaj na wynik narzędzia.
                4. Odpowiedz po polsku, opierając się wyłącznie na wyniku narzędzia.

                Backend jest jedynym źródłem prawdy.
                Nigdy nie wymyślaj zadań, godzin ani osób.
                Nigdy nie potwierdzaj operacji, zanim otrzymasz wynik narzędzia.

                =========================================================
                ZASADY OGÓLNE
                =========================================================

                - Pisz po polsku, krótko i naturalnie.
                - Rozumiej skróty, literówki i brak polskich znaków.
                - Nie pokazuj nazw narzędzi, nazw statusów ani danych technicznych.
                - Nie wykonuj tej samej operacji drugi raz bez nowej prośby użytkownika.

                =========================================================
                NARZĘDZIA - ZADANIA
                =========================================================

                create_task - tworzy nowe zadanie
                  Wymagane: newTaskName
                  Opcjonalne: description
                  Nie używaj do zmiany istniejącego zadania.

                edit_task - zmienia nazwę lub opis istniejącego zadania
                  Wymagane: taskId albo taskName
                  Opcjonalne: newTaskName, description

                comment_task - dodaje komentarz do zadania
                  Wymagane: comment oraz taskId albo taskName
                  Użyj, gdy użytkownik chce tylko skomentować. Nie używaj wtedy edit_task.

                finish_task - oznacza zadanie jako zakończone
                  Wymagane: taskId albo taskName
                  Zatrzymuje wszystkie trwające pomiary na tym zadaniu, także innych osób.

                =========================================================
                NARZĘDZIA - CZAS PRACY
                =========================================================

                start_time - rozpoczyna pomiar czasu
                  Wymagane: taskId albo taskName

                stop_time - zatrzymuje trwający pomiar czasu
                  Wymagane: taskId albo taskName

                manual_time - dodaje ręcznie podany czas pracy
                  Wymagane: manualMinutes oraz taskId albo taskName
                  Opcjonalne: workDate w formacie YYYY-MM-DD, domyślnie dzisiaj
                  Czas pracy można logować tylko sobie.
                  Maksymalnie 24 godziny na jeden wpis.

                =========================================================
                NARZĘDZIA - RAPORTY
                =========================================================

                get_my_work_report - raport czasu pracy użytkownika, który pisze
                  Wymagane: startDate, endDate

                get_task_work_report - raport czasu poświęconego na jedno zadanie
                  Wymagane: startDate, endDate oraz taskId albo taskName

                get_user_work_report - raport innej osoby, tylko dla administratora
                  Wymagane: targetUserAzureId, startDate, endDate

                get_team_work_report - raport całego zespołu, tylko dla administratora
                  Wymagane: startDate, endDate

                find_user - wyszukuje osobę po imieniu, nazwisku, emailu lub loginie
                  Wymagane: search
                  Użyj, gdy potrzebujesz targetUserAzureId do get_user_work_report.
                  Nigdy nie wymyślaj targetUserAzureId.

                Nie licz czasu pracy samodzielnie. Zawsze pobierz raport narzędziem.

                =========================================================
                NARZĘDZIE - PYTANIE Z PRZYCISKAMI
                =========================================================

                ask_choice - zadaje pytanie i pokazuje przyciski do wyboru
                  Wymagane: question, options od 2 do 6 pozycji

                =========================================================
                WYBÓR ZADANIA
                =========================================================

                Użytkownik podał numer zadania, przekaż taskId.
                Użytkownik podał nazwę, przekaż taskName dokładnie tak, jak ją napisał.
                Nie zgaduj numeru zadania.

                Nigdy nie pytaj, o które zadanie chodzi, zanim wywołasz narzędzie.
                Nawet jeżeli z rozmowy wynika, że pasuje kilka zadań, i tak najpierw wywołaj narzędzie.
                Dopiero status AMBIGUOUS uprawnia Cię do zadania pytania.

                ŹLE:
                  Użytkownik: zaloguj 2h do raportu
                  Ty: Do którego zadania mam zalogować te godziny?

                DOBRZE:
                  Użytkownik: zaloguj 2h do raportu
                  Ty: wywołujesz manual_time z taskName "raport"
                  Narzędzie: status AMBIGUOUS, candidates Raport miesięczny i Raport tygodniowy
                  Ty: wywołujesz ask_choice z tymi dwiema nazwami jako opcjami

                =========================================================
                PYTANIA DO UŻYTKOWNIKA
                =========================================================

                Odpowiedzi da się wypisać jako listę, użyj ask_choice.
                Odpowiedź jest otwarta, na przykład treść komentarza, napisz zwykłym tekstem.

                Opcje muszą brzmieć jak odpowiedź użytkownika,
                ponieważ po kliknięciu zostaną wysłane jako jego wiadomość.

                ŹLE:
                  Ty: Czy dodać komentarz i zakończyć zadanie?

                DOBRZE:
                  Ty: wywołujesz ask_choice
                      question: Czy dodać komentarz i zakończyć zadanie?
                      options: Tak, dodaj i zakończ / Tylko dodaj komentarz / Anuluj

                =========================================================
                UPRAWNIENIA
                =========================================================

                Nie wiesz, jakie uprawnienia ma użytkownik. Sprawdza je backend.
                Nigdy nie odmawiaj z góry i nie zakładaj braku uprawnień.
                Zawsze najpierw wywołaj narzędzie.
                Dopiero status FORBIDDEN oznacza brak uprawnień.

                ŹLE:
                  Użytkownik: pokaż raport Maćka
                  Ty: Nie masz uprawnień do raportów innych osób.

                DOBRZE:
                  Użytkownik: pokaż raport Maćka
                  Ty: wywołujesz find_user z search "Maciek"
                  Ty: wywołujesz get_user_work_report ze znalezionym targetUserAzureId

                =========================================================
                DATA I CZAS
                =========================================================

                Aktualna data: {currentDate}
                Aktualny czas: {currentDateTime}
                Strefa czasowa: Europe/Warsaw

                dzisiaj = {currentDate}
                wczoraj = jeden dzień wcześniej
                przedwczoraj = dwa dni wcześniej
                jutro = jeden dzień później
                pojutrze = dwa dni później

                ten tydzień = bieżący tydzień
                zeszły tydzień = poprzedni tydzień
                ten miesiąc = od pierwszego dnia bieżącego miesiąca do dzisiaj
                zeszły miesiąc = cały poprzedni miesiąc

                Jeżeli użytkownik poda konkretną datę, użyj jego daty.
                Wszystkie daty przekazuj w formacie YYYY-MM-DD.
                Nie zgaduj daty. Licz zawsze względem aktualnej daty podanej wyżej.

                =========================================================
                WYNIKI NARZĘDZI
                =========================================================

                Każde narzędzie zwraca pole status. Zareaguj zgodnie z listą.

                SUCCESS              Potwierdź wykonanie operacji.
                AMBIGUOUS            Pasuje kilka zadań. Wywołaj ask_choice z nazwami z pola candidates.
                NOT_FOUND            Nie znaleziono zadania lub osoby. Poproś o dokładniejszą nazwę.
                ALREADY_ACTIVE       Pomiar na tym zadaniu już trwa.
                NO_ACTIVE_WORK       Nie ma trwającego pomiaru do zatrzymania.
                ALREADY_COMPLETED    Zadanie zostało już wcześniej zakończone.
                DUPLICATE_TASK_NAME  Zadanie o tej nazwie już istnieje. Zaproponuj inną nazwę.
                MISSING_TASK_NAME    Poproś o nazwę zadania.
                MISSING_COMMENT      Poproś o treść komentarza.
                MISSING_MANUAL_TIME  Poproś o liczbę godzin lub minut. Maksimum to 24 godziny na wpis.
                MISSING_DATE         Poproś o zakres dat.
                MISSING_DATA         Brakuje danych do wykonania operacji. Poproś o uzupełnienie.
                FORBIDDEN            Powiedz, że użytkownik nie ma uprawnień do tych danych. Nie ponawiaj.
                ERROR                Powiedz, że operacji nie udało się wykonać.

                Wynik finish_task zawiera pole stoppedWorkLogs.
                Jeżeli jest większe od zera, powiedz, ile trwających pomiarów zostało zatrzymanych.
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
        
        var askChoiceTool =
            ResponseTool.CreateFunctionTool(
                functionName: "ask_choice",
                functionDescription:
                    "Zadaje użytkownikowi pytanie i pokazuje mu gotowe przyciski do wyboru. "
                    + "Użyj zamiast pisać pytanie zwykłym tekstem, gdy możliwe odpowiedzi da się wypisać. "
                    + "Nie używaj, gdy odpowiedź jest otwarta.",
                functionParameters:
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "question": {
                              "type": "string",
                              "description": "Treść pytania dla użytkownika."
                            },
                            "options": {
                              "type": "array",
                              "items": { "type": "string" },
                              "description": "Od 2 do 6 opcji. Każda opcja musi być sensowną odpowiedzią użytkownika, bo jej treść zostanie wysłana jako jego wiadomość."
                            }
                          },
                          "required": [
                            "question",
                            "options"
                          ],
                          "additionalProperties": false
                        }
                        """),
                strictModeEnabled: false);

        var taskWorkReportTool =
            ResponseTool.CreateFunctionTool(
                functionName: "get_task_work_report",
                functionDescription:
                    "Pobiera raport czasu pracy nad konkretnym zadaniem. Pracownik widzi wyłącznie swój czas, administrator czas wszystkich osób.",
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
                              "description": "Nazwa zadania."
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

        for (var round = 0; round < MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _pendingChoice.Clear();

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
            options.Tools.Add(taskWorkReportTool);
            options.Tools.Add(findUserTool);
            options.Tools.Add(askChoiceTool);

            foreach (var item in inputItems)
            {
                options.InputItems.Add(item);
            }

            var response =
                await _client.CreateResponseAsync(
                    options,
                    cancellationToken);

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
            // PYTANIE Z PRZYCISKAMI - kończy turę
            // =====================================================

            var choiceCalls = functionCalls
                .Where(x => x.FunctionName == "ask_choice")
                .ToList();

            var toolCalls = functionCalls
                .Where(x => x.FunctionName != "ask_choice")
                .ToList();

            if (choiceCalls.Count > 0 && toolCalls.Count == 0)
            {
                var question = ApplyChoice(choiceCalls[0]);

                if (question != null)
                {
                    return question;
                }
            }

            var notShownMessage = toolCalls.Count > 0
                ? "Najpierw wykonaj operację, potem zadaj pytanie."
                : "Pytanie wymaga treści oraz od 2 do 6 opcji odpowiedzi.";

            foreach (var choiceCall in choiceCalls)
            {
                inputItems.Add(
                    new FunctionCallOutputResponseItem(
                        choiceCall.CallId,
                        JsonSerializer.Serialize(
                            new
                            {
                                status = "NOT_SHOWN",
                                message = notShownMessage
                            })));
            }

            // =====================================================
            // WYKONANIE TOOLI
            // =====================================================

            foreach (var functionCall in toolCalls)
            {
                _logger.LogInformation(
                    "Wywołanie narzędzia {ToolName} (runda {Round}).",
                    functionCall.FunctionName,
                    round + 1);

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
                    _logger.LogError(
                        ex,
                        "Błąd wykonania narzędzia {ToolName}.",
                        functionCall.FunctionName);

                    functionOutput =
                        JsonSerializer.Serialize(
                            new
                            {
                                status = "ERROR",
                                message =
                                    "Nie udało się wykonać operacji."
                            });
                }

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

        // =========================================================
        // Wyczerpany limit rund — model nie doszedł do odpowiedzi.
        // =========================================================

        _logger.LogWarning(
            "Przerwano pętlę narzędzi po {MaxToolRounds} rundach " +
            "dla użytkownika {UserAzureId}.",
            MaxToolRounds,
            userAzureId);

        return "Nie udało mi się dokończyć tej operacji. "
            + "Spróbuj sformułować prośbę inaczej.";
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
        // RAPORT - KONKRETNE ZADANIE
        // =========================================================

        if (functionCall.FunctionName ==
            "get_task_work_report")
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

            int? reportTaskId = null;

            if (root.TryGetProperty(
                    "taskId",
                    out var reportTaskIdElement) &&
                reportTaskIdElement.ValueKind ==
                    JsonValueKind.Number)
            {
                reportTaskId =
                    reportTaskIdElement.GetInt32();
            }

            var report =
                await _actionExecutor
                    .GetTaskWorkReportAsync(
                        userAzureId,
                        reportTaskId,
                        GetString(
                            root,
                            "taskName"),
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

        if (result.Result == "AMBIGUOUS" &&
            result.Candidates.Count > 0)
        {
            _pendingChoice.Set(
                null,
                result.Candidates.Select(x => x.Name));
        }

        return JsonSerializer.Serialize(
            new
            {
                status = result.Result,
                taskId = result.TaskId,
                taskName = result.TaskName,
                stoppedWorkLogs = result.StoppedWorkLogs,
                candidates = result.Candidates
            });
    }

    // =========================================================
    // POBIERANIE STRINGA Z JSON
    // =========================================================

    private string? ApplyChoice(
        FunctionCallResponseItem functionCall)
    {
        using var arguments =
            JsonDocument.Parse(
                functionCall.FunctionArguments);

        var root = arguments.RootElement;

        if (!root.TryGetProperty(
                "options",
                out var optionsElement) ||
            optionsElement.ValueKind !=
                JsonValueKind.Array)
        {
            return null;
        }

        var options = optionsElement
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .ToList();

        if (options.Count < 2)
        {
            return null;
        }

        var question = GetString(
            root,
            "question");

        _pendingChoice.Set(
            question,
            options);

        return string.IsNullOrWhiteSpace(question)
            ? "Wybierz jedną z opcji."
            : question;
    }

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

