using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SliderFillerScript : MonoBehaviour {

    public float hidder;
    Slider slider;
    GameObject filler;
    float sliderValue = 0;
    bool changed = false;

	// Use this for initialization
	void Start ()
    {
        slider = GetComponent<Slider>();
        if (sliderValue <= 0.015f)
        {
            filler = slider.fillRect.gameObject;
            filler.SetActive(false);
        }
        else
        {
            filler = slider.fillRect.gameObject;
            filler.SetActive(true);
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
        
        if (slider.value != sliderValue)
        {
            sliderValue = slider.value;
            changed = true;
        }

        if (changed)
        {
            if (sliderValue <= hidder)
            {
                filler = slider.fillRect.gameObject;
                filler.SetActive(false);
            }
            else
            {
                filler = slider.fillRect.gameObject;
                filler.SetActive(true);
            }

            changed = false;
        }
	}
}
