﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsFunctions : MonoBehaviour {

    GameManager gm;
	// Use this for initialization
	void Start () {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
	}
	
    public void UpdateMaxDefenses(float value) {
        gm.MaxDefenses = (int)value;
    }
    
        gm.MaxBullets = (int)value;
    }
        gm.CountdownTime = (int)value;
    }
}