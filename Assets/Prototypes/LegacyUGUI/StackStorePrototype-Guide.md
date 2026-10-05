# 가판대 프로토타입

`Assets/Prototypes/LegacyUGUI/Scenes/SampleScene.unity`를 열고 Play를 누르면 실행됩니다.
Canvas와 고정 UI는 씬의 `Stack Store Prototype` 프리팹 인스턴스에 저장되어 있습니다.
Play를 누르기 전에도 Hierarchy를 펼쳐 RectTransform, Image, Text를 편집할 수 있습니다.
손님은 미리 구성된 UGUI 프리팹을 런타임에 인스턴스화합니다.

## UI 편집 위치

- `Assets/Prototypes/LegacyUGUI/Scenes/SampleScene.unity`: 게임 씬과 UI 프리팹 인스턴스
- `Assets/Prototypes/LegacyUGUI/Prefabs/StackStoreUI.prefab`: Canvas, 가판대, 골드, 접대 게이지, 통계와 안내 UI
- `Assets/Prototypes/LegacyUGUI/Prefabs/Customer.prefab`: 손님 외형, 인내시간 텍스트와 게이지
- `StackStorePrototype`: Inspector의 Scene UI References로 저장된 UI와 손님 프리팹 연결
- `CustomerView`: 손님 프리팹의 Body / Patience Bar / Patience Label 참조

프리팹을 더블클릭하면 Prefab Mode에서 레이아웃을 편집할 수 있습니다.
런타임 스크립트에는 Canvas·Image·Text 생성 코드가 없으며, 수치 표시와 이동만 제어합니다.
`Stack Store/Install Authored UI` 메뉴는 저장된 UI 프리팹을 다른 씬에 설치하는 편집 도구입니다.

- 유저는 화면 중앙의 고정된 가판대 상인입니다. 가판대는 이전 크기의 75%입니다.
- 손님은 약 0.77초 간격으로 좌·우 가장자리에서 생성됩니다. 기존 2.3초 간격 대비 등장률이 3배이며, 동시 등장 한도도 16명에서 48명으로 늘렸습니다. Y좌표는 외형과 게이지가 화면 내에 보이는 범위에서 무작위로 정합니다.
- 기본 40%의 손님은 가판대에 관심을 갖고, 잠시 이동한 뒤 줄에 합류합니다.
- 나머지는 먼저 가판대 주변에 접근한 뒤 3~6초 동안 BROWSING 상태로 천천히 돌아다닙니다. 배회 시간은 가게 주변에 도착한 시점부터 계산합니다.
- 배회가 끝나면 기본 50%는 출발했던 쪽으로 RETURNING, 나머지는 원래 목적지인 반대편으로 CONTINUING하여 퇴장합니다. 퇴장 중에도 때때로 위아래로 움직이며 가판대를 우회합니다.
- 배회 손님은 방문 손님의 인내시간·접대·골드 대상에서 제외됩니다.
- 줄에 합류한 순서대로 접대하며, 앞 사람이 나가면 뒤 사람이 앞으로 이동합니다.
- 접대 준비시간은 기본 5초이며, 준비가 끝난 후 선두 손님이 도착하면 자동 접대합니다.
- 접대마다 우측 상단 GOLD가 1 증가합니다.
- 방문 손님의 인내시간은 **생성 시점부터** 기본 10초입니다. 남은 시간과 게이지를 해당 손님 아래 표시합니다.
- 인내시간이 끝난 손님은 줄에서 빠져나가며 골드를 지급하지 않습니다.
- 하단에서 접대 수, 떠난 손님 수, 현재 줄 길이를 확인할 수 있습니다.

Hierarchy의 `Stack Store Prototype` 컴포넌트 Inspector에서 설정:

