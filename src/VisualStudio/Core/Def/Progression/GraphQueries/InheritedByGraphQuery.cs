//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.FindSymbols;
//using Microsoft.CodeAnalysis.Internal.Log;
//using Microsoft.VisualStudio.GraphModel;
//using Microsoft.VisualStudio.GraphModel.Schemas;
//using Roslyn.Utilities;

//namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

//internal sealed class InheritedByGraphQuery : IGraphQuery
//{
//    public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
//    {
//        using var _ = Logger.LogBlock(FunctionId.GraphQuery_InheritedBy, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);

//        var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

//        foreach (var node in context.InputNodes)
//        {
//            var symbol = graphBuilder.GetSymbol(node, cancellationToken);
//            if (symbol is not INamedTypeSymbol namedType)
//                continue;

//            if (namedType.TypeKind == TypeKind.Class)
//            {
//                var derivedTypes = await SymbolFinder.FindDerivedClassesArrayAsync(
//                    namedType, solution, transitive: false, cancellationToken: cancellationToken).ConfigureAwait(false);
//                foreach (var derivedType in derivedTypes)
//                {
//                    var symbolNode = await graphBuilder.AddNodeAsync(
//                        derivedType, relatedNode: node, cancellationToken).ConfigureAwait(false);
//                    graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node, cancellationToken);
//                }
//            }
//            else if (namedType.TypeKind == TypeKind.Interface)
//            {
//                var implementingClassesAndStructs = await SymbolFinder.FindImplementationsArrayAsync(
//                    namedType, solution, transitive: false, cancellationToken: cancellationToken).ConfigureAwait(false);
//                var derivedInterfaces = await SymbolFinder.FindDerivedInterfacesArrayAsync(
//                    namedType, solution, transitive: false, cancellationToken: cancellationToken).ConfigureAwait(false);
//                foreach (var derivedType in implementingClassesAndStructs.ConcatFast(derivedInterfaces))
//                {
//                    var symbolNode = await graphBuilder.AddNodeAsync(
//                        derivedType, relatedNode: node, cancellationToken).ConfigureAwait(false);
//                    graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node, cancellationToken);
//                }
//            }
//        }

//        return graphBuilder;
//    }
//}
