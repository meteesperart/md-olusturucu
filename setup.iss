#define MyAppName      "MD Oluşturucu"
#define MyAppVersion   "2.1"
#define MyAppPublisher "OKASER"
#define MyAppCompany   "OKASER"
#define MyAppExeName   "MDOlusturucu.exe"
#define SourceDir      "publish\v2.1"
#define DemoDir        "Demo"

[Setup]
AppId={{A3F2B1C4-7E8D-4F9A-B2C3-1D4E5F6A7B8C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://okaser.com
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=publish\installer
OutputBaseFilename=MDOlusturucu_v2.1_Setup
SetupIconFile=Resources\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
ShowLanguageDialog=no
LanguageDetectionMethod=locale

[Languages]
Name: "turkish";  MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english";  MessagesFile: "compiler:Default.isl"
Name: "french";   MessagesFile: "compiler:Languages\French.isl"
Name: "german";   MessagesFile: "compiler:Languages\German.isl"
Name: "spanish";  MessagesFile: "compiler:Languages\Spanish.isl"
Name: "russian";  MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}:"

[Files]
; Ana uygulama ve WPF native bağımlılıkları
Source: "{#SourceDir}\{#MyAppExeName}";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\D3DCompiler_47_cor3.dll";  DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\PenImc_cor3.dll";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\vcruntime140_cor3.dll";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\wpfgfx_cor3.dll";          DestDir: "{app}"; Flags: ignoreversion

; Demo Markdown dosyaları (Documents\MD Oluşturucu)
Source: "{#DemoDir}\Hoş Geldiniz.md";     DestDir: "{userdocs}\MD Oluşturucu"; Flags: ignoreversion onlyifdoesntexist
Source: "{#DemoDir}\Markdown Rehberi.md"; DestDir: "{userdocs}\MD Oluşturucu"; Flags: ignoreversion onlyifdoesntexist
Source: "{#DemoDir}\Örnek Belge.md";      DestDir: "{userdocs}\MD Oluşturucu"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{group}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName}'ı Kaldır";    Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";        Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[UninstallDelete]
Type: files;      Name: "{userappdata}\MDOlusturucu\settings.json"
Type: dirifempty; Name: "{userappdata}\MDOlusturucu"

[Code]

// Sistemin birincil dil kodunu (LANGID alt 10 biti) uygulama dil koduna çevirir
function GetAppLangCode: String;
var
  PrimaryLang: Integer;
begin
  PrimaryLang := GetUILanguage() and $3FF;
  case PrimaryLang of
    $001F: Result := 'tr';   // Türkçe
    $000C: Result := 'fr';   // Fransızca
    $0007: Result := 'de';   // Almanca
    $000A: Result := 'es';   // İspanyolca
    $0019: Result := 'ru';   // Rusça
    else   Result := 'en';   // Varsayılan: İngilizce
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsFile: string;
  LangCode: string;
  JsonContent: string;
begin
  if CurStep = ssInstall then
  begin
    SettingsFile := ExpandConstant('{userappdata}\MDOlusturucu\settings.json');

    // Eski ayar dosyasını temizle (eski kurulumda kalan ayarlar karışmasın)
    if FileExists(SettingsFile) then
      DeleteFile(SettingsFile);

    // Sistem diline göre varsayılan ayar dosyasını oluştur
    LangCode := GetAppLangCode();
    CreateDir(ExpandConstant('{userappdata}\MDOlusturucu'));
    JsonContent := '{"MarkdownDirectory":"","EditorFontFamily":"Cascadia Code","EditorFontColor":"","IsPreviewVisible":true,"Language":"' + LangCode + '"}';
    SaveStringToFile(SettingsFile, JsonContent, False);
  end;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
