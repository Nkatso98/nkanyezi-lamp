## GitHub Actions Secrets (Azure Web Apps for Containers)

Create these in GitHub: `Repo → Settings → Secrets and variables → Actions`.

- `AZURE_CREDENTIALS`: output of `az ad sp create-for-rbac ... --sdk-auth`
- `ACR_NAME`: your ACR name, e.g. `nkanyezilampacrb7ae7d`
- `ACR_USERNAME`: `az acr credential show -n <acr> --query username -o tsv`
- `ACR_PASSWORD`: `az acr credential show -n <acr> --query passwords[0].value -o tsv`
- `RESOURCE_GROUP`: e.g. `nkanyezi-lamp-rg`
- `API_APP_NAME`: e.g. `nkanyezi-api-app`
- `WEB_APP_NAME`: e.g. `nkanyezi-web-app`
- `API_BASE_URL`: `https://<api-app>.azurewebsites.net/api/workflow`
