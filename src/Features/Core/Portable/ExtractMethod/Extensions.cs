// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class Extensions
    {
        public static bool Succeeded(this OperationStatus status)
        {
            return status.Flag.Succeeded();
        }

        public static bool FailedWithNoBestEffortSuggestion(this OperationStatus status)
        {
            return status.Flag.Failed() && !status.Flag.HasBestEffort();
        }

        public static bool Failed(this OperationStatus status)
        {
            return status.Flag.Failed();
        }

        public static bool Succeeded(this OperationStatusFlag flag)
        {
            return (flag & OperationStatusFlag.Succeeded) != 0;
        }

        public static bool Failed(this OperationStatusFlag flag)
        {
            return !flag.Succeeded();
        }

        public static bool HasBestEffort(this OperationStatusFlag flag)
        {
            return (flag & OperationStatusFlag.BestEffort) != 0;
        }

        public static bool HasSuggestion(this OperationStatusFlag flag)
        {
            return (flag & OperationStatusFlag.Suggestion) != 0;
        }

        public static bool HasMask(this OperationStatusFlag flag, OperationStatusFlag mask)
        {
            return (flag & mask) != 0x0;
        }

        public static OperationStatusFlag RemoveFlag(this OperationStatusFlag baseFlag, OperationStatusFlag flagToRemove)
        {
            return baseFlag & ~flagToRemove;
        }

        public static ITypeSymbol? GetLambdaOrAnonymousMethodReturnType(this SemanticModel binding, SyntaxNode node)
        {
            var info = binding.GetSymbolInfo(node);
            if (info.Symbol == null)
            {
                return null;
            }

            var methodSymbol = info.Symbol as IMethodSymbol;
            if (methodSymbol?.MethodKind != MethodKind.AnonymousFunction)
            {
                return null;
            }

            return methodSymbol.GetReturnTypeWithAnnotatedNullability();
        }

        public static Task<SemanticDocument> WithSyntaxRootAsync(this SemanticDocument semanticDocument, SyntaxNode root, CancellationToken cancellationToken)
        {
            return SemanticDocument.CreateAsync(semanticDocument.Document.WithSyntaxRoot(root), cancellationToken);
        }

        /// <summary>
        /// get tokens with given annotation in current document
        /// </summary>
        public static SyntaxToken GetTokenWithAnnotation(this SemanticDocument document, SyntaxAnnotation annotation)
        {
            return document.Root.GetAnnotatedNodesAndTokens(annotation).Single().AsToken();
        }

        /// <summary>
        /// resolve the given symbol against compilation this snapshot has
        /// </summary>
        public static T ResolveType<T>(this SemanticModel semanticModel, T symbol) where T : class, ITypeSymbol
        {
            var typeSymbol = (T)symbol.GetSymbolKey().Resolve(semanticModel.Compilation).GetAnySymbol();
            return typeSymbol.WithNullability(symbol.GetNullability());
        }

        /// <summary>
        /// check whether node contains error for itself but not from its child node
        /// </summary>
        public static bool HasDiagnostics(this SyntaxNode node)
        {
            var set = new HashSet<Diagnostic>(node.GetDiagnostics());

            foreach (var child in node.ChildNodes())
            {
                set.ExceptWith(child.GetDiagnostics());
            }

            return set.Count > 0;
        }

        public static bool FromScript(this SyntaxNode node)
        {
            if (node.SyntaxTree == null)
            {
                return false;
            }

            return node.SyntaxTree.Options.Kind != SourceCodeKind.Regular;
        }
    }
}
