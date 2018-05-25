namespace Microsoft.CodeAnalysis.CodeActions
{
    internal class SuppressDiagnosticsAnnotation
    {
        public const string Kind = "CodeAction_SuppressDiagnostics";

        public static SyntaxAnnotation Create()
        {
            return new SyntaxAnnotation(Kind);
        }
    }
}
