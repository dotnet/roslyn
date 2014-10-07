namespace Roslyn.Services.Internal.Log
{
    /// <summary>
    /// Defines methods for creating IMeasurementBlocks
    /// </summary>
    internal interface IMeasurementBlockFactory
    {
        /// <summary>
        /// Starts and returns a new MeasurementBlock for the given FunctionId.
        /// 
        /// using (IMeasurementBlockFactory.BeginNew(functionId)
        /// {
        /// }
        /// </summary>
        /// <param name="functionId">The function being measured</param>
        /// <returns>A running IMeasurementBlock instance</returns>
        IMeasurementBlock BeginNew(FunctionId functionId);
    }
}