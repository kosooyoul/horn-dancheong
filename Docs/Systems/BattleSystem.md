# 전투 시스템 스펙 문서

**최종 업데이트:** 2026-06-20  
**관련 구현:** `BattleScript.cs`, `BattleTurnController.cs`, `UnitMover.cs`, `BattleCameraFollow.cs`

---

## 1. 개요

Horn Dancheong의 전투 시스템은 2D 그리드 맵 위에서 유닛이 **민첩(Agility) 기반 틱 게이지 모델**에 따라 차례로 행동하는 턴제 전술 전투입니다.

```
플레이어 조작 범위
  키보드 이동 / 마우스 클릭 이동
  ↓
BattleTurnController   (입력 처리 · 이동 범위 표시 · 행동 메뉴 GUI)
  ↓
BattleScript           (맵·유닛 로딩, 이동 규칙, 행동 순서 계산)
  ↓
UnitMover              (유닛 마커 보간 이동)
```

---

## 2. 핵심 컴포넌트

| 컴포넌트 | 역할 |
|---|---|
| `BattleScript` | 맵·유닛 로딩·배치, 행동 순서, 이동 처리 메인 클래스 |
| `BattleTurnController` | 입력 처리, 이동 범위 강조 표시, 행동 메뉴 GUI |
| `UnitMover` | 유닛 마커를 목표 좌표까지 부드럽게 보간 이동 (큐 기반) |
| `BattleCameraFollow` | 현재 턴 유닛을 카메라가 부드럽게 추적 |

---

## 3. 유닛 스탯

유닛 기본 스탯은 5개 원시 값으로 구성됩니다 (`UnitDefaultStats`).

| 스탯 | 키 | 설명 |
|---|---|---|
| 민첩 | `agility` | 행동 순서(이니셔티브), 회피율 |
| 영력 | `spirit` | 스킬 데미지 및 회복량 |
| 방어 | `guard` | 최대 체력 및 피해 감소 |
| 운 | `luck` | 치명타/회피 확률, 이니셔티브 동점 보정 |
| 이동 | `mov` | 한 턴에 이동 가능한 칸 수 (이동 범위) |

> 데이터 포맷 상세는 [유닛 정의 포맷](../DataFormats/UnitDefinition.md) 참고.

---

## 4. 유닛 배치

### 적 유닛 (`enemySpawns`)
- 맵 로딩 직후 `enemySpawns[].id`로 `ENEMIES.json` 정의를 참조하여 즉시 배치됩니다.

### 아군 유닛 (`allySpawns`)
- `id > 0`이면 시작 시 해당 유닛을 자동 배치합니다.
- `id <= 0`이면 **빈 슬롯**으로, 런타임에 `PlaceAllyAtSlot(slot, allyId)`로 채웁니다.
- 빈 슬롯 자동 채우기 테스트 옵션: `autoFillAllySpawns`.

### 배치 검증 (공통)
- 맵 범위 안 + 이동 가능 타일(`IsWalkable`)일 때만 배치됩니다.
- 이미 점유된 타일에는 배치되지 않습니다(`IsTileOccupied`).

---

## 5. 행동 순서 (틱 게이지 모델)

### 이니셔티브 계산식
```
Initiative = agility × 10 + luck
actionDelay = TickScale / agility          (TickScale = 100000)
nextActionTime = actionDelay               (첫 행동 예정 시각)
```

### 차례 결정 규칙
1. `nextActionTime`이 가장 작은 유닛이 다음 차례입니다.
2. 동시각이면 `Initiative`가 높은 쪽 우선입니다.
3. 그래도 같으면 **등록 순서(registrationOrder)** 가 빠른 쪽 우선입니다.

### 턴 진행
- `GetCurrentTurnUnit()` — 현재 턴 유닛 반환.
- `AdvanceTurn()` — 현재 유닛의 `nextActionTime += actionDelay`로 재예약 후 다음 유닛으로 전환.
- `PeekTurnOrder(count)` — 실제 상태를 바꾸지 않고 앞으로의 행동 순서를 시뮬레이션합니다 (GUI 표시용).

> 민첩이 높은 유닛은 동일 구간에서 여러 번 차례가 돌아옵니다. (민첩 8:6:2 → 행동 횟수 4:3:1)

---

## 6. 이동 시스템

### 6.1 이동 범위 규칙

모든 이동(키보드/마우스 공통)은 아래 세 가지 조건을 통과해야 합니다.

