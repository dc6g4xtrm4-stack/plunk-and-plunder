@echo off
echo =====================================================
echo   Plunk ^& Plunder - Client Build Creator
echo =====================================================
echo.

REM Check if Unity build exists
if not exist "Builds\PlunkAndPlunder\" (
    echo ERROR: Build folder not found!
    echo.
    echo Please build the game first:
    echo 1. Open Unity
    echo 2. File -^> Build Settings
    echo 3. Select Windows platform
    echo 4. Click Build
    echo 5. Choose "Builds\PlunkAndPlunder" as output folder
    echo.
    pause
    exit /b 1
)

echo ✓ Build folder found!
echo.

REM Create timestamp for versioning
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set BUILD_DATE=%datetime:~0,8%_%datetime:~8,6%

REM Create distribution folder name
set DIST_FOLDER=PlunkAndPlunder_Client_%BUILD_DATE%
set DIST_PATH=Builds\%DIST_FOLDER%

echo Creating distribution package...
echo Folder: %DIST_FOLDER%
echo.

REM Create clean distribution folder
if exist "%DIST_PATH%" rmdir /s /q "%DIST_PATH%"
mkdir "%DIST_PATH%"

REM Copy game files
echo Copying game files...
xcopy "Builds\PlunkAndPlunder\*" "%DIST_PATH%\" /E /I /Y >nul

REM Copy friend instructions
echo Copying instructions for friends...
copy "FOR_FRIENDS_ONLY.txt" "%DIST_PATH%\READ_ME_FIRST.txt" >nul

REM Create a simple README in the distribution
echo Creating README...
(
echo ╔═══════════════════════════════════════════╗
echo ║   PLUNK ^& PLUNDER - Test Build          ║
echo ╚═══════════════════════════════════════════╝
echo.
echo HOW TO RUN:
echo 1. Double-click PlunkAndPlunder.exe
echo 2. Read READ_ME_FIRST.txt for instructions
echo.
echo TO JOIN A GAME:
echo 1. Get the host's IP address
echo 2. Run PlunkAndPlunder.exe
echo 3. Type the IP:Port in the text box
echo 4. Click "Join Direct Connection"
echo.
echo System Requirements:
echo - Windows 10 or newer
echo - 2GB RAM minimum
echo - Graphics card with DirectX 11 support
echo.
echo Build Date: %date% %time%
echo.
echo ⚠️ This is a TEST version!
echo Report any bugs to the developer.
) > "%DIST_PATH%\README.txt"

REM Create ZIP file
echo.
echo Creating ZIP file...
powershell -command "Compress-Archive -Path '%DIST_PATH%\*' -DestinationPath 'Builds\%DIST_FOLDER%.zip' -Force"

if exist "Builds\%DIST_FOLDER%.zip" (
    echo.
    echo =====================================================
    echo   ✅ SUCCESS!
    echo =====================================================
    echo.
    echo Distribution package created:
    echo   Location: Builds\%DIST_FOLDER%.zip
    echo   Size:
    for %%A in ("Builds\%DIST_FOLDER%.zip") do echo   %%~zA bytes
    echo.
    echo NEXT STEPS:
    echo 1. Upload the ZIP file to Google Drive/Dropbox
    echo 2. Share the link with your friends
    echo 3. Tell them your IP address
    echo 4. Host the game and wait for them to join!
    echo.
    echo Your IP address:
    ipconfig | findstr /C:"IPv4 Address"
    echo.
    echo Share this with friends: YOUR_IP_FROM_ABOVE:7777
    echo.
    pause
) else (
    echo.
    echo ❌ ERROR: Failed to create ZIP file
    echo.
    pause
    exit /b 1
)

REM Ask if they want to open the folder
echo.
set /p OPEN_FOLDER="Open the Builds folder? (Y/N): "
if /i "%OPEN_FOLDER%"=="Y" start explorer "Builds"

echo.
echo Done!
pause
