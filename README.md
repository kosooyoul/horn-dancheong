# 단청 (Dancheong) - Team Horn 🎮

> 젬잼(ZemJam) 출품작 · 그리드 기반 택틱컬 턴제 RPG

---

## 📋 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **장르** | 택틱컬 턴제 RPG (그리드 전술 전투) |
| **테마** | 혼(魂) · 도깨비와 맞서는 민속 세계관 |
| **엔진** | Unity 6000.5.0f1 |
| **렌더링** | Universal Render Pipeline (URP) 17.5.0 |
| **입력** | New Input System 1.19.0 |
| **개발 기간** | 게임잼 기간 중 |
| **빌드 타겟** | Windows / macOS |

---

## 👥 팀 구성 & 담당




| 참여자 | 포지션 | 담당 범위 |
|------|-------|-----------|
| Sung-Minkyung | 리더/기획/아트 | 기획 메인, 시나리오 + 캐릭터 원화 작업 ++ |
| tlqkaqk07-ux | 기획 | 기획 서브, 캐릭터 수치 작업 ++ |
| Figix | 3D아트 | 바닥 큐브 셰이더/비주얼 ++ |
| kdp8781 | 개발자 | 전투 코어 + SO 데이터 모델 (Combat/Grid/SO/Core/UI) ++ |
| lend-a-villin | 개발자 | 전투 UI + 오디오 + 대화 시스템 ++ |
| kosooyoul | 개발자 | 맵/전장 로딩 + 유닛 이동 + 카메라 (JSON 맵 시스템) |

---

## 🎮 게임플레이

```
메인 메뉴 (타이틀 애니메이션)
    ↓
배치 페이즈
  · 아군 3명(탱커 / 딜러 / 힐러)을 그리드에 드래그 배치
  · 배치 확정 → 전투 시작
    ↓
전투 페이즈 (이니셔티브 순서 턴제)
  · 행동 메뉴 선택: 이동 · 스킬 · 대기
  · 스킬 범위 프리뷰 → 타겟 선택 → 실행 + VFX + SFX
  · 캐릭터 HP/SP 실시간 갱신 (패널 자동 업데이트)
    ↓
적 페이즈
  · 보스/적이 위험 타일 경고 표시 후 행동 실행
  · 보스 HP 패널 실시간 표시
    ↓
전투 종료 / 씬 전환
```

---

## 캐릭터

| 캐릭터 | 역할 | 스킬 (3~4종) | 대표 SFX |
|--------|------|--------------|----------|
| **도윤** | 탱커 (KeumKun) | Sword1 · Sword2 · Sword3 | 검 베는 소리 · 발도 슥삭 소리 · 피해 감소 |
| **솔하** | 힐러 (EuiKwan) | Book1 · Book2 · Book3 · Book4 | 힐러 공격 · 힐 스킬 · 부활 스킬 |
| **유하** | 딜러 (JipHaeng) | Bow1 · Bow2 · Bow3 | 부적 공격 · 단일 공격 · 광역 공격 |
| **도깨비** (보스) | Boss | Boss1 · Boss2 · Boss3 · Boss4 | 일반 공격 · 단일 공격(번개) |

스킬 합계 **16종** (Sword×3 / Bow×3 / Book×4 / Boss×4 + Shot · Talisman)

---

## 🏗️ 아키텍처

```
┌─────────────────────────────────────────────────────┐
│            KD.TacticalBattleManager                  │
│  배치 → 플레이어 페이즈 → 적 페이즈 → 종료             │
│  OnTurnUpdated 이벤트 → CharacterFixedStatUI 자동 갱신 │
└─────────────────┬───────────────────────────────────┘
                  │
    ┌─────────────┼─────────────────────┐
    ▼             ▼                      ▼
GridManager   TurnOrderManager    EnemyIntentController
(그리드/하이라이트) (이니셔티브 정렬)    (위험 타일 · 행동 실행)
    │
    │ BattleMapProvider (어댑터)
    ▼
KO.BattleScript ── JSON 맵 6개 ── 타일/장애물/좌표계

SW.InitiativeManager ◀── BattleUnitAdapter ◀── KD.BattleUnit
SW.SoundManager ── BGM 6트랙 / SFX 11종
SW.DialogueDisplayer ── DialogueTable (Excel 기반 SO)
SW.CombatInteractionUIManager ── 행동 메뉴 패널
KD.CharacterFixedStatUI ── HP/SP 슬라이더 (호버 페이드)
```

