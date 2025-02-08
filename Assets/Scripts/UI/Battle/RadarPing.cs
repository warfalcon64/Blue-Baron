using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarPing : MonoBehaviour
{

    public RectTransform blip;
    [Header("Radar Ping Attributes")]
    [SerializeField] private float fadeTime;

    private float fadeTimer;
    private Color color;
    private Image blipSprite;
    private ShipBase ship;

    private void Awake()
    {
        fadeTimer = 0f;
        color = new Color(1, 1f, 1, 1f);
        ship = null;
    }
    // Start is called before the first frame update
    void Start()
    {
        blipSprite = blip.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        fadeTimer += Time.deltaTime;

        color.a = Mathf.Lerp(fadeTime, 0f, fadeTimer / fadeTime);
        blipSprite.color = color;

        if (fadeTimer >= fadeTime)
        {
            Destroy(gameObject);
        }
    }

    public ShipBase GetShip()
    {
        return ship;
    }

    public void SetFadeTimer(float time)
    {
        fadeTimer = time;
    }

    public void SetFadeTime(float time)
    {
        fadeTime = time;
    }

    public void SetShip(ShipBase ship)
    {
        this.ship = ship;
    }
}
