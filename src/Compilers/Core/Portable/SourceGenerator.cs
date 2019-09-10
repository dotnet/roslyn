using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public interface ISourceGenerator
    {
        void Execute(SourceGeneratorContext context);
    }

    public struct SourceGeneratorContext
    {
        public Compilation Compilation { get; }

        // TODO: replace AnalyzerOptions with an differently named type that is otherwise identical.
        // The concern being that something added to one isn't necessarily applicable to the other.
        public AnalyzerOptions AnalyzerOptions { get; }

        public CancellationToken CancellationToken { get; }

        public void ReportDiagnostic(Diagnostic diagnostic) { throw new NotImplementedException(); }

        public void AddSource(string fileNameHint, SourceText sourceText) { throw new NotImplementedException(); }
    }

    public interface IDependsOnAdditionalFileGenerator : ISourceGenerator
    {
        ImmutableArray<string> SupportedAdditionalFileExtensions { get; }
    }
}