### 핵심 설계 원칙
- **어댑터 패턴**: `BattleMapProvider`(KO↔KD), `BattleUnitAdapter`(KD↔SW) — 서브시스템 직접 의존 제거
- **ScriptableObject 데이터 드리븐**: 유닛/스킬/그리드패턴/적패턴/대사표를 에셋으로 관리
- **이벤트 기반 UI 갱신**: `TacticalBattleManager.OnTurnUpdated` → `CharacterFixedStatUI.RefreshStatInfo()`

---

## 📁 UI 패널 구성 (8종)

| 패널 | 클래스 | 역할 |
|------|--------|------|
| 메인 메뉴 | — | 타이틀 · 시작 · 설정 · 종료 |
| 배치 | `DeploymentRosterPanelUI` 외 | 유닛 배치 드래그 UI |
| 이니셔티브 트래커 | `InitiativeManager` | 턴 순서 · 코루틴 슬라이드 애니 4종 |
| 전투 행동 메뉴 | `CombatInteractionUIManager` | 이동/스킬/대기 패널 슬라이드 |
| 캐릭터 스탯 | `CharacterFixedStatUI` | HP/SP 바 · 초상화 · 사망 시 흑백 |
| 보스 HP | `BossHPPanelUI` | 보스 체력 바 실시간 표시 |
| 대화 | `DialogueDisplayer` | 전면 패널 대사 연출 |
| ESC 메뉴 | `EscPanelUI` | BGM/SFX 볼륨 · 메인으로 · 종료 |

---

## 📁 프로젝트 구조

```
horn-dancheong/
├── Assets/
│   ├── 0.Scenes/
│   │   ├── MainScene.unity          # 메인 메뉴 씬 (비활성)
│   │   ├── TestScene_SW.unity       # ★ 빌드 등록 씬 (전체 통합)
│   │   ├── TestScene_KD.unity       # KD+KO 전투 단독 테스트
│   │   └── TestScene_KO.unity       # KO 맵 단독 테스트
│   │
│   ├── 1.Scripts/
│   │   ├── KD/
│   │   │   ├── Combat/              # TacticalBattleManager, BattleUnit, SkillExecutor 등
│   │   │   ├── Grid/                # GridManager, CombatGridQuery, GridPatternResolver
│   │   │   ├── SO/                  # UnitData, SkillData, GridPatternData (SO 정의)
│   │   │   ├── Core/                # GameEnums, BattlePhase, PlayerRosterManager
│   │   │   └── UI/                  # CharacterFixedStatUI, 배치 UI 6종
│   │   ├── KO/Battle/
│   │   │   ├── BattleScript.cs      # 맵 로딩/생성 (mapOnlyMode=true)
│   │   │   ├── BattleMapProvider.cs # KD↔KO 어댑터
│   │   │   ├── UnitMover.cs         # 8방향 이동 보간
│   │   │   ├── BattleCameraFollow.cs# 아이소메트릭 45° 카메라
│   │   │   └── MapData/             # 000~005.json, TILES, OBJECTS, ALLYS, ENEMIES
│   │   └── SW/
│   │       ├── Audio/               # SoundManager, BGMType, SFXType
│   │       ├── UI/                  # InitiativeManager, CombatInteractionUIManager,
│   │       │   │                    # BossHPPanelUI, DialogueDisplayer, EscPanelUI 등
│   │       │   └── Button/          # EasingMoveButton, EasingExpandableButton, SkillButtonStyler
│   │       └── SO/                  # DialogueTable, DialogueTableItem
│   │
│   ├── 2.ScriptableObject/
│   │   ├── Units/                   # UnitData_Tanker/Dealer/Healer, BossData, EnemyData_1
│   │   ├── Skills/
│   │   │   ├── TankerSkills/        # Sword1~3
│   │   │   ├── DealerSkills/        # Bow1~3
│   │   │   ├── HealerSkills/        # Book1~4
│   │   │   └── BossSkills/          # Boss1~4
│   │   ├── Grid/                    # GridPattern 8종 (1×1 ~ 5×5, 레이, 링)
│   │   └── Enemy/                   # EnemyPatternData, DeploymentRuleData
│   │
│   ├── 3.Prefabs/                   # 유닛·스킬·장승 프리팹
│   ├── 4.SFX/                       # 역할별 SFX 11종 (검/부적/힐러/보스)
│   ├── 98.DataSheet/                # DialogueTable.xlsx + .asset
│   ├── Art/                         # 3D 모델(Kong/Drone), 셰이더, VFX, TitleFX
│   ├── Resources/
│   │   ├── Audio/BGM/               # BGM 6트랙 (Bright2~4, Danger4, Peace3~4)
│   │   ├── Portrait/                # 초상화 3종 (doyun, solha, yuha)
│   │   └── UI/
│   │       ├── CombatInteractionMenu/
│   │       │   ├── RoundThumbnail/  # 원형 썸네일 (R_doyun, R_Sol, R_Yuha)
│   │       │   └── SquareThumbnail/ # 사각 썸네일 (S_Dohun, S_Sol, S_Yuha)
│   │       ├── Main/                # 메인 메뉴 이미지 (로고, 배경, 버튼)
│   │       ├── InitiativeTrack/     # 이니셔티브 패널 이미지
│   │       ├── Batch/               # 배치 UI 이미지
│   │       └── EscMenu/             # ESC 메뉴 이미지
│   └── ErbGameArt/ · Hovl Studio/   # 서드파티 VFX 에셋
│
└── Docs/                            # 프로젝트 문서
    ├── IMPLEMENTATION_REPORT.md     # 구현 내용 보고서
    ├── analystic/                   # 코드베이스 심층 분석 (00~05)
    ├── Systems/                     # 시스템 스펙 문서
    ├── DataFormats/                 # JSON/SO 데이터 포맷 정의
    └── Guides/                      # 맵 제작 가이드
```

