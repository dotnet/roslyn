using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AnonymousFunctionOrDelegateSymbolKey
        {
            private enum LocalSymbolType
            {
                AnonymousFunction,
                AnonymousDelegate,
            }

            public static void Create(ISymbol symbol, SymbolKeyWriter visitor)
            {
                // var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                //var filePath = reference?.SyntaxTree.FilePath ?? "";
                //var textSpan = reference?.Span ?? new TextSpan();
                Debug.Assert(symbol.IsAnonymousDelegateType() ||
                    symbol.IsAnonymousFunction());
                var type = symbol.IsAnonymousDelegateType()
                    ? LocalSymbolType.AnonymousDelegate
                    : LocalSymbolType.AnonymousFunction;

                var location = symbol.Locations.FirstOrDefault();
                var filePath = location?.SourceTree.FilePath ?? "";
                var textSpan = location?.SourceSpan ?? new TextSpan();

                visitor.WriteInteger((int)type);
                visitor.WriteString(filePath);
                visitor.WriteInteger(textSpan.Start);
                visitor.WriteInteger(textSpan.Length);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var type = (LocalSymbolType)reader.ReadInteger();
                var filePath = reader.ReadString();
                var start = reader.ReadInteger();
                var length = reader.ReadInteger();

                var syntaxTree = reader.GetSyntaxTree(filePath);
                if (syntaxTree != null)
                {
                    var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot(reader.CancellationToken);
                    var node = root.FindNode(new TextSpan(start, length), getInnermostNodeForTie: true);

                    var symbol = semanticModel.GetSymbolInfo(node, reader.CancellationToken)
                                              .GetAnySymbol();

                    if (type == LocalSymbolType.AnonymousDelegate)
                    {
                        var anonymousDelegate = (symbol as IMethodSymbol).AssociatedAnonymousDelegate;
                        symbol = anonymousDelegate;
                    }

                    return new SymbolKeyResolution(symbol);
                }

                return default(SymbolKeyResolution);
            }
        }
    }
}