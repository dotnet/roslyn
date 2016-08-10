// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Async
{
    internal abstract partial class AbstractAddAsyncCodeFixProvider : AbstractAddAsyncAwaitCodeFixProvider
    {
        protected const string SystemThreadingTasksTask = "System.Threading.Tasks.Task";
        protected const string SystemThreadingTasksTaskT = "System.Threading.Tasks.Task`1";
        protected abstract SyntaxNode AddAsyncKeyword(SyntaxNode methodNode);
        protected abstract SyntaxNode AddAsyncKeywordAndTaskReturnType(SyntaxNode methodNode, ITypeSymbol existingReturnType, INamedTypeSymbol taskTypeSymbol);
        protected abstract bool DoesConversionExist(Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

        protected async Task<IList<DescriptionAndNode>> ConvertMethodToAsync(
            Document document, SemanticModel semanticModel, SyntaxNode methodNode, CancellationToken cancellationToken)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodNode, cancellationToken) as IMethodSymbol;

            var compilation = semanticModel.Compilation;
            var taskSymbol = compilation.GetTypeByMetadataName(SystemThreadingTasksTask);

            if (methodSymbol.ReturnsVoid)
            {
                return HandleVoidMethod(methodNode, taskSymbol);
            }

            var genericTaskSymbol = compilation.GetTypeByMetadataName(SystemThreadingTasksTaskT);
            if (taskSymbol == null || genericTaskSymbol == null)
            {
                return null;
            }

            var newNode = await AddAsyncKeywordAsync(
                document, methodNode, methodSymbol, compilation,
                taskSymbol, genericTaskSymbol, cancellationToken).ConfigureAwait(false);
            return SpecializedCollections.SingletonList(
                new DescriptionAndNode(FeaturesResources.Make_containing_scope_async, newNode));
        }

        private IList<DescriptionAndNode> HandleVoidMethod(SyntaxNode methodNode, INamedTypeSymbol taskSymbol)
        {
            var result = new List<DescriptionAndNode>();

            var method = AddAsyncKeyword(methodNode);
            if (method != null)
            {
                result.Add(new DescriptionAndNode(
                    FeaturesResources.Make_containing_scope_async,
                    method));
            }

            method = AddAsyncKeywordAndTaskReturnType(
                methodNode, existingReturnType: null, taskTypeSymbol: taskSymbol);
            if (method != null)
            {
                result.Add(new DescriptionAndNode(
                    FeaturesResources.Make_containing_scope_async_return_Task,
                    method));
            }

            return result;
        }

        private async Task<SyntaxNode> AddAsyncKeywordAsync(
            Document document, SyntaxNode methodNode, IMethodSymbol methodSymbol,
            Compilation compilation, INamedTypeSymbol taskSymbol,
            INamedTypeSymbol genericTaskSymbol, CancellationToken cancellationToken)
        {
            var returnType = methodSymbol.ReturnType;
            if (returnType is IErrorTypeSymbol)
            {
                // The return type of the method will not bind.  This could happen for a lot of reasons.
                // The type may not actually exist or the user could just be missing a using/import statement.
                // We're going to try and see if there are any known types that have the same name as 
                // our return type, and then check if those are convertible to Task.  If they are then
                // we assume the user just has a missing using.  If they are not, we wrap the return
                // type in a generic Task.
                var typeName = returnType.Name;
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var results = await SymbolFinder.FindDeclarationsAsync(
                    document.Project, typeName, ignoreCase: syntaxFacts.IsCaseSensitive, filter: SymbolFilter.Type, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (results.OfType<ITypeSymbol>().Any(s => DoesConversionExist(compilation, s, taskSymbol)))
                {
                    return AddAsyncKeyword(methodNode);
                }

                return AddAsyncKeywordAndTaskReturnType(methodNode, returnType, genericTaskSymbol);
            }

            if (DoesConversionExist(compilation, returnType, taskSymbol))
            {
                return AddAsyncKeyword(methodNode);
            }

            return AddAsyncKeywordAndTaskReturnType(methodNode, returnType, genericTaskSymbol);
        }
    }
}