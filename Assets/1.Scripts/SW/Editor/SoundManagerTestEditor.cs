using UnityEditor;
using UnityEngine;

namespace HornDancheong.Seongwoo.Test
{
    /// <summary>
    /// SoundManagerTest 컴포넌트를 위한 커스텀 인스펙터 에디터 툴입니다.
    /// 플레이 모드 시 인스펙터 버튼을 눌러 손쉽게 사운드를 테스트할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(SoundManagerTest))]
    public class SoundManagerTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SoundManagerTest testScript = (SoundManagerTest)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("▼ [SW] 사운드 매니저 테스트 런타임 제어기", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 실행은 Unity 에디터가 Play 모드(재생 중)일 때만 작동합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("1. BGM 재생 (Play BGM)"))
                {
                    testScript.TestPlayBGM();
                }

                if (GUILayout.Button("2. BGM 정지 (Stop BGM)"))
                {
                    testScript.TestStopBGM();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("3. 단발성 SFX 재생 (Play SFX)"))
                {
                    testScript.TestPlaySFX();
                }

                if (GUILayout.Button("4. 동시 SFX 12개 연타 (Test Pooling Limit & Recycle)"))
                {
                    testScript.TestSFXPooledRecycling();
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
