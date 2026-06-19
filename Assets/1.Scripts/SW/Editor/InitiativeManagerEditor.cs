using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// Play 모드에서 InitiativeManager의 기능을 비주얼적으로 검증하기 위한 커스텀 인스펙터 에디터 툴입니다.
    /// </summary>
    [CustomEditor(typeof(InitiativeManager))]
    public class InitiativeManagerEditor : Editor
    {
        // ICharacterBattleInfo의 검증용 가상 구현체
        private class MockCharacterInfo : ICharacterBattleInfo
        {
            public string Id { get; set; }
            public string CharacterName { get; set; }
            public string PortraitPath { get; set; }
            public Sprite PortraitSprite { get; set; }
            public float CurrentHp { get; set; }
            public float MaxHp { get; set; }
            public bool IsPC { get; set; }
            public int Initiative { get; set; }
        }

        private string _targetCharacterId = "PC_1";
        private float _modifyHpAmount = 15f;
        private readonly List<MockCharacterInfo> _mockList = new List<MockCharacterInfo>();

        // 부활 테스트용 입력 변수
        private string _reviveId = "NPC_2";
        private string _reviveName = "고블린 전사 B (부활)";
        private float _reviveHp = 100f;
        private int _reviveInitiative = 10;
        private bool _reviveIsPC = false;

        public override void OnInspectorGUI()
        {
            // 프리팹 할당 필드 등의 기본 변수들을 인스펙터 상단에 출력
            DrawDefaultInspector();

            InitiativeManager manager = (InitiativeManager)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("▼ [SW] 이니셔티브 검증 에디터 툴", EditorStyles.boldLabel);

            // 유니티 재생(Play) 상태일 때만 동작하도록 제약
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 동작(데이터 주입, 턴 교체, 체력 변경 등)은 Unity 에디터가 Play 모드일 때만 실행 가능합니다.", MessageType.Info);
                return;
            }

            // 1. 전투 진입 상태 테스트 (이니셔티브 정렬 생성)
            if (GUILayout.Button("가상 전투 시작 (PC 3개, NPC 3개 배치)"))
            {
                _mockList.Clear();
                
                // 가상 데이터 세팅 (우선권인 Initiative 수치를 다르게 부여하여 정렬을 유도)
                _mockList.Add(new MockCharacterInfo { Id = "PC_1", CharacterName = "전사 (PC)", CurrentHp = 120f, MaxHp = 120f, IsPC = true, Initiative = 12 });
                _mockList.Add(new MockCharacterInfo { Id = "PC_2", CharacterName = "마법사 (PC)", CurrentHp = 70f, MaxHp = 70f, IsPC = true, Initiative = 8 });
                _mockList.Add(new MockCharacterInfo { Id = "PC_3", CharacterName = "도적 (PC)", CurrentHp = 90f, MaxHp = 90f, IsPC = true, Initiative = 18 });
                
                _mockList.Add(new MockCharacterInfo { Id = "NPC_1", CharacterName = "고블린 졸개 A", CurrentHp = 40f, MaxHp = 40f, IsPC = false, Initiative = 5 });
                _mockList.Add(new MockCharacterInfo { Id = "NPC_2", CharacterName = "고블린 전사 B", CurrentHp = 100f, MaxHp = 100f, IsPC = false, Initiative = 10 });
                _mockList.Add(new MockCharacterInfo { Id = "NPC_3", CharacterName = "고블린 족장 C", CurrentHp = 250f, MaxHp = 250f, IsPC = false, Initiative = 25 });

                manager.InitializeBattleUI(_mockList);
                Debug.Log("[InitiativeManagerEditor] 6개의 가상 캐릭터 데이터가 우선권 순서로 정렬되어 생성되었습니다.");
            }

            EditorGUILayout.Space(8);

            // 2. 턴 순환 기능 테스트
            if (GUILayout.Button("턴 종료 및 다음 턴 순환 (NextTurn)"))
            {
                manager.NextTurn();
                Debug.Log("[InitiativeManagerEditor] NextTurn()을 실행했습니다. 맨 위의 패널이 맨 아래로 밀려납니다.");
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("실시간 캐릭터 상태 제어", EditorStyles.boldLabel);

            // 타겟 캐릭터 지정 (UI 딕셔너리 키로 작동)
            _targetCharacterId = EditorGUILayout.TextField("대상 ID", _targetCharacterId);

            // 3. 체력 실시간 변경 테스트
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("데미지 주기"))
            {
                var targetChar = _mockList.Find(m => m.Id == _targetCharacterId);
                if (targetChar != null)
                {
                    targetChar.CurrentHp = Mathf.Max(0f, targetChar.CurrentHp - _modifyHpAmount);
                    manager.UpdateCharacterHp(targetChar.Id, targetChar.CurrentHp, targetChar.MaxHp);
                    Debug.Log($"[InitiativeManagerEditor] {targetChar.CharacterName}에게 피해를 주었습니다. HP: {targetChar.CurrentHp}/{targetChar.MaxHp}");
                }
                else
                {
                    Debug.LogWarning($"[InitiativeManagerEditor] 가상 데이터 리스트에 ID '{_targetCharacterId}'가 없습니다.");
                }
            }

            if (GUILayout.Button("치유(힐) 하기"))
            {
                var targetChar = _mockList.Find(m => m.Id == _targetCharacterId);
                if (targetChar != null)
                {
                    targetChar.CurrentHp = Mathf.Min(targetChar.MaxHp, targetChar.CurrentHp + _modifyHpAmount);
                    manager.UpdateCharacterHp(targetChar.Id, targetChar.CurrentHp, targetChar.MaxHp);
                    Debug.Log($"[InitiativeManagerEditor] {targetChar.CharacterName}을 치유했습니다. HP: {targetChar.CurrentHp}/{targetChar.MaxHp}");
                }
                else
                {
                    Debug.LogWarning($"[InitiativeManagerEditor] 가상 데이터 리스트에 ID '{_targetCharacterId}'가 없습니다.");
                }
            }

            _modifyHpAmount = EditorGUILayout.FloatField("수치", _modifyHpAmount, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // 4. 사망 시 패널 삭제 테스트
            if (GUILayout.Button("캐릭터 사망 및 UI 제거 (RemoveCharacter)"))
            {
                manager.RemoveCharacter(_targetCharacterId);
                
                var targetChar = _mockList.Find(m => m.Id == _targetCharacterId);
                if (targetChar != null)
                {
                    _mockList.Remove(targetChar);
                }
                
                Debug.Log($"[InitiativeManagerEditor] ID '{_targetCharacterId}' 캐릭터를 트랙 UI에서 제거했습니다.");
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("캐릭터 추가 및 부활 테스트 (AddCharacter)", EditorStyles.boldLabel);

            _reviveId = EditorGUILayout.TextField("추가/부활 ID", _reviveId);
            _reviveName = EditorGUILayout.TextField("이름", _reviveName);
            _reviveHp = EditorGUILayout.FloatField("체력 수치", _reviveHp);
            _reviveInitiative = EditorGUILayout.IntField("우선권 (Initiative)", _reviveInitiative);
            _reviveIsPC = EditorGUILayout.Toggle("플레이어(PC) 여부", _reviveIsPC);

            if (GUILayout.Button("캐릭터 부활/추가 및 정렬 삽입 (AddCharacter)"))
            {
                var existing = _mockList.Find(m => m.Id == _reviveId);
                if (existing != null)
                {
                    existing.CharacterName = _reviveName;
                    existing.CurrentHp = _reviveHp;
                    existing.MaxHp = _reviveHp;
                    existing.Initiative = _reviveInitiative;
                    existing.IsPC = _reviveIsPC;
                    
                    manager.AddCharacter(existing);
                    Debug.Log($"[InitiativeManagerEditor] 기존 ID '{_reviveId}' 캐릭터가 갱신되어 정렬 위치에 재삽입(부활)되었습니다.");
                }
                else
                {
                    var newMock = new MockCharacterInfo
                    {
                        Id = _reviveId,
                        CharacterName = _reviveName,
                        CurrentHp = _reviveHp,
                        MaxHp = _reviveHp,
                        Initiative = _reviveInitiative,
                        IsPC = _reviveIsPC
                    };
                    _mockList.Add(newMock);
                    manager.AddCharacter(newMock);
                    Debug.Log($"[InitiativeManagerEditor] 신규 ID '{_reviveId}' 캐릭터가 추가 및 이니셔티브 순 정렬 삽입되었습니다.");
                }
            }
        }
    }
}
