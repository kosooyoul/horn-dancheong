using UnityEngine;
using UnityEngine.UI;

namespace HornDancheong.Seongwoo.UI
{
    [RequireComponent(typeof(Button))]
    public abstract class ButtonBase : MonoBehaviour
    {
        private Button _button;

        protected virtual void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Function);
        }

        protected abstract void Function();
    }
}