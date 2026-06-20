# 01 · KD 서브시스템 — 전투 코어

> 위치: `Assets/1.Scripts/KD/` (Combat / Grid / Core / SO / UI / Test)
> 규모: ~42 스크립트, 약 4,800 LOC — **프로젝트에서 가장 크고 핵심적인 부분**

## 1. 역할 & 아키텍처

KD는 턴제 택틱컬 전투의 **풀 프레임워크**입니다.

- **유닛 시스템**: `UnitBaseStats`(4스탯) → `StatCalculator` → `UnitDerivedStats`(9 파생 스탯) → 런타임 `BattleUnit`
- **턴 순서 & 페이즈**: `TurnOrderManager`(이니셔티브 정렬), `BattlePhase`(배치 → 플레이어 → 적 → 종료)
- **이동**: `MovementRangeCalculator` — 6종 이동 타입(BFS + 기사도약/순간이동/돌진)
- **스킬**: `SkillExecutor`(데미지/힐 공식), `GridPatternResolver`(범위 패턴), `AttributeCalculator`(5색 상성)
- **그리드/타겟팅**: `GridManager`, `CombatGridQuery`, `SkillRangePreview`
- **배치 페이즈**: `DeploymentController`(후보 타일, 금지 구역, 다중 배치)
- **적 AI**: `EnemyIntentController` / `EnemyIntent`(의도 경고 → 실행)
- **입력**: `KDBattleTurnController`(키보드/마우스, 액션 메뉴)

```
TacticalBattleManager  (메인)
├─ BattleSetup            OwnedUnit/UnitData → BattleUnit 생성
├─ DeploymentController   배치 페이즈
├─ GridManager           그리드 상태/배치/이동/하이라이트
├─ TurnOrderManager      이니셔티브 턴 큐
├─ CombatGridQuery       이동/스킬 범위 질의
├─ SkillRangePreview     호버 프리뷰
├─ EnemyIntentController[]적별 의도/실행
└─ KDBattleTurnController 입력 라우팅
```

## 2. 구현 / 미구현 요약

### ✅ 완성되어 동작하는 것
- **유닛 스탯**: 4 기본 → 9 파생(maxHP, initiative, moveRange, attack, heal, defense, crit%, evasion%)
- **전투 루프**: 페이즈 흐름, 이니셔티브 턴 정렬
- **스킬 실행**: 데미지 공식 6배수(크리/방어/회피/속성/스케일) + AP/쿨다운
- **이동**: 6종(Cardinal, 8Dir, DiagonalOnly, KnightJump, Teleport, Charge), 장애물 인식
- **스킬 범위 패턴**: 고정 셀 + 레이 확장 + 방향 스냅
- **적 의도 시스템**: 위험 타일 하이라이트(DangerS/M/L/XL) + 표시 타일 실행
- **배치 페이즈**: 후보 타일/금지 구역/다중 배치 로직
- **입력**: 양 페이즈 키보드 단축키(1/M=이동, 2/K=스킬, W/Space=대기, Esc=취소, Enter=확정)
- **그리드 질의**: 이동/스킬 범위, 벽 vs 유닛 블로킹, 스킬 레이 통과

### ⚠️ 미구현 / 스텁 / TODO

| 항목 | 위치 | 상태 | 비고 |
|------|------|------|------|
| **버프/디버프 효과** | `SkillExecutor.cs:90-96` | 스텁 | `Debug.Log("미구현")` — Damage/Heal만 작동 |
| **적 AI(Simple)** | `SimpleBattleManager.cs:147-150` | 스텁 | `// TODO: 적 AI 구현` → 즉시 플레이어 페이즈로 복귀 |
| **배치 프리뷰 비주얼** | `GridManager` (TODO) | 스텁 | `PlaceDeploymentPreview()` / `ClearDeploymentPreview()` 본문 없음 → 텍스트 상태만 표시 |
| **애니메이션 훅** | `TacticalBattleManager.cs:14-15` | TODO | 애니메이터 연동 지점만 주석으로 표시, 액션이 즉시 스냅됨 |
| **적 턴 연출 대기** | `TacticalBattleManager.cs:235-237` | TODO | 애니메이션 후 `ExecuteEnemyTurn()` 호출 기대하나 현재 즉시 실행 |

## 3. 파일 인벤토리 (요약)

대부분 **Done**. 상태가 갈리는 것만 추림:

