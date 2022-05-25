// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class CodeGenerationHelpers
    {
        public static SyntaxNode? GenerateThrowStatement(
            SyntaxGenerator factory,
            SemanticDocument document,
            string exceptionMetadataName)
        {
            var compilation = document.SemanticModel.Compilation;
            var exceptionType = compilation.GetTypeByMetadataName(exceptionMetadataName);

            // If we can't find the Exception, we obviously can't generate anything.
            if (exceptionType == null)
            {
                return null;
            }

            var exceptionCreationExpression = factory.ObjectCreationExpression(
                exceptionType,
                SpecializedCollections.EmptyList<SyntaxNode>());

            return factory.ThrowStatement(exceptionCreationExpression);
        }

        [return: NotNullIfNotNull("syntax")]
        public static TSyntaxNode? AddAnnotationsTo<TSyntaxNode>(ISymbol symbol, TSyntaxNode? syntax) where TSyntaxNode : SyntaxNode
            => symbol is CodeGenerationSymbol codeGenerationSymbol
                ? syntax?.WithAdditionalAnnotations(codeGenerationSymbol.GetAnnotations())
                : syntax;

        public static TSyntaxNode AddFormatterAndCodeGeneratorAnnotationsTo<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => node.WithAdditionalAnnotations(Formatter.Annotation, CodeGenerator.Annotation);

        public static void GetNameAndInnermostNamespace(
            INamespaceSymbol @namespace,
            CodeGenerationContextInfo info,
            out string name,
            out INamespaceSymbol innermostNamespace)
        {
            if (info.Context.GenerateMembers && info.Context.MergeNestedNamespaces && @namespace.Name != string.Empty)
            {
                var names = new List<string>();
                names.Add(@namespace.Name);

                innermostNamespace = @namespace;
                while (true)
                {
                    var members = innermostNamespace.GetMembers().ToList();
                    if (members.Count == 1 &&
                        members[0] is INamespaceSymbol &&
                        CodeGenerationNamespaceInfo.GetImports(innermostNamespace).Count == 0)
                    {
                        var childNamespace = (INamespaceSymbol)members[0];
                        names.Add(childNamespace.Name);
                        innermostNamespace = childNamespace;
                        continue;
                    }

                    break;
                }

                name = string.Join(".", names.ToArray());
            }
            else
            {
                name = @namespace.Name;
                innermostNamespace = @namespace;
            }
        }

        public static bool IsSpecialType([NotNullWhen(true)] ITypeSymbol? type, SpecialType specialType)
            => type != null && type.SpecialType == specialType;

        public static int GetPreferredIndex(int index, IList<bool>? availableIndices, bool forward)
        {
            if (availableIndices == null)
                return index;

            if (forward)
            {
                for (var i = index; i < availableIndices.Count; i++)
                {
                    if (availableIndices[i])
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (var i = index; i >= 0; i--)
                {
                    if (availableIndices[i])
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static bool TryGetDocumentationComment(
            ISymbol symbol, string commentToken, [NotNullWhen(true)] out string? comment, CancellationToken cancellationToken = default)
        {
            var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
            if (string.IsNullOrEmpty(xml))
            {
                comment = null;
                return false;
            }

            var commentStarter = string.Concat(commentToken, " ");
            var newLineStarter = string.Concat("\n", commentStarter);

            // Start the comment with an empty line for visual clarity.
            comment = string.Concat(commentStarter, "\r\n", commentStarter, xml.Replace("\n", newLineStarter));
            return true;
        }

        public static bool TypesMatch(ITypeSymbol? type, object value)
            => type?.SpecialType switch
            {
                SpecialType.System_SByte => value is sbyte,
                SpecialType.System_Byte => value is byte,
                SpecialType.System_Int16 => value is short,
                SpecialType.System_UInt16 => value is ushort,
                SpecialType.System_Int32 => value is int,
                SpecialType.System_UInt32 => value is uint,
                SpecialType.System_Int64 => value is long,
                SpecialType.System_UInt64 => value is ulong,
                SpecialType.System_Decimal => value is decimal,
                SpecialType.System_Single => value is float,
                SpecialType.System_Double => value is double,
                _ => false,
            };

        public static IEnumerable<ISymbol> GetMembers(INamedTypeSymbol namedType)
        {
            if (namedType.TypeKind != TypeKind.Enum)
            {
                return namedType.GetMembers();
            }

            return namedType.GetMembers()
                            .OfType<IFieldSymbol>()
                            .OrderBy((f1, f2) =>
                            {
                                if (f1.HasConstantValue != f2.HasConstantValue)
                                {
                                    return f1.HasConstantValue ? 1 : -1;
                                }

                                return f1.HasConstantValue
                                    ? Comparer<object>.Default.Compare(f1.ConstantValue, f2.ConstantValue)
                                    : f1.Name.CompareTo(f2.Name);
                            }).ToList();
        }

        public static T RemoveLeadingDirectiveTrivia<T>(T node) where T : SyntaxNode
        {
            var leadingTrivia = node.GetLeadingTrivia().Where(trivia => !trivia.IsDirective);
            return node.WithLeadingTrivia(leadingTrivia);
        }

        public static T? GetReuseableSyntaxNodeForAttribute<T>(AttributeData attribute, CodeGenerationContextInfo info)
            where T : SyntaxNode
        {
            Contract.ThrowIfNull(attribute);

            return info.Context.ReuseSyntax && attribute.ApplicationSyntaxReference != null ?
                attribute.ApplicationSyntaxReference.GetSyntax() as T :
                null;
        }

        public static int GetInsertionIndex<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationContextInfo info,
            IList<bool>? availableIndices,
            IComparer<TDeclaration> comparerWithoutNameCheck,
            IComparer<TDeclaration> comparerWithNameCheck,
            Func<SyntaxList<TDeclaration>, TDeclaration?>? after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration?>? before = null)
            where TDeclaration : SyntaxNode
        {
            Contract.ThrowIfTrue(availableIndices != null && availableIndices.Count != declarationList.Count + 1);

            // Try to strictly obey the after option by inserting immediately after the member containing the location
            if (info.Context.AfterThisLocation != null)
            {
                var afterMember = declarationList.LastOrDefault(m => m.SpanStart <= info.Context.AfterThisLocation.SourceSpan.Start);
                if (afterMember != null)
                {
                    var index = declarationList.IndexOf(afterMember);
                    index = GetPreferredIndex(index + 1, availableIndices, forward: true);
                    if (index != -1)
                    {
                        return index;
                    }
                }
            }

            // Try to strictly obey the before option by inserting immediately before the member containing the location
            if (info.Context.BeforeThisLocation != null)
            {
                var beforeMember = declarationList.FirstOrDefault(m => m.Span.End >= info.Context.BeforeThisLocation.SourceSpan.End);
                if (beforeMember != null)
                {
                    var index = declarationList.IndexOf(beforeMember);
                    index = GetPreferredIndex(index, availableIndices, forward: false);
                    if (index != -1)
                    {
                        return index;
                    }
                }
            }

            if (info.Context.AutoInsertionLocation)
            {
                if (declarationList.IsEmpty())
                {
                    return 0;
                }

                var desiredIndex = TryGetDesiredIndexIfGrouped(
                    declarationList, declaration, availableIndices,
                    comparerWithoutNameCheck, comparerWithNameCheck);
                if (desiredIndex.HasValue)
                {
                    return desiredIndex.Value;
                }

                if (after != null)
                {
                    var member = after(declarationList);
                    if (member != null)
                    {
                        var index = declarationList.IndexOf(member);
                        if (index >= 0)
                        {
                            index = GetPreferredIndex(index + 1, availableIndices, forward: true);
                            if (index != -1)
                            {
                                return index;
                            }
                        }
                    }
                }

                if (before != null)
                {
                    var member = before(declarationList);
                    if (member != null)
                    {
                        var index = declarationList.IndexOf(member);

                        if (index >= 0)
                        {
                            index = GetPreferredIndex(index, availableIndices, forward: false);
                            if (index != -1)
                            {
                                return index;
                            }
                        }
                    }
                }
            }

            // Otherwise, add the declaration to the end.
            {
                var index = GetPreferredIndex(declarationList.Count, availableIndices, forward: false);
                if (index != -1)
                {
                    return index;
                }
            }

            return declarationList.Count;
        }

        public static int? TryGetDesiredIndexIfGrouped<TDeclarationSyntax>(
            SyntaxList<TDeclarationSyntax> declarationList,
            TDeclarationSyntax declaration,
            IList<bool>? availableIndices,
            IComparer<TDeclarationSyntax> comparerWithoutNameCheck,
            IComparer<TDeclarationSyntax> comparerWithNameCheck)
            where TDeclarationSyntax : SyntaxNode
        {
            var result = TryGetDesiredIndexIfGroupedWorker(
                declarationList, declaration, availableIndices,
                comparerWithoutNameCheck, comparerWithNameCheck);
            if (result == null)
            {
                return null;
            }

            result = GetPreferredIndex(result.Value, availableIndices, forward: true);
            if (result == -1)
            {
                return null;
            }

            return result;
        }

        private static int? TryGetDesiredIndexIfGroupedWorker<TDeclarationSyntax>(
            SyntaxList<TDeclarationSyntax> declarationList,
            TDeclarationSyntax declaration,
            IList<bool>? availableIndices,
            IComparer<TDeclarationSyntax> comparerWithoutNameCheck,
            IComparer<TDeclarationSyntax> comparerWithNameCheck)
            where TDeclarationSyntax : SyntaxNode
        {
            if (!declarationList.IsSorted(comparerWithoutNameCheck))
            {
                // Existing declarations weren't grouped.  Don't try to find a location
                // to this declaration into.
                return null;
            }

            // The list was grouped (by type, staticness, accessibility).  Try to find a location
            // to put the new declaration into.

            var result = Array.BinarySearch(declarationList.ToArray(), declaration, comparerWithoutNameCheck);
            var desiredGroupIndex = result < 0 ? ~result : result;
            Debug.Assert(desiredGroupIndex >= 0);
            Debug.Assert(desiredGroupIndex <= declarationList.Count);

            // Now, walk forward until we hit the last member of this group.
            while (desiredGroupIndex < declarationList.Count)
            {
                // Stop walking forward if we hit an unavailable index.
                if (availableIndices != null && !availableIndices[desiredGroupIndex])
                {
                    break;
                }

                if (0 != comparerWithoutNameCheck.Compare(declaration, declarationList[desiredGroupIndex]))
                {
                    // Found the index of an item not of our group.
                    break;
                }

                desiredGroupIndex++;
            }

            // Now, walk backward until we find the last member with the same name
            // as us.  We want to keep overloads together, so we'll place ourselves
            // after that member.
            var currentIndex = desiredGroupIndex;
            while (currentIndex > 0)
            {
                var previousIndex = currentIndex - 1;

                // Stop walking backward if we hit an unavailable index.
                if (availableIndices != null && !availableIndices[previousIndex])
                {
                    break;
                }

                if (0 != comparerWithoutNameCheck.Compare(declaration, declarationList[previousIndex]))
                {
                    // Hit the previous group of items.
                    break;
                }

                // Still in the same group.  If we find something with the same name
                // then place ourselves after it.
                if (0 == comparerWithNameCheck.Compare(declaration, declarationList[previousIndex]))
                {
                    // Found something with the same name.  Generate after this item.
                    return currentIndex;
                }

                currentIndex--;
            }

            // Couldn't find anything with our name.  Just place us at the end of this group.
            return desiredGroupIndex;
        }
    }
}
