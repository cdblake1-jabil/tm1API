using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullDataFromTM1
{
    class CellInfo
    {

        public List<string> Dims = new List<string>();
        public double[] Values = new double[2];
        public double Delta;
        public bool NoMatchFound = false;

    }
}
