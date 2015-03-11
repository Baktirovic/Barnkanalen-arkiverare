using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barnkanalen_arkiverare
{
    class downloadError : EventArgs
    {
        public downloadError(string  errormessage)
        {
            this.message = errormessage;
        } 
        /// <summary>
        /// Gets the progress percentage in a range from 0.0 to 100.0.
        /// </summary>
        public string message { get; private set; }
    }

}
