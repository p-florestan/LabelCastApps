@echo off
SETLOCAL

REM
REM This builds the desktop app and runs InnoSetup script to create installer
REM
REM Version 1.1
REM 12 Jan 2025
REM

echo.
echo  Build and publish LabelCast desktop app
echo.

cd ..\..\source\LabelCastDesktop\
mkdir ..\..\dist\temp

dotnet build LabelCastDesktop.csproj --configuration Release /maxcpucount:1

:: Check if MSBuild succeeded
if %ERRORLEVEL% neq 0 (
    echo Error during build process. Exiting...
    exit /b %ERRORLEVEL%
)

echo.
echo  Creating installer for LabelCast desktop application to local /dist/ folder
echo.

cd ..\..\build\desktop
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "LabelCastDesktop-SetupScript.iss"

:: Check if Inno Setup succeeded
if %ERRORLEVEL% neq 0 (
    echo Error during Inno Setup process. Exiting...
    exit /b %ERRORLEVEL%
)

del /Q /S   ..\..\dist\temp\*.*
rmdir /Q /S ..\..\dist\temp

echo.
echo  Done.
echo.
