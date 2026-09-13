@echo off
setlocal EnableExtensions
pushd "%~dp0"

set "tiktokExe=%~dp0TikTok Downloader.exe"
if exist "%tiktokExe%" goto run

set "tiktokExe=%~dp0bin\Release\net8.0-windows10.0.17763.0\TikTok Downloader.exe"
if exist "%tiktokExe%" goto run

echo Build Release belum ditemukan. Membangun aplikasi...
dotnet build "%~dp0TikTok Downloader.csproj" -c Release
if errorlevel 1 goto build_failed
if not exist "%tiktokExe%" goto executable_missing

:run
"%tiktokExe%" %*
set "tiktokExitCode=%errorlevel%"
if "%tiktokExitCode%"=="0" goto done

echo.
echo Aplikasi berhenti dengan kode %tiktokExitCode%.
if "%~1"=="" pause
goto done

:build_failed
set "tiktokExitCode=1"
echo.
echo Build gagal. Pastikan .NET 8 SDK sudah terpasang.
if "%~1"=="" pause
goto done

:executable_missing
set "tiktokExitCode=1"
echo.
echo Build selesai, tetapi TikTok Downloader.exe tidak ditemukan.
if "%~1"=="" pause

:done
popd
exit /b %tiktokExitCode%
