# 04 · 씬 / 데이터 에셋 / 빌드 설정

## 1. 씬 4종 — **완성된 "본 게임" 씬은 없음**

`.unity` YAML의 `m_Script` GUID를 스크립트 `.meta`와 대조한 결과:

| 씬 | 포함 컴포넌트 | 성격 |
|----|--------------|------|
| **MainScene.unity** | `Main Camera`, `Directional Light`, `Global Volume` **만** (게임 스크립트 0) | **빈 플레이스홀더.** 빌드에 등록된 **유일한** 씬인데 로직이 없음 → 지금 빌드하면 **빈 씬으로 실행됨** |
| **TestScene_KD.unity** (79KB) | `BattleScript`(KO) + `BattleMapProvider` + `GridManager`(KD) + `TacticalBattleManager`(KD) + `KDBattleTurnController`(KD) | **통합 샌드박스** — KD 전투가 KO 맵 위에서 도는 유일한 씬. 가장 플레이에 가까움(단 SW UI 없음) |
| **TestScene_KO.unity** | `BattleScript` + **미해결(missing) 턴 컨트롤러**(`guid 1027859ff…`, 매칭 스크립트 없음 — 옛 `BattleTurnController.cs` 삭제됨) + `BattleCameraFollow` | KO 맵/이동 단독 샌드박스. **댕글링 missing-script 참조 존재** |
| **TestScene_SW.unity** (184KB) | `BattleScript` + `InitiativeManager`(SW) + 대형 UGUI 캔버스. `CharacterPanelUI`는 씬에 없고 `Assets/99.TestPrefab/Panel_CharacterInfo.prefab`에서 런타임 인스턴스화 | SW UI 단독 샌드박스. **KD 전투 없음** |

> **씬 판정**: 세 서브시스템을 모두 합친 **단일 플레이 씬 부재**. 빌드 씬(MainScene)은 비었고, KD+KO만 `TestScene_KD`에서 결합. SW UI는 실제 전투와 한 씬에서 합쳐진 적 없음.

## 2. ScriptableObject 데이터 — **매우 빈약(플레이스홀더 수준)**

`Assets/2.ScriptableObject/`:

- **Units/** — 플레이어 3 + 적 1, 전부 `KD.UnitData`:
  - `UnitData_1` = "현도윤"(role 1, baseStats **전부 1**, moveAPCost 30) — 개발/테스트 스텁, 밸런스 안 됨
  - `UnitData_2`, `UnitData_3` (동일 템플릿)
  - `EnemyData_1` = "혼수상태"(role 0, attribute 1) — 역시 전부 1 스탯
- **Skills/** — 스킬 **딱 1개**: `SkillData_Slash`("휘두르기", skillId 1001, 1타일, AP 30, 쿨다운 1). `SkillDatabase.asset`의 `optionalSkills`에 이 하나만 등록
- **Grid/** — `GridPattern_1`(`KD.GridPatternData`): 단일 레이, 거리 1~2, patternId/name 미설정
- **Enemy/** — `EnemyPatternData_1`(1스텝: (3,3)에 Slash 시전), `DeploymentRuleData_1`(배치 후보 20타일 x=0~1, maxDeployCount 4, 금지 패턴 SO 참조)

> **콘텐츠 완성도: 최소.** 코드 경로 검증용일 뿐 실게임 분량 아님. 스탯은 균일하게 1, 이름은 농담조("혼수상태"), 스킬은 1개뿐.

## 3. 빌드 / 프로젝트 설정

- **Unity 6000.5.0f1** (Unity 6) — `ProjectSettings/ProjectVersion.txt`
- **렌더 파이프라인**: URP 17.5.0. MainScene에 Global Volume + URP 카메라/라이트 데이터 존재
- **입력**: New Input System 1.19.0 + `Assets/InputSystem_Actions.inputactions`(41KB)
- **기타 패키지**: AI Navigation 2.0.13, Timeline, Visual Scripting, VFX Graph, Test Framework 1.7.0, Multiplayer Center(미사용), TextMesh Pro
- **EditorBuildSettings.asset**: **`Assets/0.Scenes/MainScene.unity` 하나만** 등록(enabled). MainScene이 비어 있으므로 → **현재 빌드 = 빈 씬 실행**. 실제 게임플레이 테스트 씬은 빌드 목록에 없음

## 4. Git / 브랜치 상태

- 현재 체크아웃: **`features/battle-map`**
- 원격 브랜치: 기능별 분기 `features/{Animation, FX, Shader, UI, battle, battle-map}` + 스냅샷 `battle@26062013`
- 최근 작업 초점: **KD 전투 + KO 맵 통합**(GridManager/UnitMover 이동·회전, KDBattleTurnController 액션 메뉴+AP 게이팅, 바닥 셰이더, 부적 프리팹, TestScene_KD 적 유닛 설정)
- **`SkillExcuter&StatCalculator` revert**(`24c8fc9` ← `39a1a2d`): 스킬 실행+스탯 계산 기능을 커밋했다가 즉시 롤백. `SkillExecutor.cs`/`StatCalculator.cs` 파일은 트리에 잔존 → **연결만 풀린 미정착 상태**
- master ↔ features/battle-map 잦은 교차 머지(전형적 게임잼 churn)

## 5. 기존 문서(`Docs/`) — **옛 KO 아키텍처 기준, KD 방향과 괴리**

`Docs/`(README v1.3.0, 2026-06-20 갱신): `Systems/BattleSystem.md`, `Systems/MapSystem.md`, `API/BattleScript.md`, `DataFormats/{Map,Unit,Tile,Object,Skill}*.md`, `Guides/MapCreation.md`.
README는 존재하지 않는 파일들도 링크(TileSystem.md, ObjectSystem.md, Performance.md, Examples/* — **깨진 링크**).

- 문서는 **KO의 JSON 시스템**을 정본으로 서술: 스탯 agility/spirit/guard/luck/mov, `ALLYS/ENEMIES.json`, 런타임 `BattleUnitEntry`, 이니셔티브 = `agility×10+luck`, `BattleScript`가 "코어 클래스"
- 그러나 **현 방향은 KD의 SO 시스템**: `KD.UnitData/SkillData` 에셋, `BattleUnit/TacticalBattleManager/StatCalculator`, AP 코스트(moveAPCost/apCost), role/attribute/weaponType
- 문서는 **TacticalBattleManager, GridManager, SO 에셋, AP, 배치 시스템을 전혀 언급하지 않음**

> **결론**: 문서는 KO 맵 레이어엔 정확하나 **전투 레이어(KD)엔 낡고 오해 유발.** 단일 시스템이던 초기 설계를 반영, 현 이원 구조를 반영 못함.

## 6. 정리

| 영역 | 상태 |
|------|------|
| 씬 | 통합 플레이 씬 없음 / 빌드 씬은 빈 MainScene / TestScene_KO에 missing-script |
| SO 데이터 | 플레이스홀더(전부 1스탯, 스킬 1개) |
| 빌드 설정 | Unity6 + URP 정상, 단 빌드 씬 부적절 |
| 문서 | KO엔 정확, KD엔 낡음 + 깨진 링크 |
