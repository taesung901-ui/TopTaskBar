# TopTaskBar 아키텍처

이 문서는 현재 구현을 기준으로 주요 책임과 실행 흐름을 설명한다. 초기 기능 계획과 개발 과정은 루트의 `Plan.md`, `LauncherPlan.md`에 역사 문서로 남겨 둔다.

## 실행 생명주기

1. `App`이 로컬 Mutex를 획득해 단일 인스턴스를 보장한다.
2. `MainWindow`가 설정, 타이머, 알람 스케줄러와 Outlook 모니터를 생성한다.
3. HWND가 준비되면 `AppBarHelper`가 Shell AppBar를 등록하고 주 모니터 상단 50 DIP를 예약한다.
4. 2초 주기로 날짜·시간과 실행 중인 창 목록을 갱신한다.
5. 종료 시 타이머, 파일 감시기, 시스템 이벤트, COM 모니터와 AppBar 등록을 해제한다.

## AppBar와 화면 좌표

`AppBarHelper`는 Shell AppBar 메시지로 화면 영역을 예약한다. WPF의 DIP와 Win32 물리 픽셀 차이는 `GetDpiForWindow`로 보정한다. 디스플레이 변경, DPI 변경, 절전 복귀, 세션 전환 및 AppBar 위치 변경 알림을 받으면 여러 차례 지연 갱신하여 원격 데스크톱 전환 후 작업 영역을 복구한다.

일반 상태에서는 `WS_EX_NOACTIVATE`를 사용하고, 검색창 등 입력이 필요한 팝업이 열릴 때만 활성화 가능한 모드로 전환한다.

## 창 목록과 전환

`WindowCatalog`가 `EnumWindows`로 최상위 창을 수집한다. 숨김 창, cloaked 창, 도구 창, 소유된 보조 창과 TopTaskBar 자체 창은 제외한다.

- 비활성 창: 마지막 활성 팝업을 복원하고 포그라운드로 전환
- 활성 창: 최소화
- 최소화 전 최대화 창: 다음 활성화 때 최대화 상태로 복원

아이콘은 창 메시지, 창 클래스 아이콘, 실행 파일 아이콘 순으로 찾는다.

## 설정

`SettingsStore`는 `%LocalAppData%\TopTaskBar\settings.json`에 창 버튼 폭, 고정 런처 항목, 최근 실행 경로와 알람 목록을 저장한다.

저장은 같은 디렉터리의 임시 파일을 완성한 뒤 대상 파일로 교체한다. 읽을 수 없는 JSON은 원본을 보존하고 `settings.invalid-날짜.json`으로 백업한다. 실행 중 파일 재로드가 실패하면 기존 메모리 설정을 유지한다.

## 런처

`LauncherAppSetting`은 저장 모델이고 `LauncherAppItem`은 아이콘과 fallback 문자를 포함한 표시 모델이다. 대상 유형은 경로에서 EXE, LNK, 디렉터리, HTTP/HTTPS URL로 판별한다. 검색은 이름 부분 일치이며, 실행 성공 시 최근 목록의 앞으로 이동하고 최대 5개를 유지한다.

## 타이머와 알람

타이머는 `TimerToolController`와 `TimerToolState`가 담당한다. 알람은 `AlarmEntry` 목록과 `AlarmScheduler`가 담당한다. 활성 알람의 다음 도래 시각을 계산하여 가장 이른 시각을 감시하되, 같은 시각의 알람은 모두 완료 이벤트로 전달한다.

- 요일 없음: 다음 오늘/내일 시각에 한 번 실행 후 비활성화
- 요일 있음: 선택한 요일에 반복

## Outlook Classic

`OutlookClassicUnreadMonitor`는 실행 중인 `Outlook.Application` COM 객체에 연결하여 기본 받은편지함의 미확인 메일 수를 확인한다. Outlook을 새로 실행하지 않으며 실패는 기능 미지원 상태로 처리한다.

## 진단 로그

`InteractionLogger`는 `%LocalAppData%\TopTaskBar\logs\interaction.log`에 앱 생명주기, AppBar 재배치, 창 전환 및 런처 실행 정보를 기록한다. 로그 실패는 앱 동작을 중단시키지 않는다.
