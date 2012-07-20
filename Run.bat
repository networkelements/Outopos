set BATDIR=%~dp0
cd %BATDIR%

set target="Lair\bin\Debug\Core"

md "Lair\bin\Debug 2"
rd /s /q "Lair\bin\Debug 2\Core"
xcopy %target% "Lair\bin\Debug 2\Core" /c /s /e /q /h /i /k /r /y

md "Lair\bin\Debug 3"
rd /s /q "Lair\bin\Debug 3\Core"
xcopy %target% "Lair\bin\Debug 3\Core" /c /s /e /q /h /i /k /r /y

md "Lair\bin\Debug 4"
rd /s /q "Lair\bin\Debug 4\Core"
xcopy %target% "Lair\bin\Debug 4\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug 2\Core\Lair.exe"  "Lair\bin\Debug 2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug 3\Core\Lair.exe"  "Lair\bin\Debug 3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug 4\Core\Lair.exe"  "Lair\bin\Debug 4\Core"