---

## 🔧 실행 방법

**요구 사항**: Unity 6000.5.0f1 이상

1. Unity Hub에서 `horn-dancheong/` 폴더 열기
2. `Assets/0.Scenes/TestScene_SW.unity` 씬 열기 (빌드 등록 씬)
3. `▶ Play`로 실행

> `TestScene_KD.unity` — 전투 로직 단독 테스트 시 사용

### 키보드 단축키

| 키 | 동작 |
|----|------|
| `1` / `M` | 이동 모드 |
| `2` / `K` | 스킬 모드 |
| `W` / `Space` | 대기 (턴 종료) |
| `Esc` | 선택 취소 / ESC 메뉴 |
| `Enter` | 배치·행동 확정 |
| 마우스 우클릭 | 전투 메뉴 취소 |
| `Space` / `B` | 다음/이전 맵 (TestScene_KO) |

---

## 📖 문서 목차

| 시스템 | 상태 |
|--------|------|
| 전투 루프 (배치·플레이어·적 페이즈) | ✅ 완료 |
| 스탯 파이프라인 (4기본 → 9파생 + SP) | ✅ 완료 |
| 이동 6종 (Cardinal / 8Dir / Diagonal / Knight / Teleport / Charge) | ✅ 완료 |
| 스킬 실행 (Damage / Heal, AP · 쿨다운) | ✅ 완료 |
| 그리드 패턴 8종 (1×1 ~ 5×5, 레이, 링) | ✅ 완료 |
| 배치 페이즈 (드래그 배치) | ✅ 완료 |
| 적 의도 시스템 (위험 타일 경고 → 실행) | ✅ 완료 |
| JSON 맵 6개 로딩 (15×15 이상) | ✅ 완료 |
| 8방향 유닛 이동 보간 | ✅ 완료 |
| 아이소메트릭 카메라 | ✅ 완료 |
| 이니셔티브 트래커 UI (코루틴 애니 4종) | ✅ 완료 |
| 전투 행동 메뉴 (슬라이드 전환) | ✅ 완료 |
| 캐릭터 스탯 패널 (HP/SP · 호버 페이드) | ✅ 완료 |
| 보스 HP 패널 | ✅ 완료 |
| 사운드 시스템 (BGM 6종 · SFX 11종) | ✅ 완료 |
| 대화 시스템 (Excel 기반 DialogueTable) | ✅ 완료 |
| ESC 메뉴 (볼륨 컨트롤) | ✅ 완료 |
| VFX (스킬 이펙트 · 타이틀 애니) | ✅ 완료 |
| 속성 상성 (5색 배율) | ✅ 완료 |
| 버프 / 디버프 효과 | ⚠️ 미구현 (타입 정의만) |
| 배치 프리뷰 비주얼 | ⚠️ 미구현 (텍스트만) |

---

## 🔗 관련 링크

- **Repository**: [github.com/kosooyoul/horn-dancheong](https://github.com/kosooyoul/horn-dancheong)

---

*v0.1.0 · Unity 6000.5.0f1 · 2026-06-23*
