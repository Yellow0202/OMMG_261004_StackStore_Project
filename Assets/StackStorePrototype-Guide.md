# 가판대 프로토타입

`Assets/Scenes/SampleScene.unity`를 열고 Play를 누르면 실행됩니다.
Canvas와 고정 UI는 씬의 `Stack Store Prototype` 프리팹 인스턴스에 저장되어 있습니다.
Play를 누르기 전에도 Hierarchy를 펼쳐 RectTransform, Image, Text를 편집할 수 있습니다.
손님은 미리 구성된 UGUI 프리팹을 런타임에 인스턴스화합니다.

## UI 편집 위치

- `Assets/Scenes/SampleScene.unity`: 게임 씬과 UI 프리팹 인스턴스
- `Assets/Prefabs/StackStoreUI.prefab`: Canvas, 가판대, 골드, 접대 게이지, 통계와 안내 UI
- `Assets/Prefabs/Customer.prefab`: 손님 외형, 인내시간 텍스트와 게이지
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
