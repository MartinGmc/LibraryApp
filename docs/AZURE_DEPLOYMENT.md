# Azure App Service Deployment Guide

## Prerequisites
- Azure subscription
- Azure App Service (Web App) created
- SQLite file will be stored in persistent storage at `/home/site/data/app.db`

## Azure App Service Configuration

### App Settings
Configure the following Application Settings in Azure Portal (Configuration → Application settings):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=/home/site/data/app.db
```

**Important Notes:**
- Use `__` (double underscore) to represent `:` in nested configuration keys
- The `/home/site/data` directory is persisted across deployments and restarts
- Ensure the app service is configured for **single instance** deployment (SQLite does not support scale-out)

### Platform Settings
1. **Operating System**: Linux (recommended) or Windows
2. **Stack**: .NET 8.0
3. **Always On**: Enabled (recommended for production)
4. **HTTPS Only**: Enabled

### Scale Configuration
⚠️ **CRITICAL**: Do NOT scale out to multiple instances. SQLite is a file-based database and does not support multiple writers concurrently.

- Set **Scale out** to **Manual** with **1 instance**

### Path Configuration
The application automatically creates `/home/site/data` directory on startup if it doesn't exist. The SQLite database file will be stored at:
```
/home/site/data/app.db
```

## Deployment Steps

### Option 1: Manual Zip Deploy via Azure Portal

1. Run the PowerShell script to create the deployment package:
   ```powershell
   .\scripts\Publish-ZipDeploy.ps1
   ```

2. Navigate to Azure Portal → Your App Service → **Deployment Center** → **Zip Deploy**

3. Upload the generated ZIP file: `artifacts\deploy\SCIOStatusApp.Web.zip`

4. The deployment will automatically extract and start the application

### Option 2: Azure CLI Zip Deploy

```bash
# Login
az login

# Deploy
az webapp deployment source config-zip \
  --resource-group <YourResourceGroup> \
  --name <YourAppServiceName> \
  --src artifacts/deploy/SCIOStatusApp.Web.zip
```

### Option 3: GitHub Actions (Future)

You can set up continuous deployment via GitHub Actions by:
1. Adding Azure credentials as GitHub Secrets
2. Creating a workflow file in `.github/workflows/azure-deploy.yml`

## Post-Deployment Verification

1. **Check Application Logs**:
   - Azure Portal → Your App Service → **Log stream**
   - Look for: "Database migrations applied successfully"

2. **Verify Database File**:
   - SSH into the App Service (if enabled)
   - Check: `/home/site/data/app.db` exists

3. **Test the API Endpoint**:
   ```bash
   curl https://<YourAppServiceName>.azurewebsites.net/api/status/first
   ```
   Expected response: `"OK"`

4. **Test the Blazor UI**:
   - Navigate to: `https://<YourAppServiceName>.azurewebsites.net`
   - Click the "Load Status" button
   - Should display: "OK"

## Troubleshooting

### Database Migration Errors
- Check Application Settings for correct connection string
- Verify `/home/site/data` directory permissions
- Review application logs for migration errors

### Application Startup Issues
- Verify .NET 8.0 runtime is installed on the App Service
- Check `ASPNETCORE_ENVIRONMENT` setting
- Review startup logs for dependency injection errors

### SQLite File Lock Issues
- Ensure only **1 instance** is running
- Check for long-running transactions
- Review Serilog logs in `logs/` directory

### Cookie Authentication Issues
- **Problem**: User logs in successfully but API calls fail with 401 Unauthorized
- **Cause**: SameSite cookie policy blocking cookies on cross-site requests
- **Solution**: Application is configured with `SameSite=None` and `Secure=Always` for production
- **Verify**: Check `appsettings.Production.json` or Program.cs for cookie configuration

## Backup Recommendations

SQLite database files can be backed up by:
1. **Azure Blob Storage**: Copy `/home/site/data/app.db` to blob storage periodically
2. **Azure Files**: Mount Azure Files share to `/home/site/data`
3. **Kudu API**: Use Kudu REST API to download the file

## Security Considerations

1. **HTTPS Only**: Ensure HTTPS Only is enabled
2. **Authentication**: Consider adding Azure AD B2C or JWT authentication for production
3. **Secrets**: Store sensitive connection strings in Azure Key Vault (future enhancement)
4. **CORS**: Configure CORS if calling from external domains

