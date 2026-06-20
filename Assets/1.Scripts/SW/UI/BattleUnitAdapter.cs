// using UnityEngine;
// using KD;

// namespace HornDancheong.Seongwoo.UI
// {
//     /// <summary>
//     /// KD.BattleUnit 모델을 ICharacterBattleInfo 인터페이스 형식으로 변환하여
//     /// 이니셔티브 UI에서 즉시 사용할 수 있도록 매핑해주는 어댑터 클래스입니다.
//     /// </summary>
//     public class BattleUnitAdapter : ICombatCharacterInfo
//     {
//         private readonly BattleUnit _unit;
//         private readonly bool _isPC;

//         public string Id => _unit.Data.unitId;
//         public string CharacterName => _unit.Data.unitName;
        
//         // 만약 Resources 폴더 경로가 필요하다면 사용 가능하나, 기본적으로 UnitData에 할당된 icon Sprite를 사용하도록 연동합니다.
//         public string PortraitPath => string.Empty;
//         public Sprite PortraitSprite => _unit.Data.icon;
        
//         public float CurrentHp => _unit.CurrentHP;
//         public float MaxHp => _unit.Stats.maxHP;
//         public bool IsPC => _isPC;
//         public int Initiative => _unit.Stats.initiative;

//         // ICombatCharacterInfo 구현
//         public float CurrentSp => _unit.CurrentSP;
//         public float MaxSp => _unit.Stats.maxSP;

//         public string ClassName
//         {
//             get
//             {
//                 if (_unit.Data == null) return "없음";
//                 switch (_unit.Data.role)
//                 {
//                     case UnitRole.Dealer: return "집행";
//                     case UnitRole.Healer: return "의관";
//                     case UnitRole.Tanker: return "금군";
//                     case UnitRole.Supporter: return "보조";
//                     default: return _unit.Data.role.ToString();
//                 }
//             }
//         }

//         public Sprite ClassIcon => null;

//         public string AttributeName
//         {
//             get
//             {
//                 if (_unit.Data == null) return "없음";
//                 switch (_unit.Data.attribute)
//                 {
//                     case UnitAttribute.Red: return "적";
//                     case UnitAttribute.Blue: return "청";
//                     case UnitAttribute.Yellow: return "황";
//                     case UnitAttribute.White: return "백";
//                     case UnitAttribute.Black: return "흑";
//                     default: return _unit.Data.attribute.ToString();
//                 }
//             }
//         }

//         public Sprite AttributeIcon => null;
//         public Sprite[] ActiveBuffDebuffs => System.Array.Empty<Sprite>();

//         public UnitData UnitData => _unit.Data;

//         public System.Collections.Generic.List<SkillData> Skills
//         {
//             get
//             {
//                 var list = new System.Collections.Generic.List<SkillData>();
//                 if (_unit.Data.uniqueSkill1 != null) list.Add(_unit.Data.uniqueSkill1);
//                 if (_unit.Data.uniqueSkill2 != null) list.Add(_unit.Data.uniqueSkill2);
//                 if (_unit.EquippedOptionalSkill != null) list.Add(_unit.EquippedOptionalSkill);
//                 return list;
//             }
//         }

//         // 원본 BattleUnit 객체 접근용 프로퍼티
//         public BattleUnit Unit => _unit;

//         public BattleUnitAdapter(BattleUnit unit, bool isPC)
//         {
//             _unit = unit;
//             _isPC = isPC;
//         }
//     }
// }
