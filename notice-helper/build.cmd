@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Cannot find .NET Framework compiler: %CSC%
  if not defined CI pause
  exit /b 1
)
if not exist dist mkdir dist
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /win32manifest:app.manifest /win32icon:app.ico ^
  /out:dist\QQNotify.exe src\*.cs
if errorlevel 1 (
  echo Build failed.
  if not defined CI pause
  exit /b 1
)
echo Build OK: dist\QQNotify.exe
