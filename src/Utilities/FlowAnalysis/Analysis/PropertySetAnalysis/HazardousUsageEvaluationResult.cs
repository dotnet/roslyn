namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Result of evaluating potentially hazardous usage.
    /// </summary>
    public enum HazardousUsageEvaluationResult
    {
        /// <summary>
        /// The usage is not hazardous.
        /// </summary>
        Unflagged,

        /// <summary>
        /// The usage might be hazardous.
        /// </summary>
        MaybeFlagged,

        /// <summary>
        /// The usage is definitely hazardous.
        /// </summary>
        Flagged,
    }
}
