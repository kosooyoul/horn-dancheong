# 단청 (Dancheong) - Team Horn 🎮

> **젬잼(ZemJam) 출품작** · 그리드 기반 택틱컬 턴제 전투 시뮬레이션

---

## 📋 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **장르** | 택틱컬 턴제 RPG (그리드 기반 전술 전투) |
| **테마** | 혼(魂) — 영혼을 소재로 한 전투 세계관 |
| **엔진** | Unity **6000.5.0f1** (Unity 6) |
| **렌더링** | Universal Render Pipeline (URP) 17.5.0 |
| **입력** | New Input System 1.19.0 |
| **개발 기간** | 게임잼 기간 중 |
| **빌드 타겟** | Windows / macOS |

---

## 👥 팀 구성 & 담당

| 참여자 | 포지션 | 담당 범위 |
|------|-------|-----------|
| Sung-Minkyung | 리더/기획/아트 | 기획 메인, 시나리오 + 캐릭터 원화 작업 |
| tlqkaqk07-ux | 기획 | 기획 서브, 캐릭터 수치 작업 |
| Figix | 3D아트 | 바닥 큐브 셰이더/비주얼 |
| kdp8781 | 개발자 | 전투 코어 + SO 데이터 모델 (Combat/Grid/SO/Core/UI) |
| lend-a-villin | 개발자 | 전투 UI + 오디오 + 대화 시스템 |
| kosooyoul | 개발자 | 맵/전장 로딩 + 유닛 이동 + 카메라 (JSON 맵 시스템) |

---

## 🎮 게임플레이

### 핵심 흐름
```
타이틀 화면
    ↓
배치 페이즈 (Deployment Phase)
  - 아군 유닛 3명(탱커/딜러/힐러)을 지정 타일에 드래그 배치
  - 배치 확정 버튼으로 전투 진입
    ↓
전투 페이즈 (Player Phase)
  - 이니셔티브 순서에 따른 턴제 전투
  - 행동 메뉴: 이동 / 스킬 사용 / 대기
  - 스킬 범위 프리뷰 → 타겟 선택 → 실행
    ↓
적 페이즈 (Enemy Phase)
  - 보스/적 AI가 의도 경고 타일 표시 후 행동 실행
    ↓
전투 종료 (Victory / Defeat)
```

### 캐릭터 로스터

| 유닛 | 역할 | 주요 스킬 |
|------|------|-----------|
| 도윤 | 탱커 (KeumKun) | Sword1 · Sword2 · Sword3 |
| 솔하 | 힐러 (EuiKwan) | Book1 · Book2 · Book3 · Book4 |
| 유하 | 딜러 (JipHaeng) | Bow1 · Bow2 · Bow3 |
| 혼수상태 (보스) | 보스 | Boss1 · Boss2 · Boss3 · Boss4 |

### 스킬 시스템
- **역할별 스킬 트리**: 탱커 3종 / 딜러 3종 / 힐러 4종 / 보스 4종
- **그리드 패턴 8종**: 1×1 ~ 5×5, 레이, 링 패턴
- **속성 상성**: 5색 속성 공격 배율 계산 (`AttributeCalculator`)
- **AP 코스트**: 행동력(AP) 소비 기반 스킬 사용 제한
- **쿨다운**: 스킬별 쿨다운 턴 관리

---

## 🏗️ 아키텍처

```
┌──────────────────────────────────────────────────────────┐
│               KD.TacticalBattleManager                    │
│    배치 페이즈 → 플레이어 페이즈 → 적 페이즈 → 종료          │
└──────────────────┬───────────────────────────────────────┘
                   │
     ┌─────────────┼──────────────────────┐
     ▼             ▼                       ▼
KD.GridManager  KD.TurnOrderManager  KD.EnemyIntentController
(그리드 상태/     (이니셔티브 정렬/        (적 의도 표시/실행)
  하이라이트)      턴 큐 관리)
     │
     │ mapProvider (어댑터 패턴)
     ▼
KO.BattleMapProvider ──▶ KO.BattleScript
                          (JSON 맵 로드, 타일/장애물
                           좌표 변환, 유닛 이동)

SW.InitiativeManager ◀── SW.BattleUnitAdapter ◀── KD.BattleUnit
(이니셔티브 UI 시각화)     (어댑터 패턴)

SW.SoundManager ── BGM 6트랙 / SFX
SW.DialogueDisplayer ── 대화 전면 패널
SW.CombatInteractionUIManager ── 전투 행동 메뉴
```

### 설계 원칙
- **어댑터 패턴**: `BattleMapProvider`(KO↔KD), `BattleUnitAdapter`(KD↔SW) — 서브시스템 간 직접 의존 제거
- **ScriptableObject 기반 데이터**: 유닛/스킬/그리드패턴/적패턴을 SO 에셋으로 관리, 코드 수정 없이 밸런싱 가능
- **모듈 격리**: KD(전투 로직) / KO(맵 인프라) / SW(UI+오디오) 독립적 개발, 통합 시 인터페이스로 결합

