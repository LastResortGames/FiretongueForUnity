﻿/**
 * Copyright (c) 2013 Level Up Labs, LLC,
 * Copyright (c) 2014 Last Resort Games, LLC
 *  
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class FiretongueManager : MonoBehaviour {

    private Firetongue tongue;
    private List<string> locales;
    public string text;
    public string currentFilePath;
    public string currentScene;
    public bool isFileGroups;

	// Use this for initialization
	void Start () {
        tongue = new Firetongue();
        tongue.init("en-US", currentScene, new Action(onFinish), true, false, currentFilePath);

        locales = tongue.locales;
        DontDestroyOnLoad(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    public void OnGUI()
    {
        GUI.Label(new Rect(10, Screen.height / 3, 1000, 500), text);
        if (GUI.Button(new Rect(10, 10, 150, 40), "Load from CSV"))
        {
            currentFilePath = "";
            currentScene = "";
            isFileGroups = false;
            tongue.init("en-US", currentScene, new Action(onFinish), true, false, currentFilePath);
        }
        if (GUI.Button(new Rect(170, 10, 150, 32), "Load from TSV"))
        {
            currentFilePath = "tsv/";
            currentScene = "";
            isFileGroups = false;
            tongue.init("en-US", currentScene, new Action(onFinish), true, false, currentFilePath);
        }
        if (GUI.Button(new Rect(330, 10, 200, 32), "Load FileGroups"))
        {
            currentFilePath = "FileGroups/";
            currentScene = "scene1";
            isFileGroups = true;
            tongue.init("en-US", currentScene, new Action(onFinish), true, false, currentFilePath);
        }
        

        if (tongue != null && locales != null)
        {
            for (int i = 0; i < locales.Count; i++)
            {
                if (GUI.Button(new Rect(10 + (i * 100), 50, 32, 32), tongue.getIcon(locales[i])))
                {
                    string locale = "";
                    if (i >= 0 && i < locales.Count)
                    {
                        locale = locales[i];
                        tongue.init(locale, currentScene, new Action(onFinish), true, false, currentFilePath);
                    }
                }
            }
            if (isFileGroups)
            {
                if (GUI.Button(new Rect(10, 80, 200, 32), "Load Scene 2"))
                {
                    tongue.LoadNewFileGroup("scene2");
                }
            }
        }

    }

    private void onFinish()
    {
        text = tongue.locale + "\n";
        text += tongue.get("$INSTRUCTIONS") + "\n\n";
        text += tongue.get("$HELLO_WORLD") + "\n";
        text += tongue.get("$TEST_STRING") + "\n";
        text += tongue.get("$LOOK_MORE_STRINGS") + "\n";
        text += tongue.get("$TEST1") + "\n";

        if (tongue.missing_files != null)
        {
            string str = tongue.get("$MISSING_FILES");
            str = Replace.flags(str, new List<string>() { "<X>" }, new List<string>() { "" + tongue.missing_files.Count });
            text += str + "\n";
            foreach (string file in tongue.missing_files)
            {
                text += "\t" + file + "\n";
            }
        }

        if (tongue.missing_flags != null)
        {
            Dictionary<string, List<string>> missing_flags = tongue.missing_flags;

            string miss_str = tongue.get("$MISSING_FLAGS");

            int count = 0;
            string flag_str = "";

            foreach (string key in missing_flags.Keys)
            {
                List<string> list = missing_flags[key];
                count += list.Count;
                foreach (string flag in list)
                {
                    flag_str += "\t" + flag + "\n";
                }
            }

            miss_str = Replace.flags(miss_str, new List<string>() { "<X>" }, new List<string>() { "" + count });
            text += miss_str + "\n";
            text += flag_str + "\n";
        }
    }
}
