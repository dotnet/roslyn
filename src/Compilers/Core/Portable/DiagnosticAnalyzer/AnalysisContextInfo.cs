// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// this hold onto analyzer executor context which will be used later to put context information in analyzer exception if it occurs.
    /// </summary>
    internal readonly struct AnalysisContextInfo
    {
        private readonly Compilation? _compilation;
        private readonly IOperation? _operation;
        private readonly ISymbol? _symbol;
        private readonly SourceOrNonSourceFile? _file;
        private readonly SyntaxNode? _node;

        public AnalysisContextInfo(Compilation compilation) :
            this(compilation: compilation, operation: null, symbol: null, file: null, node: null)
        {
        }

        public AnalysisContextInfo(SemanticModel model) :
            this(model.Compilation, SourceOrNonSourceFile.Create(model.SyntaxTree))
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol) :
            this(compilation: compilation, operation: null, symbol: symbol, file: null, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SourceOrNonSourceFile file) :
            this(compilation: compilation, operation: null, symbol: null, file: file, node: null)
        {
        }

        public AnalysisContextInfo(Compilation compilation, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: null, file: SourceOrNonSourceFile.Create(node.SyntaxTree), node)
        {
        }

        public AnalysisContextInfo(Compilation compilation, IOperation operation) :
            this(compilation: compilation, operation: operation, symbol: null, file: SourceOrNonSourceFile.Create(operation.Syntax.SyntaxTree), node: operation.Syntax)
        {
        }

        public AnalysisContextInfo(Compilation compilation, ISymbol symbol, SyntaxNode node) :
            this(compilation: compilation, operation: null, symbol: symbol, file: SourceOrNonSourceFile.Create(node.SyntaxTree), node)
        {
        }

        private AnalysisContextInfo(
            Compilation? compilation,
            IOperation? operation,
            ISymbol? symbol,
            SourceOrNonSourceFile? file,
            SyntaxNode? node)
        {
            RoslynDebug.Assert(node == null || file?.SourceTree != null);
            RoslynDebug.Assert(operation == null || file?.SourceTree != null);

            _compilation = compilation;
            _operation = operation;
            _symbol = symbol;
            _file = file;
            _node = node;
        }

        public string GetContext()
        {
            var sb = new StringBuilder();

            if (_compilation?.AssemblyName != null)
            {
                sb.AppendLine($"{nameof(Compilation)}: {_compilation.AssemblyName}");
            }

            if (_operation != null)
            {
                sb.AppendLine($"{nameof(IOperation)}: {_operation.Kind}");
            }

            if (_symbol?.Name != null)
            {
                sb.AppendLine($"{nameof(ISymbol)}: {_symbol.Name} ({_symbol.Kind})");
            }

            if (_file != null)
            {
                if (_file.SourceTree != null)
                {
                    sb.AppendLine($"{nameof(SyntaxTree)}: {_file.SourceTree.FilePath}");
                }
                else
                {
                    RoslynDebug.Assert(_file.NonSourceFile != null);
                    sb.AppendLine($"{nameof(AdditionalText)}: {_file.NonSourceFile.Path}");
                }
            }

            if (_node != null)
            {
                RoslynDebug.Assert(_file != null);
                RoslynDebug.Assert(_file.SourceTree != null);

                var text = _file.SourceTree.GetText();
                var lineSpan = text?.Lines?.GetLinePositionSpan(_node.Span);

                // can't use Kind since that is language specific. instead will output typename.
                sb.AppendLine($"{nameof(SyntaxNode)}: {GetFlattenedNodeText(_node)} [{_node.GetType().Name}]@{_node.Span} {(lineSpan.HasValue ? lineSpan.Value.ToString() : string.Empty)}");
            }

            return sb.ToString();
        }

        private string GetFlattenedNodeText(SyntaxNode node)
        {
            const int cutoff = 30;

            var lastEnd = node.Span.Start;
            var sb = new StringBuilder();
            foreach (var token in node.DescendantTokens(descendIntoTrivia: false))
            {
                if (token.Span.Start - lastEnd > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(token.ToString());
                lastEnd = token.Span.End;

                if (sb.Length > cutoff)
                {
                    break;
                }
            }

            return sb.ToString() + (sb.Length > cutoff ? " ..." : string.Empty);
        }
    }
}
