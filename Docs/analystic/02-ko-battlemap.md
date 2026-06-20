# 02 · KO 서브시스템 — 배틀맵 / 이동 / 카메라

> 위치: `Assets/1.Scripts/KO/Battle/` (BattleScript, BattleMapProvider, UnitMover, BattleCameraFollow, MapTester) + `MapData/` (JSON)
> 관련 문서: `Docs/API/BattleScript.md`, `Docs/Systems/MapSystem.md`, `Docs/DataFormats/MapDataFormat.md`

## 1. 역할 & 아키텍처

KO는 전장(맵)의 **로딩·표현·이동 인프라**입니다.

```
MapData (JSON)
 ├─ 001.json ~ 005.json   맵 레이아웃(21×21 그리드)
 ├─ TILES.json            타일 속성(보행 가능, 이동 비용, 색)
 ├─ OBJECTS.json          장애물(블로킹, 높이, 프리팹)
 └─ ALLYS.json / ENEMIES.json  유닛 스탯(agility/spirit/guard/luck/mov)
        ↓
BattleScript (로드 → 해석 → 표현)
 ├─ 바닥 큐브 2D 그리드 생성 (BattleMap)
 ├─ 장애물 배치 (ObjectMap)
 ├─ 유닛 마커 스폰 (UnitMap, BattleUnitEntry)
 └─ 맵 질의 API (IsWalkable, GetMovementCost, GridToWorld...)
        ↓
BattleMapProvider (어댑터) ──▶ KD.GridManager 에 맵 함수 노출
```

- **맵 칩 32비트 포맷**: `(ObjectID << 8) | TileID` — 한 셀에 타일+장애물 동시 표현
- **행 우선 저장**: 1D 배열, 인덱스 `z * width + x`
- **좌표계**: 좌상단 원점, X→오른쪽, Z→아래

## 2. 파일 인벤토리

| 파일 | 줄수 | 역할 | 상태 |
|------|------|------|------|
| `BattleScript.cs` | 1105 | 맵 로딩/생성/타일·유닛 관리/이동 API | **Partial** |
| `BattleMapProvider.cs` | 55 | 어댑터: BattleScript API를 KD.GridManager에 노출 | **Done** |
| `UnitMover.cs` | 208 | 부드러운 이동 보간 + 자동 회전, 이동 큐 | **Done** |
| `BattleCameraFollow.cs` | 94 | 아이소메트릭 45° 직교 카메라, 스무스 추적 | **Done** |
| `MapTester.cs` | 123 | 런타임 맵 전환(Space/B 키) 테스트 | **Done** |

## 3. 구현 / 미구현

### ✅ 동작하는 것
- 맵 로딩(JSON 파싱, 실패 시 10×10 폴백, `LoadMap()`)
- 맵 생성(바닥 큐브 인스턴스화, 장애물 배치, 계층 정리)
- 타일/오브젝트 정의 파싱(TILES/OBJECTS.json → 딕셔너리)
- 유닛 정의 로딩(ALLYS/ENEMIES.json → 스탯 딕셔너리)
- 좌표 변환(GridToWorld / WorldToGrid, 중앙 오프셋)
- 보행/이동 비용 체크(타일+장애물 로직)
- 유닛 마커 스폰/이동(MoveUnit 단일 스텝, 점유 추적)
- 도달 가능 타일 BFS(ComputeReachable, 맨해튼 거리 예산)
- 카메라(아이소메트릭 45°, 직교, 스무스 댐핑)
- 런타임 맵 전환(MapTester)

### ⚠️ 미구현 / 스텁
| 항목 | 위치 | 상태 |
|------|------|------|
| **JSON 스폰으로 유닛 배치** | `PlaceUnits()` (621-634) | 빈 리스트 초기화만, `enemySpawns`/`allySpawns` **미사용** → 맵 디자이너의 스폰 데이터가 조용히 버려짐 |
| **턴/이니셔티브 API** | `GetTurnOrder/GetCurrentTurnUnit/AdvanceTurn/TryMoveCurrentUnit/GetCurrentTurnMovesRemaining` | **전부 미구현** — 문서엔 있으나 C# 본문 없음 |
| **HP/데미지** | 생성자 136-139 | TODO 주석으로 막힘(`int guard...` 주석 처리). `CurrentHP` 프로퍼티만 lazy-init로 존재, 공격/피격 메서드 없음 |

