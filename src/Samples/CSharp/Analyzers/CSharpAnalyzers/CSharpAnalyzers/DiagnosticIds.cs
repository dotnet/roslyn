namespace CSharpAnalyzers
{
    public static class DiagnosticIds
    {
        // Stateless analyzer IDs.
        public const string SymbolAnalyzerRuleId = "CSS0001";
        public const string SyntaxNodeAnalyzerRuleId = "CSS0002";
        public const string SyntaxTreeAnalyzerRuleId = "CSS0003";
        public const string SemanticModelAnalyzerRuleId = "CSS0004";
        public const string CodeBlockAnalyzerRuleId = "CSS0005";
        public const string CompilationAnalyzerRuleId = "CSS0006";

        // Stateful analyzer IDs.
        public const string CodeBlockStartedAnalyzerRuleId = "CSS0101";
        public const string CompilationStartedAnalyzerRuleId = "CSS0102";
        public const string CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId = "CSS0103";
    }
}