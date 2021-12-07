using System;
using System.Collections.Generic;
using System.Text;

namespace Caravela.Compiler
{
    internal class RefAssemblyResourceMarker
    {
        private RefAssemblyResourceMarker() { }

        public static RefAssemblyResourceMarker Instance { get; } = new RefAssemblyResourceMarker();
    }
}
