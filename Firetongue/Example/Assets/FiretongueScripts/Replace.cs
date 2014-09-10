﻿/**
 * Copyright (c) 2013 Level Up Labs, LLC
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

public class Replace
{
    /**
   * Simple class to do variable replacements for localization
   * 
   * USAGE:
       * var str:String = fire_tongue.get("$GOT_X_GOLD"); //str now = "You got <X> gold coins!"
       * str = Replace.flags(str,["<X>"],[num_coins]);	//num_coins = "10"
       * 
       * //str now = "You got 10 gold coins!"
   * 
   * This method is preferably to schemes that do this:
   * (str = "You got" + num_coins + " gold coins!")
   *  
   * Even if you translate the sentence fragments, each language has
   * its own unique word order and sentence structure, so trying to embed
   * that in code is a lost cuase. It's better to just let the translator 
   * specify where the variable should fall, and replace it accordingly. 
   */

    public Replace()
    {
    }

    public static string flags(string str, List<string> flags, List<string> values)
    {
        int j = 0;
        while (j < flags.Count)
        {
            var flag = flags[j];
            var value = values[j];
            while (str.IndexOf(flag) != -1)
            {
                str = str.Replace(flag, value);
            }
            j++;
        }
        return str;
    }
}