- **Combat/** — `TacticalBattleManager`(Done, 470), `SimpleBattleManager`(Done, 346 — *중복 위험*), `KDBattleTurnController`(Done, 448), `BattleUnit`/`OwnedUnit`/`UnitDerivedStats`/`StatCalculator`/`BattleActionConfig`(Done), `SkillExecutor`(Done이나 *revert 영향*), `SkillEquipValidator`/`AttributeCalculator`/`MoveOption`/`MovementRangeCalculator`(Done), `DeploymentController`/`DeploymentPlacement`(Done), `EnemyIntent`/`EnemyIntentController`(Done)
- **Grid/** — `GridManager`(Done, 500+), `CombatGridQuery`/`GridPatternResolver`/`SkillRangePreview`/`GridTile`/`DirectionHelper`(Done)
- **Core/** — `GameEnums`/`BattlePhase`/`PlayerRosterManager`(Done)
- **SO/** — `UnitData`/`UnitBaseStats`/`SkillData`/`SkillDatabase`/`GridPatternData`/`EnemyPatternData`/`DeploymentRuleData`(Done)
- **Test/** — `CombatSmokeTest`/`RosterSmokeTest`(Done) — *단, revert로 5개 테스트 삭제됨(아래 참조)*
- **UI/** — `DeploymentConfirmButtonUI`(Done), `DeploymentRosterPanelUI`/`DeploymentUnitSlotUI`/`DeploymentSelectedUnitUI`/`DeploymentStatusUI`/`PreBattleUI`(**Partial** — 비주얼 미완)

## 4. 버그 / 리스크 / 중복

1. **🔴 이중 배틀 매니저 (중복)** — `TacticalBattleManager`(배치 포함 풀버전)와 `SimpleBattleManager`(배치 없는 MVP)가 **둘 다 동작하지만 호환되지 않음**. 씬당 하나만 활성화돼야 하는데 런타임 선택/문서가 없음. → 사용 안 하는 쪽 `[Obsolete]` 처리 또는 제거 권장.

2. **🔴 SkillExecutor & StatCalculator revert 상태** — 커밋 `24c8fc9`가 `39a1a2d`("SkillExcuter&StatCalculator")를 revert. **파일은 남아 있으나 연결(wiring)이 풀린 미정착 상태.** 이때 테스트 5개(BattleFlowSmokeTest, DeploymentSmokeTest, EnemyIntentSmokeTest, StatCalculatorSmokeTest, TurnOrderSmokeTest, 총 ~1,469 LOC)가 함께 삭제되어 **테스트 커버리지 손실**.

3. **🟡 배치 프리뷰 미구현** — 배치 확정 전 시각 피드백 없음(텍스트만).

4. **🟡 버프/디버프 미작동** — `SkillEffectType`에 Buff/Debuff가 정의돼 있으나 효과가 적용되지 않고 경고만 남김.

5. **🟡 적 턴 즉시 실행** — 의도 표시 후 극적 멈춤 없이 바로 실행. 연출 부재.

6. **🟢 GridManager 좌표 폴백** — `BattleMapProvider` 미연결 시 cellSize 기반으로 폴백(맵 오프셋 무시). 단순 그리드는 OK, 비-제로 원점/회전 맵에선 깨질 수 있음.

7. **🟢 AttributeCalculator 엣지케이스** — 새 속성 추가 시 switch 미갱신이면 조용히 false 반환(밸런스 사일런트 버그 가능).

8. **🟢 주석 처리된 액션 메뉴 로직** — `KDBattleTurnController.cs:140-149`에 open/close 로직이 주석 처리됨(코드 스멜, 기능엔 영향 적음).

## 5. 의존성

- **외부 필수**: Unity(MonoBehaviour, ScriptableObject, Input System, Physics raycast)
- **소프트 의존**: `BattleMapProvider`(KO) — 연결 시 맵 질의 위임, 없으면 단순 그리드 수학으로 폴백 → **KO에 대한 하드 의존 없음, 깔끔한 격리**
- **SO 의존 그래프**:
  ```
  UnitData → UnitBaseStats, SkillData[], GridPatternData(이동 타입)
  SkillData → GridPatternData(타겟 패턴)
  DeploymentController → DeploymentRuleData, GridPatternData(금지 패턴)
  EnemyIntentController → EnemyPatternData → SkillData[]
  ```

## 6. 종합 평가

| 항목 | 상태 |
|------|------|
| 아키텍처 | 견고 — 관심사 분리, 정적 유틸 적절히 사용 |
| 전투 루프 / 스탯 / 이동 / 그리드 / 입력 | Done |
| 스킬 | Partial — Damage/Heal 100%, Buff/Debuff 0% |
| 배치 | Partial — 로직 Done, 비주얼 미완 |
| 적 AI | Partial — 의도 Done, 실행 연출 없음 |
| 테스트 | 빈약 — 스모크 2개(revert로 5개 손실) |
| 코드 건강도 | 양호 — 주석/네이밍/에러처리 일관 |
