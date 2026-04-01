# 프로젝트: TopTaskBar (WPF 기반 상단 작업표시줄)

## 1. 목적
- 윈도우 11 하단 작업표시줄의 시선 분산을 해결하기 위해, 화면 상단에 고정된 앱 실행/전환 바를 구현함.

## 2. 핵심 기술 스택
- 언어/프레임워크: C# / .NET 8 WPF
- API: Win32 API (P/Invoke) - user32.dll, shell32.dll

## 3. 주요 기능 및 요구사항
### Phase 1: AppBar 등록 및 화면 예약
- `SHAppBarMessage`를 사용하여 화면 상단에 영역(약 50px)을 예약함.
- 다른 창들이 최대화되어도 이 영역을 가리지 않아야 함 (AppBar 동작).
- 앱 종료 시 반드시 `ABM_REMOVE`를 호출하여 영역 예약을 해제해야 함.

### Phase 2: UI 구성
- `WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"` 설정.
- 배경은 반투명한 다크 그레이(또는 시스템 테마 연동).
- 가로로 긴 막대 형태이며 화면 상단에 밀착됨.

### Phase 3: 실행 중인 앱 목록 및 전환
- 현재 실행 중인 윈도우(HWND) 목록을 가져와 아이콘으로 나열함.
- 아이콘 클릭 시 해당 윈도우를 `SetForegroundWindow`로 활성화함.

## 4. 제약 사항
- 외부 커스텀 라이브러리 최소화 (순수 Win32 API 선호).
- 코드는 유지보수가 쉽도록 `AppBarHelper.cs` 등으로 로직을 분리함.