| # | 조건 | 관련 메서드 |
|---|---|---|
| 1 | 턴 시작 위치(원점)에서 **맨해튼 거리 `mov` 이내** | `ManhattanDistance` |
| 2 | 이동 가능 타일 (`TileInfo.isWalkable`, `ObjectInfo.isBlocking` 반영) | `IsWalkable` |
| 3 | 다른 유닛이 없는 칸 | `IsTileOccupied` |

```
ManhattanDistance(a, b) = |a.x - b.x| + |a.y - b.y|
허용 조건: ManhattanDistance(목표, 원점) ≤ mov
```

- 이동력은 **누적 차감되지 않습니다.** 원점 기준 `mov` 반경(다이아몬드 형태) 안에서는 앞뒤로 자유롭게 이동할 수 있습니다.
- 턴이 시작/전환될 때 유닛의 현재 위치를 **원점**(`currentTurnOrigin`)으로 저장합니다.
- `GetCurrentTurnMovesRemaining()` = `mov - ManhattanDistance(현재 위치, 원점)` (GUI 표시용).

### 6.2 키보드 이동

| 키 | 동작 |
|---|---|
| `↑` / `W` | 위(+Y) 한 칸 |
| `↓` / `S` | 아래(−Y) 한 칸 |
| `←` / `A` | 왼쪽(−X) 한 칸 |
| `→` / `D` | 오른쪽(+X) 한 칸 |

- `TryMoveCurrentUnit(direction)` 호출 → 이동 범위 검증 → `MoveUnit` → `UnitMover.MoveTo` (보간 이동).

### 6.3 마우스 클릭 이동

1. 좌클릭 시 카메라에서 레이캐스트 (`Physics.Raycast` 우선, 실패 시 `y=0` 평면 폴백).
2. 클릭한 월드 좌표를 `WorldToGrid`로 그리드 좌표로 변환합니다.
3. **BFS(`ComputeReachable`)** 로 도달 가능한 칸인지 판정합니다.
4. 가능하면 원점 → 목표까지의 최단 경로를 복원해 칸 단위로 `MoveUnit`을 연속 호출합니다.
5. `UnitMover` 큐가 경로를 순서대로 부드럽게 이어 처리합니다.

> 키보드 이동(1칸씩)과 마우스 이동(경로 BFS) 모두 동일한 이동 범위 규칙을 사용합니다.

---

## 7. 이동 범위 강조 표시

턴이 시작되거나 유닛/위치가 바뀔 때마다 이동 가능한 칸을 자동으로 강조합니다.

| 상태 | 타일 색 처리 |
|---|---|
| 이동 선택 중 | 도달 가능 칸: 원래 색에 **초록(reachableTint)** 을 `reachableTintStrength` 비율로 섞어 표시 |
| 목적지 클릭 / 이동 시작 | 강조 즉시 제거, 별도 표시 없음 |
| 이동 완료 후 (행동 메뉴 열림) | 강조 없음 |
| 취소 → 원위치 복귀 | 강조 다시 표시 |
| 턴 전환 | 다음 유닛 기준으로 강조 재계산 |

**구현 방식:** 타일 `Renderer.material.color`를 직접 수정하고 원래 색을 사전에 보관(`highlightedTiles`)했다가 복원합니다. 별도 머티리얼/셰이더 불필요.

**인스펙터 옵션**

| 필드 | 기본값 | 설명 |
|---|---|---|
| `Reachable Tint` | `(0.3, 1.0, 0.45)` | 강조 색상 |
| `Reachable Tint Strength` | `0.55` | 강조 색 혼합 강도 (0=원색, 1=강조색) |

---

## 8. 플레이어 턴 진행 흐름

```
① 턴 시작
    → 현재 유닛의 이동 가능 영역(초록) 표시

② 이동 선택
    [키보드] 방향키/WASD 한 칸씩 이동
    [마우스] 강조된 칸 클릭
        → 이동 가능 영역(초록) 즉시 제거
        → 유닛이 목적지까지 보간 이동 시작
        → 이동 중 모든 입력 잠금

③ 도착 확인
    → UnitMover.IsMoving == false 감지
    → 행동 메뉴 표시

    [제자리(현재 칸) 클릭]
    → 이동 없이 즉시 행동 메뉴 표시 (휴식 포함)

④ 행동 메뉴 선택
    ┌─ 이동 후  ─────────────────────────┐
    │  공격 / 스킬 / 대기 / 취소         │
    └────────────────────────────────────┘
    ┌─ 제자리  ──────────────────────────┐
    │  공격 / 스킬 / 대기 / 휴식 / 취소  │
    └────────────────────────────────────┘

    [취소 선택]
    → 유닛을 턴 시작 위치(원점)로 복귀
    → 이동 가능 영역(초록) 다시 표시
    → ②로 돌아감

⑤ 행동 확정 (공격/스킬/대기/휴식)
    → 다음 턴으로 전환 (AdvanceTurn)
    → ①로 돌아감
```

