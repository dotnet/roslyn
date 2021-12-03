// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal static class StackTraceExplorerUtilities
    {
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

            var methodIdentifier = compilationUnit.MethodDeclaration.MemberAccessExpression.Right;
            var methodTypeArguments = compilationUnit.MethodDeclaration.TypeArguments;
            var methodArguments = compilationUnit.MethodDeclaration.ArgumentList;

            var methodName = methodIdentifier.ToString();

            //
            // Do a first pass to find projects with the type name to check first 
            //
            using var _ = PooledObjects.ArrayBuilder<Project>.GetInstance(out var candidateProjects);
            foreach (var project in solution.Projects)
            {
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
                    var matchingMethods = await GetMatchingMembersFromCompilationAsync(project).ConfigureAwait(false);
                    if (matchingMethods.Any())
                    {
                        return await GetDefinitionAsync(matchingMethods[0]).ConfigureAwait(false);
                    }
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
                var matchingMethods = await GetMatchingMembersFromCompilationAsync(project).ConfigureAwait(false);
                if (matchingMethods.Any())
                {
                    return await GetDefinitionAsync(matchingMethods[0]).ConfigureAwait(false);
                }
            }

            return null;

            //
            // Local Functions
            //

            async Task<ImmutableArray<IMethodSymbol>> GetMatchingMembersFromCompilationAsync(Project project)
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var type = compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
                if (type is null)
                {
                    return ImmutableArray<IMethodSymbol>.Empty;
                }

                var members = type.GetMembers();
                return members
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Name == methodName)
                    .Where(m => MatchTypeArguments(m.TypeArguments, methodTypeArguments))
                    .Where(m => MatchParameters(m.Parameters, methodArguments))
                    .ToImmutableArrayOrEmpty();
            }

            Task<DefinitionItem> GetDefinitionAsync(IMethodSymbol method)
            {
                ISymbol symbol = method;
                if (symbolPart == StackFrameSymbolPart.ContainingType)
                {
                    symbol = method.ContainingType;
                }

                return symbol.ToNonClassifiedDefinitionItemAsync(solution, includeHiddenLocations: true, cancellationToken);
            }
        }

        private static bool MatchParameters(ImmutableArray<IParameterSymbol> parameters, StackFrameParameterList stackFrameParameters)
        {
            if (parameters.Length != stackFrameParameters.Parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < stackFrameParameters.Parameters.Length; i++)
            {
                var stackFrameParameter = stackFrameParameters.Parameters[i];
                var paramSymbol = parameters[i];

                if (paramSymbol.Name != stackFrameParameter.Identifier.ToString())
                {
                    return false;
                }

                if (!MatchType(paramSymbol.Type, stackFrameParameter.Type))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchTypeArguments(ImmutableArray<ITypeSymbol> typeArguments, StackFrameTypeArgumentList? stackFrameTypeArgumentList)
        {
            if (stackFrameTypeArgumentList is null)
            {
                return typeArguments.IsEmpty;
            }

            if (typeArguments.IsEmpty)
            {
                return false;
            }

            var stackFrameTypeArguments = stackFrameTypeArgumentList.TypeArguments;
            return typeArguments.Length == stackFrameTypeArguments.Length;
        }

        private static bool MatchType(ITypeSymbol type, StackFrameTypeNode stackFrameType)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                if (stackFrameType is not StackFrameArrayTypeNode arrayTypeNode)
                {
                    return false;
                }

                ITypeSymbol currentType = arrayType;

                // Iterate through each array expression and make sure the dimensions
                // match the element types in an array.
                // Ex: string[,][] 
                // [,] is a 2 dimension array with element type string[]
                // [] is a 1 dimension array with element type string
                foreach (var arrayExpression in arrayTypeNode.ArrayRankSpecifiers)
                {
                    if (currentType is not IArrayTypeSymbol currentArrayType)
                    {
                        return false;
                    }

                    if (currentArrayType.Rank != arrayExpression.CommaTokens.Length + 1)
                    {
                        return false;
                    }

                    currentType = currentArrayType.ElementType;
                }

                // All array types have been exchausted from the
                // stackframe identifier and the type is still an array
                if (currentType is IArrayTypeSymbol)
                {
                    return false;
                }

                return MatchType(currentType, arrayTypeNode.TypeIdentifier);
            }

            return type.Name == stackFrameType.ToString();
        }
    }
}
