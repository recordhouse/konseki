# Konseki

일본의 밤 골목을 배경으로 한 1인칭 미행 게임 프로토타입입니다. 외부 에셋 없이 유니티 기본 도형과 런타임 생성 코드만으로 플레이 가능한 기본 틀을 구성했습니다.

## 실행

1. Unity Hub에서 이 폴더를 프로젝트로 추가합니다.
2. Unity 2022.3 LTS 이상으로 프로젝트를 엽니다.
3. `Assets/Scenes/NightAlley.unity`를 열고 Play를 누릅니다.

씬이 시작되면 현대식 메인 도로, 동·서쪽 순환 샛길, 조명, 플레이어, 타깃, HUD가 자동으로 생성됩니다.

## 조작

- `W A S D` — 천천히 걷기
- `마우스` — 시점 이동
- `Esc` — 마우스 잠금 해제
- `화면 클릭` — 마우스 다시 잠금
- `R` — 현재 게임 재시작
- `1` — 쇼와 시대 이자카야 골목
- `2` — 비 내리는 네온 요코초
- `3` — 고요한 일본 주택가

## 게임 규칙

- 코트를 입은 타깃을 놓치지 말고 여러 교차로와 순환 샛길을 따라 골목 끝까지 미행합니다.
- 너무 가까이 접근하거나 타깃의 시야에 오래 머물면 의심 수치가 올라갑니다.
- 너무 멀리 떨어진 상태가 지속되면 타깃을 놓칩니다.
- 타깃이 목적지에 도착할 때 적절한 거리를 유지하고 있으면 성공합니다.

## 코드 구성

- `NightAlleyBootstrap` — 실행 시 프로토타입 전체 조립
- `BakedAlleyLevel` — 미리 구운 골목 Prefab을 로드하고 조명/환경 설정 복원
- `PerformanceGovernor` — 60 FPS 상한, 비활성 창 15 FPS, High 품질 적용
- `NightAlleyWorldBuilder` — 일본 야간 골목과 조명 생성
- `AlleyRouteCatalog` — 타깃 이동 경로만 분리해 관리
- `NightAlleyPrefabBaker` — 골목과 재질을 편집기에서 Prefab으로 변환
- `PixelArtRenderer` — 저해상도 픽셀화, 제한 팔레트와 디더링
- `AlleyVariantSwitcher` — 세 가지 골목 테마 전환
- `FirstPersonController` — 느린 1인칭 이동/시점
- `TargetWalker` — 타깃의 경로 이동과 보행 모션
- `TailGameManager` — 거리, 의심도, 성공/실패 판정
- `TailHud` — 상태 HUD와 안내 문구

`NightAlleyWorldBuilder.cs`는 골목 구조를, `AlleyRouteCatalog.cs`는 타깃 동선을 담당합니다.

## 제작 구조

- 편집 모드에서 골목 소스가 바뀌면 `Assets/Konseki/Resources/Levels`에 테마별 Prefab 3종을 자동 생성합니다.
- 게임 실행 시에는 Prefab을 불러오므로 건물·도로·재질을 매번 코드로 다시 만들지 않습니다.
- Prefab이 아직 없거나 손상된 경우에만 기존 절차 생성 방식을 안전한 폴백으로 사용합니다.
- 수동 재생성이 필요하면 Unity 메뉴에서 `Konseki > Level > Bake All Alley Prefabs`를 실행합니다.
- 타깃 경로는 `Assets/Konseki/Scripts/World/AlleyRouteCatalog.cs`에서 수정합니다.

## 성능 기준

- 게임 실행은 전경 60 FPS, 비활성 창 15 FPS를 넘지 않습니다.
- 기본 품질은 High이며 Ultra를 프로젝트 기본값으로 사용하지 않습니다.
- 실시간 그림자는 달빛 방향광 하나에만 사용하고 포인트라이트에는 사용하지 않습니다.
- 움직이지 않는 골목 메시들은 런타임 정적 배칭 대상으로 유지합니다.
- 반복 효과는 개수를 제한하고 30 Hz 업데이트를 기본으로 합니다.
- HDR, 추가 후처리, 실시간 그림자 조명은 실제 시각 효과가 필요한 경우에만 추가합니다.
