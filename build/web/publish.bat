@echo off
SETLOCAL

REM
REM This builds the web app and publishes to the /dist/web/ folder
REM
REM Version 1.0
REM 12 Jan 2025
REM

echo.
echo  Publishing LabelCast web application to local /dist/web/ folder
echo.

cd ..\..\source\LabelCastWeb\
dotnet publish LabelCastWeb.csproj --configuration Release /maxcpucount:1 --property:PublishDir=..\..\dist\web\

:: Check if MSBuild succeeded
if %ERRORLEVEL% neq 0 (
    echo Error during build process. Exiting...
    exit /b %ERRORLEVEL%
)

echo.
echo  Done.
echo.
