namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Denotes the kind of reference parameter.
    /// </summary>
    public enum RefKind
    {
        /// <summary>
        /// Indicates a "value" parameter.
        /// </summary>
        None,

        /// <summary>
        /// Indicates a "ref" parameter.
        /// </summary>
        Ref,

        /// <summary>
        /// Indicates an "out" parameter.
        /// </summary>
        Out
    }

    internal static partial class Extensions
    {
        public static SyntaxToken GetToken(this RefKind refKind)
        {
            if (refKind == RefKind.Out)
            {
                return Syntax.Token(SyntaxKind.OutKeyword);
            }
            if (refKind == RefKind.Ref)
            {
                return Syntax.Token(SyntaxKind.RefKeyword);
            }
            return default(SyntaxToken);
        }

        public static RefKind GetRefKind(this SyntaxTokenList modifiers)
        {
            for (int m = 0; m < modifiers.Count; ++m)
            {
                var refkind = modifiers[m].Kind.GetRefKind();
                if (refkind != RefKind.None)
                {
                    return refkind;
                }
            }
            return RefKind.None;
        }

        public static RefKind GetRefKind(this SyntaxKind syntaxKind)
        {
            if (syntaxKind == SyntaxKind.RefKeyword)
            {
                return RefKind.Ref;
            }
            if (syntaxKind == SyntaxKind.OutKeyword)
            {
                return RefKind.Out;
            }
            return RefKind.None;
        }

    }
}