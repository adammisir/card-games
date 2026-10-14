using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class Dice : MonoBehaviour
{
    public Image diceImage;
    public Sprite[] diceSprites;
    public float jumpHeight = 300f;  // Inspector'dan ayarla
  

    private bool hasRest = false;
    private Vector2 restPosition;
    private Vector3 restScale;

    public IEnumerator RollAnimation(System.Action<int> onFinished)
    {
        int result = Random.Range(1, diceSprites.Length + 1);
        RectTransform rect = diceImage.rectTransform;

        // Dinlenme konumunu ve boyutunu ilk atışta, animasyon başlamadan kaydet
        if (!hasRest)
        {
            restPosition = rect.anchoredPosition;
            restScale = rect.localScale;
            hasRest = true;
        }

        rect.DOKill();

        // Yukarı fırlat + büyü + dön
        rect.DOAnchorPosY(restPosition.y + jumpHeight, 0.4f).SetEase(Ease.OutQuad);
      
        rect.DORotate(new Vector3(0, 0, 360), 0.4f, RotateMode.FastBeyond360);

        // Havada hızlı sprite değişimi (8 x 0.05 = 0.4 sn, yükselişle aynı süre)
        for (int i = 0; i < 8; i++)
        {
            diceImage.sprite = diceSprites[Random.Range(0, diceSprites.Length)];
            yield return new WaitForSeconds(0.05f);
        }

        // Aşağı in
        rect.DOAnchorPosY(restPosition.y, 0.25f).SetEase(Ease.InQuad);
      
        yield return new WaitForSeconds(0.25f);

        // Tam yerine oturt
        rect.anchoredPosition = restPosition;
        rect.localScale = restScale;
        rect.localRotation = Quaternion.identity;

        diceImage.sprite = diceSprites[result - 1];
        onFinished?.Invoke(result);
    }
}