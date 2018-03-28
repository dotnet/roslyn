namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class SymbolMatchPriority
    {
        internal static int Keyword = 100;
        internal static int PreferType = 200;
        internal static int PreferNamedArgument = 300;
        internal static int PreferEventOrMethod = 400;
        internal static int PreferFieldOrProperty = 500;
        internal static int PreferLocalOrParameterOrRangeVariable = 600;
    }
}
