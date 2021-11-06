// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    /// <summary>
    /// A line from <see cref="StackTraceAnalyzer.Parse(string, CancellationToken)"/> that
    /// was parsed by <see cref="StackFrameParser"/>
    /// </summary>
    internal sealed class ParsedStackFrame : ParsedFrame
    {
        public readonly StackFrameTree Tree;

        public ParsedStackFrame(
            string originalText,
            StackFrameTree tree)
            : base(originalText)
        {
            Tree = tree;
        }

        public StackFrameCompilationUnit Root => Tree.Root;

        public async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            // MemberAccessExpression is [Expression].[Identifier], and Identifier is the 
            // method name.
            var typeExpression = Root.MethodDeclaration.MemberAccessExpression.Left;
            var fullyQualifiedTypeName = typeExpression.ToString(skipTrivia: true);

            RoslynDebug.AssertNotNull(fullyQualifiedTypeName);

            var methodIdentifier = Root.MethodDeclaration.MemberAccessExpression.Right;
            var methodTypeArguments = Root.MethodDeclaration.TypeArguments;
            var methodArguments = Root.MethodDeclaration.ArgumentList;

            var methodName = methodIdentifier.ToString(skipTrivia: true);

            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var service = project.GetLanguageService<IStackTraceExplorerService>();
                if (service is null)
                {
                    continue;
                }

                var metadataName = service.GetTypeMetadataName(fullyQualifiedTypeName);
                var memberName = service.GetMethodSymbolName(methodName);

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var type = compilation.GetTypeByMetadataName(metadataName);
                if (type is null)
                {
                    continue;
                }

                var members = type.GetMembers();
                var matchingMembers = members
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Name == memberName)
                    .Where(m => MatchTypeArguments(m.TypeArguments, methodTypeArguments))
                    .Where(m => MatchParameters(m.Parameters, methodArguments))
                    .ToImmutableArrayOrEmpty();

                if (matchingMembers.Length == 0)
                {
                    continue;
                }

                if (matchingMembers.Length == 1)
                {
                    return matchingMembers[0];
                }
            }

            return null;

            static bool MatchParameters(ImmutableArray<IParameterSymbol> parameters, StackFrameParameterList stackFrameParameters)
            {
                if (parameters.Length != stackFrameParameters.Parameters.Length)
                {
                    return false;
                }

                for (var i = 0; i < stackFrameParameters.Parameters.Length; i++)
                {
                    var stackFrameParameter = stackFrameParameters.Parameters[i];
                    var paramSymbol = parameters[i];

                    if (paramSymbol.Name != stackFrameParameter.Identifier.ToString(skipTrivia: true))
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

            static bool MatchTypeArguments(ImmutableArray<ITypeSymbol> typeArguments, StackFrameTypeArgumentList? stackFrameTypeArgumentList)
            {
                if (stackFrameTypeArgumentList is null)
                {
                    return typeArguments.IsDefaultOrEmpty;
                }

                if (typeArguments.IsDefaultOrEmpty)
                {
                    return false;
                }

                var stackFrameTypeArguments = stackFrameTypeArgumentList.TypeArguments;
                return typeArguments.Length == stackFrameTypeArguments.Length;
            }

            static bool MatchType(ITypeSymbol type, StackFrameTypeNode stackFrameType)
            {
                if (type is IArrayTypeSymbol arrayType)
                {
                    if (stackFrameType is not StackFrameArrayTypeNode arrayTypeNode)
                    {
                        return false;
                    }

                    if (arrayType.Rank != arrayTypeNode.ArrayExpressions.Sum(exp => exp.CommaTokens.Length + 1))
                    {
                        return false;
                    }

                    return MatchType(arrayType.ElementType, arrayTypeNode.TypeIdentifier);
                }

                // Default to just comparing the display name
                return type.ToDisplayString() == stackFrameType.ToString();
            }
        }

        /// <summary>
        /// If the <see cref="Root"/> has file information, attempts to map it to existing documents
        /// in a solution. Does fulle filepath match if possible, otherwise does an approximate match
        /// since the file path may be very different on different machines
        /// </summary>
        internal (Document? document, int line) GetDocumentAndLine(Solution solution)
        {
            var fileMatches = GetFileMatches(solution, out var lineNumber);
            if (fileMatches.IsEmpty)
            {
                return (null, 0);
            }

            return (fileMatches.First(), lineNumber);
        }

        private ImmutableArray<Document> GetFileMatches(Solution solution, out int lineNumber)
        {
            lineNumber = 0;
            if (Root.FileInformationExpression is null)
            {
                return ImmutableArray<Document>.Empty;
            }

            var fileName = Root.FileInformationExpression.Path.ToString();
            var lineString = Root.FileInformationExpression.Line.ToString();
            RoslynDebug.AssertNotNull(lineString);
            lineNumber = int.Parse(lineString);

            var documentName = Path.GetFileName(fileName);
            var potentialMatches = new HashSet<Document>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == fileName)
                    {
                        return ImmutableArray.Create(document);
                    }

                    else if (document.Name == documentName)
                    {
                        potentialMatches.Add(document);
                    }
                }
            }

            return potentialMatches.ToImmutableArray();
        }
    }
}
