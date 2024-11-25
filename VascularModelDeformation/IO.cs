using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public class IO
    {
        /// <summary>
        /// constructor
        /// </summary>
        public IO()
        {
            Debug.WriteLine($"This is IO constructor.");
        }
        /// <summary>
        /// destructor
        /// </summary>
        ~IO()
        {
            Debug.WriteLine($"This is IO destructor");
        }
    }
}
