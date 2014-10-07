using System;

namespace Roslyn.Services.Internal.Log
{
    /// <summary>
    /// Defines methods for logging events before and after a section of code
    /// executes.  An end event is logged when the IMeasurementBlock is disposed
    /// </summary>
    internal interface IMeasurementBlock : IDisposable
    {
        /// <summary>
        /// Logs a Begin event
        /// </summary>
        void Begin();
    }
}
