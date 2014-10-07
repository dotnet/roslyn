using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Services.Internal.Log
{
    /// <summary>
    /// Implementation of IMeasurementBlockFactory that does nothing and returns EmptyMeasurementBlocks
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class EmptyMeasurementBlockFactory : IMeasurementBlockFactory
    {
        public IMeasurementBlock Create(ulong size = 0, string category = null, uint nestingLevel = 0)
        {
            return EmptyMeasurementBlock.Instance;
        }

        public IMeasurementBlock BeginNew(FunctionId functionId)
        {
            return EmptyMeasurementBlock.Instance;
        }
    }
}
