# 구현 내용 보고서 — Horn Dancheong

> 작성일: **2026-06-23** · 기준 브랜치: `master`

---

## 1. 프로젝트 한 줄 요약

> **3명의 개발자가 역할을 분담하여 JSON 맵 인프라(KO) 위에 ScriptableObject 기반 택틱컬 전투 엔진(KD)을 얹고, 풀 UI/오디오/대화 시스템(SW)으로 완성한 그리드 택틱컬 RPG 게임잼 출품작.**

---

## 2. 전체 구현 지표

| 항목 | 수치 |
|------|------|
| **총 C# 스크립트** | ~70개 |
| **총 코드 라인** | ~7,500 LOC 추정 |
| **ScriptableObject 에셋** | 36개 (유닛 7 + 스킬 16 + 그리드 8 + 적 3 + 배치 2) |
| **맵 데이터 (JSON)** | 6개 전투 맵 (000~005.json, 15×15 이상) |
| **BGM 트랙** | 6개 (Bright×3, Danger×1, Peace×2) |
| **캐릭터 초상화** | 3종 (도윤, 솔하, 유하) |
| **씬** | 4개 (MainScene, TestScene_KD, TestScene_KO, TestScene_SW) |
| **VFX 에셋** | ErbGameArt Fantasy Effects + Hovl Studio Magic Effects + 커스텀 FX |

---

## 3. KD 서브시스템 — 전투 코어

### 3.1 위치 & 규모
`Assets/1.Scripts/KD/` · ~50 스크립트 · ~5,000+ LOC

### 3.2 구현 완료 기능

#### 스탯 파이프라인
```
UnitBaseStats (4 기본)
  ├─ power   → attack, critChance
  ├─ agility → initiative, evasion, moveRange
  ├─ guard   → defense, maxHP
  └─ spirit  → heal, (속성 계수)
       ↓ StatCalculator
UnitDerivedStats (9 파생)
  maxHP / initiative / moveRange / attack / heal
  defense / critChance / evasion / apMax
```

#### 전투 루프 (`TacticalBattleManager`)
- **BattlePhase 흐름**: `Deployment → PlayerPhase → EnemyPhase → End`
- **이니셔티브 정렬**: `TurnOrderManager.BuildTurnOrder()` — 내림차순 정렬 + 동점 처리
- **AP 게이팅**: 각 행동(이동/스킬)별 AP 소비, 0이면 자동 턴 종료

#### 이동 시스템 (`MovementRangeCalculator`)
| 이동 타입 | 설명 |
|-----------|------|
| `Cardinal` | 상하좌우 4방향 BFS |
| `EightDir` | 8방향 BFS |
| `DiagonalOnly` | 대각선만 |
| `KnightJump` | 체스 나이트 도약 |
| `Teleport` | 범위 내 즉시 이동 |
| `Charge` | 직선 돌진 |

#### 스킬 실행 (`SkillExecutor`)
```
데미지 = baseAtk × attributeMult × scaleFactor
       × (1 + critBonus if isCrit)
       × (1 - defenseReduction)
       × (miss = 0 if evaded)

힐 = baseHeal × scaleFactor
```
- **속성 상성** (`AttributeCalculator`): 5색 속성 배율 계산
- **AP/쿨다운**: 스킬별 `apCost`, `cooldownTurns` 관리

#### 그리드 패턴 (`GridPatternResolver`)
| 패턴 에셋 | 형태 |
|-----------|------|
| `GridPattern_1` | 1칸 단일 타겟 |
| `GridPattern_1X2~4` | 1×N 직선 레이 |
| `GridPattern_3` | 3칸 레이 |
| `GridPattern_3X3` | 3×3 정방형 |
| `GridPattern_3x3_Self` | 3×3 (자기 포함) |
| `GridPattern_5X5` | 5×5 광역 |
| `GridPattern_All15x15` | 전체 맵 |
| `GridPattern_Ring_3x3To4x4` | 링 패턴 |

#### 배치 페이즈 (`DeploymentController` + `DeploymentDragController`)
- 드래그&드롭으로 유닛 배치
- `DeploymentRuleData` SO로 후보 타일 / 금지 구역 / 최대 배치 수 정의
- `DeploymentRuleData_1`: 후보 20타일(x=0~1), maxDeployCount=4
- `DeploymentRule_Exclude_Center5x5`: 중앙 5×5 금지

#### 적 AI (`EnemyIntentController`)
- 적 턴 시작 전 **위험 타일 하이라이트** 표시 (DangerS/M/L/XL)
- `EnemyPatternData` SO로 행동 패턴 정의 (스텝별 위치·스킬 지정)
- 의도 표시 → 실행 2단계 구조

