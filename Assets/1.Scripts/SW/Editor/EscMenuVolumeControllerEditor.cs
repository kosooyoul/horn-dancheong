using UnityEditor;
using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// EscMenuVolumeController 컴포넌트를 위한 커스텀 인스펙터 에디터 툴입니다.
    /// 플레이 모드 시 인스펙터 버튼을 눌러 슬라이더 설정 도중 손쉽게 사운드를 테스트할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(EscMenuVolumeController))]
    public class EscMenuVolumeControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 표시
            DrawDefaultInspector();

            EscMenuVolumeController controller = (EscMenuVolumeController)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("▼ [SW] 볼륨 테스트용 디버그 컨트롤러", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 재생은 Unity 에디터가 Play 모드(재생 중)일 때만 작동합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("디버그 BGM 재생 (Play BGM)"))
                {
                    controller.DebugPlayBGM();
                }

                if (GUILayout.Button("디버그 BGM 정지 (Stop BGM)"))
                {
                    controller.DebugStopBGM();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("디버그 SFX 재생 (Play SFX)"))
                {
                    controller.DebugPlaySFX();
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
