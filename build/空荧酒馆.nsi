!define UWPVER 1.2.13
Name "空荧酒馆·悬浮窗"
OutFile "..\dist\空荧酒馆-悬浮窗_${UWPVER}.exe"
Unicode True
InstallDir "$TEMP"
RequestExecutionLevel admin
ManifestSupportedOS {8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}
ManifestDPIAware true
ShowInstDetails show
BrandingText " "
SetCompressor lzma
Icon "空荧酒馆.ico"
InstallColors 0 4008636142
LoadLanguageFile "${NSISDIR}\Contrib\Language files\SimpChinese.nlf"
XPStyle on
!include x64.nsh
!include WinVer.nsh
!include LogicLib.nsh
!include WinMessages.nsh
!finalize "sign.bat ..\dist\空荧酒馆-悬浮窗_${UWPVER}.exe"
Page instfiles "" LogFont
Function LogFont
    Push $R0
    Push $R1
    FindWindow $R0 "#32770" "" $HWNDPARENT
    CreateFont $R1 "Microsoft Yahei" "8" "400"
    GetDlgItem $R0 $R0 1016
    SendMessage $R0 ${WM_SETFONT} $R1 0
    FindWindow $R0 "#32770" "" $HWNDPARENT
    CreateFont $R1 "Microsoft Yahei" "8" "400"
    GetDlgItem $R0 $R0 1006
    SendMessage $R0 ${WM_SETFONT} $R1 0
    Pop $R1
    Pop $R0
FunctionEnd
Function installWebView2
	# If this key exists and is not empty then webview2 is already installed
	ReadRegStr $0 HKLM \
        	"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
	${If} ${Errors} 
	${OrIf} $0 == ""
		DetailPrint "    Webview2未安装。"
		File /oname=$PLUGINSDIR\MicrosoftEdgeWebview2Setup.exe "MicrosoftEdgeWebview2Setup.exe"
		nsExec::ExecToLog '"$PLUGINSDIR\MicrosoftEdgeWebview2Setup.exe" /install'
		SetDetailsPrint both
    ${Else}
		DetailPrint "    Webview2已安装。"
	${EndIf}
FunctionEnd
Function checkXboxGameBar
    nsExec::Exec "powershell -Command if((Get-AppxPackage Microsoft.XboxGamingOverlay).Version -gt [System.Version]'5.420.9252.0'){ exit 0 }else{ exit 1 }"
    Pop $0
    ${If} $0 != 0 
        MessageBox MB_OK|MB_ICONSTOP "当前系统未安装Xbox Game Bar或版本过低，请手动安装或升级后继续。"
        DetailPrint "> 请访问以下链接或到应用商店安装或升级Xbox Game Bar："
        DetailPrint "https://apps.microsoft.com/store/detail/xbox-game-bar/9NZKPSTSNW4P"
        Abort
    ${Else}
		DetailPrint "    Xbox Game Bar已安装。"
    ${EndIf}
FunctionEnd
Section "Dummy Section" SecDummy
    DetailPrint "> 检查系统运行环境..."
    ${IfNot} ${AtLeastWin10}
        MessageBox MB_OK|MB_ICONSTOP "本程序仅支持Windows 10及以上版本"
        Quit
    ${EndIf}
    ${IfNot} ${RunningX64}
        MessageBox MB_OK|MB_ICONSTOP "本程序仅支持64位系统"
        Quit
    ${EndIf} 
    nsExec::Exec "powershell -Command echo 1"
    Pop $0
    ${If} $0 != 0 
        MessageBox MB_OK|MB_ICONSTOP "当前系统的PowerShell运行环境存在问题，请修复后重试。"
        Quit
    ${EndIf}
    DetailPrint "    完成。"

    DetailPrint "> 检查Xbox Game Bar..."
    Call checkXboxGameBar

    DetailPrint "> 检查Webview2运行环境..."
    Call installWebView2

    DetailPrint "> 信任签名证书..."
    MessageBox MB_OKCANCEL|MB_ICONEXCLAMATION "本次安装需要在您的系统中信任该软件的签名根证书，请确认您信任该软件的发布者！" IDOK OK IDCANCEL CANCEL
    CANCEL:
        Quit
    OK:
        File /oname=$PLUGINSDIR\weixitianli_yusuixian.cer "维系天理.YuSuiXian.cer"
        nsExec::ExecToLog "powershell.exe -Command certutil -addstore 'Root' '$PLUGINSDIR\weixitianli_yusuixian.cer'"

        
        DetailPrint "> 解压依赖文件..."
        File /oname=$PLUGINSDIR\Microsoft.NET.Native.Framework.2.2.appx "..\空荧酒馆-悬浮窗\AppPackages\空荧酒馆-悬浮窗_${UWPVER}.0_x64_Test\Dependencies\x64\Microsoft.NET.Native.Framework.2.2.appx"
        File /oname=$PLUGINSDIR\Microsoft.NET.Native.Runtime.2.2.appx "..\空荧酒馆-悬浮窗\AppPackages\空荧酒馆-悬浮窗_${UWPVER}.0_x64_Test\Dependencies\x64\Microsoft.NET.Native.Runtime.2.2.appx"
        File /oname=$PLUGINSDIR\Microsoft.VCLibs.x64.14.00.appx "..\空荧酒馆-悬浮窗\AppPackages\空荧酒馆-悬浮窗_${UWPVER}.0_x64_Test\Dependencies\x64\Microsoft.VCLibs.x64.14.00.appx"
        File /oname=$PLUGINSDIR\Microsoft.VCLibs.x64.14.00.Desktop.appx "..\空荧酒馆-悬浮窗\AppPackages\空荧酒馆-悬浮窗_${UWPVER}.0_x64_Test\Dependencies\x64\Microsoft.VCLibs.x64.14.00.Desktop.appx"
        File /oname=$PLUGINSDIR\app.msix "..\空荧酒馆-悬浮窗\AppPackages\空荧酒馆-悬浮窗_${UWPVER}.0_x64_Test\空荧酒馆-悬浮窗_${UWPVER}.0_x64.msix"
        DetailPrint "> 安装程序组件..."
        nsExec::ExecToLog "powershell.exe -Command Add-AppxPackage -Path '$PLUGINSDIR\app.msix' -DeferRegistrationWhenPackagesAreInUse -DependencyPath '$PLUGINSDIR\Microsoft.NET.Native.Framework.2.2.appx','$PLUGINSDIR\Microsoft.NET.Native.Runtime.2.2.appx','$PLUGINSDIR\Microsoft.VCLibs.x64.14.00.appx','$PLUGINSDIR\Microsoft.VCLibs.x64.14.00.Desktop.appx'"
        Pop $0
        ${If} $0 != 0 
            MessageBox MB_OK|MB_ICONSTOP "安装程序组件出错，请检查错误信息。"
            DetailPrint "> 安装程序组件出错，请检查错误信息。"
            Abort
        ${EndIf}
        
        DetailPrint "> 放行本地连接..."
        nsExec::ExecToLog "CheckNetIsolation LoopbackExempt -a -n=WeiXiTianLi.Xbox.KongYingJiuGuanOverlay_y1t4g62m900s2"

        DetailPrint "> 激活应用模块..."
        nsExec::ExecToLog "powershell.exe -Command start ms-gamebar:activate/WeiXiTianLi.Xbox.KongYingJiuGuanOverlay_y1t4g62m900s2"

        DetailPrint "> 即将完成..."
        MessageBox MB_OK|MB_ICONINFORMATION "安装完成！请按Win+G打开Xbox Game Bar使用悬浮地图。"
        Quit
SectionEnd