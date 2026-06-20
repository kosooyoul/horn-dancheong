using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    public interface ICharacterBattleInfo
    {
        string Id { get; }
        string CharacterName { get; }
        string PortraitPath { get; }
        Sprite PortraitSprite { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
        bool IsPC { get; }
        int Initiative { get; }
    }
}
