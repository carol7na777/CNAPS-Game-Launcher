@echo off
REM ------------------------------------------------------------------
REM Run Premake to generate the VS2022 solution/project files
REM ------------------------------------------------------------------
call vendor\bin\premake\premake5.exe vs2022

REM ------------------------------------------------------------------
REM Define the path to the C# project file
REM ------------------------------------------------------------------
set "CSPROJ_FILE=Dave\Dave.csproj"

REM ------------------------------------------------------------------
REM Ensure dotnet CLI is available
REM ------------------------------------------------------------------
where dotnet >nul 2>&1
if errorlevel 1 (
    echo dotnet CLI not found. Please install the .NET SDK.
    exit /b 1
)

REM ------------------------------------------------------------------
REM Restore packages to ensure referenced packages are downloaded
REM ------------------------------------------------------------------
echo Restoring packages...
dotnet restore "%CSPROJ_FILE%"
if errorlevel 1 (
    echo dotnet restore failed.
    exit /b 1
)

REM ------------------------------------------------------------------
REM Patch the .csproj file using PowerShell
REM ------------------------------------------------------------------
echo Patching Avalonia resources...
powershell -NoProfile -ExecutionPolicy Bypass -File "Dave\patch_csproj.ps1"

if errorlevel 1 (
    echo Failed to patch Avalonia resources.
    exit /b 1
)

echo Setup complete! You can now build the project.
pause
