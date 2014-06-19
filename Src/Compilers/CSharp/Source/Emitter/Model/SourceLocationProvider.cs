using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    internal class SourceLocationProvider : Microsoft.Cci.ISourceLocationProvider
    {
        public IEnumerable<Microsoft.Cci.SequencePoint> GetPrimarySourceLocationsFor(Microsoft.Cci.SequencePoint locations)
        {
            yield return locations;
        }

        public string GetSourceNameFor(Microsoft.Cci.ILocalDefinition localDefinition, out bool isCompilerGenerated)
        {
            isCompilerGenerated = false;
            return localDefinition.Name;
        }
    }
}