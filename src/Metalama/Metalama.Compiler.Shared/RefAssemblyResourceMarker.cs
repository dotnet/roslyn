using System;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Compiler
{
    internal class RefAssemblyResourceMarker
    {
        private RefAssemblyResourceMarker() { }

        public static RefAssemblyResourceMarker Instance { get; } = new RefAssemblyResourceMarker();
    }
}
