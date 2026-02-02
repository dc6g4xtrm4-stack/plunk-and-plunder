@echo off
echo Converting Friend Guide to PDF...

REM Check if pandoc is installed
where pandoc >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Pandoc not found. Please use online converter or browser method.
    echo See: https://www.markdowntopdf.com/
    pause
    exit /b
)

REM Convert with pandoc
pandoc FRIEND_MULTIPLAYER_GUIDE.md -o FRIEND_MULTIPLAYER_GUIDE.pdf --pdf-engine=wkhtmltopdf

if %ERRORLEVEL% EQU 0 (
    echo Success! PDF created: FRIEND_MULTIPLAYER_GUIDE.pdf
) else (
    echo Conversion failed. Try online converter instead.
    echo See: https://www.markdowntopdf.com/
)

pause
