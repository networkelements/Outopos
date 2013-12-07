set BATDIR=%~dp0
cd %BATDIR%

set target="Lair\bin\Debug\Core"

md "Lair\bin\Test"
rd /s /q "Lair\bin\Test\Core"
xcopy %target% "Lair\bin\Test\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Lair\bin\Test\Core\Lair.exe"  "Lair\bin\Test\Core"
