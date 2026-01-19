# Azure Deployment Guide - MageKnightOnline

Denna guide beskriver hur du konfigurerar Azure för att publicera MageKnightOnline-applikationen.

## Förutsättningar

- Azure Web App: **mageknightonline**
- Subscription ID: **a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43**
- Resource Group: **DefaultResourceGroup-SEC**
- App Service Plan: **ASP-DefaultResourceGroupSEC-b6fc**
- Plan: **F1 (Free tier)**
- OS: **Linux**
- Runtime: **.NET 10** ✅ (Redan konfigurerat korrekt)
- URL: **mageknightonline-fccmhne3hvfjdghg.swedencentral-01.azurewebsites.net**

## ✅ Steg 1: Runtime Stack (REDAN KONFIGURERAT)

Din Azure Web App har redan korrekt runtime stack konfigurerad:
- **LinuxFxVersion**: `DOTNETCORE|10.0` ✅
- **Stack**: .NET Core 10.0 ✅

Ingen åtgärd krävs för detta steg!

## Steg 2: Konfigurera Connection String för databas

1. I Azure Portal, gå till din Web App: **mageknightonline**
2. I vänstermenyn, gå till **Configuration** (Konfiguration)
3. Klicka på fliken **Connection strings** (Anslutningssträngar)
4. Klicka på **+ New connection string** (Ny anslutningssträng)
5. Ange:
   - **Name**: `DefaultConnection`
   - **Value**: Din Azure SQL Database connection string (se format nedan)
   - **Type**: SQLAzure
6. Klicka på **OK** och sedan **Save** (Spara)

**Format för Azure SQL Connection String:**
```
Server=tcp:<servername>.database.windows.net,1433;Initial Catalog=<databasename>;Persist Security Info=False;User ID=<username>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**Alternativt:** Om du använder Azure SQL Database, kan du kopiera connection string direkt från Azure SQL Database-resursen:
1. Gå till din Azure SQL Database
2. Klicka på **Connection strings** i vänstermenyn
3. Kopiera ADO.NET connection string
4. Ersätt `{your_username}` och `{your_password}` med dina faktiska värden

## Steg 3: Skapa Azure SQL Database (om den inte finns)

Om du inte redan har en Azure SQL Database:

1. I Azure Portal, klicka på **+ Create a resource** (Skapa en resurs)
2. Sök efter "SQL Database" och välj det
3. Klicka på **Create**
4. Fyll i:
   - **Subscription**: Välj din subscription (a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43)
   - **Resource group**: DefaultResourceGroup-SEC (eller skapa en ny)
   - **Database name**: MageKnightOnline (eller valfritt namn)
   - **Server**: Skapa en ny server eller välj befintlig
   - **Compute + storage**: Basic tier (för F1-planen, välj den billigaste)
5. Klicka på **Review + create** och sedan **Create**

## Steg 4: Konfigurera GitHub Secrets

För att GitHub Actions ska kunna deploya till Azure behöver du skapa en service principal och lägga till den som secret:

### Metod 1: Använda Azure CLI (Rekommenderat)

1. Öppna Azure Cloud Shell eller installera Azure CLI lokalt
2. Kör följande kommando (ersätt `<subscription-id>` med ditt subscription ID):

```bash
az ad sp create-for-rbac --name "mageknightonline-github-actions" \
  --role contributor \
  --scopes /subscriptions/a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43/resourceGroups/DefaultResourceGroup-SEC \
  --sdk-auth
```

**Alternativt:** Om du vill ge behörighet specifikt till Web App:
```bash
az ad sp create-for-rbac --name "mageknightonline-github-actions" \
  --role contributor \
  --scopes /subscriptions/a9c4cfc5-7ca7-4640-a87c-c4e1799b9a43/resourceGroups/DefaultResourceGroup-SEC/providers/Microsoft.Web/sites/mageknightonline \
  --sdk-auth
```

3. Kopiera hela JSON-utdata som visas
4. Gå till ditt GitHub repository
5. Klicka på **Settings** → **Secrets and variables** → **Actions**
6. Klicka på **New repository secret**
7. Namn: `AZURE_CREDENTIALS`
8. Value: Klistra in JSON-utdata från steg 3
9. Klicka på **Add secret**

### Metod 2: Använda Publish Profile (Alternativ)

Om du föredrar att använda publish profile istället:

1. I Azure Portal, gå till din Web App: **mageknightonline**
2. Klicka på **Get publish profile** (Hämta publiceringsprofil) i övre högra hörnet
3. Ladda ner `.PublishSettings`-filen
4. Öppna filen i en textredigerare
5. Kopiera innehållet
6. Gå till GitHub repository → **Settings** → **Secrets and variables** → **Actions**
7. Skapa en ny secret:
   - Namn: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: Klistra in innehållet från `.PublishSettings`-filen

**OBS:** Om du använder publish profile-metoden, behöver du också uppdatera workflow-filen för att använda `publish-profile` istället för `creds`.

## Steg 5: Konfigurera Firewall Rules för Azure SQL

Om du använder Azure SQL Database:

1. Gå till din Azure SQL Server (inte databasen)
2. I vänstermenyn, gå till **Networking** (Nätverk)
3. Under **Firewall rules**, lägg till:
   - **Allow Azure services and resources to access this server**: Aktivera detta
   - **Allow public network access**: Aktivera om nödvändigt
4. Klicka på **Save**

## Steg 6: Verifiera Deployment

1. Pusha ändringar till `main`-branchen i GitHub
2. GitHub Actions kommer automatiskt att starta deployment
3. Du kan följa deployment-progressen i GitHub under **Actions**-fliken
4. När deployment är klar, besök din Web App URL: 
   - `https://mageknightonline-fccmhne3hvfjdghg.swedencentral-01.azurewebsites.net`
   - Eller använd standard-URL: `https://mageknightonline.azurewebsites.net` (om konfigurerad)

## Ytterligare Azure-inställningar

### Application Settings (Valfritt)

Om du behöver ytterligare miljövariabler:

1. Gå till **Configuration** → **Application settings**
2. Klicka på **+ New application setting**
3. Lägg till nycklar och värden efter behov
4. Klicka på **Save**

### Logging

1. Gå till **App Service logs** i vänstermenyn
2. Aktivera **Application Logging (Filesystem)**
3. Välj log level (Information rekommenderas)
4. Klicka på **Save**

### Custom Domain (Valfritt)

Om du vill använda en egen domän:

1. Gå till **Custom domains** i vänstermenyn
2. Följ instruktionerna för att lägga till din domän

## Felsökning

### Appen startar inte
- Kontrollera **Log stream** i Azure Portal för felmeddelanden
- Verifiera att connection string är korrekt konfigurerad
- Kontrollera att runtime stack är satt till .NET 10

### Databasanslutning misslyckas
- Verifiera firewall rules för Azure SQL
- Kontrollera att connection string är korrekt
- Se till att databasen är skapad och tillgänglig

### Deployment misslyckas i GitHub Actions
- Kontrollera att `AZURE_CREDENTIALS` secret är korrekt konfigurerad
- Verifiera att service principal har rätt behörigheter
- Kontrollera workflow-loggarna i GitHub Actions för detaljerade felmeddelanden

## Support

Om du stöter på problem, kontrollera:
- Azure Portal → Web App → **Log stream** för realtidsloggar
- GitHub Actions → Workflow runs för deployment-loggar
- Azure Portal → Web App → **Diagnose and solve problems** för automatisk diagnostik
