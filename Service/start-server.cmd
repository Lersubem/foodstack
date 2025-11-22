@echo off
setlocal

set PORT=5124

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="-port" (
    if not "%~2"=="" (
        set PORT=%~2
        shift
    )
)
shift
goto parse_args

:args_done

cd /d "%~dp0"

set ASPNETCORE_URLS=http://localhost:%PORT%

echo Starting FoodStack on http://localhost:%PORT%
echo (Press Ctrl+C to stop)

dotnet run

endlocal
