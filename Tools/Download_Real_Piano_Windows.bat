@echo off
setlocal
cd /d "%~dp0"
echo =========================================================
echo  Air Piano 7 - Download REAL piano recordings
 echo =========================================================
echo This replaces Piano_C5 ... Piano_B5 WAV files ONLY after ALL
 echo seven genuine samples are downloaded successfully.
echo Requires Python 3 and FFmpeg on PATH.
echo.
where py >nul 2>&1
if %ERRORLEVEL% EQU 0 (
  py -3 "%~dp0download_real_piano.py" --source meta --project "%~dp0.."
) else (
  python "%~dp0download_real_piano.py" --source meta --project "%~dp0.."
)
echo.
if %ERRORLEVEL% EQU 0 (echo Download and conversion finished!) else (echo Please check the error above. See the error above for the stage that failed.)
pause

