using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal class EsentInstanceShutdownException : EsentException
    {
        public EsentInstanceShutdownException(string message) : base(message)
        {
        }
    }
}
