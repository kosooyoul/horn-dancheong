using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// Play 모드에서 CombatInteractionUIManager의 기능을 비주얼적으로 검증하기 위한 커스텀 인스펙터 에디터 툴입니다.
    /// </summary>
    [CustomEditor(typeof(CombatInteractionUIManager))]
    public class CombatInteractionUIEditorTool : Editor, ICombatInteractionController
    {
        // ICombatCharacterInfo의 검증용 가상 구현체
        private class MockCombatCharacterInfo : ICombatCharacterInfo
        {
            public string Id { get; set; } = "Mock_PC";
            public string CharacterName { get; set; } = "가상 캐릭터";
            public string PortraitPath { get; set; } = string.Empty;
            public Sprite PortraitSprite { get; set; }
            public float CurrentHp { get; set; } = 100f;
            public float MaxHp { get; set; } = 100f;
            public bool IsPC { get; set; } = true;
            public int Initiative { get; set; } = 10;

            public float CurrentSp { get; set; } = 50f;
            public float MaxSp { get; set; } = 100f;
            public string ClassName { get; set; } = "Warrior";
            public Sprite ClassIcon { get; set; }
            public string AttributeName { get; set; } = "Fire";
            public Sprite AttributeIcon { get; set; }
            public Sprite[] ActiveBuffDebuffs { get; set; } = new Sprite[0];

            public KD.UnitData UnitData => null;
            public System.Collections.Generic.List<KD.SkillData> Skills => null;
        }

        // 입력 데이터
        private string _charName = "아군 전사 (PC)";
        private string _className = "Warrior";
        private string _attributeName = "Fire";
        private float _hpCurrent = 80f;
        private float _hpMax = 120f;
        private float _spCurrent = 30f;
        private float _spMax = 100f;

        // 스프라이트 할당용 필드 (테스트용)
        private Sprite _portraitSprite;
        private Sprite _classSprite;
        private Sprite _attributeSprite;
        
        // 가상 버프/디버프 아이콘 배열
        private List<Sprite> _buffDebuffSprites = new List<Sprite>();

        // 실제 캐릭터 데이터 테스트용 필드
        private KD.UnitData _realUnitData;

        // 컨트롤러 상태값
        public bool IsMoveModeActive { get; private set; }

        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 필드 출력 (UI 바인딩 목록)
            DrawDefaultInspector();

            CombatInteractionUIManager manager = (CombatInteractionUIManager)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("▼ [SW] 전투 조작 UI 검증 에디터 툴", EditorStyles.boldLabel);

            // 유니티 재생(Play) 상태일 때만 테스트 기능 제공
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("UI 시연 및 상태 주입 테스트는 Unity 에디터가 Play 모드일 때만 실행 가능합니다.", MessageType.Info);
                return;
            }

            // 가상 데이터 세팅 폼
            EditorGUILayout.LabelField("1. 가상 캐릭터 데이터 구성", EditorStyles.miniBoldLabel);
            _charName = EditorGUILayout.TextField("이름", _charName);
            _className = EditorGUILayout.TextField("클래스 (Warrior/Archer/Healer)", _className);
            _attributeName = EditorGUILayout.TextField("속성 이름", _attributeName);

            EditorGUILayout.Space(5);
            _hpCurrent = EditorGUILayout.FloatField("현재 체력 (HP)", _hpCurrent);
            _hpMax = EditorGUILayout.FloatField("최대 체력 (HP)", _hpMax);
            _spCurrent = EditorGUILayout.FloatField("현재 기력 (SP)", _spCurrent);
            _spMax = EditorGUILayout.FloatField("최대 기력 (SP)", _spMax);

            EditorGUILayout.Space(5);
            _portraitSprite = (Sprite)EditorGUILayout.ObjectField("초상화 Sprite", _portraitSprite, typeof(Sprite), false);
            _classSprite = (Sprite)EditorGUILayout.ObjectField("클래스 아이콘 Sprite", _classSprite, typeof(Sprite), false);
            _attributeSprite = (Sprite)EditorGUILayout.ObjectField("속성 아이콘 Sprite", _attributeSprite, typeof(Sprite), false);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("버프/디버프 아이콘 등록 (최대 7개)", EditorStyles.boldLabel);
            int buffCount = EditorGUILayout.IntSlider("버프 개수", _buffDebuffSprites.Count, 0, 7);
            
            // 크기 조정
            while (_buffDebuffSprites.Count < buffCount) _buffDebuffSprites.Add(null);
            while (_buffDebuffSprites.Count > buffCount) _buffDebuffSprites.RemoveAt(_buffDebuffSprites.Count - 1);

            for (int i = 0; i < _buffDebuffSprites.Count; i++)
            {
                _buffDebuffSprites[i] = (Sprite)EditorGUILayout.ObjectField($"버프 #{i + 1} Sprite", _buffDebuffSprites[i], typeof(Sprite), false);
            }

            EditorGUILayout.Space(10);

            // 주입 실행 버튼
            if (GUILayout.Button("가상 캐릭터 UI 적용 (주입)"))
            {
                MockCombatCharacterInfo mockChar = new MockCombatCharacterInfo
                {
                    CharacterName = _charName,
                    ClassName = _className,
                    AttributeName = _attributeName,
                    CurrentHp = Mathf.Clamp(_hpCurrent, 0f, _hpMax),
                    MaxHp = _hpMax,
                    CurrentSp = Mathf.Clamp(_spCurrent, 0f, _spMax),
                    MaxSp = _spMax,
                    PortraitSprite = _portraitSprite,
                    ClassIcon = _classSprite,
                    AttributeIcon = _attributeSprite,
                    ActiveBuffDebuffs = _buffDebuffSprites.ToArray()
                };

                manager.Initialize(mockChar, this);
                Debug.Log($"[CombatInteractionUIEditorTool] 가상 캐릭터 '{_charName}' ({_className}) 데이터가 UI에 주입되었습니다.");
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("2. 실제 캐릭터 데이터로 테스트", EditorStyles.boldLabel);
            _realUnitData = (KD.UnitData)EditorGUILayout.ObjectField("실제 UnitData 에셋", _realUnitData, typeof(KD.UnitData), false);

            if (GUILayout.Button("실제 캐릭터 UI 적용 (주입)"))
            {
                if (_realUnitData == null)
                {
                    Debug.LogError("[CombatInteractionUIEditorTool] 적용할 실제 UnitData가 지정되지 않았습니다.");
                }
                else
                {
                    // KD.BattleUnit 생성 (플레이어 팀=0, 시작 좌표=(0,0))
                    KD.BattleUnit battleUnit = new KD.BattleUnit(_realUnitData, 0, Vector2Int.zero);
                    BattleUnitAdapter adapter = new BattleUnitAdapter(battleUnit, true);
                    manager.Initialize(adapter, this);
                    Debug.Log($"[CombatInteractionUIEditorTool] 실제 캐릭터 '{_realUnitData.unitName}' ({_realUnitData.role}) 데이터가 UI에 주입되었습니다.");
                }
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("3. 컨트롤러 피드백 상태 모니터링", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"이동 모드 상태 (IsMoveModeActive): {IsMoveModeActive}");
        }

        // ── ICombatInteractionController 인터페이스 구현 ──

        public void SetMoveMode(bool active)
        {
            IsMoveModeActive = active;
            Debug.Log($"[MockController] SetMoveMode({active}) 가 호출되었습니다. (이동 상태 변화)");
            Repaint();
        }

        public void ExecuteSkill(string skillId)
        {
            Debug.Log($"[MockController] ExecuteSkill(스킬 ID: {skillId}) 이(가) 실행되었습니다! (행동 수행)");
        }

        public void ExecuteWait()
        {
            Debug.Log("[MockController] ExecuteWait() 가 호출되었습니다! (대기/턴 종료)");
        }


    }
}
