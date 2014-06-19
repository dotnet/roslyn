using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents the value of #r reference along with its source location.
    /// </summary>
    internal struct ReferenceDirective
    {
        public readonly string File;
        public readonly SourceLocation Location;

        public ReferenceDirective(string file, SourceLocation location)
        {
            Contract.Assert(file != null);
            Contract.Assert(location != null);

            File = file;
            Location = location;
        }            
    }
}
