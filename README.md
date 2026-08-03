# TopTaskBar

TopTaskBar는 Windows 화면 상단에 고정되는 .NET 8 WPF 작업 전환 바입니다. 화면 상단에 50 DIP의 AppBar 영역을 예약하고, 실행 중인 창 전환과 자주 쓰는 항목 실행, 달력, 타이머 및 알람을 한곳에서 제공합니다.

현재 릴리스 버전은 `1.0.5`입니다.

## Download

[Latest Release](../../releases)에서 설치 프로그램 또는 압축 배포본을 받을 수 있습니다.

## 주요 기능

- 화면 상단 50 DIP AppBar 영역 예약 및 종료 시 해제
- DPI, 디스플레이 변경, 절전 복귀 및 원격 데스크톱 세션 전환 대응
- 실행 중인 데스크톱 창과 아이콘 표시
- 창 버튼 클릭으로 활성화, 활성 창 재클릭으로 최소화
- 최소화 전 최대화 상태를 기억하여 다시 선택할 때 최대화 상태 복원
- 실행 중 창의 표시 순서 유지 및 창 버튼 폭 설정
- Windows 밝은/어두운 테마와 강조색 반영
- 날짜·시간 및 월간 달력
- JSON 기반 런처: EXE, LNK, 폴더, HTTP/HTTPS URL, 이름 검색, 최근 실행 5개
- 실행 중인 앱을 우클릭하여 런처에 추가
- 카운트다운 타이머
- 다중 알람, 1회성 알람 및 요일 반복 알람
- Outlook Classic 받은편지함의 미확인 메일 표시
- 단일 인스턴스 실행과 로컬 진단 로그

## 지원 환경

- Windows 10 또는 Windows 11
- x64 PC
- 소스 빌드: .NET 8 SDK 필요
- self-contained 배포본 및 설치 프로그램 실행: 별도 .NET 설치 불필요

## 프로젝트 구조

- `TopTaskBar.sln`: 앱과 테스트 솔루션
- `TopTaskBar/TopTaskBar.csproj`: WPF 앱 프로젝트
- `TopTaskBar.Tests/`: 알람 및 설정 저장 자동 테스트
- `TopTaskBar/AppBarHelper.cs`: AppBar 등록, 화면 예약 및 디스플레이 복구
- `TopTaskBar/WindowCatalog.cs`: 창 열거, 아이콘 추출 및 창 전환
- `TopTaskBar/MainWindow.xaml`: 상단 바와 팝업 UI
- `TopTaskBar/MainWindow.xaml.cs`: UI 상태 및 기능 연결
- `TopTaskBar/SettingsStore.cs`: 설정 저장, 복구 및 손상 파일 백업
- `TopTaskBar/AlarmScheduler.cs`: 다중 알람 스케줄링
- `InnoSetupScript.iss`: Inno Setup 설치 프로그램 정의

상세 구조는 [아키텍처 문서](docs/ARCHITECTURE.md)를 참고하세요.

## 빌드 및 실행

Visual Studio에서는 `TopTaskBar.sln`을 열고 `F5` 또는 `Ctrl+F5`로 실행합니다.

```powershell
dotnet build .\TopTaskBar.sln -c Release
dotnet test .\TopTaskBar.sln -c Release
dotnet run --project .\TopTaskBar\TopTaskBar.csproj -c Release
```

## 설정과 로그

설정 파일:

```text
%LocalAppData%\TopTaskBar\settings.json
```

런처의 `JSON 수정` 버튼으로 열 수 있으며, 정상적으로 저장하면 실행 중인 앱에 자동 반영됩니다. JSON을 읽을 수 없으면 현재 실행 중인 설정을 유지하고 같은 폴더에 `settings.invalid-날짜.json` 백업을 만듭니다.

진단 로그:

```text
%LocalAppData%\TopTaskBar\logs\interaction.log
```

## 런처 항목 추가

실행 파일과 바로가기는 런처 하단의 `앱 추가` 버튼에서 `.exe` 또는 `.lnk` 파일을 선택합니다. 실행 중인 창 버튼을 우클릭하여 해당 프로그램을 추가할 수도 있습니다.

폴더와 URL은 `JSON 수정`에서 `PinnedApps`에 추가합니다.

```json
{
  "Name": "! Documents",
  "Path": "C:\\Users\\USER\\Documents",
  "Arguments": "",
  "WorkingDirectory": "C:\\Users\\USER\\Documents"
}
```

```json
{
  "Name": "! Google",
  "Path": "https://www.google.com",
  "Arguments": "",
  "WorkingDirectory": ""
}
```

런처 항목은 이름순으로 표시됩니다. 최근 실행 목록은 `RecentLauncherPaths`에 저장되며 최대 5개를 유지합니다.

## 타이머와 알람

- 타이머는 분·초를 지정하여 시작, 중지 및 리셋할 수 있습니다.
- 요일을 선택하지 않은 알람은 다음 도래 시각에 한 번 울리고 자동으로 꺼집니다.
- 요일을 선택한 알람은 해당 요일마다 반복됩니다.
- 같은 시각의 알람이 여러 개면 모두 처리합니다.
- 타이머와 알람은 TopTaskBar가 실행 중일 때만 동작합니다.

## Outlook 알림

Outlook Classic이 실행 중이면 기본 받은편지함의 미확인 메일 여부를 5초 간격으로 확인하고 Outlook 창 버튼에 표시합니다. 새 Outlook 및 Outlook이 실행되지 않은 상태는 지원 대상이 아닙니다.

## 릴리스

publish와 설치 프로그램 생성 절차, 버전 확인 및 수동 테스트 목록은 [릴리스 문서](docs/RELEASE.md)에 정리되어 있습니다.

## 제한 사항

- 일부 관리자 권한 앱, UWP 앱 및 특수 시스템 창은 경로 획득이나 포커스 전환이 제한될 수 있습니다.
- 일부 앱은 자체 전체 화면 모드나 Windows 포커스 정책 때문에 창 전환 방식이 다르게 보일 수 있습니다.
- Outlook 미확인 메일 표시는 Outlook Classic의 실행 중 COM 인스턴스를 사용합니다.
