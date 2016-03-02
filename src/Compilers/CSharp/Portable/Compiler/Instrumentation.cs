using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static void GenerateInstrumentationTables(MethodSymbol method, BoundBlock methodBody, DebugDocumentProvider debugDocumentProvider)
        {
            if (methodBody != null)
            {
                ArrayBuilder<Cci.SequencePoint> pointsBuilder = new ArrayBuilder<Cci.SequencePoint>();
                BoundTreeWalker collector = new InstrumentationCollectionWalker(pointsBuilder, debugDocumentProvider);
                collector.Visit(methodBody);

                ImmutableArray<Cci.SequencePoint> points = pointsBuilder.ToImmutableAndFree();
            }
        }
    }

    internal sealed class InstrumentationCollectionWalker : BoundTreeWalkerWithStackGuard
    {
        private readonly ArrayBuilder<Cci.SequencePoint> _pointsBuilder;
        private readonly DebugDocumentProvider _debugDocumentProvider;

        public InstrumentationCollectionWalker(ArrayBuilder<Cci.SequencePoint> pointsBuilder, DebugDocumentProvider debugDocumentProvider)
        {
            _pointsBuilder = pointsBuilder;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override BoundNode Visit(BoundNode operation)
        {
            if (operation == null)
            {
                return null;
            }

            BoundStatement statement = operation as BoundStatement;
            if (statement != null)
            {
                FileLinePositionSpan lineSpan = statement.Syntax.GetLocation().GetMappedLineSpan();
                string path = lineSpan.Path;
                if (path == "")
                {
                    path = statement.Syntax.SyntaxTree.FilePath;
                }

                _pointsBuilder.Add(new Cci.SequencePoint(_debugDocumentProvider.Invoke(path, ""), 0, lineSpan.Span.Start.Line, lineSpan.Span.Start.Character, lineSpan.Span.End.Line, lineSpan.Span.End.Character));
            }

            base.Visit(operation);
            return null;
        }
    }
}
