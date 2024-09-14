# Downloads https://github.com/HectorMF/BRDFGenerator/releases/download/1.0/BRDFGenerator.exe and adds it to the commands directory.

# Check if brdfgenerator is already installed
if (!(Get-Command "BRDFGenerator.exe" -ErrorAction SilentlyContinue)) {
  # Install brdfGenerator
  Write-Host "Installing brdfGenerator..."
  $rootDir = "D:/commands"
  $installDir = "$rootDir/brdfGenerator"
  New-Item -ItemType Directory -Force -Path $rootDir
  New-Item -ItemType Directory -Force -Path $installDir

  try {
    # Store the current location
    $currentDir = Get-Location
    Set-Location -Path $installDir

    # If no BRDFGenerator.exe file is found, download it
    if (!(Test-Path -Path "./BRDFGenerator.exe")) {
      Write-Host "BRDFGenerator.exe not found. Downloading..."
      $client = New-Object System.Net.WebClient
      $client.DownloadFile("https://github.com/HectorMF/BRDFGenerator/releases/download/1.0/BRDFGenerator.exe", "$installDir/BRDFGenerator.exe")
      $client.Dispose()

      Write-Host "BRDFGenerator.exe downloaded successfully!"
    }

    Write-Host "BRDFGenerator installed successfully!"

    # Add C:/commands to PATH
    $env:Path += ";$installDir"
    [Environment]::SetEnvironmentVariable("Path", $env:Path)
  }
  finally {
    # return location to the original directory
    Set-Location -Path $currentDir
  }
}
else {
  Write-Host "BRDFGenerator is already installed."
}