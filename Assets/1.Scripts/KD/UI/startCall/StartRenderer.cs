using UnityEngine;
using UnityEngine.UI;

public class StartRenderer : MonoBehaviour
{
    [SerializeField] private TitleController titleController;
    [SerializeField] private RawImage rawImage;

    private void Start()
{
    if (titleController != null)
    {
        Debug.Log("[StartRenderer] TitleController.Play() 호출");
        titleController.Play();
    }
    else
    {
        Debug.LogError("[StartRenderer] titleController 레퍼런스가 없음!");
    }}
}
