using UnityEngine;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject canvasVideo;

    private void Start()
    {
        videoPlayer.loopPointReached += VideoFinished;
    }

    private void VideoFinished(VideoPlayer vp)
    {
        Debug.Log("VIDEO TERMINADO");

        canvasVideo.SetActive(false);

        // Opcional
        videoPlayer.Stop();
    }
}