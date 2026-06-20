using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 스킬 선택 중 마우스 위치에 따라 범위 하이라이트를 갱신
    // 마우스 타일이 바뀌었을 때만 범위 재계산 (최적화)
    //
    // 생성 예 (BattleManager 등에서):
    //   var query   = new CombatGridQuery(isValidTile, isBlockedForMove, isBlockedForSkillRay);
    //   var preview = new SkillRangePreview(query,
    //       tiles => gridManager.HighlightSkillTiles(tiles),
    //       ()    => gridManager.ClearHighlight());
    //
    // hover: preview.Show(caster, skill, hoveredTilePos)
    // click: if (preview.Contains(clickedTilePos)) { ... }
    // 해제:  preview.Clear()
    public class SkillRangePreview
    {
        private readonly CombatGridQuery          _query;
        private readonly Action<List<Vector2Int>> _onHighlight;
        private readonly Action                   _onClear;

        private BattleUnit       _caster;
        private SkillData        _skill;
        private Vector2Int       _lastMouseTile;
        private List<Vector2Int> _currentRange = new List<Vector2Int>();
        public IReadOnlyList<Vector2Int> CurrentRange => _currentRange;

        public SkillRangePreview(
            CombatGridQuery query,
            Action<List<Vector2Int>> highlightCallback,
            Action clearCallback)
        {
            _query       = query;
            _onHighlight = highlightCallback;
            _onClear     = clearCallback;
        }

        public void Show(BattleUnit caster, SkillData skill, Vector2Int mouseTile)
        {
            bool changed = _caster != caster || _skill != skill || _lastMouseTile != mouseTile;
            if (!changed) return;

            _caster        = caster;
            _skill         = skill;
            _lastMouseTile = mouseTile;

            _currentRange = _query.GetSkillRange(caster, skill, mouseTile);
            _onHighlight?.Invoke(_currentRange);
        }

        // 클릭한 타일이 현재 스킬 범위 안에 있는지 검사
        public bool Contains(Vector2Int tilePos) => _currentRange.Contains(tilePos);

        public void Clear()
        {
            _caster       = null;
            _skill        = null;
            _currentRange.Clear();
            _onClear?.Invoke();
        }
    }
}
