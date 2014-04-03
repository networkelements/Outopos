set BATDIR=%~dp0
cd %BATDIR%

set target="Outopos\bin\Debug\Core"

md "Outopos\bin\Debug_1"
rd /s /q "Outopos\bin\Debug_1\Core"
xcopy %target% "Outopos\bin\Debug_1\Core" /c /s /e /q /h /i /k /r /y

md "Outopos\bin\Debug_2"
rd /s /q "Outopos\bin\Debug_2\Core"
xcopy %target% "Outopos\bin\Debug_2\Core" /c /s /e /q /h /i /k /r /y

md "Outopos\bin\Debug_3"
rd /s /q "Outopos\bin\Debug_3\Core"
xcopy %target% "Outopos\bin\Debug_3\Core" /c /s /e /q /h /i /k /r /y

md "Outopos\bin\Debug_4"
rd /s /q "Outopos\bin\Debug_4\Core"
xcopy %target% "Outopos\bin\Debug_4\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Outopos\bin\Debug_1\Core\Outopos.exe" "Outopos\bin\Debug_1\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Outopos\bin\Debug_2\Core\Outopos.exe" "Outopos\bin\Debug_2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Outopos\bin\Debug_3\Core\Outopos.exe" "Outopos\bin\Debug_3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Outopos\bin\Debug_4\Core\Outopos.exe" "Outopos\bin\Debug_4\Core"
