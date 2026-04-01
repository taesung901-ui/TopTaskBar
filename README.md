# TopTaskBar

Windows 화면 상단에 고정되는 WPF 기반 커스텀 작업 전환 바입니다.  
기본 Windows 작업표시줄을 대체하는 수준까지는 아니지만, 상단 AppBar 영역을 예약하고 실행 중인 앱을 빠르게 전환할 수 있는 실험용 프로젝트로 만들고 있습니다.

## Download
** 실행 파일입니다. 바로 다운로드해서 사용하실 수 있습니다. **

[Latest Release](../../releases)

## 현재 구현된 기능

- 화면 상단 `50px` AppBar 영역 예약
- 종료 시 AppBar 예약 해제
- 상단 고정 바 UI
- 실행 중인 앱 창 목록 표시
- 앱 아이콘 클릭 시 창 활성화
- 활성 창 버튼 클릭 시 최소화 시도
- 앱 버튼 순서 유지
- Windows 테마 색상 반영
- 앱 버튼 오른쪽에 날짜/시간 버튼 표시
- 우클릭 메뉴 및 일부 설정 저장

## 기술 스택

- C#
- .NET 8
- WPF
- Win32 API (P/Invoke)

## 프로젝트 구조

- `TopTaskBar.sln`: 솔루션 파일
- `TopTaskBar/TopTaskBar.csproj`: WPF 프로젝트
- `TopTaskBar/AppBarHelper.cs`: AppBar 등록/해제 및 상단 영역 예약
- `TopTaskBar/WindowCatalog.cs`: 실행 중 창 열거, 아이콘 추출, 창 전환
- `TopTaskBar/MainWindow.xaml`: 상단 바 UI
- `TopTaskBar/MainWindow.xaml.cs`: UI 상태, 갱신, 설정 처리
- `TopTaskBar/SettingsStore.cs`: 설정 저장/불러오기

## 빌드 방법

### Visual Studio

1. `TopTaskBar.sln` 을 엽니다.
2. 구성은 `Debug`, 플랫폼은 기본값으로 둡니다.
3. `F5` 로 실행하거나 `Ctrl+F5` 로 디버그 없이 실행합니다.

### CLI

빌드:

```powershell
dotnet build TopTaskBar.sln -c Release
```

실행:

```powershell
dotnet run --project .\TopTaskBar\TopTaskBar.csproj -c Release
```

### Publish

```powershell
dotnet publish .\TopTaskBar\TopTaskBar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\TopTaskBar\bin\Release\manual-publish\win-x64
```

출력 폴더:

```text
TopTaskBar\bin\Release\manual-publish\win-x64
```

참고:
- GitHub Release에는 이 폴더 내용을 zip으로 묶어서 올립니다.
- `TopTaskBar.pdb` 는 배포 zip에서 제외해도 됩니다.

## 실행 확인 포인트

- 상단에 바가 표시되는지
- 다른 창을 최대화했을 때 상단 영역이 유지되는지
- 앱 아이콘 버튼 클릭 시 창 전환이 되는지
- 날짜/시간 버튼이 앱 버튼의 오른쪽 끝에 표시되는지
- `...` 버튼 좌클릭 또는 상단 바 우클릭으로 설정 메뉴가 열리는지

## 설정

현재는 아래 항목이 설정 메뉴에 연결되어 있습니다.

- 앱 버튼 폭 확대/축소
- 앱 버튼 폭 기본값 복원
- 창 목록 새로고침
- 앱 종료

설정은 로컬에 저장됩니다.

## 런처 항목 추가

런처에는 실행 파일뿐 아니라 폴더, URL, 바로가기도 넣을 수 있습니다.

### 실행 파일과 바로가기

- 런처 하단의 `앱 추가` 버튼을 누릅니다.
- 파일 선택 창에서 `.exe` 또는 `.lnk` 파일을 고릅니다.
- 선택한 항목이 런처 목록에 바로 추가됩니다.

예:
- 바탕화면의 `Visual Studio 2022.lnk`
- 시작 메뉴 폴더 안의 프로그램 바로가기

### 폴더와 URL

- 런처 하단의 `JSON 수정` 버튼으로 `settings.json`을 엽니다.
- `PinnedApps`에 항목을 직접 추가합니다.
- 저장하면 파일 변경 감시로 앱에 바로 반영됩니다.

폴더 예:

```json
{
  "Name": "! Documents",
  "Path": "C:\\Users\\USER\\Documents",
  "Arguments": "",
  "WorkingDirectory": "C:\\Users\\USER\\Documents"
}
```

URL 예:

```json
{
  "Name": "! Google",
  "Path": "https://www.google.com",
  "Arguments": "",
  "WorkingDirectory": ""
}
```

정렬 참고:
- 런처 항목은 이름 기준으로 정렬됩니다.
- 필요하면 `!` 같은 특수문자를 이름 앞에 붙여 위쪽으로 올릴 수 있습니다.

### 최근 실행 앱

- 런처로 실행한 항목은 상단 `Recent` 아이콘 영역에 최대 5개까지 표시됩니다.
- 최근 목록은 `settings.json`의 `RecentLauncherPaths`에 저장되며, 앱을 다시 실행해도 유지됩니다.

### 알람 스케줄러
- 알람 스케줄러는 활성화된 알람 중에서 가장 가까운 다음 알람 1개를 기준으로 동작합니다.
- 현재 시간 확인은 `AlarmScheduler`가 1초마다 한 번만 수행하며, 이 정도 주기는 시스템 부하가 거의 없는 수준입니다.

## 참고

- `bin`, `obj`, `.vs` 는 Git에 포함하지 않도록 `.gitignore` 가 설정되어 있습니다.
- 일부 앱은 창 전환/최소화 동작이 Windows 포커스 정책이나 앱 종류에 따라 다르게 보일 수 있습니다.
