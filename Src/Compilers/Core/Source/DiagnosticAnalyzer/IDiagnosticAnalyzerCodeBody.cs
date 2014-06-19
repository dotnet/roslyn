using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Obsolete]
    public interface IDiagnosticAnalyzerCodeBody : IDiagnosticAnalyzer
    {
        [Obsolete]
        ICodeBodyAnalyzer OnCodeBody(SyntaxNode syntax, ISymbol symbol, SemanticModel model, DiagnosticSink diagnostics);
    }

    [Obsolete]
    public interface ICodeBodyAnalyzer
    {
        /// <summary>
        /// Returns list of diagnostics that can be produced by this code body analyzer
        /// </summary>
        IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics();

        [Obsolete]
        void OnCodeBodyCompleted(SyntaxNode syntax, ISymbol symbol, SemanticModel model, DiagnosticSink diagnostics);
    }

    [Obsolete]
    public interface INodeInCodeBodyAnalyzer<TSyntaxKind> : ICodeBodyAnalyzer
    {
        ImmutableArray<TSyntaxKind> KindsOfInterest { get; }
        [Obsolete]
        void OnNode(SyntaxNode syntax, ISymbol symbol, SemanticModel model, DiagnosticSink diagnostics);
    }
}
