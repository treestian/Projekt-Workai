#!/bin/sh
set -eu

RESOURCE_GROUP="rg-teamstimebot"
APP_NAME="api"

az containerapp secret set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --secrets \
    azuread-client-secret="$AZURE_AD_CLIENT_SECRET_VALUE" \
    microsoft-app-password="$AZURE_AD_CLIENT_SECRET_VALUE" \
    azure-sql-connection="$AZURE_SQL_CONNECTION_STRING_VALUE"

az containerapp update \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --set-env-vars \
    "AzureAd__ClientSecret=secretref:azuread-client-secret" \
    "ConnectionStrings__DefaultConnection=secretref:azure-sql-connection" \
    "MicrosoftAppType=SingleTenant" \
    "MicrosoftAppId=592f5759-2d96-4aa8-916e-04d2b3a0d655" \
    "MicrosoftAppTenantId=5452339e-6596-48fb-acc1-27bcdf09abf2" \
    "MicrosoftAppPassword=secretref:microsoft-app-password"