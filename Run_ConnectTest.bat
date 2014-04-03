set BATDIR=%~dp0
cd %BATDIR%

set target="Outopos\bin\Debug\Core"

md "Outopos\bin\Test"
rd /s /q "Outopos\bin\Test\Core"
xcopy %target% "Outopos\bin\Test\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Outopos\bin\Test\Core\Outopos.exe" "Outopos\bin\Test\Core"
