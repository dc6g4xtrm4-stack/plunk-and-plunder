@echo off
echo Building Plunk and Plunder with combat visualization enhancements...
"C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe" -quit -batchmode -projectPath "%CD%" -buildWindows64Player "Build/PlunkAndPlunder.exe" -logFile build.log 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
) else (
    echo Build failed! Check build.log for details
    type build.log | findstr /i "error"
)
