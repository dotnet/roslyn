using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Services.Internal.Log
{
    /// <summary>
    /// Implementation of IMeasurementBlock that does nothing
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class EmptyMeasurementBlock : IMeasurementBlock
    {
        internal static readonly EmptyMeasurementBlock Instance = new EmptyMeasurementBlock();

        #region IMeasurementBlock Members

        public void Begin()
        {
        }

        #endregion

        #region IDisposable Members

        public void Dispose() { }

        #endregion
    }
}
