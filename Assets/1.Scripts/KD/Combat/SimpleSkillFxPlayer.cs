using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 스킬 연출 재생기
    // SkillData.fxType에 따라 SlashBeam / Projectile / Pillar / AreaRise /
    // DroneDeliver / Lightning / DustShockwave 연출을 분기 처리한다.
    //
    // 호출부(SkillActionRunner)는 PlaySkillFx() 코루틴 하나만 yield return하면 된다.
    // onImpact 콜백이 실제 데미지/효과 적용 타이밍을 결정한다.
    [RequireComponent(typeof(AudioSource))]
    public class SimpleSkillFxPlayer : MonoBehaviour
    {
        [SerializeField] private GridManager        gridManager;
        [SerializeField] private CameraShakeManager cameraShake;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        // ── 메인 진입점 ──────────────────────────────────────────────────────

        public IEnumerator PlaySkillFx(
            BattleUnit       caster,
            List<BattleUnit> targets,
            List<Vector2Int> targetTiles,
            SkillData        skill,
            Action           onImpact)
        {
            if (skill == null || skill.fxType == SkillFxType.None)
            {
                onImpact?.Invoke();
                yield break;
            }

            switch (skill.fxType)
            {
                case SkillFxType.SlashBeam:
                    yield return StartCoroutine(PlaySlashBeam(caster, targets, skill, onImpact));
                    break;

                case SkillFxType.Projectile:
                    yield return StartCoroutine(PlayProjectile(caster, targets, skill, onImpact));
                    break;

                case SkillFxType.Pillar:
                    yield return StartCoroutine(PlayPillar(caster, targetTiles, skill, onImpact));
                    break;

                case SkillFxType.AreaRise:
                    yield return StartCoroutine(PlayAreaRise(caster, targetTiles, skill, onImpact));
                    break;

                case SkillFxType.DroneDeliver:
                    yield return StartCoroutine(PlayDroneDeliver(caster, targets, skill, onImpact));
                    break;

                case SkillFxType.Lightning:
                    yield return StartCoroutine(PlayLightning(caster, targets, skill, onImpact));
                    break;

                case SkillFxType.DustShockwave:
                    yield return StartCoroutine(PlayDustShockwave(caster, skill, onImpact));
                    break;

                default:
                    onImpact?.Invoke();
                    break;
            }
        }

        // ── SlashBeam ────────────────────────────────────────────────────────
        private IEnumerator PlaySlashBeam(
            BattleUnit       caster,
            List<BattleUnit> targets,
            SkillData        skill,
            Action           onImpact)
        {
            Vector3    casterPos = GetUnitWorldPos(caster);
            Quaternion casterRot = GetCasterFacingRotation(caster);

            SpawnVfx(skill.castVfxPrefab, casterPos, skill.vfxLifetime, casterRot);
            PlaySfx(skill.castSfx);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            onImpact?.Invoke();
            PlaySfx(skill.impactSfx);

            if (skill.impactVfxPrefab != null)
            {
                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    SpawnVfx(skill.impactVfxPrefab, GetUnitWorldPos(target), skill.vfxLifetime);
                }
            }

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── Projectile ───────────────────────────────────────────────────────
        private IEnumerator PlayProjectile(
            BattleUnit       caster,
            List<BattleUnit> targets,
            SkillData        skill,
            Action           onImpact)
        {
            Vector3    casterPos = GetUnitWorldPos(caster);
            Quaternion casterRot = GetCasterFacingRotation(caster);

            PlaySfx(skill.castSfx);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            if (skill.projectilePrefab != null && targets.Count > 0)
            {
                float maxDist = 0f;
                Debug.Log($"[Proj] Caster grid={caster.CurrentTilePos} world={casterPos:F2}");

                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    Vector3 targetPos = GetUnitWorldPos(target);
                    Debug.Log($"[Proj] Target grid={target.CurrentTilePos} world={targetPos:F2}");
                    float   dist      = Vector3.Distance(casterPos, targetPos);
                    if (dist > maxDist) maxDist = dist;

                    // 스폰 즉시 타겟 방향을 바라보도록 — 첫 프레임 파티클이 대각선으로 튀는 현상 방지
                    Vector3    toDir     = targetPos - casterPos;
                    Quaternion spawnRot  = toDir.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(-toDir.normalized)
                        : casterRot;

                    GameObject proj = Instantiate(skill.projectilePrefab, casterPos, spawnRot);
                    StartCoroutine(MoveProjectile(proj, casterPos, targetPos, skill));
                }

                PlaySfx(skill.projectileSfx);

                float travelTime = maxDist / Mathf.Max(0.1f, skill.projectileSpeed);
                yield return new WaitForSeconds(travelTime);
            }

            if (skill.impactDelay > 0f)
                yield return new WaitForSeconds(skill.impactDelay);

            onImpact?.Invoke();
            PlaySfx(skill.impactSfx);

            if (skill.impactVfxPrefab != null)
            {
                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    SpawnVfx(skill.impactVfxPrefab, GetUnitWorldPos(target), skill.vfxLifetime);
                }
            }

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── Pillar ───────────────────────────────────────────────────────────
        private IEnumerator PlayPillar(
            BattleUnit       caster,
            List<Vector2Int> targetTiles,
            SkillData        skill,
            Action           onImpact)
        {
            PlaySfx(skill.castSfx);
            SpawnCastVfxFixed(skill.castVfxPrefab, GetUnitWorldPos(caster), skill.vfxLifetime);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            onImpact?.Invoke();
            SpawnAreaVfxScaled(skill.areaVfxPrefab, targetTiles, skill.vfxLifetime);
            PlaySfx(skill.impactSfx);

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── AreaRise ─────────────────────────────────────────────────────────
        private IEnumerator PlayAreaRise(
            BattleUnit       caster,
            List<Vector2Int> targetTiles,
            SkillData        skill,
            Action           onImpact)
        {
            PlaySfx(skill.castSfx);
            SpawnCastVfxFixed(skill.castVfxPrefab, GetUnitWorldPos(caster), skill.vfxLifetime);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            onImpact?.Invoke();
            SpawnAreaVfxScaled(skill.areaVfxPrefab, targetTiles, skill.vfxLifetime);
            PlaySfx(skill.impactSfx);

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── DroneDeliver ─────────────────────────────────────────────────────
        private IEnumerator PlayDroneDeliver(
            BattleUnit       caster,
            List<BattleUnit> targets,
            SkillData        skill,
            Action           onImpact)
        {
            Vector3    casterPos  = GetUnitWorldPos(caster);
            Quaternion casterRot  = GetCasterFacingRotation(caster);
            Vector3    droneSpawn = casterPos + new Vector3(0f, 3f, 0f);

            GameObject drone = null;
            if (skill.castVfxPrefab != null)
                drone = Instantiate(skill.castVfxPrefab, droneSpawn, casterRot);
            PlaySfx(skill.castSfx);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            if (skill.projectilePrefab != null && targets.Count > 0)
            {
                PlaySfx(skill.projectileSfx);
                float maxTravelTime = 0f;

                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    Vector3 targetPos  = GetUnitWorldPos(target);
                    float   dist       = Vector3.Distance(droneSpawn, targetPos);
                    float   travelTime = dist / Mathf.Max(0.1f, skill.projectileSpeed);
                    if (travelTime > maxTravelTime) maxTravelTime = travelTime;

                    GameObject item = Instantiate(skill.projectilePrefab, droneSpawn, casterRot);
                    StartCoroutine(MoveProjectile(item, droneSpawn, targetPos, skill));
                }

                yield return new WaitForSeconds(maxTravelTime);
            }

            if (skill.impactDelay > 0f)
                yield return new WaitForSeconds(skill.impactDelay);

            onImpact?.Invoke();
            PlaySfx(skill.impactSfx);

            if (skill.impactVfxPrefab != null)
            {
                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    SpawnVfx(skill.impactVfxPrefab, GetUnitWorldPos(target), skill.vfxLifetime);
                }
            }

            if (drone != null)
                Destroy(drone, skill.endDelay + 0.1f);

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── Lightning ────────────────────────────────────────────────────────
        private IEnumerator PlayLightning(
            BattleUnit       caster,
            List<BattleUnit> targets,
            SkillData        skill,
            Action           onImpact)
        {
            PlaySfx(skill.castSfx);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            if (skill.castVfxPrefab != null)
            {
                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    SpawnVfx(skill.castVfxPrefab, GetUnitWorldPos(target), skill.vfxLifetime);
                }
            }

            if (skill.impactDelay > 0f)
                yield return new WaitForSeconds(skill.impactDelay);

            onImpact?.Invoke();
            PlaySfx(skill.impactSfx);

            if (skill.impactVfxPrefab != null)
            {
                foreach (BattleUnit target in targets)
                {
                    if (target == null) continue;
                    SpawnVfx(skill.impactVfxPrefab, GetUnitWorldPos(target), skill.vfxLifetime);
                }
            }

            if (skill.useCameraShake && cameraShake != null)
                cameraShake.Shake(skill.shakeDuration, skill.shakeMagnitude);

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── DustShockwave ────────────────────────────────────────────────────
        private IEnumerator PlayDustShockwave(
            BattleUnit caster,
            SkillData  skill,
            Action     onImpact)
        {
            Vector3    casterPos = GetUnitWorldPos(caster);
            Quaternion casterRot = GetCasterFacingRotation(caster);

            SpawnVfx(skill.castVfxPrefab, casterPos, skill.vfxLifetime, casterRot);
            PlaySfx(skill.castSfx);

            if (skill.useCameraShake && cameraShake != null)
                cameraShake.Shake(skill.shakeDuration, skill.shakeMagnitude);

            if (skill.castDelay > 0f)
                yield return new WaitForSeconds(skill.castDelay);

            onImpact?.Invoke();
            PlaySfx(skill.impactSfx);

            if (skill.endDelay > 0f)
                yield return new WaitForSeconds(skill.endDelay);
        }

        // ── 투사체 이동 ──────────────────────────────────────────────────────

        private IEnumerator MoveProjectile(
            GameObject proj,
            Vector3    from,
            Vector3    to,
            SkillData  skill)
        {
            float dist     = Vector3.Distance(from, to);
            float duration = dist / Mathf.Max(0.1f, skill.projectileSpeed);
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 pos = Vector3.Lerp(from, to, t);
                if (skill.projectileArc)
                    pos.y += Mathf.Sin(t * Mathf.PI) * skill.projectileArcHeight;

                proj.transform.position = pos;

                Vector3 dir = to - from;
                if (dir.sqrMagnitude > 0.001f)
                    proj.transform.rotation = Quaternion.LookRotation(-dir.normalized);

                yield return null;
            }

            proj.transform.position = to;
            Destroy(proj);
        }

        // ── 회전 계산 (8방향 UnitMover 기반) ────────────────────────────────
        // UnitMover.forward → atan2(x,z) 각도 → 45° 단위 스냅 → 커스텀 euler.y 매핑
        //
        // UnitMover 기준 각도 → 투사체 euler.y
        //   0°  (+Z, 위)       → 90
        //   45° (우상)         → 22.5
        //   90° (+X, 오른쪽)   → -45
        //   135°(우하)         → -22.5
        //   180°(-Z, 아래)     → 0
        //   225°(좌하)         → 22.5
        //   270°(-X, 왼쪽)     → 45
        //   315°(좌상)         → 67.5
        private Quaternion GetCasterFacingRotation(BattleUnit caster)
        {
            if (gridManager == null) return Quaternion.identity;

            UnitMover mover = gridManager.GetUnitMover(caster);
            if (mover == null) return Quaternion.identity;

            Vector3 forward = mover.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) return Quaternion.identity;
            forward.Normalize();

            float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            // 45° 단위로 스냅
            int snapped = Mathf.RoundToInt(angle / 45f) * 45;
            if (snapped >= 360) snapped -= 360;

            float yRot;
            switch (snapped)
            {
                case 0:   yRot =  90f;   break; // 위
                case 45:  yRot =  22.5f; break; // 우상
                case 90:  yRot = -45f;   break; // 오른쪽
                case 135: yRot = -22.5f; break; // 우하
                case 180: yRot =   0f;   break; // 아래
                case 225: yRot =  22.5f; break; // 좌하
                case 270: yRot =  45f;   break; // 왼쪽
                case 315: yRot =  67.5f; break; // 좌상
                default:  yRot =   0f;   break;
            }

            return Quaternion.Euler(0f, yRot, 0f);
        }

        // ── 유틸 ─────────────────────────────────────────────────────────────

        private Vector3 GetUnitWorldPos(BattleUnit unit)
        {
            if (gridManager == null)
                return new Vector3(unit.CurrentTilePos.x, 0.3f, unit.CurrentTilePos.y);
            Vector3 pos = gridManager.GridToWorld(unit.CurrentTilePos);
            pos.y = 0.3f;
            return pos;
        }

        private Vector3 GetTileWorldPos(Vector2Int tile)
        {
            if (gridManager == null)
                return new Vector3(tile.x, 0.1f, tile.y);
            Vector3 pos = gridManager.GridToWorld(tile);
            pos.y += 0.1f;
            return pos;
        }

        // castVfxPrefab 전용 — 크기 고정, caster 위 y+1에 1개만 스폰
        private void SpawnCastVfxFixed(GameObject prefab, Vector3 casterWorldPos, float lifetime)
        {
            if (prefab == null) return;
            Vector3 pos = casterWorldPos;
            pos.y += 1f;
            GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
            Destroy(vfx, lifetime);
        }

        // areaVfxPrefab 전용 — 타일 범위 중앙에 1개, 전체 타일 크기만큼 Scale 적용
        private void SpawnAreaVfxScaled(GameObject prefab, List<Vector2Int> tiles, float lifetime)
        {
            if (prefab == null || tiles == null || tiles.Count == 0) return;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;
            Vector3 centerWorld = Vector3.zero;

            foreach (Vector2Int t in tiles)
            {
                if (t.x < minX) minX = t.x;
                if (t.x > maxX) maxX = t.x;
                if (t.y < minZ) minZ = t.y;
                if (t.y > maxZ) maxZ = t.y;
                centerWorld += GetTileWorldPos(t);
            }
            centerWorld /= tiles.Count;

            int widthTiles = maxX - minX + 1;
            int depthTiles = maxZ - minZ + 1;

            GameObject vfx = Instantiate(prefab, centerWorld, Quaternion.identity);
            Vector3 s = vfx.transform.localScale;
            vfx.transform.localScale = new Vector3(s.x * widthTiles, s.y, s.z * depthTiles);
            Destroy(vfx, lifetime);
        }

        private void SpawnVfx(GameObject prefab, Vector3 pos, float lifetime)
        {
            SpawnVfx(prefab, pos, lifetime, Quaternion.identity);
        }

        private void SpawnVfx(GameObject prefab, Vector3 pos, float lifetime, Quaternion rotation)
        {
            if (prefab == null) return;
            GameObject vfx = Instantiate(prefab, pos, rotation);
            Destroy(vfx, lifetime);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }
    }
}
