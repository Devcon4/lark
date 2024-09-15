# Check if glslc is already installed
if (!(Get-Command "glslc.exe" -ErrorAction SilentlyContinue)) {
  # Install glslc
  Write-Host "Installing glslc..."
  $rootDir = "D:/commands"
  $installDir = "$rootDir/shaderrc"
  New-Item -ItemType Directory -Force -Path $rootDir
  New-Item -ItemType Directory -Force -Path $installDir

  try {
    # Store the current location
    $currentDir = Get-Location
    Set-Location -Path $installDir

    # If no install.zip file is found, download it
    if (!(Test-Path -Path "./install.zip")) {
      Write-Host "install.zip not found. Downloading..."
      $client = New-Object System.Net.WebClient
      $client.DownloadFile("https://storage.googleapis.com/shaderc/artifacts/prod/graphics_shader_compiler/shaderc/windows/continuous_release_2017/453/20240308-114157/install.zip", "$installDir/install.zip")
      $client.Dispose()
      # Invoke-WebRequest "https://storage.googleapis.com/shaderc/artifacts/prod/graphics_shader_compiler/shaderc/windows/continuous_release_2017/453/20240308-114157/install.zip" -OutFile "./install.zip" -UseBasicParsing
      Write-Host "install.zip downloaded successfully!"
    }

    # If not extracted, extract it
    if (!(Test-Path -Path "./install")) {
      Write-Host "install directory not found. Extracting..."
      Expand-Archive -Path "$installDir/install.zip" -DestinationPath "$installDir"
    }

    # Copy the results from install/bin to C:/commands
    Copy-Item -Path "./install/bin/*" -Destination $installDir -Recurse -Force

    # Add the extracted folder to PATH
    Write-Host "glslc installed successfully!"

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
  Write-Host "glslc is already installed."
}