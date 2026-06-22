# Horn Dancheong — 문서 인덱스

> 최종 업데이트: **2026-06-23** · 엔진: Unity 6000.5.0f1

---

## 📋 빠른 링크

| 문서 | 설명 |
|------|------|
| **[구현 보고서](IMPLEMENTATION_REPORT.md)** | 전체 구현 현황 · 완성도 매트릭스 · 기술 부채 정리 |
| **[분석 리포트](analystic/README.md)** | 코드베이스 심층 분석 (KD/KO/SW 서브시스템별) |

---

## 📁 문서 구조

### 1. 구현 보고서
- **[IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md)** — 전체 구현 내용, 완성도, 주요 커밋 타임라인

### 2. 코드베이스 분석 (analystic/)
- [00-overview.md](analystic/00-overview.md) — 프로젝트 전체 개요 & 통합 상태 판정
- [01-kd-combat.md](analystic/01-kd-combat.md) — KD 서브시스템 (전투 코어) 분석
- [02-ko-battlemap.md](analystic/02-ko-battlemap.md) — KO 서브시스템 (배틀맵/이동/카메라) 분석
- [03-sw-ui-initiative.md](analystic/03-sw-ui-initiative.md) — SW 서브시스템 (UI/이니셔티브) 분석
- [04-assets-scenes.md](analystic/04-assets-scenes.md) — 씬/에셋/빌드 설정 분석
- [05-diagnosis-and-roadmap.md](analystic/05-diagnosis-and-roadmap.md) — 종합 진단 & 통합 로드맵

### 3. 시스템 스펙 (Systems/)
- [BattleSystem.md](Systems/BattleSystem.md) — 전투 시스템 전체 사양
- [MapSystem.md](Systems/MapSystem.md) — 맵 시스템 전체 사양 (KO 기반)

### 4. 데이터 포맷 (DataFormats/)
- [MapDataFormat.md](DataFormats/MapDataFormat.md) — 맵 데이터 JSON 포맷
- [TileDefinition.md](DataFormats/TileDefinition.md) — 타일 정의 포맷
- [ObjectDefinition.md](DataFormats/ObjectDefinition.md) — 오브젝트 정의 포맷
- [UnitDefinition.md](DataFormats/UnitDefinition.md) — 유닛 정의 포맷
- [SkillDefinition.md](DataFormats/SkillDefinition.md) — 스킬 정의 포맷

### 5. API 레퍼런스 (API/)
- [BattleScript.md](API/BattleScript.md) — BattleScript 클래스 API (~60% 정확, 턴 API 일부 미구현)

### 6. 가이드 (Guides/)
- [MapCreation.md](Guides/MapCreation.md) — 맵 제작 가이드 (JSON 포맷 기준)

---

## ⚠️ 문서 정확도 주의사항

기존 문서(`Systems/`, `API/`, `DataFormats/`)는 **KO의 JSON 시스템**을 기준으로 작성되었습니다.
현재 전투의 실제 핵심은 **KD의 ScriptableObject 시스템** (`TacticalBattleManager`, `GridManager`, `StatCalculator`, AP 코스트 등)입니다.

- **KO 맵 레이어 문서** (MapSystem, MapDataFormat, TileDefinition, ObjectDefinition) → 비교적 정확
- **전투 레이어 문서** (BattleSystem, BattleScript API의 턴/이니셔티브 부분) → KD 아키텍처 미반영, 참고 시 주의
- **최신 전투 구현 상세** → [구현 보고서](IMPLEMENTATION_REPORT.md) 참조

---

*Horn Dancheong · 젬잼 게임잼 출품작*
