#define MyAppName "MiTiendaEnLineaMX"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Tecnologías Administrativas ELAD"
#define MyAppExeName "MiTiendaEnLineaMX.exe"

[Setup]
AppId={{A7F1C6E2-4B3E-4D47-9A4D-MTELMX2026}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=MiTiendaEnLineaMX
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Assets\icono.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "MiTiendaEnLineaMX\bin\Release\net10.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MiTiendaEnLineaMX"; Filename: "{app}\MiTiendaEnLineaMX.exe"
Name: "{autodesktop}\MiTiendaEnLineaMX"; Filename: "{app}\MiTiendaEnLineaMX.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MiTiendaEnLineaMX.exe"; Description: "Abrir MiTiendaEnLineaMX"; Flags: nowait postinstall skipifsilent