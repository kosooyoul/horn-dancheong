using UnityEditor;
using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// Play 모드에서 DialogueDisplayer의 기능을 에디터 인스펙터 상에서 편리하게 검증하기 위한 커스텀 인스펙터 에디터 툴입니다.
    /// </summary>
    [CustomEditor(typeof(DialogueDisplayer))]
    public class DialogueDisplayerEditor : Editor
    {
        private int _testStartIndex = 1;

        public override void OnInspectorGUI()
        {
            // DialogueDisplayer 클래스의 기본 직렬화 변수들을 표시
            DrawDefaultInspector();

            DialogueDisplayer displayer = (DialogueDisplayer)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("▼ [SW] 대화 시스템 검증 에디터 툴", EditorStyles.boldLabel);

            // 유니티 재생(Play) 상태일 때만 테스트 기능 활성화
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 동작(대화 시작, 스킵 등)은 Unity 에디터가 Play 모드일 때만 실행 가능합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("1. 대화 수동 시작 테스트", EditorStyles.miniBoldLabel);
                _testStartIndex = EditorGUILayout.IntField("시작 인덱스 (Index)", _testStartIndex);

                // 다이얼로그 창이 이미 켜진 경우(대화가 진행 중인 경우) 시작 버튼을 비활성화하여 다시 활성화되지 않도록 함
                bool isAlreadyActive = displayer.IsDialogueActive;
                EditorGUI.BeginDisabledGroup(isAlreadyActive);
                if (GUILayout.Button("대화 시작 (Start Dialogue)"))
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowPanel(UIPanelType.Panel_Dialogue, _testStartIndex);
                    }
                    else
                    {
                        // UIManager가 씬에 없거나 초기화되지 않은 경우 직접 시작하는 Fallback 처리
                        displayer.StartDialogue(_testStartIndex);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (displayer.IsDialogueActive)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("2. 런타임 제어", EditorStyles.miniBoldLabel);

                    if (GUILayout.Button("다음 라인으로 (Next / Click Screen)"))
                    {
                        displayer.OnFrontPanelClicked();
                    }

                    if (GUILayout.Button("대화 강제 종료 (End Dialogue)"))
                    {
                        displayer.EndDialogue();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("현재 진행 중인 대화가 없습니다.", MessageType.None);
            }
        }
    }
}
