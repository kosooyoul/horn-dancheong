using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    public class GameObjectActivateControllerButton : ButtonBase
    {
        [SerializeField] private GameObject _targetGameObject;
        [SerializeField] private bool _isEnableMode = true;

        protected override void Function()
        {
            if (_targetGameObject != null)
            {
                _targetGameObject.SetActive(_isEnableMode);
            }
        }
    }
}