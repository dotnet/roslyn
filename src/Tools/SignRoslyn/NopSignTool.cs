using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    /// <summary>
    /// Sign tool which does nothing.  Allows for useful validation and debugging an local machines.
    /// </summary>
    internal sealed class NopSignTool : ISignTool
    {
        void ISignTool.Sign(IEnumerable<string> filePaths)
        {

        }
    }
}