## 4. 핵심 설계 결정: `mapOnlyMode = true`

`BattleScript`는 **MVP에서 맵 전용 모드로 동작**합니다(인스펙터 기본값 `mapOnlyMode = true`, line 175).
- 주석: *"true면 맵 생성까지만 실행. 유닛 배치/턴 시스템은 KD 시스템이 담당한다."*
- 즉 `PlaceUnits()`는 빈 컨테이너만 만들고, 턴/유닛 로직은 KD가 전담.

→ **의도된 분리지만 부작용 3가지**:
1. 문서화된 턴 API(`GetTurnOrder` 등)가 실제론 비어 있어 오해 유발
2. JSON 스폰 데이터(enemySpawns/allySpawns)가 낭비됨
3. 문서에 이 제약이 명시돼 있지 않음

## 5. 버그 / 리스크 / 데드코드

1. **🔴 God-class** — `BattleScript`(~1,100줄)가 JSON 로딩(256줄) + 3D 씬 생성(400+줄) + 좌표 수학(60+줄) + 유닛 배치/이동(200+줄) + 턴 API 스텁(50+줄) + 직렬화 클래스(200+줄)를 전부 담당. **SRP 위반.** 데이터 로딩/렌더링/게임로직 분리 권장.
2. **🟡 JSON 스폰 미연동** — `currentMapData.enemySpawns/allySpawns`를 읽지 않음. 런타임 `PlaceAllyAtSlot()`로 수동 배치만 가능.
3. **🟡 죽은 UI 참조** — 169-171, 242-245줄에 `initiativeUIGameObject`/`IInitiativeUI`가 주석 처리됨(SW UI 연동 흔적, 미완).
4. **🟢 하드코딩 폴백 맵** — 199-211줄, JSON 실패 시 10×10 전부 바닥(장애물 없음).
5. **🟢 전투 해결 부재** — HP는 계산되나 `Attack()`/`TakeDamage()` 없음(전투는 KD 담당).

## 6. KD와의 통합 (중요)

**병렬이 아니라 깔끔히 분담**되어 있습니다.

- **KO = 맵 소유**: 그리드 데이터, 타일/장애물 정의, 보행 체크. `BattleMapProvider`로 4개 핵심 API 노출:
  `IsValidTile / IsWalkable / GridToWorld·WorldToGrid / GetFloorTile`
- **KD.GridManager = 전술 로직**: 인스펙터에서 `mapProvider`(BattleMapProvider)를 받아 맵 질의를 위임. 없으면 자체 폴백.
- **중복 없음** — GridManager 기능이 KO에 중복 구현되지 않음. 단, KD에 잔존하는 `width/height/wallTiles` 폴백은 약간 중복(문제는 아님).

> ✅ **결합 설계 자체는 깨끗**. KO는 맵, KD는 로직. `TestScene_KD`에서 실제로 함께 동작.

## 7. 기존 문서(`Docs/`) 정확도

| 문서 | 정확도 | 비고 |
|------|--------|------|
| `API/BattleScript.md` | **~60%** | 턴/이니셔티브 API 5종은 **문서엔 있으나 미구현**. 맵/타일/좌표/이동 API는 정확 |
| `Systems/MapSystem.md` | ~90% | 32비트 칩, 행 우선, 좌표계 정확. `mapOnlyMode` 제약 미언급 |
| `DataFormats/MapDataFormat.md` | ~95% | JSON 스키마·비트필드 정확. allySpawns/enemySpawns가 현재 미사용임을 미언급 |

## 8. 권장 (잼 기준)

- **HIGH**: 문서에 `mapOnlyMode` 제약 명시 / 향후 mapOnlyMode=false 시 스폰 로딩 구현
- **MED**: god-class 리팩터(로딩·렌더링·데이터 분리), 턴 API 구현(필요 시)
- **LOW**: 죽은 UI 코드 정리, 큐브 인스턴스화 풀링
