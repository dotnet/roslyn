namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal class NullErrorLogger : ErrorLogger
    {
        internal static ErrorLogger Instance => new NullErrorLogger();

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
        }
    }
}
