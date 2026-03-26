using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IceSushiTutorialContent : TutorialContent
{
    [SerializeField] private Image sushiImage;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private ParticleSystem iceBreakVfxPrefab;

    private int currentIndex = 0;
    private bool isPlaying = false;
    private Sequence loopSequence;

    public override void PlayAnimation()
    {
        isPlaying = true;
        currentIndex = 0;
        PlayLoop();
    }

    private void PlayLoop()
    {
        if (!isPlaying) return;

        sushiImage.gameObject.SetActive(true);
        sushiImage.sprite = sprites[currentIndex];

        loopSequence = DOTween.Sequence();
        loopSequence.AppendInterval(1f);
        loopSequence.OnComplete(() =>
        {
            if (!isPlaying) return;

            currentIndex++;

            if (currentIndex >= sprites.Length)
            {
                sushiImage.gameObject.SetActive(false);

                /*if (isPlaying && iceBreakVfxPrefab != null)
                {
                    var vfx = Instantiate(iceBreakVfxPrefab, sushiImage.transform.position, Quaternion.identity);
                    vfx.Play();
                    Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
                }*/

                loopSequence = DOTween.Sequence();
                loopSequence.AppendInterval(1f);
                loopSequence.OnComplete(() =>
                {
                    if (!isPlaying) return;
                    currentIndex = 0;
                    PlayLoop();
                });
            }
            else
            {
                PlayLoop();
            }
        });
    }

    public override void StopAnimation()
    {
        isPlaying = false;
        loopSequence?.Kill();
        loopSequence = null;
        sushiImage.gameObject.SetActive(true);
        if (sprites.Length > 0)
            sushiImage.sprite = sprites[0];
    }
}