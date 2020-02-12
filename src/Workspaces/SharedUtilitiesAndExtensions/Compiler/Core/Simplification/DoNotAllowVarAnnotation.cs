namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// When applied to a SyntaxNode, prevents the simplifier from converting a type to 'var'.
    /// </summary>
    internal class DoNotAllowVarAnnotation
    {
        public static readonly SyntaxAnnotation Annotation = new SyntaxAnnotation(Kind);
        public const string Kind = "DoNotAllowVar";
    }
}
