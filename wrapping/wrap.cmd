
set PROJECTDIR=..
REM location of 7zip utility
set CMD7Z=7z

rd /q /s apps
rd /q /s working

mkdir apps
mkdir working

for %%p in (srrafc
dfdcafc
srhkafc) do (
xcopy /e /i scripts\%%ps.cmd working\%%p\
xcopy %PROJECTDIR%\%%p\bin\Debug\net35\%%p.exe working\%%p\
xcopy %PROJECTDIR%\%%p\bin\Debug\net35\%%p.exe.config working\%%p\
xcopy %PROJECTDIR%\%%p\bin\Debug\net35\FluentCommandLineParser.dll working\%%p\
xcopy %PROJECTDIR%\%%p\bin\Debug\net35\Newtonsoft.Json.dll working\%%p\
cd working\%%p
%CMD7Z% a ..\..\apps\%%p.zip *.*
cd ..\..
)