- Equipment / Interval Seconds: 접대 주기
- Patience Seconds: 손님 인내시간
- Spawn Seconds: 생성 간격
- Walk Speed: 이동 속도
- Max Customers: 화면에 존재하는 손님 수 제한
- Visit Chance: 가판대 방문 확률 (0: 모두 지나감, 1: 모두 방문)
- Vertical Wander Distance: 위아래 이동 목표의 최대 변화량
- Avoidance Padding: 가판대와 손님 크기 외에 추가할 회피 여유 공간
- Min Browse Seconds / Max Browse Seconds: 가게 주변에 도착한 뒤의 배회 시간 범위
- Return To Origin Chance: 배회 후 출발했던 쪽으로 돌아갈 확률 (0: 모두 반대편, 1: 모두 되돌아감)
- Stall: 씬에 저장된 가판대 RectTransform 참조. 줄 위치와 회피 범위는 이 가판대를 기준으로 합니다.

장비 교체는 `Equip(new StackStorePrototype.ServiceEquipment { displayName = "New stall", intervalSeconds = 3f })`로 수행합니다.
교체 시 새 장비의 접대 주기가 처음부터 시작됩니다.

UI 문구는 기본 내장 폰트로 안정적으로 표시되도록 영문으로 구성했습니다.
손님 이동과 줄서기는 프로토타입용 2D UGUI 좌표 기반입니다.

## 누적 골드 레벨업과 아이템 선택

- 레벨 1 / 골드 0으로 시작합니다. 별도 경험치는 없습니다.
- 접대로 얻은 보유 골드가 3 / 8 / 15 / 24 / 35 / 48…에 도달하면 레벨이 하나 올라갑니다.
- 골드는 레벨업이나 아이템 선택으로 소비되지 않습니다.
- 레벨업 시 게임을 멈추고 서로 다른 후보 3개를 표시합니다. SELECT 버튼을 클릭하면 아이템을 획득하고 게임이 재개됩니다.
- 처음 획득한 아이템은 레벨 1입니다. 같은 키의 아이템을 다시 선택하면 기존 아이템 레벨이 1 올라갑니다.
- 여러 골드 조건을 한 번에 넘으면 레벨업 선택을 순서대로 처리합니다.
- 상단 중앙에 플레이어 레벨과 다음 레벨에 필요한 누적 골드를, 좌측 하단에 보유 아이템과 현재 레벨을 표시합니다. 보유 목록은 스크롤할 수 있습니다.

초기 테스트 아이템:

- 음식 / Warm Soup: 아이템 레벨마다 새 방문 손님의 인내시간 +1초.
- 능력 / Quick Service: 아이템 레벨마다 접대 속도 +15%. 기본 접대 간격은 `5 / (1 + 0.15 × 아이템 레벨)`초입니다.
- 가게 부품 / Display Shelf: 아이템 레벨마다 방문 확률 +5%p. 방문 확률의 상한은 100%입니다.

## 아이템과 레벨 조건 확장

- `Assets/Prototypes/LegacyUGUI/Items/GoldLevelCurve.asset`: 첫 레벨업 골드 및 이후 필요 골드 증가량을 조정합니다. 기본 첫 필요량 3 / 증가량 2입니다. 추가 필요량은 3, 5, 7…로 늘지만 판정은 누적 보유 골드 3, 8, 15…를 기준으로 합니다.
- `Assets/Prototypes/LegacyUGUI/Items/PrototypeCatalog.asset`: 선택 후보가 될 아이템 목록입니다. 새 아이템을 추가하면 코드 수정 없이 후보 추첨에 포함됩니다.
- Project에서 Create > Stack Store > Item Definition으로 아이템 데이터를 만듭니다.
- 아이템 데이터에는 Picture, Name Key, Description Key, 고유 Key, Kind, Max Level, 레벨당 Effects가 있습니다. 이름·설명 문구는 문자열 테이블에서 관리합니다. 사진은 Sprite이며 현재는 교체 가능한 테스트 아이콘을 사용합니다.
- Key는 비어 있지 않은 고유 문자열이어야 합니다. 중복 키는 후보에서 제외하며 경고를 표시합니다.
- Kind는 Food / Ability / ShopPart입니다. 종류와 효과는 분리되어 한 아이템에 여러 효과를 구성할 수 있습니다.
- Max Level이 0이면 계속 강화 가능합니다. 양수이면 해당 레벨에 도달한 아이템을 선택 후보에서 제외합니다.
- 아이템의 정의 데이터와 플레이 중 보유 레벨은 별도로 관리합니다. 실제 보유 레벨은 GoldLevelSystem의 Owned Items에서 확인합니다. 현재 프로토타입은 Play 종료 시 보유 상태를 저장하지 않습니다.
- 후보가 3개보다 적으면 남은 후보만 표시합니다. 모든 후보가 최대 레벨이면 CONTINUE로 재개할 수 있어 선택창에 갇히지 않습니다.
- 선택창은 `StackStoreUI.prefab > Stack Store UGUI > Level Up Choice`에 저장되어 있습니다. 편집 시 해당 오브젝트를 활성화하면 카드 배치를 확인할 수 있습니다.
- 선택 버튼 입력은 저장된 EventSystem과 기존 `InputSystem_Actions.inputactions`의 UI 액션을 사용합니다.

