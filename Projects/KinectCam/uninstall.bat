@set DRV=%~d0
@%DRV%

@set OLDDIR=%~dp0
@cd %OLDDIR%

@c:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo BaseClassesNET.dll
@c:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo KinectCam.dll
@c:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo KinectCamBodyIndex.dll

@c:\Windows\Microsoft.NET\Framework\v4.0.30319\ngen.exe uninstall BaseClassesNET.dll
@c:\Windows\Microsoft.NET\Framework\v4.0.30319\ngen.exe uninstall KinectCam.dll
@c:\Windows\Microsoft.NET\Framework\v4.0.30319\ngen.exe uninstall KinectCamBodyIndex.dll