#### 입력 (`KDBattleTurnController`)
- 키보드 단축키: `1/M`(이동), `2/K`(스킬), `W/Space`(대기), `Esc`(취소), `Enter`(확정)
- 마우스 우클릭으로 전투 메뉴 취소
- UI 위 클릭 시 그리드 인터랙션 차단 (`bb7bb5b`)

### 3.3 캐릭터 데이터 현황

| 유닛 | 역할 (Role) | 스킬 |
|------|------------|------|
| 탱커 (도윤) | KeumKun (근거리) | Sword1, Sword2, Sword3 |
| 힐러 (솔하) | EuiKwan (지원) | Book1, Book2, Book3, Book4 |
| 딜러 (유하) | JipHaeng (원거리) | Bow1, Bow2, Bow3 |
| 적 일반 | — | — |
| 보스 (혼수상태) | Boss | Boss1, Boss2, Boss3, Boss4 |

### 3.4 미구현 / 제한 사항

| 항목 | 상태 | 비고 |
|------|------|------|
| 버프/디버프 효과 | `Debug.Log("미구현")` | `SkillEffectType`에 정의만 존재 |
| 배치 프리뷰 비주얼 | 스텁 | `PlaceDeploymentPreview()` 본문 없음 |
| 적 턴 연출 대기 | TODO | 현재 즉시 실행 |
| `SimpleBattleManager` | 중복 위험 | `TacticalBattleManager`와 공존, 하나만 사용 |

---

## 4. KO 서브시스템 — 맵 인프라

### 4.1 위치 & 규모
`Assets/1.Scripts/KO/Battle/` · 5 스크립트 · ~1,630 LOC

### 4.2 구현 완료 기능

#### 맵 데이터 (`BattleScript.cs`, 1,100줄)
- **JSON 파싱**: 6개 전투 맵 로드, 실패 시 15×15 폴백
- **32비트 칩 포맷**: `(ObjectID << 8) | TileID` — 타일+장애물 동시 표현
- **좌표계**: 좌상단 원점, X→오른쪽, Z→아래, 행 우선 1D 배열
- **맵 생성**: 바닥 큐브 인스턴스화, 장애물 배치, 계층 정리
- **이동 체크**: `IsWalkable()`, `GetMovementCost()`, 타일+장애물 결합 로직
- **BFS 도달범위**: `ComputeReachable()` — 맨해튼 거리 예산 기반
- **런타임 맵 전환**: `MapTester.cs` (Space/B 키)

#### 어댑터 (`BattleMapProvider.cs`, 55줄)
KO↔KD 경계를 4개 API로 최소화:
```csharp
bool IsValidTile(int x, int z)
bool IsWalkable(int x, int z)
Vector3 GridToWorld(int x, int z)
Vector2Int WorldToGrid(Vector3 worldPos)
```

#### 이동 보간 (`UnitMover.cs`, 208줄)
- 8방향 부드러운 이동 보간 (`6c6aba1`)
- 이동 큐 + 자동 Y축 회전
- 이동 완료 콜백

#### 카메라 (`BattleCameraFollow.cs`, 94줄)
- 45° 직교 아이소메트릭 투영
- 스무스 댐핑 추적
- 전투 유닛 포커스 전환

#### 맵 데이터 파일

| 파일 | 설명 |
|------|------|
| `000~005.json` | 전투 맵 레이아웃 (15~21×21 그리드) |
| `TILES.json` | 타일 속성 정의 (보행 가능, 이동 비용, 색) |
| `OBJECTS.json` | 장애물 정의 (블로킹, 높이, 프리팹) |
| `ALLYS.json` | 아군 스탯 (agility/spirit/guard/luck/mov) |
| `ENEMIES.json` | 적군 스탯 |

### 4.3 운용 방식 (mapOnlyMode)
`BattleScript`는 `mapOnlyMode = true`(기본값)로 동작:
- 맵 생성까지만 수행, **유닛 배치·턴 시스템은 KD가 담당**
- `BattleMapProvider`가 맵 질의 API만 KD에 노출

---

## 5. SW 서브시스템 — UI / 오디오 / 대화

### 5.1 위치 & 규모
`Assets/1.Scripts/SW/` · ~30 스크립트 · ~2,500+ LOC

### 5.2 구현 완료 기능

