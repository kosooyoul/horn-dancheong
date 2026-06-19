# 전투 시스템 스펙 문서

## 📋 개요

Horn Dancheong의 전투 시스템은 2D 그리드 맵 위에서 유닛이 **행동 순서(이니셔티브)** 에 따라 차례로 행동하는 턴제 전술 전투입니다. 본 문서는 유닛 정의, 배치, 행동 순서, 이동(이동 거리 제한)까지의 사양을 정의합니다.

관련 구현: `BattleScript.cs`, `BattleTurnController.cs`

## 🏗️ 시스템 구성

### 핵심 컴포넌트
1. **BattleScript** - 맵/유닛 로딩·배치, 행동 순서, 이동 처리의 메인 클래스
2. **BattleTurnController** - 키 입력(방향키/WASD, Enter)으로 현재 턴 유닛을 조작하는 테스트용 컨트롤러 및 GUI
3. **ALLYS.json / ENEMIES.json** - 유닛 종류 및 기본 스탯 정의
4. **맵 JSON (`allySpawns` / `enemySpawns`)** - 유닛 스폰 위치 정의

### 데이터 흐름
```
맵 JSON(allySpawns/enemySpawns) ─┐
ALLYS.json / ENEMIES.json ───────┴→ BattleScript → 유닛 배치 → 행동 순서 계산 → 턴 진행/이동
```

## 🧬 유닛 스탯

유닛 기본 스탯은 5개 원시 값으로 구성됩니다 (`UnitDefaultStats`).

| 스탯 | 키 | 설명 |
|------|------|------|
| 민첩 | `agility` | 행동 순서(이니셔티브)와 회피율 |
| 영력 | `spirit` | 스킬 데미지 및 회복량 |
| 방어 | `guard` | 최대 체력 및 피해 감소 |
| 운 | `luck` | 치명타/회피 확률, 이니셔티브 동점 보정 |
| 이동 | `mov` | 한 턴에 이동 가능한 칸 수 (이동 범위) |

> 데이터 포맷 상세는 [유닛 정의 포맷](../DataFormats/UnitDefinition.md) 참고.

## 🪖 유닛 배치

### 적 유닛 (`enemySpawns`)
- 맵 로딩 직후 `enemySpawns[].id`로 `ENEMIES.json` 정의를 참조하여 즉시 배치됩니다.

### 아군 유닛 (`allySpawns`)
- `allySpawns[]`는 아군이 배치될 수 있는 슬롯 목록입니다.
- `id > 0`이면 시작 시 해당 유닛을 자동 배치합니다.
- `id <= 0`이면 **빈 슬롯**으로, 런타임에 `PlaceAllyAtSlot(slot, allyId)`로 채웁니다.
- 빈 슬롯 자동 채우기(테스트용) 옵션: `autoFillAllySpawns`.

### 배치 검증
- 맵 범위 안 + 이동 가능 타일(`IsWalkable`)일 때만 배치됩니다.
- 이미 점유된 타일에는 배치되지 않습니다(`IsTileOccupied`).

## 🔄 행동 순서 (이니셔티브)

### 계산식
```
Initiative = agility × 10 + luck
```

### 정렬 규칙
- 이니셔티브 **내림차순** 정렬.
- 동점이면 운(luck)이 이미 반영되어 있으며, 그래도 같으면 **등록 순서 유지**(안정 정렬).

### 턴 진행
- `GetCurrentTurnUnit()` — 현재 턴 유닛.
- `AdvanceTurn()` — 다음 턴 유닛으로 라운드 순환.
- 유닛이 추가 합류(`PlaceAllyAtSlot`)하면 행동 순서를 재계산합니다.

## 🦶 이동 시스템

### 입력 (테스트 컨트롤러)
- 방향키 / WASD: 현재 턴 유닛을 상하좌우 한 칸씩 이동.
- Enter: 다음 턴으로 전환.

### 이동 거리 제한 (mov)
이동 거리는 **턴 시작 위치(원점)** 기준으로 제한됩니다.

- 턴이 시작/전환될 때 현재 유닛의 위치를 **원점**(`currentTurnOrigin`)으로 저장합니다.
- 이동 시도 시, **목표 칸이 원점에서 맨해튼 거리 `mov` 이내**인 경우에만 이동을 허용합니다.
  ```
  ManhattanDistance(target, origin) ≤ mov
  ManhattanDistance(a, b) = |a.x - b.x| + |a.y - b.y|
  ```
- 이동력은 **누적 차감되지 않습니다.** 원점 기준 mov 반경(다이아몬드 형태) 안에서는 앞뒤로 자유롭게 이동할 수 있고, 원점 가까이 돌아오면 그만큼 다시 멀리 이동할 수 있습니다.
- `GetCurrentTurnMovesRemaining()` = `mov - ManhattanDistance(현재 위치, 원점)` (GUI 표시용).

### 칸 단위 이동 검증
원점 기준 거리 조건을 통과해도, 각 한 칸 이동은 다음을 추가로 검증합니다 (`MoveUnit`).
- 맵 범위 안일 것.
- 이동 가능 타일일 것 (`IsWalkable` — 타일 `isWalkable`, 오브젝트 `isBlocking` 반영).
- 다른 유닛이 점유하지 않은 칸일 것 (`IsTileOccupied`).

> **현재 한계**: 거리 판정은 맨해튼 거리로 계산하므로, 장애물을 우회해야 하는 실제 경로 길이와 다를 수 있습니다. 또한 타일별 `movementCost`(어려운 지형)는 이동 거리 계산에 반영되지 않습니다. 정확한 경로 기반 제한이 필요하면 BFS 기반 도달 가능 칸 계산으로 확장해야 합니다.

## 🧩 주요 API 요약

| 분류 | 메서드 |
|------|--------|
| 유닛 조회 | `GetAllyDefinition`, `GetEnemyDefinition`, `GetEnemyUnits`, `GetAllyUnits`, `GetAllySpawnSlots` |
| 유닛 배치 | `PlaceAllyAtSlot`, `IsTileOccupied` |
| 행동 순서 | `GetTurnOrder`, `GetCurrentTurnUnit`, `AdvanceTurn` |
| 이동 | `TryMoveCurrentUnit`, `MoveUnit`, `GetCurrentTurnMovesRemaining` |

> 메서드 시그니처/반환값 상세는 [BattleScript API](../API/BattleScript.md) 참고.

## 🔄 향후 확장

- 타일 `movementCost` 기반 이동력 소모.
- 장애물 우회를 고려한 BFS 도달 범위 및 경로 미리보기(하이라이트).
- 공격/스킬 사용 등 이동 외 행동 통합 (KD 전투 모듈 `BattleUnit`, `StatCalculator` 연동).
- AI 적 턴 처리.

---
**관련 문서:**
- [맵 시스템 스펙](MapSystem.md)
- [유닛 정의 포맷](../DataFormats/UnitDefinition.md)
- [맵 데이터 포맷](../DataFormats/MapDataFormat.md)
- [BattleScript API](../API/BattleScript.md)
