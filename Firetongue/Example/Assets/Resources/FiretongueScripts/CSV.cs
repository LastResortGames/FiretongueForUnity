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
using System.Text.RegularExpressions;
using System.Threading;

public class CSV
{
    public List<string> fields;
    public List<List<string>> grid;
    protected bool _quoted = true;

    public CSV(string input, char delimeter = ',')
    {
        if (input != "")
        {
            Regex rgx = new Regex(",(?=(?:[^\x22]*\x22[^\x22]*\x22)*(?![^\x22]*\x22))", RegexOptions.Multiline | RegexOptions.Singleline);
            // Matches a well formed CSV cell, ie "thing1" or "thing ,, 5" etc
            // "\x22" is the invocation for the double-quote mark.
            // UTF-8 ONLY!!!!

            //You can provide your own customer delimeter, but we generally don't recommend it
            if (delimeter != ',')
            {
                ///TODO: Investigate GM
                rgx = new Regex(delimeter + "(?=(?:[^\x22]*\x22[^\x22]*\x22)*(?![^\x22]*\x22))", RegexOptions.Multiline | RegexOptions.Singleline);
            }

            // Get all the cells
            string[] cells = rgx.Split(input);
            processCells(cells);
        }
    }

    protected void processCells(string[] cells)
    {
        int row = 0;
        int col = 0;
        bool newline = false;
        List<string> row_array = null;

        grid = new List<List<String>>();
        fields = new List<String>();

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].Length >= 2)
            {
                newline = false;

                //If the first character is a line break, we are at a new row
                string firstchar = cells[i].Substring(0, 2);

                if (firstchar == "\n\r" || firstchar == "\r\n")
                {
                    newline = true;
                    cells[i] = cells[i].Substring(2, cells[i].Length - 2);	//strip off the newline
                }
                else
                {
                    firstchar = cells[i].Substring(0, 1);
                    if (firstchar == "\n" || firstchar == "\r")
                    {
                        newline = true;
                        cells[i] = cells[i].Substring(1, cells[i].Length - 1);	//strip off the newline
                    }
                }

                if (cells[i].Length >= 2)
                {
                    if (newline)
                    {
                        if (row_array != null)
                        {
                            grid.Add(row_array);	//add the built up row array
                        }
                        row_array = new List<String>();
                        col = 0;
                        row++;
                    }

                    string cell = "";
                    if (_quoted)
                    {
                        cell = cells[i].Substring(1, cells[i].Length - 2);
                    }
                    else
                    {
                        cell = cells[i];
                    }

                    if (row == 0)
                    {
                        fields.Add(cell);		//get the fields
                    }
                    else
                    {
                        row_array.Add(cell);	//get the row cells
                    }
                }
            }
        }

        if (row_array != null)
        {
            grid.Add(row_array);
        }

        clearArray(cells);
        cells = null;
    }

    public void destroy()
    {
        clearArray(grid);
        clearArray(fields);
        grid = null;
        fields = null;
    }

    private void clearArray(string[] array)
    {
        if (array == null) return;
        int i = array.Length - 1;
        while (i >= 0)
        {
            destroyThing(array[i]);
            array[i] = null;
            i--;
        }
        array = null;
    }

    private void clearArray(List<List<string>> array)
    {
        if (array == null) return;
        int i = array.Count - 1;
        while (i >= 0)
        {
            destroyThing(array[i]);
            array[i] = null;
            array.RemoveAt(i);
            i--;
        }
        array = null;
    }

    private void clearArray(List<string> array)
    {
        if (array == null) return;
        int i = array.Count - 1;
        while (i >= 0)
        {
            destroyThing(array[i]);
            array[i] = null;
            array.RemoveAt(i);
            i--;
        }
        array = null;
    }


    private void destroyThing(System.Object thing)
    {
        if (thing == null) return;

        if (thing as List<string> != null)
        {
            clearArray(thing as List<string>);
        }

        thing = null;
    }


}


