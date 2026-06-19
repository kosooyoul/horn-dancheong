using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HornDancheong.Seongwoo.UI
{
    public class AppExitButton : ButtonBase
    {
        protected override void Function()
        {
#if UNITY_EDITOR
            // 유니티 에디터에서 플레이 모드 종료
            EditorApplication.isPlaying = false;
#else
            // 빌드된 게임(실행 파일) 종료
            Application.Quit();
#endif
        }
    }
}