---

## 📁 프로젝트 구조

```
horn-dancheong/
├── Assets/
│   ├── 0.Scenes/
│   │   ├── MainScene.unity          # 메인 씬 (진입점)
│   │   ├── TestScene_KD.unity       # KD+KO 통합 전투 씬
│   │   ├── TestScene_KO.unity       # KO 맵 단독 테스트
│   │   └── TestScene_SW.unity       # SW UI 단독 테스트
│   │
│   ├── 1.Scripts/
│   │   ├── KD/
│   │   │   ├── Combat/              # 전투 코어 (TacticalBattleManager, BattleUnit, SkillExecutor 등)
│   │   │   ├── Grid/                # 그리드 시스템 (GridManager, CombatGridQuery, GridPatternResolver)
│   │   │   ├── SO/                  # ScriptableObject 정의 (UnitData, SkillData, GridPatternData)
│   │   │   ├── Core/                # 공통 열거형, BattlePhase, PlayerRosterManager
│   │   │   ├── UI/                  # 배치 페이즈 UI, 프리배틀 UI
│   │   │   └── Test/                # 스모크 테스트
│   │   │
│   │   ├── KO/Battle/
│   │   │   ├── BattleScript.cs      # 맵 로딩/생성/관리 (1,100줄)
│   │   │   ├── BattleMapProvider.cs # KD↔KO 어댑터
│   │   │   ├── UnitMover.cs         # 8방향 이동 보간
│   │   │   ├── BattleCameraFollow.cs# 아이소메트릭 카메라
│   │   │   └── MapData/             # JSON 맵 데이터 (000~005.json, TILES, OBJECTS, ALLYS, ENEMIES)
│   │   │
│   │   └── SW/
│   │       ├── Audio/               # SoundManager (BGM/SFX), BGMType, SFXType
│   │       ├── UI/                  # InitiativeManager, CharacterPanelUI, CombatInteractionUIManager
│   │       │   └── Button/          # EasingMoveButton, EasingExpandableButton, SkillButtonStyler
│   │       ├── SO/                  # DialogueTable, DialogueTableItem
│   │       └── Editor/              # 인스펙터 테스트 도구
│   │
│   ├── 2.ScriptableObject/
│   │   ├── Units/                   # UnitData (Tanker/Dealer/Healer/Boss)
│   │   ├── Skills/
│   │   │   ├── TankerSkills/        # Sword1~3
│   │   │   ├── DealerSkills/        # Bow1~3
│   │   │   ├── HealerSkills/        # Book1~4
│   │   │   └── BossSkills/          # Boss1~4
│   │   ├── Grid/                    # GridPattern 8종
│   │   └── Enemy/                   # EnemyPatternData, DeploymentRuleData
│   │
│   ├── 3.Prefabs/                   # 유닛·스킬·UI 프리팹
│   ├── 4.SFX/                       # 사운드 에셋
│   ├── Art/                         # 3D 모델 (Kong, Drone), 셰이더, VFX, 텍스처
│   ├── Resources/                   # 런타임 로드 에셋
│   │   ├── Audio/BGM/               # BGM 6트랙
│   │   ├── Portrait/                # 캐릭터 초상화 (도윤/솔하/유하)
│   │   └── UI/                      # UI 이미지 (배치/전투메뉴/ESC)
│   └── ErbGameArt/ / Hovl Studio/   # 서드파티 VFX 에셋
│
├── Docs/                            # 프로젝트 문서
│   ├── analystic/                   # 아키텍처 분석 리포트 (00~05)
│   ├── Systems/                     # 시스템 스펙 문서
│   ├── DataFormats/                 # JSON/SO 데이터 포맷 정의
│   ├── API/                         # BattleScript API 레퍼런스
│   └── Guides/                      # 맵 제작 가이드
│
└── ProjectSettings/                 # Unity 프로젝트 설정
```

---

## 🔧 실행 방법

### 요구 사항
- **Unity 6000.5.0f1** 이상
- Universal Render Pipeline 17.5.0

### 에디터에서 실행
1. Unity Hub에서 프로젝트 폴더(`horn-dancheong/`) 열기
2. `Assets/0.Scenes/MainScene.unity` 또는 `TestScene_KD.unity` 씬 열기
3. `▶ Play` 버튼으로 실행

### 전투 테스트 (권장 씬)
- **`TestScene_KD`** — KD 전투 + KO 맵 통합 씬. 가장 완성된 전투 플레이 가능
- **`TestScene_SW`** — SW UI 단독 확인

### 키보드 단축키 (전투 중)
| 키 | 동작 |
|----|------|
| `1` / `M` | 이동 모드 |
| `2` / `K` | 스킬 모드 |
| `W` / `Space` | 대기 (턴 종료) |
| `Esc` | 선택 취소 |
| `Enter` | 배치/행동 확정 |
| `마우스 우클릭` | 전투 메뉴 취소 |

