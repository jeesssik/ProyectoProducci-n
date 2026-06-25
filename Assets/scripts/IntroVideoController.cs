using UnityEngine;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject gameplayObjects;

    void Start()
    {
        gameplayObjects.SetActive(false);

        videoPlayer.loopPointReached += EndReached;
    }

    void EndReached(VideoPlayer vp)
    {
        gameplayObjects.SetActive(true);
        gameObject.SetActive(false);
    }
}