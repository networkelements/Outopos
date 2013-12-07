set BATDIR=%~dp0
cd %BATDIR%

set target="Lair\bin\Debug\Core"

md "Lair\bin\Debug_1"
rd /s /q "Lair\bin\Debug_1\Core"
xcopy %target% "Lair\bin\Debug_1\Core" /c /s /e /q /h /i /k /r /y

md "Lair\bin\Debug_2"
rd /s /q "Lair\bin\Debug_2\Core"
xcopy %target% "Lair\bin\Debug_2\Core" /c /s /e /q /h /i /k /r /y

md "Lair\bin\Debug_3"
rd /s /q "Lair\bin\Debug_3\Core"
xcopy %target% "Lair\bin\Debug_3\Core" /c /s /e /q /h /i /k /r /y

md "Lair\bin\Debug_4"
rd /s /q "Lair\bin\Debug_4\Core"
xcopy %target% "Lair\bin\Debug_4\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug_1\Core\Lair.exe"  "Lair\bin\Debug_1\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug_2\Core\Lair.exe"  "Lair\bin\Debug_2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug_3\Core\Lair.exe"  "Lair\bin\Debug_3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Debug_4\Core\Lair.exe"  "Lair\bin\Debug_4\Core"