---

## ⚙️ 주요 시스템 상세

### KD — 전투 코어
- **스탯 파이프라인**: `UnitBaseStats`(4기본) → `StatCalculator` → `UnitDerivedStats`(9파생: maxHP, initiative, moveRange, attack, heal, defense, crit%, evasion%)
- **이동 6종**: Cardinal · 8Dir · DiagonalOnly · KnightJump · Teleport · Charge (BFS + 특수 이동)
- **스킬 실행**: 데미지/힐 공식 (크리·방어·회피·속성·스케일 6배수), AP·쿨다운 관리
- **배치 시스템**: 드래그 배치(`DeploymentDragController`), 후보 타일 / 금지 구역 / 최대 배치 수
- **적 의도 시스템**: 위험 타일 미리 표시(DangerS/M/L/XL) → 실행
- **카메라 흔들기**: `CameraShakeManager` — 피격·스킬 연출

### KO — 맵 인프라
- **JSON 맵 포맷**: 32비트 칩 `(ObjectID << 8) | TileID`, 21×21 그리드 6개 맵
- **좌표 변환**: GridToWorld · WorldToGrid, 아이소메트릭 오프셋
- **이동 보간**: `UnitMover` 8방향 부드러운 이동 + 자동 회전
- **카메라**: 45° 직교 아이소메트릭, 스무스 댐핑 추적

### SW — UI / 오디오 / 대화
- **이니셔티브 트래커**: 코루틴 애니메이션 4종 (슬라이드/페이드/삽입/턴전환), 인터럽트 안전
- **전투 행동 메뉴**: `CombatInteractionUIManager` — 이동/스킬/대기 패널 슬라이드 전환
- **보스 HP 패널**: `BossHPPanelUI` — 보스 체력 시각화
- **사운드 시스템**: `SoundManager` — BGM 6트랙(Bright×3, Danger, Peace×2) + SFX, 씬별 자동 전환
- **대화 시스템**: `DialogueDisplayer` + `DialogueTable` SO — 전면 패널 대화 연출
- **ESC 메뉴**: 볼륨 컨트롤(`EscMenuVolumeController`), 메인으로/앱 종료

---

## 📊 구현 완성도

| 시스템 | 완성도 | 상태 |
|--------|--------|------|
| KD 전투 코어 (이동/스탯/턴) | ✅ Done | 안정 동작 |
| KD 스킬 실행 (Damage/Heal) | ✅ Done | 공식 적용 |
| KD 배치 페이즈 | ✅ Done | 드래그 배치 구현 |
| KD 적 의도 시스템 | ✅ Done | 경고 타일 표시 |
| KO 맵 로딩/생성 | ✅ Done | 6개 맵, JSON 기반 |
| KO 이동 보간/카메라 | ✅ Done | 8방향, 아이소메트릭 |
| SW 이니셔티브 UI | ✅ Done | 코루틴 애니 4종 |
| SW 전투 행동 메뉴 | ✅ Done | 패널 슬라이드 전환 |
| SW 사운드 시스템 | ✅ Done | BGM/SFX |
| SW 대화 시스템 | ✅ Done | SO 기반 |
| VFX (스킬 이펙트) | ✅ Done | Hovl/ErbGameArt 활용 |
| 캐릭터 역할 3종 | ✅ Done | 탱커/딜러/힐러 |
| 보스 시스템 | ✅ Done | 보스 스킬 4종 |
| 버프/디버프 효과 | ⚠️ Partial | Damage/Heal만 작동 |
| 배치 프리뷰 비주얼 | ⚠️ Partial | 텍스트 상태만 표시 |

---

## 📖 문서 목차

- [`Docs/analystic/`](Docs/analystic/README.md) — 아키텍처 분석 리포트 (코드베이스 심층 분석)
- [`Docs/Systems/BattleSystem.md`](Docs/Systems/BattleSystem.md) — 전투 시스템 스펙
- [`Docs/Systems/MapSystem.md`](Docs/Systems/MapSystem.md) — 맵 시스템 스펙
- [`Docs/API/BattleScript.md`](Docs/API/BattleScript.md) — BattleScript API 레퍼런스
- [`Docs/DataFormats/`](Docs/DataFormats/) — JSON / SO 데이터 포맷 정의
- [`Docs/Guides/MapCreation.md`](Docs/Guides/MapCreation.md) — 맵 제작 가이드

---

## 🔗 관련 링크

- **Repository**: [github.com/kosooyoul/horn-dancheong](https://github.com/kosooyoul/horn-dancheong)
- **Branch**: `master` (활성 개발: `features/battle-map`)

---

*최종 업데이트: 2026-06-23 · Unity 6000.5.0f1 · Branch: master*
