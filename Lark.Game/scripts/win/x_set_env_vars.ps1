# Check all folders in D:\commands. Add each folder to a new environment variable called LARK_COMMANDS. Add the LARK_COMMANDS environment variable to the PATH environment variable.

Write-Output "Setting LARK_COMMANDS and PATH environment variables..."

# Get all directories in D:\commands
# Only imediately nested directories are considered
$directories = Get-ChildItem -Path 'D:\commands' -Directory

# Join the directories into a single string separated by semicolons
$larkCommands = ($directories | ForEach-Object { $_.FullName }) -join ';'

# Set the LARK_COMMANDS environment variable
[System.Environment]::SetEnvironmentVariable('LARK_COMMANDS', $larkCommands, [System.EnvironmentVariableTarget]::User)

# Get the current PATH environment variable
$currentPath = [System.Environment]::GetEnvironmentVariable('PATH', [System.EnvironmentVariableTarget]::User)

# Add LARK_COMMANDS variable to the PATH environment variable if it's not already there
if ($currentPath -notlike "*%LARK_COMMANDS%*") {
  $newPath = "$currentPath;%LARK_COMMANDS%"
  [System.Environment]::SetEnvironmentVariable('PATH', $newPath, [System.EnvironmentVariableTarget]::User)
}


# Output the new environment variables for verification
Write-Output "Done setting environment variables!"
Write-Output "LARK_COMMANDS: $larkCommands"