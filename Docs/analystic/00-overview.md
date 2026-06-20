# 00 · 프로젝트 전체 개요 & 통합 상태 판정

## 1. 프로젝트 정체성

- **주제**: "혼(魂)" — 게임잼/젬잼 출품작
- **장르**: 택틱컬 턴제 시뮬레이션 (그리드 기반 전술 전투)
- **엔진**: Unity **6000.5.0f1** (Unity 6), **URP 17.5.0**, New Input System 1.19.0
- **현재 브랜치**: `features/battle-map` (master와 잦은 교차 머지)

## 2. 팀 / 코드 소유 구조

코드는 `Assets/1.Scripts/` 아래 개발자 이니셜로 깔끔히 분리되어 있습니다.

| 영역 | 담당 범위 | 네임스페이스 | 규모 |
|------|-----------|--------------|------|
| **KD** | 전투 코어 + SO 데이터 모델 (Combat/Grid/SO/Core/UI/Test) | `KD.*` | 가장 큼 (~42 스크립트, ~4,800 LOC) |
| **KO** | 맵/전장 로딩 + 유닛 이동 + 카메라 (JSON 맵 시스템) | 전역(네임스페이스 없음) | 5 스크립트 (`BattleScript` 1105줄 포함) |
| **SW** | 전투 UI (이니셔티브 바, 캐릭터 패널, 어댑터) | `HornDancheong.Seongwoo.UI` | 9 스크립트, ~1,073 LOC |
| (Art) | 바닥 큐브 셰이더/비주얼 | `Assets/Art/Scripts/` | 4 스크립트 |

## 3. 아키텍처 한눈에 보기

```
                ┌─────────────────────────────────────────────┐
                │  KD.TacticalBattleManager  (전투 오케스트레이터)  │
                │   배치 → 플레이어 페이즈 → 적 페이즈 → 종료        │
                └───────────────┬─────────────────────────────┘
                                │
          ┌─────────────────────┼──────────────────────┐
          ▼                     ▼                       ▼
   KD.GridManager        KD.TurnOrderManager      KD.EnemyIntentController
   (그리드 상태/하이라이트)   (이니셔티브 정렬)         (적 의도/실행)
          │
          │ mapProvider (어댑터)
          ▼
   KO.BattleMapProvider ──▶ KO.BattleScript (맵 전용 모드, mapOnlyMode=true)
                              (JSON 맵 로드, 타일/장애물, 좌표 변환, 이동)

   SW.InitiativeManager ◀── SW.BattleUnitAdapter ◀── KD.BattleUnit
   (UI, 코드상으로만 연결 — 씬 결합 없음)
```

**핵심**: 두 개의 전투/데이터 모델이 공존합니다.
- KO의 **JSON 기반** 모델 (`ALLYS/ENEMIES.json`, 스탯 = agility/spirit/guard/luck/mov, `BattleUnitEntry`)
- KD의 **ScriptableObject 기반** 모델 (`UnitData`/`SkillData` 에셋, role/attribute/weaponType, AP 코스트)

→ `TestScene_KD`에서는 KO의 `BattleScript`가 **맵 제공자로 강등**되고 KD가 실제 전투를 담당합니다.
JSON 유닛 데이터와 SO 유닛 데이터는 **아직 일원화되지 않았습니다.**

## 4. 통합 상태 종합 판정 ⚖️

> **결론: 부분 통합. 대부분은 "개발자별 샌드박스 + 절반쯤 만든 KD+KO 통합 씬" 상태.**

| 연결 | 상태 | 근거 |
|------|------|------|
| **KD ↔ KO** | ✅ **실제 연결됨** | `BattleMapProvider` 어댑터로 `BattleScript` 맵 함수를 `GridManager`에 노출. `TestScene_KD`에 BattleScript + BattleMapProvider + GridManager + TacticalBattleManager + KDBattleTurnController가 모두 함께 배치됨 |
| **SW ↔ KD** | ⚠️ **코드만 연결, 씬 결합 없음** | `BattleUnitAdapter`가 `KD.BattleUnit`을 `ICharacterBattleInfo`로 감쌈. 그러나 `TestScene_SW`엔 KD 전투 컴포넌트가 없고, `TestScene_KD`엔 SW UI가 없음 → **두 시스템이 한 씬에서 같이 인스턴스화된 적 없음** |
| **빌드 가능한 통합 씬** | ❌ **없음** | 빌드 목록엔 `MainScene`만 등록되어 있는데 **MainScene은 카메라/라이트/볼륨만 있는 빈 씬** |

### 씬별 역할 (자세한 내용은 [04-assets-scenes.md](./04-assets-scenes.md))

| 씬 | 내용 | 성격 |
|----|------|------|
| `MainScene` | 게임 로직 0개 (URP 빈 템플릿) | **빌드 등록된 유일한 씬인데 비어 있음** |
| `TestScene_KD` | BattleScript+Provider+GridManager+TacticalBattleManager+TurnController | **가장 플레이에 가까운 통합 샌드박스** (단, SW UI 없음) |
| `TestScene_KO` | BattleScript + 카메라 + **깨진(missing) 턴 컨트롤러 참조** | KO 맵/이동 단독 샌드박스 |
| `TestScene_SW` | BattleScript + InitiativeManager + UGUI 캔버스 | SW UI 단독 샌드박스 (KD 전투 없음) |

## 5. 가장 시급한 3가지 (상세: [05](./05-diagnosis-and-roadmap.md))

1. **통합 플레이 씬 부재** — KD+KO+SW를 한 씬에 합치고 그것을 빌드 씬으로 등록해야 함.
2. **스킬 실행 로직 미정착** — `SkillExcuter&StatCalculator` 커밋이 revert(`24c8fc9`)되어 어정쩡한 상태. 파일은 남았으나 연결이 풀림.
3. **데이터 모델 이원화** — KO JSON vs KD SO. 어느 쪽을 정본으로 할지 결정 필요(현 방향은 KD SO).
