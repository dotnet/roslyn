// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal static class StackTraceExplorerUtilities
{
    // Order is important here. Resolution should happen from most specific to least specific. 
    private static readonly AbstractStackTraceSymbolResolver[] _resolvers =
        [
            new StackFrameLocalMethodResolver(),
            new StackFrameMethodSymbolResolver(),
        ];

    public static async Task<DefinitionItem?> GetDefinitionAsync(Solution solution, StackFrameCompilationUnit compilationUnit, StackFrameSymbolPart symbolPart, CancellationToken cancellationToken)
    {
        // MemberAccessExpression is [Expression].[Identifier], and Identifier is the 
        // method name.
        var typeExpression = compilationUnit.MethodDeclaration.MemberAccessExpression.Left;

        // typeExpression.ToString() returns the full expression (or identifier)
        // including arity for generic types. 
        var fullyQualifiedTypeName = typeExpression.ToString();

        var typeName = typeExpression is StackFrameQualifiedNameNode qualifiedName
            ? qualifiedName.Right.ToString()
            : typeExpression.ToString();

        RoslynDebug.AssertNotNull(fullyQualifiedTypeName);

        var methodNode = compilationUnit.MethodDeclaration.MemberAccessExpression.Right;
        var methodTypeArguments = compilationUnit.MethodDeclaration.TypeArguments;
        var methodArguments = compilationUnit.MethodDeclaration.ArgumentList;

        //
        // Do a first pass to find projects with the type name to check first 
        //
        using var _ = PooledObjects.ArrayBuilder<Project>.GetInstance(out var candidateProjects);
        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!project.SupportsCompilation)
            {
                continue;
            }

            var containsSymbol = await project.ContainsSymbolsWithNameAsync(
                typeName,
                SymbolFilter.Type,
                cancellationToken).ConfigureAwait(false);

            if (containsSymbol)
            {
                var method = await TryGetBestMatchAsync(project, fullyQualifiedTypeName, methodNode, methodArguments, methodTypeArguments, cancellationToken).ConfigureAwait(false);
                if (method is not null)
                    return await GetDefinitionAsync(method).ConfigureAwait(false);
            }
            else
            {
                candidateProjects.Add(project);
            }
        }

        //
        // Do a second pass to check the remaining compilations
        // for the symbol, which may be a metadata symbol in the compilation
        //
        foreach (var project in candidateProjects)
        {
            var method = await TryGetBestMatchAsync(project, fullyQualifiedTypeName, methodNode, methodArguments, methodTypeArguments, cancellationToken).ConfigureAwait(false);
            if (method is not null)
                return await GetDefinitionAsync(method).ConfigureAwait(false);
        }

        return null;

        //
        // Local Functions
        //

        Task<DefinitionItem> GetDefinitionAsync(IMethodSymbol method)
        {
            ISymbol symbol = method;
            if (symbolPart == StackFrameSymbolPart.ContainingType)
            {
                symbol = method.ContainingType;
            }

            return symbol.ToNonClassifiedDefinitionItemAsync(
                solution,
                FindReferencesSearchOptions.Default with { UnidirectionalHierarchyCascade = true },
                includeHiddenLocations: true,
                cancellationToken);
        }
    }

    private static async Task<IMethodSymbol?> TryGetBestMatchAsync(Project project, string fullyQualifiedTypeName, StackFrameSimpleNameNode methodNode, StackFrameParameterList methodArguments, StackFrameTypeArgumentList? methodTypeArguments, CancellationToken cancellationToken)
    {
        var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var type = compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
        if (type is null)
        {
            return null;
        }

        foreach (var resolver in _resolvers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchingMethod = await resolver.TryGetBestMatchAsync(project, type, methodNode, methodArguments, methodTypeArguments, cancellationToken).ConfigureAwait(false);
            if (matchingMethod is not null)
            {
                return matchingMethod;
            }
        }

        return null;
    }
}
