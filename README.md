# TopTaskBar

Windows 화면 상단에 고정되는 WPF 기반 커스텀 작업 전환 바입니다.  
기본 Windows 작업표시줄을 대체하는 수준까지는 아니지만, 상단 AppBar 영역을 예약하고 실행 중인 앱을 빠르게 전환할 수 있는 실험용 프로젝트로 만들고 있습니다.

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

```powershell
dotnet build TopTaskBar.sln
```

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

## 참고

- `bin`, `obj`, `.vs` 는 Git에 포함하지 않도록 `.gitignore` 가 설정되어 있습니다.
- 일부 앱은 창 전환/최소화 동작이 Windows 포커스 정책이나 앱 종류에 따라 다르게 보일 수 있습니다.

