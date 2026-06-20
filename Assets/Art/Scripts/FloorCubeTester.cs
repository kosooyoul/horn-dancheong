using UnityEngine;
using UnityEngine.InputSystem;

public class FloorCubeTester : MonoBehaviour
{
    [SerializeField]
    private FloorCubeStater stater;

    private void Update()
    {
        for (int i = 1; i <= 5; i++)
        {
            Key key = (Key)((int)Key.Digit1 + (i - 1));

            if (Keyboard.current[key].wasPressedThisFrame)
            {
                stater.ChangeSafetyType(
                    (SafetyType)(i - 1));
            }
        }
    }
}