using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class TSV : CSV
{
    /**
 * Parses TSV formatted string into a useable data structure
 * @param	input tsv-formatted string
 */

    public TSV(string input)
        : base("")
    {
        _quoted = false;			//No quotation marks in TSV files!

        // Get all the cells
        string[] cells;
        cells = input.Split('\t');	//Get all the cells in a much simpler way
        processCells(cells);
    }

}
