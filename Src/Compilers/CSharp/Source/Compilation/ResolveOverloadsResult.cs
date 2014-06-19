namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents the results of SyntaxBinding.ResolveOverloads.  Overload resolution can return up
    /// to two symbols found during execution.  The first, ExactMatch, represents the method found if
    /// overload resolution completed successfully and found a viable method with any errors.  The
    /// second, BestMatch, represents the closest method found if no ExactMatch could be found.  Both
    /// results might be null of no exact or best match could be matched in the candidate set.  If
    /// ExactMatch is non null, then BestMatch will be the same symbol as ExactMatch.  it is possible
    /// for BestMatch to be non-null and ExactMatch be null.
    /// </summary>
    public struct ResolveOverloadsResult
    {
        public MethodSymbol ExactMatch { get; private set; }
        public MethodSymbol BestMatch { get; private set; }

        public ResolveOverloadsResult(MethodSymbol exactMatch, MethodSymbol bestMatch)
            : this()
        {
            this.ExactMatch = exactMatch;
            this.BestMatch = bestMatch;
        }
    }
}