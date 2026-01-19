# Azure Konfigurationsstatus - MageKnightOnline

## ✅ Redan Konfigurerat

Baserat på din nuvarande Azure-konfiguration är följande redan korrekt inställt:

1. **Runtime Stack**: ✅
   - LinuxFxVersion: `DOTNETCORE|10.0`
   - Stack: .NET Core 10.0
   - OS: Linux

2. **Web App Status**: ✅
   - State: Running
   - Location: swedencentral
   - SKU: Free
   - HTTPS Only: Enabled

3. **URL**: 
   - `mageknightonline-fccmhne3hvfjdghg.swedencentral-01.azurewebsites.net`

## ⚠️ Behöver Konfigureras

### 1. Connection String (KRITISKT)

Din Web App har för närvarande **ingen connection string konfigurerad** (`"connectionStrings": null`).

**Vad du behöver göra:**

1. Gå till Azure Portal → **mageknightonline** → **Configuration**
2. Klicka på fliken **Connection strings**
3. Klicka på **+ New connection string**
4. Fyll i:
   - **Name**: `DefaultConnection`
   - **Value**: Din Azure SQL Database connection string
   - **Type**: `SQLAzure`
5. Klicka på **Save** (viktigt!)

**Om du inte har en Azure SQL Database än:**
- Se Steg 3 i `AZURE_DEPLOYMENT.md` för instruktioner om hur du skapar en

### 2. GitHub Secrets (KRITISKT för Deployment)

För att GitHub Actions ska kunna deploya behöver du:

1. Skapa en service principal i Azure CLI:
```bash
az ad sp create-for-rbac --name "mageknightonline-github-actions" \
  --role contributor \
  --scopes /subscriptions/a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43/resourceGroups/DefaultResourceGroup-SEC \
  --sdk-auth
```

2. Kopiera JSON-utdata och lägg till som GitHub Secret:
   - Repository → **Settings** → **Secrets and variables** → **Actions**
   - **New repository secret**
   - Name: `AZURE_CREDENTIALS`
   - Value: Klistra in JSON från steg 1

### 3. Application Settings (Valfritt)

För närvarande är `"appSettings": null`. Om du behöver ytterligare miljövariabler:

1. Gå till **Configuration** → **Application settings**
2. Lägg till efter behov
3. Klicka på **Save**

## Nästa Steg

1. ✅ Runtime stack är redan korrekt - inget att göra
2. ⚠️ **Konfigurera Connection String** - se ovan
3. ⚠️ **Konfigurera GitHub Secret** - se ovan
4. ✅ Pusha till `main`-branch för att trigga deployment

## Viktiga Länkar

- **Azure Portal**: https://portal.azure.com
- **Web App direktlänk**: https://portal.azure.com/#@/resource/subscriptions/a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43/resourceGroups/DefaultResourceGroup-SEC/providers/Microsoft.Web/sites/mageknightonline
- **Web App URL**: https://mageknightonline-fccmhne3hvfjdghg.swedencentral-01.azurewebsites.net

## Snabbchecklista

- [ ] Azure SQL Database skapad (om inte redan finns)
- [ ] Connection String konfigurerad i Azure Portal
- [ ] GitHub Secret `AZURE_CREDENTIALS` skapad
- [ ] Push till `main`-branch för att testa deployment