### 행동 메뉴 위치
- 목적지 칸의 화면 좌표(WorldToScreenPoint) 우측 옆에 표시합니다.
- 화면 밖으로 나가면 우측 상단으로 폴백합니다.

### 행동 메뉴 인스펙터 옵션

| 필드 | 기본값 | 설명 |
|---|---|---|
| `Action Menu Width` | `150px` | 행동 메뉴 패널 너비 |

---

## 9. 행동 순서 패널 (GUI)

현재 턴을 포함해 앞으로의 예상 행동 순서를 화면에 표시합니다.

- 첫 번째(현재 차례) 항목: `▶` 마커 + **노란색 Bold**.
- 아군: 하늘색, 적군: 분홍색.
- 각 항목에 `이동 X/MOV` 표시 (현재 유닛만 남은 이동 거리 반영).

| 인스펙터 필드 | 기본값 | 설명 |
|---|---|---|
| `Turn Order Panel Position` | `(10, 300)` | 패널 좌상단 위치 |
| `Turn Order Panel Width` | `260px` | 패널 너비 |
| `Turn Order Preview Count` | `12` | 표시할 예상 행동 수 |

---

## 10. BattleScript 주요 API 요약

| 분류 | 메서드 | 설명 |
|---|---|---|
| 유닛 조회 | `GetCurrentTurnUnit()` | 현재 턴 유닛 반환 |
| 유닛 조회 | `GetAllyDefinition(id)` | 아군 유닛 정의 반환 |
| 유닛 조회 | `GetEnemyDefinition(id)` | 적군 유닛 정의 반환 |
| 유닛 배치 | `PlaceAllyAtSlot(slot, allyId)` | 빈 슬롯에 아군 배치 |
| 행동 순서 | `AdvanceTurn()` | 다음 턴으로 전환 |
| 행동 순서 | `PeekTurnOrder(count)` | 앞으로의 행동 순서 시뮬레이션 |
| 행동 순서 | `GetCurrentTurnMovesRemaining()` | 남은 이동력 |
| 행동 순서 | `GetCurrentTurnOrigin()` | 턴 시작 위치(취소 복귀 지점) |
| 이동 | `TryMoveCurrentUnit(direction)` | 키보드 한 칸 이동 |
| 이동 | `MoveCurrentUnitTo(target)` | 마우스 클릭 목적지 이동 (BFS 경로) |
| 이동 | `MoveUnit(unit, target)` | 유닛을 단일 칸으로 이동 |
| 이동 범위 | `GetReachableTilesForCurrentUnit()` | 도달 가능 칸 목록 반환 |
| 맵 유틸 | `GridToWorld(x, z)` | 그리드 → 월드 좌표 변환 |
| 맵 유틸 | `WorldToGrid(worldPos)` | 월드 → 그리드 좌표 변환 |
| 맵 유틸 | `IsWalkable(x, z)` | 이동 가능 여부 |
| 맵 유틸 | `IsTileOccupied(tile)` | 유닛 점유 여부 |
| 맵 유틸 | `GetFloorCube(x, z)` | 바닥 타일 GameObject 반환 |

---

## 11. 현재 한계 및 향후 확장

| 항목 | 현황 | 향후 |
|---|---|---|
| 이동 거리 계산 | 맨해튼 거리 (장애물 우회 경로 길이 미반영) | BFS 이동 비용 누적으로 교체 |
| 지형 이동 비용 | `movementCost` 필드 존재하나 이동 범위 계산에 미반영 | BFS 비용 합산으로 반영 |
| 행동 처리 | 공격/스킬/대기/휴식 선택 시 로그 출력 후 턴만 넘김 | 타겟팅·데미지·스킬 시스템 연동 (`PerformAction`) |
| 적 AI | 없음 (수동 턴 전환) | AI 행동 로직 추가 |
| 카메라 | 현재 턴 유닛 추적 | — |

---

**관련 문서:**
- [맵 시스템 스펙](MapSystem.md)
- [유닛 정의 포맷](../DataFormats/UnitDefinition.md)
- [맵 데이터 포맷](../DataFormats/MapDataFormat.md)
- [BattleScript API](../API/BattleScript.md)
