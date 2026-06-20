; NGM proofread Windows Installer
; NSIS 3 script

Unicode true
RequestExecutionLevel admin
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

; ── Metadata ──
!define PRODUCT_NAME "NGM proofread"
!define PRODUCT_VERSION "1.1.1"
!define PRODUCT_PUBLISHER "NJRin"
!define PRODUCT_WEB_SITE "https://github.com/NJRin/NGMproofread"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\NGMproofread.Windows.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "..\publish\NGMproofread_Setup_${PRODUCT_VERSION}.exe"
InstallDir "$PROGRAMFILES64\NGMproofread"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
BrandingText "${PRODUCT_NAME} — 日语 OCR + AI 翻译校对"
SetCompressor /SOLID lzma
SetCompressorDictSize 64

; ── Icon ──
!define MUI_ICON "..\NGMproofread.Windows\NGMproofread.ico"
!define MUI_UNICON "..\NGMproofread.Windows\NGMproofread.ico"

; ── Pages ──
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "SimpChinese"

; ── Installer ──
Section "install" SectionInstall
    SetOutPath "$INSTDIR"

    ; Check for .NET 8 Desktop Runtime
    ReadRegDWORD $0 HKLM "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" "8.0"
    ${If} $0 == ""
        ReadRegDWORD $0 HKLM "SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" "8.0"
    ${EndIf}
    ${If} $0 == ""
        MessageBox MB_YESNO|MB_ICONEXCLAMATION ".NET 8 桌面运行时未安装，程序无法运行。$\n$\n是否现在打开 .NET 8 下载页面？" IDYES download_dotnet IDNO abort_install
        download_dotnet:
            ExecShell "open" "https://dotnet.microsoft.com/download/dotnet/8.0"
        abort_install:
            Abort ".NET 8 桌面运行时是运行本程序的必要条件。请先安装 .NET 8 运行时。"
    ${EndIf}

    ; Copy all published files
    File /r "..\publish-fd\*.*"

    ; Create shortcuts
    CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
    CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\NGMproofread.Windows.exe" "" "$INSTDIR\NGMproofread.ico"
    CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\卸载 ${PRODUCT_NAME}.lnk" "$INSTDIR\uninst.exe"
    CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\NGMproofread.Windows.exe" "" "$INSTDIR\NGMproofread.ico"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\uninst.exe"

    ; Registry entries for uninstall
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\NGMproofread.Windows.exe"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoRepair" 1

    ; Estimate size
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "EstimatedSize" "$0"
SectionEnd

; ── Uninstaller ──
Section "Uninstall"
    ; Remove files
    RMDir /r "$INSTDIR"

    ; Remove shortcuts
    Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
    RMDir /r "$SMPROGRAMS\${PRODUCT_NAME}"

    ; Remove registry
    DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"
    DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"

    ; Clean up empty data (optional, user data in LocalAppData preserved)
    SetAutoClose true
SectionEnd
