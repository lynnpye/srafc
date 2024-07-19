@echo off
SETLOCAL

SET EXENAME=srhkafc.exe
SET MANAGE_FOLDER=SRHK_Data\Managed
SET INSTALL_DIR_VAR=SRHKInstallDir
SET GAME_INSTALL_DIR=%SRHKInstallDir%

IF "%GAME_INSTALL_DIR%"=="" (
    echo %INSTALL_DIR_VAR% variable is not set.
    exit /b 1
)

SET THISDIR=%~dp0

SET THISDLLDIR=%THISDIR%dfdcdll

SET DLLDIR=%GAME_INSTALL_DIR%\%MANAGE_FOLDER%

SET EXEFILE=%THISDIR%%EXENAME%


IF NOT EXIST "%DLLDIR%" (
	echo "%DLLDIR% not found"
	exit /b 1
)

IF NOT EXIST "%EXEFILE%" (
    echo %EXENAME% not found in %THISDIR%.
    exit /b 1
)

IF NOT EXIST "%THISDLLDIR%" (
	MKDIR "%THISDLLDIR%"
)

FOR %%D IN (
ShadowrunDTO.dll
protobuf-net.dll
ShadowrunSerializer.dll
) DO (
IF NOT EXIST "%THISDLLDIR%\%%D" COPY /Y "%DLLDIR%\%%D" "%THISDLLDIR%\"
)

"%EXEFILE%" %*

ENDLOCAL
