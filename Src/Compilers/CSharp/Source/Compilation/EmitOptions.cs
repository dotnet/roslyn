using System;

namespace Roslyn.Compilers.CSharp
{
    public class EmitOptions
    {
        public AssemblyKind AssemblyKind { get; set; }

        public EmitOptions()
        {
            this.AssemblyKind = AssemblyKind.ConsoleApplication;
        }
    }
}