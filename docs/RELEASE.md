# 릴리스 절차

모든 명령은 저장소 루트에서 실행한다. 공식 배포 대상은 `win-x64`, self-contained, 다중 파일 publish이다.

## 사전 준비

- .NET 8 SDK
- Inno Setup 6
- 최초 self-contained publish에 필요한 인터넷 연결

## 1. 버전 확인

다음 값이 동일해야 한다.

- `TopTaskBar/TopTaskBar.csproj`의 `Version`, `AssemblyVersion`, `FileVersion`
- `InnoSetupScript.iss`의 `MyAppVersion`
- `CHANGELOG.md`의 최신 버전
- Git 태그와 GitHub Release 버전

## 2. 테스트와 빌드

```powershell
dotnet test .\TopTaskBar.sln -c Release
dotnet build .\TopTaskBar.sln -c Release --no-restore
```

## 3. Publish

```powershell
dotnet publish .\TopTaskBar\TopTaskBar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o .\TopTaskBar\bin\Release\manual-publish\win-x64
```

Visual Studio의 `FolderProfile`도 같은 출력 폴더와 옵션을 사용한다. 압축 배포본은 `TopTaskBar\bin\Release\manual-publish\win-x64` 폴더 전체를 ZIP으로 묶는다. `TopTaskBar.pdb`는 공개 ZIP에서 제외할 수 있다.

## 4. 설치 프로그램

```powershell
ISCC.exe .\InnoSetupScript.iss
```

PATH에 없으면:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\InnoSetupScript.iss
```

결과는 `Output\TopTaskBar_Setup.exe`이다.

## 5. 수동 확인

- 앱을 두 번 실행해 단일 인스턴스인지 확인
- 상단 바와 다른 창의 최대화 작업 영역 확인
- 배율이 다른 모니터 및 원격 데스크톱 연결·해제 후 위치 확인
- 창 버튼 활성화와 최소화 확인
- 최대화 창을 최소화한 뒤 다시 선택해 최대화 상태 유지 확인
- 런처의 EXE, LNK, 폴더, URL, 검색과 최근 목록 확인
- `settings.json` 자동 반영과 잘못된 JSON의 invalid 백업 확인
- 타이머 완료 확인
- 1회성, 반복 및 같은 시각 다중 알람 확인
- Outlook Classic 미확인 메일 표시 확인
- 종료 후 다른 창의 최대화 영역 원상복구 확인

## 6. 게시

- `CHANGELOG.md` 갱신
- 설치 프로그램과 필요 시 ZIP 업로드
- 같은 버전으로 Git 태그와 GitHub Release 생성
- 설치·업그레이드 후 프로그램 버전과 아이콘 확인
