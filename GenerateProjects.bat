@echo off
REM ------------------------------------------------------------------
REM Run Premake to generate the VS2022 solution/project files
REM ------------------------------------------------------------------
call vendor\bin\premake\premake5.exe vs2022

REM ------------------------------------------------------------------
REM Check if dotnet CLI is available
REM ------------------------------------------------------------------
where dotnet >nul 2>&1
if errorlevel 1 (
    echo dotnet CLI not found. Please install the .NET SDK.
    exit /b 1
)

REM ------------------------------------------------------------------
REM Define the path to the C# project file
REM ------------------------------------------------------------------
set "CSPROJ_FILE=Dave\Dave.csproj"

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
REM Check installed packages and capture output into a temporary file
REM ------------------------------------------------------------------
echo Checking installed packages in %CSPROJ_FILE%...
dotnet list "%CSPROJ_FILE%" package > packages.txt

REM List of expected Avalonia packages
set "expectedPackages=Avalonia Avalonia.Desktop Avalonia.Themes.Fluent Avalonia.Fonts.Inter Avalonia.Diagnostics"

for %%p in (%expectedPackages%) do (
    REM Look for the package name in the packages.txt file
    findstr /C:"%%p" packages.txt >nul
    if errorlevel 1 (
        echo Package %%p not found. Installing...
        dotnet add "%CSPROJ_FILE%" package %%p --version 11.2.1
    ) else (
        echo Package %%p is installed.
    )
)

REM Clean up the temporary file
del packages.txt

echo Setup complete! You can now build the project.
pause
