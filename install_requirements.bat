@echo off
setlocal

cd /d "%~dp0"

echo Installing Python requirements from requirements.txt...
python -m pip install -r requirements.txt

if errorlevel 1 (
    echo.
    echo Installation failed.
    pause
    exit /b 1
)

echo.
echo Installation complete.
pause
