@echo off
echo Build and push to NuGet.
echo.
echo Hint 1: Have you used Visual Studio to build the "Release / AnyCPU" version of the package?
echo.
echo Hint 2: Do we have the latest version of "nuget.exe"?
echo.
dir .\bin\Release\*.*
echo.
echo Hint 3: Is the "Release" build above recent?
echo.
pause
set version=0.9.4-rc5
nuget.alpha pack NetMQ.ReactiveExtensions.nuspec -Version %version%
rem TODO: Figure out why the new version is giving that error - hopefully they will release a new alpha that fixes the issue.
nuget push NetMQ.ReactiveExtensions.%version%.nupkg 9871c41e-c402-4e85-8c96-5486fb725868
pause