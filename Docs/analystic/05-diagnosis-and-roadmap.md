# 05 · 종합 진단 & 통합 로드맵

분석 기준일 **2026-06-20** · 브랜치 `features/battle-map`

## 1. 한 문장 진단

> **세 서브시스템(KD 전투 / KO 맵 / SW UI)은 각자 완성도가 높지만, 하나의 플레이 가능한 게임으로 합쳐지지 않았다.**
> 막바지 애먹는 지점은 ① **통합 씬 부재**, ② **스킬 실행 로직 revert로 미정착**, ③ **데이터 모델 이원화(JSON vs SO)**, ④ **전투↔UI 배선 없음** 네 가지에 집중되어 있다.

## 2. 서브시스템 성숙도 매트릭스

| 서브시스템 | 코어 로직 | 비주얼/연출 | 통합 | 데이터 | 종합 |
|------------|-----------|-------------|------|--------|------|
| **KD 전투** | 🟢 강함 | 🟡 애니 훅 미연결 | 🟡 KO와만 결합 | 🔴 플레이스홀더 | **70%** |
| **KO 맵** | 🟢 강함(맵), 🔴 턴/스폰 스텁 | 🟢 카메라/이동 | 🟢 KD에 제공자로 연결 | 🟡 JSON 5맵 | **75%** |
| **SW UI** | 🟢 강함 | 🟢 정교한 코루틴 애니 | 🔴 코드만, 씬 결합 0 | 🟡 목 데이터 | **65% (고립)** |

## 3. 리스크 우선순위 (P0 = 게임 성립 차단)

### 🔴 P0 — 데모/빌드 성립 차단
1. **통합 플레이 씬 부재 + 빈 빌드 씬**
   - `MainScene`이 비어 있고 빌드 목록에 그것만 등록 → **빌드 시 빈 화면**.
   - KD+KO+SW가 한 씬에 모인 적 없음.
   - **영향**: 제출/시연 불가.
2. **스킬 실행 미정착 (revert 잔재)**
   - `SkillExecutor`/`StatCalculator` 연결이 `24c8fc9` revert로 풀림. 테스트 5종 동반 삭제.
   - **영향**: 전투의 핵심(스킬 데미지)이 신뢰 불가 상태.

### 🟡 P1 — 게임은 되나 반쪽
3. **데이터 모델 이원화** — KO JSON(agility/spirit/guard/luck/mov) vs KD SO(role/attribute/AP). 정본 미결정(현 방향 KD SO). 유닛 데이터가 두 곳에 따로 존재.
4. **전투 ↔ UI 배선 없음** — `BattleUnitAdapter`는 있으나 KD 전투 이벤트(피격/사망/턴진행)가 SW `InitiativeManager`에 전달 안 됨. 양쪽이 이니셔티브를 각자 계산(중복).
5. **이중 배틀 매니저** — `TacticalBattleManager` vs `SimpleBattleManager` 둘 다 동작/비호환. 정본 미지정.
6. **콘텐츠 빈약** — 유닛 스탯 전부 1, 스킬 1개, 적 1종. 실제 전투 밸런스/재미 검증 불가.

### 🟢 P2 — 폴리시/정리
7. KD 배치 프리뷰 비주얼 미구현 / 버프·디버프 미작동 / 적 턴 연출 즉시 실행
8. KO `BattleScript` god-class(1,100줄) + 미사용 JSON 스폰 + 죽은 UI 코드
9. `TestScene_KO`의 missing-script 참조 정리
10. 문서(`Docs/`)가 KD 전투 방향과 괴리 + 깨진 링크 다수

## 4. 통합 로드맵 (잼 마감 역산 권장 순서)

```
[1] 정본 결정 (반나절)
    └ 전투 매니저: TacticalBattleManager 채택, SimpleBattleManager [Obsolete]
    └ 데이터 모델: KD SO 채택, KO JSON 유닛 스탯은 "맵/스폰 위치"로만 한정
    └ 이니셔티브 계산: KD.TurnOrderManager 단일화, SW는 결과만 소비

[2] 스킬 실행 재정착 (P0-2) (1일)
    └ SkillExecutor/StatCalculator 연결 복구 + CombatSmokeTest로 데미지 공식 검증
    └ revert로 사라진 핵심 테스트 최소 2개(StatCalculator, BattleFlow) 복원

[3] 통합 씬 구축 (P0-1) (1~2일)
    └ TestScene_KD(KD+KO 결합) 기반으로 SW InitiativeManager + 캔버스 추가
    └ KO BattleScript의 주석 처리된 InitiativeUI 배선(124-132줄) 복구·전환
    └ KD 전투 이벤트 → SW UI 메서드 연결(피격→UpdateCharacterHp, 사망→RemoveCharacter, 턴→NextTurn)
    └ 완성 씬을 EditorBuildSettings에 등록(빈 MainScene 대체 또는 MainScene을 진입점으로)

[4] 최소 콘텐츠 (P1-6) (1일)
    └ 유닛 3~4종 실제 스탯, 스킬 3~5개(범위/효과 다양화), 적 2~3종, 맵 1~2개 선별

[5] 폴리시 (P2) (여유 시)
    └ 배치 프리뷰 비주얼, 적 턴 연출 대기, 애니 훅 연결, 버프/디버프(여유 시)
    └ 문서 갱신(현 KD 아키텍처 반영), missing-script 정리
```

## 5. "지금 애먹는 부분" 추정 (코드 흔적 기반)

- `features/battle-map` 브랜치명 + 최근 커밋(GridManager/UnitMover 이동·회전, AP 게이팅, TestScene_KD 적 설정) → **KD 전투를 KO 맵 위에 얹는 통합 작업이 현재 진행 중**.
- `SkillExcuter&StatCalculator` revert → **스킬 실행/스탯 계산 통합에서 한 번 막혀 롤백**한 흔적. 여기가 가장 최근의 난관으로 보임.
- KO BattleScript의 주석 처리된 InitiativeUI 코드 + SW의 미결합 → **UI 연동을 시도하다 보류**한 상태.

## 6. 강점 (유지할 것)

- **KD–KO 어댑터 분리(`BattleMapProvider`)가 깨끗** — 맵/로직 경계가 명확해 통합 비용이 낮음.
- **SW UI 코드 품질 우수** — 코루틴 애니/인터럽트 안전성 등 프로덕션 수준. 배선만 하면 됨.
- **KD 전투 로직 견고** — 이동 6종, 스킬 범위 패턴, 적 의도 시스템, 배치 페이즈까지 폭넓게 구현됨.
- **모듈 격리 양호** — 서브시스템 간 하드 의존이 적어 병렬 작업/통합이 수월.

---

### 부록: 문서 교차 참조
- KD 상세 → [01-kd-combat.md](./01-kd-combat.md)
- KO 상세 → [02-ko-battlemap.md](./02-ko-battlemap.md)
- SW 상세 → [03-sw-ui-initiative.md](./03-sw-ui-initiative.md)
- 씬/에셋/빌드 → [04-assets-scenes.md](./04-assets-scenes.md)
