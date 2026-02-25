param(
  [string]$ResourceGroup = "nkanyezi-lamp-rg",
  [string]$Location = "southafricanorth",
  [string]$AcrName = "nkanyezilampacrb7ae7d",
  [string]$PlanName = "nkanyezi-linux-plan",
  [string]$ApiAppName = "nkanyezi-api-app",
  [string]$WebAppName = "nkanyezi-web-app"
)

Write-Host "Creating Linux App Service plan..."
az appservice plan create -g $ResourceGroup -n $PlanName --is-linux --sku B1 | Out-Null

Write-Host "Enabling ACR admin and fetching credentials..."
az acr update -n $AcrName --admin-enabled true | Out-Null
$AcrUser = az acr credential show -n $AcrName --query username -o tsv
$AcrPass = az acr credential show -n $AcrName --query passwords[0].value -o tsv

Write-Host "Creating Web Apps for containers..."
az webapp create -g $ResourceGroup -p $PlanName -n $ApiAppName --deployment-container-image-name "$AcrName.azurecr.io/nkanyezi/api:1.0" | Out-Null
az webapp create -g $ResourceGroup -p $PlanName -n $WebAppName --deployment-container-image-name "$AcrName.azurecr.io/nkanyezi/web:1.0" | Out-Null

Write-Host "Configuring container registry creds..."
az webapp config container set -g $ResourceGroup -n $ApiAppName `
  --docker-custom-image-name "$AcrName.azurecr.io/nkanyezi/api:1.0" `
  --docker-registry-server-url "https://$AcrName.azurecr.io" `
  --docker-registry-server-user $AcrUser `
  --docker-registry-server-password $AcrPass | Out-Null

az webapp config container set -g $ResourceGroup -n $WebAppName `
  --docker-custom-image-name "$AcrName.azurecr.io/nkanyezi/web:1.0" `
  --docker-registry-server-url "https://$AcrName.azurecr.io" `
  --docker-registry-server-user $AcrUser `
  --docker-registry-server-password $AcrPass | Out-Null

Write-Host "Setting API app settings..."
az webapp config appsettings set -g $ResourceGroup -n $ApiAppName --settings `
  WEBSITES_PORT=8080 `
  Ocr__TessdataPath=/usr/share/tesseract-ocr/4.00/tessdata | Out-Null

$ApiUrl = az webapp show -g $ResourceGroup -n $ApiAppName --query defaultHostName -o tsv
$WebUrl = az webapp show -g $ResourceGroup -n $WebAppName --query defaultHostName -o tsv

Write-Host ""
Write-Host "API URL: https://$ApiUrl"
Write-Host "WEB URL: https://$WebUrl"
Write-Host ""
Write-Host "Use these GitHub secrets:"
Write-Host "ACR_NAME=$AcrName"
Write-Host "ACR_USERNAME=$AcrUser"
Write-Host "ACR_PASSWORD=<redacted>"
Write-Host "RESOURCE_GROUP=$ResourceGroup"
Write-Host "API_APP_NAME=$ApiAppName"
Write-Host "WEB_APP_NAME=$WebAppName"
Write-Host "API_BASE_URL=https://$ApiUrl/api/workflow"
