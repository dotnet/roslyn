using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static void GenerateInstrumentationTables(MethodSymbol method, BoundBlock methodBody, DebugDocumentProvider debugDocumentProvider)
        {
            if (methodBody != null)
            {
                ArrayBuilder<Cci.SequencePoint> pointsBuilder = new ArrayBuilder<Cci.SequencePoint>();
                OperationWalker collector = new InstrumentationCollectionWalker(pointsBuilder, debugDocumentProvider);
                collector.Visit(methodBody);

                ImmutableArray<Cci.SequencePoint> points = pointsBuilder.ToImmutableAndFree();
            }
        }
    }

    internal sealed class InstrumentationCollectionWalker : OperationWalker
    {
        private readonly ArrayBuilder<Cci.SequencePoint> _pointsBuilder;
        private readonly DebugDocumentProvider _debugDocumentProvider;

        public InstrumentationCollectionWalker(ArrayBuilder<Cci.SequencePoint> pointsBuilder, DebugDocumentProvider debugDocumentProvider)
        {
            _pointsBuilder = pointsBuilder;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override void Visit(IOperation operation)
        {
            if (operation == null)
            {
                return;
            }
            
            if (operation.IsStatement())
            {
                FileLinePositionSpan lineSpan = operation.Syntax.GetLocation().GetMappedLineSpan();
                string path = lineSpan.Path;
                if (path == "")
                {
                    path = operation.Syntax.SyntaxTree.FilePath;
                }

                _pointsBuilder.Add(new Cci.SequencePoint(_debugDocumentProvider.Invoke(path, ""), 0, lineSpan.Span.Start.Line, lineSpan.Span.Start.Character, lineSpan.Span.End.Line, lineSpan.Span.End.Character));
            }

            base.Visit(operation);
        }
    }
}
