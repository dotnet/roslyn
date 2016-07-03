using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal interface ISignTool
    {
        void Sign(IEnumerable<string> filePaths);
    }
}
