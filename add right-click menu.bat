@echo off
chcp 65001
:: Get the directory of the currently running batch file
set "batchDir=%~dp0"

:: Set the absolute path of the executable
set "exePath=%batchDir%Zero.exe"

:: Add registry keys
reg add "HKEY_CLASSES_ROOT\*\shell\Share with Zero"
reg add "HKEY_CLASSES_ROOT\*\shell\Share with Zero" /v Icon /d "%exePath%"
reg add "HKEY_CLASSES_ROOT\*\shell\Share with Zero\command" /ve /d "\"%exePath%\" \"%%1\""
reg add "HKEY_CLASSES_ROOT\Folder\shell\Share with Zero"
reg add "HKEY_CLASSES_ROOT\Folder\shell\Share with Zero" /v Icon /d "%exePath%"
reg add "HKEY_CLASSES_ROOT\Folder\shell\Share with Zero\command" /ve /d "\"%exePath%\" \"%%1\""
pause

