param (
    [string]$SonarProjectKey = "RecouvrementAPI",
    [string]$SonarHostUrl    = "http://localhost:9000",
    [string]$SonarLoginToken = $env:SONAR_TOKEN
)

$env:DB_PASSWORD = "takwa2004"

Write-Host "=== SonarQube Analysis ===" -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($SonarLoginToken)) {
    Write-Host "ERREUR : Le token SonarQube est manquant !" -ForegroundColor Red
    Write-Host '  $env:SONAR_TOKEN = "votre_token"' -ForegroundColor Yellow
    Write-Host '  .\Run-SonarAnalysis.ps1 -SonarLoginToken "votre_token"' -ForegroundColor Yellow
    exit 1
}

$TestProject  = "$PSScriptRoot\RecouvrementAPI.Tests"
$CoveragePath = "$TestProject\coverage.opencover.xml"

# Fichiers espace client uniquement
$EspaceClientInclusions = "controllers/ClientController.cs," +
                          "controllers/IntentionController.cs," +
                          "Models/Client.cs," +
                          "Models/Communication.cs," +
                          "Models/DossierRecouvrement.cs," +
                          "Models/Echeance.cs," +
                          "Models/HistoriquePaiement.cs," +
                          "Models/IntentionClient.cs," +
                          "Models/RelanceClient.cs"

Write-Host "Chemin couverture : $CoveragePath" -ForegroundColor Gray

# 1. Start Sonar
dotnet sonarscanner begin `
    /k:$SonarProjectKey `
    /d:sonar.host.url=$SonarHostUrl `
    /d:sonar.token=$SonarLoginToken `
    /d:sonar.cs.opencover.reportsPaths="$CoveragePath" `
    /d:sonar.inclusions="$EspaceClientInclusions"

if ($LASTEXITCODE -ne 0) { Write-Host "Erreur : SonarQube begin a échoué." -ForegroundColor Red; exit 1 }

# 2. Build
dotnet build --no-incremental
if ($LASTEXITCODE -ne 0) { Write-Host "Erreur : Build échoué." -ForegroundColor Red; exit 1 }

# 3. Test + Coverage
dotnet test $TestProject `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:CoverletOutput="$TestProject\"

if ($LASTEXITCODE -ne 0) { Write-Host "Attention : certains tests ont échoué." -ForegroundColor Yellow }

if (-not (Test-Path $CoveragePath)) {
    Write-Host "ERREUR : Le rapport de couverture n'a pas été généré !" -ForegroundColor Red
    Write-Host "Installez coverlet.msbuild : dotnet add package coverlet.msbuild" -ForegroundColor Yellow
    exit 1
}

Write-Host "Rapport de couverture généré : $CoveragePath" -ForegroundColor Green

# 4. End Sonar
dotnet sonarscanner end /d:sonar.token=$SonarLoginToken

# 5. Cleanup
Write-Host "Nettoyage des fichiers temporaires pour VSCode..." -ForegroundColor Cyan
dotnet clean > $null

Write-Host "=== Analyse terminée ===" -ForegroundColor Cyanparam (
    [string]$SonarProjectKey = "RecouvrementAPI",
    [string]$SonarHostUrl    = "http://localhost:9000",
    [string]$SonarLoginToken = $env:SONAR_TOKEN
)

$env:DB_PASSWORD = "takwa2004"

Write-Host "=== SonarQube Analysis ===" -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($SonarLoginToken)) {
    Write-Host "ERREUR : Le token SonarQube est manquant !" -ForegroundColor Red
    Write-Host '  $env:SONAR_TOKEN = "votre_token"' -ForegroundColor Yellow
    Write-Host '  .\Run-SonarAnalysis.ps1 -SonarLoginToken "votre_token"' -ForegroundColor Yellow
    exit 1
}

$TestProject  = "$PSScriptRoot\RecouvrementAPI.Tests"
$CoveragePath = "$TestProject\coverage.opencover.xml"

# Fichiers espace client uniquement
$EspaceClientInclusions = "controllers/ClientController.cs," +
                          "controllers/IntentionController.cs," +
                          "Models/Client.cs," +
                          "Models/Communication.cs," +
                          "Models/DossierRecouvrement.cs," +
                          "Models/Echeance.cs," +
                          "Models/HistoriquePaiement.cs," +
                          "Models/IntentionClient.cs," +
                          "Models/RelanceClient.cs"

Write-Host "Chemin couverture : $CoveragePath" -ForegroundColor Gray

# 1. Start Sonar
dotnet sonarscanner begin `
    /k:$SonarProjectKey `
    /d:sonar.host.url=$SonarHostUrl `
    /d:sonar.token=$SonarLoginToken `
    /d:sonar.cs.opencover.reportsPaths="$CoveragePath" `
    /d:sonar.inclusions="$EspaceClientInclusions"

if ($LASTEXITCODE -ne 0) { Write-Host "Erreur : SonarQube begin a échoué." -ForegroundColor Red; exit 1 }

# 2. Build
dotnet build --no-incremental
if ($LASTEXITCODE -ne 0) { Write-Host "Erreur : Build échoué." -ForegroundColor Red; exit 1 }

# 3. Test + Coverage
dotnet test $TestProject `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:CoverletOutput="$TestProject\"

if ($LASTEXITCODE -ne 0) { Write-Host "Attention : certains tests ont échoué." -ForegroundColor Yellow }

if (-not (Test-Path $CoveragePath)) {
    Write-Host "ERREUR : Le rapport de couverture n'a pas été généré !" -ForegroundColor Red
    Write-Host "Installez coverlet.msbuild : dotnet add package coverlet.msbuild" -ForegroundColor Yellow
    exit 1
}

Write-Host "Rapport de couverture généré : $CoveragePath" -ForegroundColor Green

# 4. End Sonar
dotnet sonarscanner end /d:sonar.token=$SonarLoginToken

# 5. Cleanup
Write-Host "Nettoyage des fichiers temporaires pour VSCode..." -ForegroundColor Cyan
dotnet clean > $null

Write-Host "=== Analyse terminée ===" -ForegroundColor Cyan
```
