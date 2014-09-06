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
