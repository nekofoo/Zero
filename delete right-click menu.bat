:: Delete registry keys
chcp 65001
reg delete "HKEY_CLASSES_ROOT\*\shell\Share with Zero" /f
reg delete "HKEY_CLASSES_ROOT\Folder\shell\Share with Zero" /f
pause