#### 이니셔티브 트래커 (`InitiativeManager.cs`, 624줄)
- 이니셔티브 내림차순 정렬, 동적 패널 레이아웃 (인덱스 0 = 100%, 나머지 85%)
- **코루틴 애니메이션 4종**:
  - `AnimateTransitionCoroutine` — 턴 순서 변경 슬라이드+스케일+페이드
  - `RemoveCharacterCoroutine` — 사망 제거 + 나머지 끌어올림
  - `AddCharacterCoroutine` — 부활/삽입 페이드인
  - `NextTurnCoroutine` — 2단계 턴 회전
- 코루틴 인터럽트 안전: `ForceApplyTargets()`로 즉시 타겟 리셋
- PC(파랑)/NPC(빨강) 색상 구분, 초상화 로딩, HP 슬라이더+텍스트

#### 전투 행동 메뉴 (`CombatInteractionUIManager`)
- 이동 / 스킬 사용 / 대기 버튼 패널
- UI 패널 슬라이드 전환 (`CombatInteractionPanelTransition`, `d74fe30`)
- 사용 불가 스킬 버튼 그레이스케일 처리 (`d74fe30`)
- 스킬 선택 실패 시 지연 경고 플로우 (`d74fe30`)

#### 보스 HP 패널 (`BossHPPanelUI`)
- 보스 체력 바 시각화
- 체력 변화 실시간 반영

#### 사운드 시스템 (`SoundManager`)
- BGM 6트랙 자동 관리: `BGM-Bright.4`, `BGM-Bright2`, `BGM-Bright3`, `BGM-Danger4`, `BGM-Peace3`, `BGM-Peace4`
- SFX 재생 (히트 이펙트 등)
- ESC 메뉴에서 볼륨 실시간 조절 (`EscMenuVolumeController`)

#### 대화 시스템 (`DialogueDisplayer` + `DialogueTable`)
- `DialogueTable` ScriptableObject — 대사 데이터 SO 관리
- `DialogueFrontPanel` — 전면 패널 연출
- `DialogueDisplayerEditor` — 에디터 내 미리보기 도구

#### ESC 메뉴 (`EscPanelUI`)
- 배경음/효과음 볼륨 슬라이더
- 메인 화면으로 돌아가기
- 앱 종료

#### 버튼 프레임워크
| 클래스 | 기능 |
|--------|------|
| `ButtonBase` | 이벤트 핸들러 추상 베이스 |
| `EasingMoveButton` | 이징 이동 애니메이션 버튼 |
| `EasingExpandableButton` | 이징 확장 버튼 |
| `SkillButtonStyler` | 스킬 버튼 상태별 스타일링 |
| `UIPanelTransitionButton` | 패널 전환 버튼 |
| `GameObjectActivateControllerButton` | GameObject 활성 토글 |
| `AppExitButton` | 앱/플레이모드 종료 |

#### 어댑터 (`BattleUnitAdapter`)
```csharp
// KD.BattleUnit → ICharacterBattleInfo 변환
Id           ← unit.Data.unitId
CharacterName← unit.Data.unitName
Initiative   ← unit.Stats.initiative
CurrentHp    ← unit.CurrentHP
PortraitSprite← unit.Data.icon
IsPC         ← 생성자 파라미터
```

---

## 6. Art 서브시스템 — 비주얼

### 6.1 바닥 큐브 셰이더 (`Assets/Art/Scripts/`)
- `FloorCubeVisual.cs` — 타일 상태(기본/이동범위/위험/선택)별 색상 변환
- `FloorCubeStater.cs` — 큐브 상태 관리
- `SafetyVisualData.cs` — 안전/위험 타일 시각 데이터

### 6.2 3D 모델
- `Assets/Art/Models/Kong/` — 주요 캐릭터 모델
- `Assets/Art/Models/Drone/` — 드론 모델
- `Assets/Art/Prefebs/Jangseungs/` — 장승 프리팹

### 6.3 VFX
- **ErbGameArt Fantasy Effects Pack** — 슬래시, 히트, 파티클 이펙트
- **Hovl Studio Magic Effects Pack** — AoE, 캐릭터 오라, 마법진, 포탈, 슬래시, 연기
- **커스텀 FX** (`Assets/Art/FX/`, `Assets/Art/TitleFX/`) — 타이틀 애니메이션
- **`SimpleSkillFxPlayer.cs`** — 스킬 실행 시 VFX 재생 연동

### 6.4 타이틀 애니메이션
- `StartRenderer.cs` — 타이틀 화면 렌더링 연출 (`fd89097`)

---

## 7. 씬별 구성 요약

### `MainScene.unity`
- URP 기본 셋업 (Camera, Directional Light, Global Volume)
- 게임 진입점 (타이틀 → 전투 씬 로드)

