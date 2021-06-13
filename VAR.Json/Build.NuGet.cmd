@echo off

:: MSBuild and tools path
if exist "%ProgramFiles%\MSBuild\14.0\bin" set PATH=%ProgramFiles%\MSBuild\14.0\bin;%PATH%
if exist "%ProgramFiles(x86)%\MSBuild\14.0\bin" set PATH=%ProgramFiles(x86)%\MSBuild\14.0\bin;%PATH%

:: NuGet
set nuget="nuget"
if exist "%~dp0..\packages\NuGet.CommandLine.3.4.3\tools\NuGet.exe" set nuget="%~dp0\..\packages\NuGet.CommandLine.3.4.3\tools\NuGet.exe"

:: Release
Title Building Release
msbuild VAR.Json.csproj /t:Build /p:Configuration="Release" /p:Platform="AnyCPU"

:: Packing Nuget
Title Packing Nuget
%nuget% pack VAR.Json.csproj -Verbosity detailed -OutputDir "NuGet" -MSBuildVersion "14.0" -Properties Configuration="Release" -Prop Platform=AnyCPU

title Finished
pause
