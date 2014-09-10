/**
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

public class Firetongue
{
    
    public static string default_locale = "en-US";
        
    //All of the game's localization data
    private Dictionary<string, Dictionary<string, string>> _index_data;
    
    //All of the locale entries
    private Dictionary<string, XmlNode> _index_locales;

    //All of the text notations
    private Dictionary<string, string> _index_notes;

    //All of the icons from various languages
    private Dictionary<string, Texture2D> _index_icons;

    //Any custom images loaded
    private Dictionary<string, Texture2D> _index_images;

    //Font replacement rules
    private Dictionary<string, XmlNode> _index_font;

    private bool _loaded = false;    

    private Action _callback_finished;

    /// <summary>
    /// List of file nodes to be loaded at start
    /// </summary>
    private List<XmlNode> _list_files;

    /// <summary>
    /// List of file nodes to be loaded based on current scene.
    /// </summary>
    private Dictionary<string, List<XmlNode>> _unique_list_files;

    private string _group_name;
    private int _files_loaded = 0;

    private int _safety_bit = 0;

    private bool _check_missing = false;
    private bool _replace_missing = false;
    private Dictionary<string, List<string>> _missing_flags;
    private List<string> _missing_files;

    private string _directory = "";


    public Firetongue()
    {
        //does nothing
    }

    public void clear(bool hard)
    {
        clearData(hard);
    }

    public bool isLoaded
    {
        get { return _loaded; }
    }


    public string locale;

    public List<string> locales
    {
        get
        {
            List<string> arr = new List<string>();
            foreach (string key in _index_locales.Keys)
            {
                arr.Add(key);
            }
            return arr;
        }
    }

    public List<string> missing_files
    {
        get
        {
            return _missing_files;

        }
    }

    public Dictionary<string, List<string>> missing_flags
    {
        get
        {
            return _missing_flags;
        }
    }
    

    /// <summary>
    /// Initialize the localization structure
    /// </summary>
    /// <param name="locale_">desired locale string, ie, "en-US"</param>
    /// <param name="finished_">callback for when it's done loading stuff</param>
    /// <param name="check_missing_">if true, compares against default locale for missing files/flags</param>
    /// <param name="replace_missing_">if true, replaces any missing files & flags with default locale values</param>
    /// <param name="directory_">Alternate directory to look for locale. Otherwise, assumes "Resources/locales". Must be relative to the Resources folder</param>
    public void init(string locale_, string group_ = "", Action finished_ = null, bool check_missing_ = false, bool replace_missing_ = false, string directory_ = "")
    {
        Debug.Log("LocaleData.init(" + locale_ + "," + finished_ + "," + check_missing_ + "," + replace_missing_ +"," +directory_+")");
       

        locale = localeFormat(locale_);
        _directory = directory_;

        if (_loaded)
        {
            clearData();	//if we have an existing locale already loaded, clear it out first
        }

        _callback_finished = finished_;
        _group_name = group_;
        _check_missing = false;
        _replace_missing = false;

        if (locale != default_locale)
        {
            _check_missing = check_missing_;
            _replace_missing = replace_missing_;
        }

        if (_check_missing)
        {
            _missing_files = new List<String>();
            _missing_flags = new Dictionary<string, List<string>>();
        }

        startLoad();
    }

    public void LoadNewFileGroup(string group_ = "")
    {
        _files_loaded -= _unique_list_files[_group_name].Count;
        
        if (_check_missing)
        {
            if (_missing_files == null)
            {
                _missing_files = new List<String>();
            }
            if (_missing_flags == null)
            {
                _missing_flags = new Dictionary<string, List<string>>();
            }
        }

        _group_name = group_;
        
        LoadFileGroup();
    }

    /*****LOOKUP FUNCTIONS*****/

    /// <summary>
    /// Provide a localization flag to get the proper text in the current locale.
    /// </summary>
    /// <param name="flag">a flag string, like "$HELLO"</param>
    /// <param name="context">a string specifying which index, in case you want that</param>
    /// <param name="safe">if true, suppresses errors and returns the untranslated flag if not found</param>
    /// <returns>the translated string</returns>
    public string get(string flag, string context = "data", bool safe = true)
    {
        string orig_flag = flag;
        flag = flag.ToUpper();

        if (context == "index")
        {
            return getIndexString(flag);
        }

        Dictionary<string, string> index;
        index = _index_data[context];
        if (index == null)
        {
            if (!safe)
            {
                throw new Exception("no localization context \"+data+\"");
            }
            else
            {
                return flag;
            }
        }

        string str = "";
        try
        {
            str = (index.ContainsKey(flag) ? index[flag] : "");

            if (str != null && str != "")
            {
                //Replace standard stuff:

                if (str.IndexOf("<RE>") == 0)
                {	//it's a redirect
                    bool done = false;
                    int failsafe = 0;
                    str = str.Replace("<RE>", "");	//cut out the redirect
                    while (!done)
                    {
                        string new_str = index[str];	//look it up again
                        if (new_str != null && new_str != "")
                        {	
                            //string exists
                            str = new_str;
                            if (str.IndexOf("<RE>") != 0)
                            {			
                                //if it's not ANOTHER redirect, stop looking
                                done = true;
                            }
                            else
                            {
                                //another redirect, keep looking
                                str = str.Replace("<RE>", "");
                            }
                        }
                        else
                        {				
                            //give up
                            done = true;
                            str = new_str;
                        }
                        failsafe++;
                        if (failsafe > 100)
                        {	
                            //max recursion: 100
                            done = true;
                            str = new_str;
                        }
                    }
                }

                string[] fix_a = new string[6] { "<N>", "<T>", "<LQ>", "<RQ>", "<C>", "<Q>" };
                string[] fix_b = new string[6] { "\n", "\t", "“", "”", ",", "\"" };

                if (str != null && str != "")
                {
                    for (int i = 0; i < fix_a.Length; i++)
                    {
                        while (str.IndexOf(fix_a[i]) != -1)
                        {
                            str = str.Replace(fix_a[i], fix_b[i]);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (safe)
            {
                return orig_flag;
            }
            else
            {
                throw new Exception("LocaleData.getText(" + flag + "," + context + ")");
            }
        }

        index = null;

        if (str == null)
        {
            if (safe)
            {
                return orig_flag;
            }
        }
        return str;
    }

    private string localeFormat(string str)
    {
        string[] arr = str.Split('-');
        if (arr != null && arr.Length == 2)
        {
            str = arr[0].ToLower() + "-" + arr[1].ToUpper();
        }
        return str;
    }

    public string getIndexString(string flag)
    {
        string str = "";

        string[] arr = null;
        if (flag.IndexOf(":") != 0)
        {
            arr = flag.Split(':');
            if (arr != null && arr.Length == 2)
            {
                string target_locale = localeFormat(arr[1]);
                string index_flag = arr[0];

                //get the locale entry for the target locale from the index
                XmlNode lindex = _index_locales[target_locale];

                XmlNode currLangNode = null;
                XmlNode nativeNode = null;

                if (lindex.HasChildNodes)
                {
                    foreach (XmlNode lNode in lindex.ChildNodes)
                    {		
                        //look through each label
                        if (lNode.Attributes["id"] != null)
                        {
                            string lnid = lNode.Attributes["id"].Value;
                            if (lnid.IndexOf(locale) != -1)
                            {	
                                //if it matches the CURRENT locale
                                currLangNode = lNode;			//labels in CURRENT language
                            }
                            if (lnid.IndexOf(target_locale) != -1)
                            {	
                                //if it matches its own NATIVE locale
                                nativeNode = lNode;						//labels in NATIVE language
                            }
                            if (currLangNode != null && nativeNode != null)
                            {
                                break;
                            }
                        }
                    }
                }
                string lang = "";
                string reg = "";
                switch (index_flag)
                {
                    case "$UI_LANGUAGE":	//return the localized word "LANGUAGE"
                        if (nativeNode.SelectSingleNode("child::ui") != null && nativeNode.SelectSingleNode("child::ui").Attributes["language"] != null)
                        {
                            return currLangNode.SelectSingleNode("child::ui").Attributes["language"].Value;
                        }
                        break;
                    case "$UI_REGION":		//return the localized word "REGION"
                        if (nativeNode.SelectSingleNode("child::ui") != null && nativeNode.SelectSingleNode("child::ui").Attributes["region"] != null)
                        {
                            return currLangNode.SelectSingleNode("child::ui").Attributes["region"].Value;
                        }
                        break;
                    case "$LANGUAGE":		//return the name of this language in CURRENT language
                        if (currLangNode != null && currLangNode.Attributes["language"] != null)
                        {
                            return currLangNode.Attributes["language"].Value;
                        }
                        break;
                    case "$LANGUAGE_NATIVE"://return the name of this language in NATIVE language
                        if (nativeNode != null && nativeNode.Attributes["language"] != null)
                        {
                            return nativeNode.Attributes["language"].Value;
                        }
                        break;
                    case "$REGION":			//return the name of this region in CURRENT language
                        if (currLangNode != null && nativeNode.Attributes["region"] != null)
                        {
                            return currLangNode.Attributes["region"].Value;
                        }
                        break;
                    case "$REGION_NATIVE":	//return the name of this region in NATIVE language
                        if (nativeNode != null && nativeNode.Attributes["region"] != null)
                        {
                            return nativeNode.Attributes["region"].Value;
                        }
                        break;
                    case "$LANGUAGE_BILINGUAL": //return the name of this language in both CURRENT and NATIVE, if different

                        string langnative = "";
                        if (nativeNode != null && nativeNode.Attributes["language"] != null)
                        {
                            langnative = nativeNode.Attributes["language"].Value;
                        }
                        if (currLangNode != null && currLangNode.Attributes["language"] != null)
                        {
                            lang = currLangNode.Attributes["language"].Value;
                        }
                        if (lang == langnative)
                        {
                            return lang;
                        }
                        else
                        {
                            return lang + " (" + langnative + ")";
                        }
                    case "$LANGUAGE(REGION)":	//return something like "Inglés (Estados Unidos)" in CURRENT language (ex: curr=spanish native=english)
                        lang = getIndexString("$LANGUAGE:" + target_locale);
                        reg = getIndexString("$REGION:" + target_locale);
                        return lang + "(" + reg + ")";
                    case "$LANGUAGE(REGION)_NATIVE": //return something like "English (United States)" in NATIVE language (ex: curr=spanish native=english)
                        lang = getIndexString("$LANGUAGE_NATIVE:" + target_locale);
                        reg = getIndexString("$REGION_NATIVE:" + target_locale);
                        return lang + "(" + reg + ")";
                }
            }
        }
        return flag;
    }


  
    /// <summary>
    /// Get the title of a localization note (locale menu purposes)
    /// </summary>
    /// <param name="locale"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public string getNoteTitle(string locale, string id)
    {
        try
        {
            string str = _index_notes[id + "_" + locale + "_title"];
            return Replace.flags(str, new List<string>() { "$N" }, new List<string>() { "\n" });
        }
        catch (Exception e)
        {
            return "ERROR:(" + id + ") for (" + locale + ") title not found";
        }
    }

    /// <summary>
    /// Get the body of a localization note (locale menu purposes)
    /// </summary>
    /// <param name="locale"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public string getNoteBody(string locale, string id)
    {
        try
        {
            string str = _index_notes[id + "_" + locale + "_body"];
            return Replace.flags(str, new List<string>() { "$N" }, new List<string>() { "\n" });
        }
        catch (Exception e)
        {
            return "ERROR:(" + id + ") for (" + locale + ") body not found";
        }
    }

    
    /// <summary>
    /// Get a locale (flag) icon
    /// </summary>
    /// <param name="locale_id"></param>
    /// <returns></returns>
    public Texture2D getIcon(string locale_id)
    {
        return _index_icons[locale_id];
    }

    public string getFont(string str)
    {
        string replace = "";
        try
        {
            XmlNode xml = _index_font[str];
            if (xml != null && xml.SelectSingleNode("child::font") != null)
            {
                replace = xml.SelectSingleNode("child::font").Attributes["replace"].Value;
            }
            if (replace == "" || replace == null)
            {
                replace = str;
            }
        }
        catch (Exception e)
        {
            replace = str;
        }
        return replace;
    }

    public int getFontSize(string str, int size)
    {
        int replace = size;
        try
        {
            XmlNode xml = _index_font[str];
            if (xml != null && xml.SelectSingleNode("child::font") != null && xml.SelectSingleNode("child::font").SelectSingleNode("child::size") != null)
            {
                foreach (XmlNode sizeNode in xml.SelectSingleNode("child::font").SelectSingleNode("child::size"))
                {
                    string sizestr = size.ToString();
                    if (sizeNode.Attributes["value"].Value == sizestr)
                    {
                        string replacestr = sizeNode.Attributes["replace"].Value;
                        if (replacestr != "" && replacestr != null)
                        {
                            replace = int.Parse(replacestr);
                            if (replace == 0)
                            {
                                replace = size;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            replace = size;
        }
        return replace;
    }


    /******PRIVATE FUNCTIONS******/

    private void startLoad()
    {

        //if we don't have a list of files, we need to process the index first
        if (_list_files == null)
        {
            loadIndex();
        }

        //we need new ones of these no matter what:
        _index_data = new Dictionary<String, Dictionary<String, String>>();
        _index_font = new Dictionary<String, XmlNode>();

        loadRootDirectory();		//make sure we can find our root directory
        
        //Load all the files in our list of files
        foreach (XmlNode fileNode in _list_files)
        {
            string value = "";
            if (fileNode.Attributes["value"] != null)
            {
                value = fileNode.Attributes["value"].Value;
            }
            if (value != "")
            {
                onLoadFile(loadFile(fileNode));

                if (_check_missing)
                {
                    onLoadFile(loadFile(fileNode, true),true);
                }
            }
            else
            {
                Debug.Log("ERROR: undefined file in localization index");
            }
        }

        LoadFileGroup();
    }

    private void LoadFileGroup()
    {
        //Load all the files in our list of files
        if (!string.IsNullOrEmpty(_group_name) && _unique_list_files[_group_name] != null && _unique_list_files.Count != 0)
        {
            foreach (XmlNode fileNode in _unique_list_files[_group_name])
            {
                string value = "";
                if (fileNode.Attributes["value"] != null)
                {
                    value = fileNode.Attributes["value"].Value;
                }
                if (value != "")
                {
                    //Check desired Locale
                    onLoadFile(loadFile(fileNode));

                    if (_check_missing)
                    {
                        //Checks default locales
                        onLoadFile(loadFile(fileNode, true),true);
                    }
                }
                else
                {
                    Debug.Log("ERROR: undefined file in localization index. Group Name: " + _group_name);
                }
            }
        }
    }

   
    /// <summary>
    /// Just a quick way to deep-copy a Fast object
    /// </summary>
    /// <param name="fast"></param>
    /// <returns></returns>
    private XmlNode copyFast(XmlNode fast)
    {
        XmlNode xml = new XmlDocument();
        xml = fast.Clone();
        return xml;
    }

    private Texture2D loadImage(string fname)
    {
        
        Texture2D img = null;
        try
        {
            if (_directory == "")
            {
                img = Resources.Load<Texture2D>("locales/" + fname);
            }
            else
            {
                img = Resources.Load<Texture2D>("tsv/locales/" + fname);
            }
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: loadImage(" + fname + ") failed");

            if (_check_missing) {
            	logMissingFile(fname);
            }
        }
        return img;
    }

    private string loadText(string fname)
    {
        string text = "";
        try
        {
            if (_directory == "")
            {
                text = Resources.Load<TextAsset>("locales/" + fname).text.Replace('\"', '\x22');
            }
            else
            {
                text = Resources.Load<TextAsset>(_directory + "locales/" + fname).text.Replace('\"', '\x22');                
            }
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: loadText(" + fname + ") failed" + "..." + text);

        }
        return text;
    }

    private void loadRootDirectory()
    {
        XmlNode firstFile = null;
        if (_list_files.Count != 0)
        {
            firstFile = _list_files[0];
        }
        else if (!string.IsNullOrEmpty(_group_name) && _unique_list_files[_group_name].Count != 0)
        {
            firstFile = _unique_list_files[_group_name][0];
        }
        string value = "";
        if (firstFile.Attributes["value"] != null)
        {
            value = firstFile.Attributes["value"].Value;
        }
        if (value != "")
        {
            string testText = loadText(locale + "/" + value);
            if (testText == "" || testText == null)
            {
                Debug.Log("ERROR: default locale(" + locale + ") not found, searching for closest match...");

                string newLocale = findClosestExistingLocale(locale, value);
                Debug.Log("--> going with: " + newLocale);

                if (newLocale != "")
                {
                    locale = newLocale;
                }
            }
        }
        else
        {
            Debug.Log(value);
        }
    }

    private string findClosestExistingLocale(string localeStr, string testFile)
    {
        List<string> paths = null;
        string dirpath = "";
        string bestLocale = "";
        int bestDiff = 99999;
        dirpath = _directory + "locales/";

        Debug.Log("--> looking in: " + dirpath);

        paths = getDirectoryContents(dirpath);

        List<string> localeCandidates = new List<string>();

        foreach (string str in paths)
        {
            string strTemp = str.Replace(dirpath, "");
            string newLocale = "";
            if (str.IndexOf("/") != -1)
            {
                newLocale = strTemp.Substring(0, str.IndexOf("/"));
            }
            if (newLocale.IndexOf("_") != 0 && newLocale.IndexOf(".") == -1)
            {
                if (localeCandidates.IndexOf(newLocale) == -1)
                {
                    localeCandidates.Add(newLocale);
                }
            }
        }

        Debug.Log("--> candidates: " + localeCandidates);

        bestLocale = localeStr;
        bestDiff = 99999;

        foreach (string loc in localeCandidates)
        {
            int diff = stringDiff(localeStr, loc, false);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestLocale = loc;
            }
        }
        return bestLocale;
    }

    private int stringDiff(string a, string b, bool caseSensitive = true)
    {
        int totalDiff = 0;
        if (caseSensitive == false)
        {
            a = a.ToLower();
            b = b.ToLower();
        }
        for (int i = 0; i < a.Length; i++)
        {
            char char_a = a[i];
            char char_b = ' ';
            if (b.Length > i)
            {
                char_b = b[i];
            }
            int diff = 0;
            if (char_a != char_b)
            {
                diff += 1;
            }
            totalDiff += diff;
        }
        return totalDiff;
    }


    private List<string> getDirectoryContents(string str)
    {
        List<string> arr = new List<string>();
#if UNITY_WEBPLAYER
        foreach (string file in _index_locales.Keys)
        {
            arr.Add(file);
        }
        
#endif

#if !UNITY_WEBPLAYER
                

                    DirectoryInfo info = new DirectoryInfo(Application.dataPath + "/Resources/" + str);
                    if (info != null)
                    {
                        FileInfo[] files = info.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            arr.Add(file.FullName);
                        }
                    }
    
#endif
        return arr;
    }   
       
    /// <summary>
    /// Loads and processes the index file
    /// </summary>
    private void loadIndex()
    {
        ///index.xml
        string index = loadText("index");
        XmlDocument xml = null;

        _list_files = new List<XmlNode>();
        _unique_list_files = new Dictionary<string, List<XmlNode>>();

        if (index == "" || index == null)
        {
            throw new Exception("Couldn't load index.xml!");
        }
        else
        {
            xml = new XmlDocument();
            xml.LoadXml(index);
            //Create a list of file metadata from the list in the index
            if (xml.SelectSingleNode("child::data") != null && xml.SelectSingleNode("child::data").SelectNodes("child::file").Count != 0)
            {
                foreach (XmlNode fileNode in xml.SelectSingleNode("child::data").SelectNodes("child::file"))
                {
                    _list_files.Add(copyFast(fileNode));
                }
            }
            if (xml.SelectSingleNode("child::data") != null && xml.SelectSingleNode("child::data").SelectNodes("child::fileGroup").Count != 0)
            {
                foreach (XmlNode fileGroupNode in xml.SelectSingleNode("child::data").SelectNodes("child::fileGroup"))
                {
                    if (fileGroupNode != null && fileGroupNode.SelectNodes("child::file").Count != 0)
                    {
                        _unique_list_files.Add(fileGroupNode.Attributes["id"].Value, new List<XmlNode>());
                        foreach (XmlNode fileNode in fileGroupNode.SelectNodes("child::file"))
                        {
                            _unique_list_files[fileGroupNode.Attributes["id"].Value].Add(copyFast(fileNode));
                        }
                    }
                }
            }
        }
        
        if (_index_locales == null)
        {
            _index_locales = new Dictionary<String, XmlNode>();
        }
        if (_index_notes == null)
        {
            _index_notes = new Dictionary<String, String>();
        }
        if (_index_icons == null)
        {
            _index_icons = new Dictionary<String, Texture2D>();
        }
        if (_index_images == null)
        {
            _index_images = new Dictionary<String, Texture2D>();
        }

        string id = "";
        foreach (XmlNode localeNode in xml.SelectSingleNode("child::data").SelectNodes("child::locale"))
        {
            id = localeNode.Attributes["id"].Value;
            _index_locales[id] = localeNode;

            //load & store the flag image
            Texture2D flag = loadImage("_flags/" + id);
            _index_icons[id] = flag;


            bool isDefault = localeNode.Attributes["is_default"] != null && localeNode.Attributes["is_default"].Value == "true";
            if (isDefault)
            {
                default_locale = id;
            }
        }

        //If default locale is not defined yet, make it American English
        if (default_locale == "")
        {
            default_locale = "en-US";
        }

        //If the current locale is not defined yet, make it the default
        if (locale == "")
        {
            locale = default_locale;
        }

        //Load and store all the translation notes
        foreach (XmlNode noteNode in xml.SelectSingleNode("child::data").SelectNodes("child::note"))
        {
            id = noteNode.Attributes["id"].Value;
            foreach (XmlNode textNode in noteNode.SelectSingleNode("child::text").ChildNodes)
            {
                string lid = textNode.Attributes["id"].Value;
                string[] larr = null;
                if (lid.IndexOf(",") != -1)
                {
                    larr = lid.Split(',');
                }
                else
                {
                    larr = new string[1] { lid };
                }
                string title = textNode.Attributes["title"].Value;
                string body = textNode.Attributes["body"].Value;
                foreach (string each_lid in larr)
                {
                    _index_notes[id + "_" + each_lid + "_title"] = title;
                    _index_notes[id + "_" + each_lid + "_body"] = body;
                }
            }
        }
    }

    private void printIndex(string id, Dictionary<string, object> index)
    {
        Debug.Log("printIndex(" + id + ")");

        foreach (string key in index.Keys)
        {
            Debug.Log("..." + key + "," + index[key]);
        }
    }

   
    /// <summary>
    /// Loads a file and processes its contents in the data structure
    /// </summary>
    /// <param name="fileData"><file> node entry from index.xml</param>
    /// <param name="check_vs_default">if true, will use to do safety check rather than immediately store the data</param>
    /// <returns></returns>
    private string loadFile(XmlNode fileData, bool check_vs_default = false)
    {

        string fileName = fileData.Attributes["value"].Value;
        string fileType = fileData.Attributes["extension"].Value;
        string fileID = fileData.Attributes["id"].Value;

        string raw_data = "";

        string loc = locale;
        if (check_vs_default)
        {
            loc = default_locale;
        }

        switch (fileType)
        {
            case "tsv":
                raw_data = loadText(loc + "/" + fileName);
                if (raw_data != "" && raw_data != null)
                {
                    TSV tsv = new TSV(raw_data);
                    processCSV(tsv, fileID, check_vs_default);
                }
                else if (_check_missing)
                {
                    logMissingFile(fileName);
                }
                break;
            case "csv":
                raw_data = loadText(loc + "/" + fileName);
                char delimeter = ',';
                if (fileData.Attributes["delimiter"] != null)
                {
                    delimeter = fileData.Attributes["delimiter"].Value.ToCharArray()[0];
                }
                if (raw_data != "" && raw_data != null)
                {
                    CSV csv = new CSV(raw_data, delimeter);
                    processCSV(csv, fileID, check_vs_default);
                }
                else if (_check_missing)
                {
                    logMissingFile(fileName);
                }
                break;
            case "xml":
                if (!check_vs_default)
                {	//xml (ie font rules) don't need safety checks
                    raw_data = loadText(loc + "/" + fileName);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(raw_data);
                    if (raw_data != "" && raw_data != null)
                    {
                        processXML(xml, fileID);
                    }
                    else if (_check_missing)
                    {
                        logMissingFile(fileName);
                    }
                }
                break;
            case "png":
                var bmp_data = loadImage(loc + "/" + fileName);
                if (bmp_data != null)
                {
                    processPNG(bmp_data, fileID, check_vs_default);
                }
                else if (_check_missing)
                {
                    logMissingFile(fileName);
                }
                break;
        }
        return fileName;
    }

    private void onLoadFile(string result, bool check_vs_default_ = false)
    {
        if (!check_vs_default_)
        {
            _files_loaded++;
        }
        int fileCount = _list_files.Count;
        fileCount += (!string.IsNullOrEmpty(_group_name) ? _unique_list_files[_group_name].Count : 0);

        if (_files_loaded == fileCount)
        {

            _loaded = true;

            if (_check_missing)
            {
                if (_missing_files.Count == 0)
                {
                    _missing_files = null;
                }
                int i = 0;
                foreach (string key in _missing_flags.Keys)
                {
                    i++;
                }
                if (i == 0)
                {
                    _missing_flags = null;
                }
            }

            if (_callback_finished != null)
            {
                _callback_finished();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="csv">The CSV file</param>
    /// <param name="id">The id of the file tag (not the file name) </param>
    /// <param name="check_vs_default"></param>
    private void processCSV(CSV csv, string id, bool check_vs_default = false)
    {
        string flag = "";
        int field_num = csv.fields.Count;

        if (!_index_data.ContainsKey(id))
        {
            _index_data.Add(id, new Dictionary<string, string>());	//create the index for this i
        }

        Dictionary<string, string> _index = _index_data[id];
        int _real_fields = 1;

        //count the number of non-comment fields 
        //(ignore 1st field, which is flag root field)
        for (int fieldi = 1; fieldi < csv.fields.Count; fieldi++)
        {
            string field = csv.fields[fieldi];
            if (field != "comment")
            {
                _real_fields++;
            }
        }

        //Go through each row
        for (int rowi = 0; rowi < csv.grid.Count; rowi++)
        {
            List<string> row = csv.grid[rowi];

            //Get the flag root
            flag = row[0];

            if (_real_fields > 2)
            {
                //Count all non-comment fields as suffix fields to the flag root
                //Assume ("flag","suffix1","suffix2") pattern
                //Write each cell as flag_suffix1, flag_suffix2, etc.
                for (int fieldi = 1; fieldi < csv.fields.Count; fieldi++)
                {
                    string field = csv.fields[fieldi];
                    if (field != "comment")
                    {
                        writeIndex(_index, flag + "_" + field, row[fieldi], id, check_vs_default);
                    }
                }
            }
            else if (_real_fields == 2)
            {
                //If only two non-comment fields, 
                //Assume it's the standard ("flag","value") pattern
                //Just write the first cell
                writeIndex(_index, flag, row[1], id, check_vs_default);
            }
        }

        csv.destroy();
        csv = null;
    }

    /// <summary>
    /// Add entry to dictionary
    /// </summary>
    /// <param name="_index">The dictionary of terms</param>
    /// <param name="flag">The Key of the term</param>
    /// <param name="value">The value of the term</param>
    /// <param name="id">The id of the file tag (not the file name)</param>
    /// <param name="check_vs_default"></param>
    private void writeIndex(Dictionary<string, string> _index, string flag, string value, string id, bool check_vs_default = false)
    {
        if (check_vs_default && _check_missing)
        {
            //flag exists in default locale but not current locale
            if (!_index.ContainsKey(flag))
            {
                logMissingFlag(id, flag);
                if (_replace_missing)
                {
                    _index[flag] = value;
                }
            }
        }
        else
        {
            //just store the flag/translation pair
            _index[flag] = value;
        }
    }

    private void logMissingFlag(string id, string flag)
    {
        if (_missing_flags.ContainsKey(id) == false)
        {
            _missing_flags[id] = new List<string>();
            _missing_flags[id].Add(flag);
        }
        else if (!_missing_flags[id].Contains(flag))
        {
            _missing_flags[id].Add(flag);
        }
    }

    private void logMissingFile(string fname)
    {
        if (!_missing_files.Contains(fname))
        {
            _missing_files.Add(fname);
        }
    }

    private void processXML(XmlDocument xml, string id)
    {
        //what this does depends on the id
        switch (id)
        {
            case "fonts":
                processFonts(xml);
                break;
            default:
                //donothing
                break;
        }
    }

    private void processPNG(Texture2D img, string id, bool check_vs_default = false)
    {
        if (check_vs_default && _check_missing)
        {
            if (_index_images.ContainsKey(id) == false)
            {
                //image exists in default locale but not current locale				
                logMissingFile(id);
                //log the missing PNG file					
                if (_replace_missing)
                {
                    //replace with default locale version if necessary
                    _index_images[id] = img;
                }
            }
        }
        else
        {
            //just store the image
            _index_images[id] = img;
        }
    }

    private void processFonts(XmlDocument xml)
    {
        if (xml != null && xml.SelectSingleNode("child::data") != null && xml.SelectSingleNode("child::data").SelectNodes("child::font").Count != 0)
        {
            foreach (XmlNode fontNode in xml.SelectSingleNode("child::data").SelectNodes("child::font"))
            {
                string value = fontNode.Attributes["value"].Value;
                _index_font[value] = copyFast(fontNode);
            }
        }
    }

    
    /// <summary>
    /// Clear all the current localization data.
    /// </summary>
    /// <param name="hard">Also clear all the index-related data, restoring it to a pre-initialized state.</param>
    private void clearData(bool hard = false)
    {
        _callback_finished = null;

        if (_list_files != null)
        {
            while (_list_files.Count > 0)
            {
                _list_files.RemoveAt(0);
            }
            _list_files = null;
        }
        if (_unique_list_files != null)
        {
            List<string> keys = _unique_list_files.Keys.ToList();
            while (_unique_list_files.Count > 0)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    _unique_list_files.Remove(keys[i]);
                }
            }
            _unique_list_files = null;
        }

        _loaded = false;
        _files_loaded = 0;

        int len = _index_data.Keys.Count;
        for(int i=len-1;i>=0;i--)
        {
            string sub_key = _index_data.Keys.ElementAt(i);
            Dictionary<string, string> sub_index = _index_data[sub_key];
            _index_data.Remove(sub_key);
            clearMap(sub_index);
            sub_index = null;
        }

        clearBitmapDataMap(_index_images);
        clearMap(_index_font);

        _index_images = null;
        _index_font = null;

        if (hard)
        {
            clearMap(_index_locales);
            clearBitmapDataMap(_index_icons);
            clearMap(_index_notes);
            _index_locales = null;
            _index_icons = null;
            _index_notes = null;
        }

        clearMap(_missing_flags);
        if (_missing_files != null)
        {
            while (_missing_files.Count > 0)
            {
                _missing_files.RemoveAt(0);
            }
        }

        _missing_files = null;
        _missing_flags = null;
    }

    private void clearBitmapDataMap(Dictionary<string, Texture2D> map)
    {
        clearMap(map, new Action<Texture2D>(TextureDispose));
    }

    public void TextureDispose(Texture2D bitmapData)
    {
        if (bitmapData != null) GameObject.Destroy(bitmapData);
    }

    private void clearMap<T1, T2>(Dictionary<T1, T2> map, Action<T2> onRemove = null)
    {
        if (map == null) return;

        int len = map.Keys.Count;
        for(int i=len-1;i>=0;i--)            
        {
            T1 key = map.Keys.ElementAt(i);
            var element = map[key];
            if (onRemove != null)
            {
                onRemove(element);
            }
            map.Remove(key);
        }
    }
}