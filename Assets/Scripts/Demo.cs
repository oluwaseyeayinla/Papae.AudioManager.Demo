using UnityEngine;
using System.Collections;

public class Demo : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        Debug.Log("Demo Start");
        AudioManager.PlayBGMFromResource("Piano");
	}

}
