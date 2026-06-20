# horn-dancheong 현황 분석 문서 (Analytic)

> 젬잼/게임잼 출품작 · 주제 **"혼(魂)"** · 택틱컬 턴제 시뮬레이션
> 분석 기준일: **2026-06-20** · 브랜치: `features/battle-map` · Unity **6000.5.0f1 (URP 17.5)**

이 디렉터리는 현재 프로젝트의 구현 상태를 **있는 그대로 진단**한 문서 모음입니다.
"무엇이 되어 있고 / 무엇이 덜 됐고 / 무엇이 위험한가"를 빠르게 파악하기 위한 자료입니다.

## 문서 구성

| 문서 | 내용 |
|------|------|
| [00-overview.md](./00-overview.md) | 프로젝트 전체 그림, 팀 구조, **통합 상태 종합 판정** |
| [01-kd-combat.md](./01-kd-combat.md) | **KD** — 전투 코어 / 그리드 / 스킬 / 배치 / 적 AI |
| [02-ko-battlemap.md](./02-ko-battlemap.md) | **KO** — 맵 로딩 / 타일 / 유닛 이동 / 카메라 |
| [03-sw-ui-initiative.md](./03-sw-ui-initiative.md) | **SW** — 이니셔티브 UI / 캐릭터 패널 / 어댑터 |
| [04-assets-scenes.md](./04-assets-scenes.md) | 씬 4종 / ScriptableObject 데이터 / 빌드·패키지 설정 |
| [05-diagnosis-and-roadmap.md](./05-diagnosis-and-roadmap.md) | **종합 진단, 리스크 우선순위, 통합 로드맵** |

## 한 줄 요약

> 세 개발자(**KD 전투 / KO 맵 / SW UI**)의 서브시스템이 각자 완성도는 높지만,
> **하나의 플레이 가능한 씬으로 합쳐지지 않은** 상태입니다.
> KD↔KO는 `TestScene_KD`에서 실제로 연결되어 있고, SW↔KD는 **코드(어댑터)만 있고 씬 결합은 없음**.
> 빌드 씬(`MainScene`)은 **비어 있고**, 스킬 실행 로직은 **revert로 어정쩡한 상태**입니다.

자세한 우선순위와 권장 작업은 [05-diagnosis-and-roadmap.md](./05-diagnosis-and-roadmap.md) 참고.
