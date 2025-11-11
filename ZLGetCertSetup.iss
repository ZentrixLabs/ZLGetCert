; ZLGetCert Inno Setup Script
; This script creates an installer for ZLGetCert

#define MyAppName "ZLGetCert"
#ifndef MyAppVersion
#define MyAppVersion "1.8.5"
#endif
#ifndef EnableSigning
#define EnableSigning "1"
#endif
#ifndef ZLGetCertBin
#define ZLGetCertBin "ZLGetCert\bin\Release"
#endif
#define MyAppPublisher "ZentrixLabs"
#define MyAppURL "https://github.com/ZentrixLabs/ZLGetCert"
#define MyAppExeName "ZLGetCert.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
; Version info shown in installer file Properties → Details
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=Copyright © ZentrixLabs 2025
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=artifacts
OutputBaseFilename=ZLGetCertInstaller
SetupIconFile=ZLGetCert_icon_v2.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\ZLGetCert_icon_v2.ico
UninstallDisplayName=ZLGetCert - Certificate Requester
#if EnableSigning == "1"
SignTool=SignTool
SignedUninstaller=yes
#else
SignedUninstaller=no
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Sign the main application executable
Source: "{#ZLGetCertBin}\ZLGetCert.exe"; DestDir: "{app}"; Flags: ignoreversion
; Distribute application icon for shortcuts and control panel
Source: "ZLGetCert_icon_v2.ico"; DestDir: "{app}"; Flags: ignoreversion
; Include the rest of the build output
Source: "{#ZLGetCertBin}\*"; Excludes: "ZLGetCert.exe"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ZLGetCert_icon_v2.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ZLGetCert_icon_v2.ico"; Tasks: desktopicon

[Run]
; Removed auto-launch since application requires admin privileges