### `TestScene_KD.unity` ⭐ (핵심 전투 씬)
- `BattleScript` (KO) + `BattleMapProvider`
- `GridManager` + `TacticalBattleManager` + `KDBattleTurnController` (KD)
- `BattleCameraFollow` (KO)
- KD 전투가 KO 맵 위에서 동작하는 **통합 씬**

### `TestScene_KO.unity`
- `BattleScript` + `BattleCameraFollow` + `MapTester`
- KO 맵/이동 단독 테스트용

### `TestScene_SW.unity`
- `BattleScript` + `InitiativeManager` + UGUI 캔버스
- SW UI 단독 테스트용 (KD 전투 없음)

---

## 8. 인터페이스 & 추상화 목록

| 인터페이스 | 위치 | 역할 |
|-----------|------|------|
| `IInitiativeUI` | `SW/UI/` | 턴 순서 UI 라이프사이클 (7 메서드) |
| `ICharacterBattleInfo` | `SW/UI/` | 캐릭터 전투 데이터 읽기 계약 (8 프로퍼티) |
| `ICombatCharacterInfo` | `SW/UI/` | 전투 캐릭터 정보 확장 계약 |
| `ICombatInteractionController` | `SW/UI/` | 전투 행동 메뉴 컨트롤러 계약 |

---

## 9. 데이터 아키텍처

### ScriptableObject 의존 그래프
```
UnitData
  ├─ UnitBaseStats        (4 기본 스탯)
  └─ SkillData[]          (장착 스킬 목록)
       └─ GridPatternData (타겟 범위 패턴)

SkillDatabase
  └─ SkillData[]          (전체 스킬 등록부)

DeploymentController
  └─ DeploymentRuleData
       └─ GridPatternData (금지 구역 패턴)

EnemyIntentController
  └─ EnemyPatternData
       └─ SkillData[]     (행동 패턴별 스킬)

DialogueDisplayer
  └─ DialogueTable
       └─ DialogueTableItem[] (대사 항목)
```

### 맵 데이터 흐름
```
MapData/*.json
    ↓ BattleScript.LoadMap()
MapData (C# struct)
    ├─ layout[]      (32bit chip 배열)
    ├─ enemySpawns[] (스폰 위치, mapOnlyMode=true 시 미사용)
    └─ allySpawns[]  (스폰 위치, mapOnlyMode=true 시 미사용)
    ↓ BattleScript.CreateMap()
3D 씬 오브젝트 (바닥 큐브 + 장애물 프리팹)
    ↓ BattleMapProvider
KD.GridManager (전술 로직 질의)
```

---

## 10. 기술 부채 & 알려진 이슈

| 우선순위 | 항목 | 현황 |
|----------|------|------|
| 🟡 | `SimpleBattleManager` 중복 | `TacticalBattleManager`와 공존, `[Obsolete]` 처리 미완 |
| 🟡 | 버프/디버프 효과 | `SkillExecutor` 내 `Debug.Log("미구현")` — Buff/Debuff 타입 미작동 |
| 🟡 | 배치 프리뷰 비주얼 | `PlaceDeploymentPreview()` 본문 없음 |
| 🟢 | `BattleScript` god-class | ~1,100줄, SRP 위반 — 잼 기간 내 리팩터 부적합 |
| 🟢 | `TestScene_KO` missing-script | 삭제된 `BattleTurnController` 참조 댕글링 |
| 🟢 | 기존 `Docs/` 문서 일부 낡음 | KO 아키텍처 기준 서술 (KD 방향과 괴리) |

---

## 11. 주요 커밋 타임라인 (최신 순)

| 커밋 | 내용 |
|------|------|
| `8cb1525` | BattleScript 유닛 로딩 주석 처리 (리팩터링) |
| `4d081aa` | 폴백 맵 10×10 → 15×15 확장 |
| `71cb9f5` | 캐릭터 적용 및 추가 리소스 임포트 |
| `83cfb35` | **플레이루프 완결** |
| `af98944` | VFX, SFX 추가 |
| `7aaea69` | KongPocket, KongVFX 추가 |
| `c9cfcc9` | 스킬 프리팹 및 색상 오류 수정 |
| `bb7bb5b` | UI 위 클릭 시 그리드 인터랙션 차단 (fix) |
| `d74fe30` | 전투 UI 패널 슬라이드 전환, 스킬 사용 불가 그레이스케일, 경고 플로우 |
| `fd89097` | 타이틀 애니메이션 |
| `6c6aba1` | `UnitMover` 8방향 이동으로 전환 |

---

*Horn Dancheong — 젬잼 게임잼 출품작 · 2026*