## 표시 언어와 문자열 관리

- 게임은 한글로 시작합니다. 기준 및 번역 누락 시 대체 언어는 영어입니다.
- `Assets/Prototypes/LegacyUGUI/Localization/PrototypeStrings.asset`의 Entries에서 Key / English / Korean을 관리합니다. 아이템 이름·설명, HUD, 손님 상태, 버튼과 고정 안내 문구가 이 테이블을 참조합니다.
- 고정 Text에는 LocalizedLabel의 Key를 연결하고, 데이터 문구는 `LocalizationService.Text("hud.gold", "gold", 17)`처럼 키와 이름·값 쌍으로 조회합니다.
- 사전 형태의 데이터는 `LocalizationService.Instance.Format(key, data)`로 전달할 수도 있습니다. 템플릿의 `{gold}` 같은 이름이 전달 데이터와 일치해야 합니다.
- 키·변수 누락은 경고와 식별 가능한 표시로 확인하며, 한글 값이 없으면 영어 값으로 대체합니다.
- 게임 중 `LocalizationService.SetLanguage(DisplayLanguage.English)` 또는 Korean으로 표시 언어를 바꿀 수 있습니다. Inspector의 Language 변경도 반영합니다.
- 아이템 설명의 능력 수치는 아이템 Effects에서 가져와 문구에 치환합니다. 문자열에 능력 수치를 별도로 복사하여 보관하지 않습니다.
- StackStoreUI와 Customer 프리팹의 `PrototypeFontBinding`이 Windows에 설치된 맑은 고딕을 직접 불러와 기존 UGUI Text에 연결합니다. 에디터 미리보기와 Play 모두 적용하며, 비활성 선택창과 새로 생성되는 손님도 포함합니다. 폰트 원본 파일은 프로젝트나 저장소에 포함하지 않았습니다.
- OS 동적 폰트를 `.fontsettings`로 저장하면 재질과 글자 이미지가 복원되지 않아 표시되지 않습니다. 해당 저장 자산 대신 실행 중 생성한 폰트와 글자 이미지를 공유합니다. 맑은 고딕이 설치되지 않은 환경에는 Console 경고가 표시됩니다.
- **알파 버전 개발 시 프로젝트에 포함·배포 가능한 정식 한글 폰트를 추가해야 합니다.**

## 아이템 목록 입력 액션

- `Assets/Prototypes/LegacyUGUI/Localization/PrototypeInput.inputactions`의 Interface / OpenInventory 액션으로 보유 목록을 열고 닫습니다. 초기 바인딩은 K이며, Game View에 포커스를 두고 사용합니다.
- InventoryInput은 물리 키를 Update에서 검사하지 않고 입력 액션의 performed 콜백을 사용합니다. 아이템 선택창이 열린 동안 목록 토글은 무시합니다.
- 향후 설정 UI에서는 `InventoryInput.SetInventoryBinding("<Keyboard>/j")`처럼 바인딩을 변경할 수 있습니다. 오버라이드는 PlayerPrefs에 저장하여 다음 실행 때 불러옵니다.
- 현재는 변경 가능한 액션과 저장 API를 구현한 상태이며 별도의 키 설정 화면은 아직 없습니다.
