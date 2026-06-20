# 03 · SW 서브시스템 — 이니셔티브 UI / 캐릭터 패널

> 위치: `Assets/1.Scripts/SW/` (UI, UI/Button, TestCode/InitiativeManager, Editor)
> 네임스페이스: `HornDancheong.Seongwoo.UI` · 규모: 9 스크립트, ~1,073 LOC

## 1. 역할 & 아키텍처

SW는 **이니셔티브(턴 순서) 시각화 + 캐릭터 전투 정보 UI**입니다.

- `IInitiativeUI` — 턴 순서 표시 라이프사이클 인터페이스(7 메서드: init/update/remove/add/nextTurn)
- `ICharacterBattleInfo` — 캐릭터 전투 데이터 읽기 전용 계약(8 프로퍼티: Id/Name/Portrait/HP/Initiative/IsPC)
- `BattleUnitAdapter` — **어댑터 패턴**: `KD.BattleUnit`을 `ICharacterBattleInfo`로 변환(비즈니스 로직 0)
- `InitiativeManager` — 핵심 MonoBehaviour. 패널 라이프사이클 + 코루틴 애니메이션
- `CharacterPanelUI` — 캐릭터 1명 패널(초상화/이름/HP 슬라이더·텍스트/PC·NPC 색)
- **버튼 프레임워크** — `ButtonBase` + `GameObjectActivateControllerButton` + `AppExitButton`
- `InitiativeManagerEditor` — 플레이 모드 커스텀 인스펙터(목 데이터 6개 테스트 버튼)

## 2. 파일 인벤토리

| 파일 | 줄수 | 역할 | 상태 |
|------|------|------|------|
| `TestCode/InitiativeManager.cs` | 624 | 턴 순서 UI 매니저(패널 라이프사이클/애니/HP) | **Done** |
| `Editor/InitiativeManagerEditor.cs` | 177 | 플레이 모드 테스트 하네스(목 데이터 6버튼) | **Done** |
| `UI/IInitiativeUI.cs` | 42 | UI 상태 관리 인터페이스(7 메서드) | **Done** |
| `UI/ICharacterBattleInfo.cs` | 16 | 캐릭터 전투 데이터 인터페이스(8 프로퍼티) | **Done** |
| `UI/CharacterPanelUI.cs` | 126 | 개별 캐릭터 패널 렌더 | **Done** |
| `UI/BattleUnitAdapter.cs` | 33 | KD.BattleUnit → ICharacterBattleInfo 어댑터 | **Done** |
| `UI/Button/ButtonBase.cs` | 18 | 버튼 핸들러 추상 베이스 | **Done** |
| `UI/Button/GameObjectActivateControllerButton.cs` | 17 | GameObject 활성 토글 버튼 | **Done** |
| `UI/Button/AppExitButton.cs` | 20 | 플레이 모드/앱 종료 버튼 | **Done** |

> **TODO/FIXME/throw/placeholder 0건.** SW 영역은 코드 품질이 가장 안정적.

## 3. 구현 상태

### ✅ 완성 (프로덕션 수준)
- 프리팹 기반 패널 인스턴스화 + null 체크
- 이니셔티브 내림차순 정렬(line 84)
- 스케일/알파 동적 레이아웃(인덱스 0은 100%, 나머지 85%)
- **정교한 코루틴 애니메이션 4종**:
  - `AnimateTransitionCoroutine`(186-235) — 턴 순서 변경 시 슬라이드+스케일+페이드
  - `RemoveCharacterCoroutine`(265-328) — 사망 제거 + 나머지 끌어올림
  - `AddCharacterCoroutine`(390-453) — 부활/삽입 페이드인
  - `NextTurnCoroutine`(472-554) — 2단계 턴 회전
- 코루틴 인터럽트 안전성: `ForceApplyTargets`(591-602)로 취소 시 즉시 타깃 상태 리셋
- 초상화 로딩(Sprite 우선, Resources.Load 폴백), HP 슬라이더+텍스트 동기화, PC/NPC 색(파랑/빨강)
- 에디터 테스트: 플레이 모드 한정, 6개 시나리오(HP 변경/제거/부활)

## 4. 버그 / 리스크

**치명적 이슈 없음.** 경미한 항목:

1. **🟢 코루틴 안전성(엣지)** — 패널 파괴 중 코루틴이 프레임 경계를 넘으면 `_panelsList[i]` null 가능. 단, 루프마다 null 체크가 있어 저위험.
2. **🟢 패널 딕셔너리 키 충돌** — `charInfo.Id`를 키로 사용(106/172/378). 동일 ID 두 캐릭터면 덮어씀. ID 유일성은 호출자 책임, 검증 없음.
3. **🟢 오브젝트 풀링 없음** — Instantiate/Destroy. 6~12명 수준이라 무시 가능.
4. **AnimationCurve 미사용** — 전부 `Mathf.SmoothStep`. 일관적이고 적절.

## 5. KD와의 통합 (중요)

> **상태: 코드상 준비 완료, 그러나 실제 씬 결합 ❌**

### BattleUnitAdapter (준비됨)
`KD.BattleUnit` → `ICharacterBattleInfo` 매핑:
```
Id           ← _unit.Data.unitId
CharacterName← _unit.Data.unitName
Initiative   ← _unit.Stats.initiative
CurrentHp    ← _unit.CurrentHP   (KD는 int, SW는 float 수용)
PortraitSprite← _unit.Data.icon
_isPC        ← 생성자 파라미터(별도 저장)
```

### 연결되지 않은 지점
- `KO.BattleScript`의 InitiativeUI 연동 코드(131-132, 124-130)가 **주석 처리**됨 → SW는 **아키텍처상 준비됐으나 배선 안 됨**.
- `TestScene_SW`에는 KD 전투 컴포넌트(TacticalBattleManager)가 **없음**. `TestScene_KD`에는 SW UI가 **없음**.
- → 어댑터는 존재하나 **두 시스템이 한 씬에서 함께 인스턴스화된 적이 없음.**

### 중복 우려 ⚠️
- KD의 `TurnOrderManager.BuildTurnOrder()`가 이니셔티브 순서를 **독립적으로 계산**.
- SW의 `InitiativeManager`도 `OrderByDescending(c => c.Initiative)`로 **동일 정렬을 또 수행**.
- **자동 동기화 없음**: KD가 턴 순서/HP를 바꿔도 SW는 명시 호출(`UpdateCharacterHp`, `RemoveCharacter`, `AddCharacter`) 없이는 갱신 안 됨. 이벤트 구독/자동 전파 부재.

## 6. 종합 평가

| 항목 | 상태 |
|------|------|
| Null 안전성 / 네이밍 / 문서(주석) | 우수 |
| 코루틴 관리 | 양호(Stop→Start, ForceApplyTargets 클린업) |
| 관심사 분리(인터페이스+어댑터) | 양호 |
| 테스트 인프라 | 에디터 하네스 우수(유닛 테스트는 없음 — 잼 맥락상 정상) |
| **KD 연동** | ❌ 코드만, 씬 미결합 |
| **중복** | ⚠️ 이니셔티브 정렬을 KD/SW가 각자 수행 |

> **권장**: SW가 KD의 `TurnOrderManager` 결과를 **소비**하도록 단일화. KD 전투 이벤트(피격/사망/턴진행)를 SW UI 메서드에 연결하는 배선 작업 필요(상세 [05](./05-diagnosis-and-roadmap.md)).
