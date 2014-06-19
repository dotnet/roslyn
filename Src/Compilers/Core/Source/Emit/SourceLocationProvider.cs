using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceLocationProvider : Microsoft.Cci.ISourceLocationProvider
    {
        internal static readonly SourceLocationProvider Instance = new SourceLocationProvider();

        private SourceLocationProvider()
        {
        }

        public IEnumerable<Microsoft.Cci.SequencePoint> GetPrimarySourceLocationsFor(Microsoft.Cci.SequencePoint locations)
        {
            yield return locations;
        }

        public string GetSourceNameFor(Microsoft.Cci.ILocalDefinition localDefinition, out bool isCompilerGenerated)
        {
            isCompilerGenerated = localDefinition.IsCompilerGenerated;
            return localDefinition.Name;
        }
    }
}