// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class CodeGenerationHelpers
    {
        public static SyntaxNode GenerateThrowStatement(
            SyntaxGenerator factory,
            SemanticDocument document,
            string exceptionMetadataName,
            CancellationToken cancellationToken)
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

        public static TSyntaxNode AddAnnotationsTo<TSyntaxNode>(ISymbol symbol, TSyntaxNode syntax)
            where TSyntaxNode : SyntaxNode
        {
            if (syntax != null && symbol is CodeGenerationSymbol)
            {
                return syntax.WithAdditionalAnnotations(
                    ((CodeGenerationSymbol)symbol).GetAnnotations());
            }

            return syntax;
        }

        public static TSyntaxNode AddFormatterAndCodeGeneratorAnnotationsTo<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => node.WithAdditionalAnnotations(Formatter.Annotation, CodeGenerator.Annotation);

        public static void CheckNodeType<TSyntaxNode1>(SyntaxNode node, string argumentName)
            where TSyntaxNode1 : SyntaxNode
        {
            if (node == null || node is TSyntaxNode1)
            {
                return;
            }

            throw new ArgumentException(WorkspacesResources.Node_is_of_the_wrong_type, argumentName);
        }

        public static void GetNameAndInnermostNamespace(
            INamespaceSymbol @namespace,
            CodeGenerationOptions options,
            out string name,
            out INamespaceSymbol innermostNamespace)
        {
            if (options.GenerateMembers && options.MergeNestedNamespaces && @namespace.Name != string.Empty)
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

        public static bool IsSpecialType(ITypeSymbol type, SpecialType specialType)
        {
            return type != null && type.SpecialType == specialType;
        }

        public static int GetPreferredIndex(int index, IList<bool> availableIndices, bool forward)
        {
            if (availableIndices == null)
            {
                return index;
            }

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

        public static bool TryGetDocumentationComment(ISymbol symbol, string commentToken, out string comment, CancellationToken cancellationToken = default)
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

        public static bool TypesMatch(ITypeSymbol type, object value)
        {
            // No context type, have to assume that they don't match.
            if (type != null)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_SByte:
                        return value is sbyte;
                    case SpecialType.System_Byte:
                        return value is byte;
                    case SpecialType.System_Int16:
                        return value is short;
                    case SpecialType.System_UInt16:
                        return value is ushort;
                    case SpecialType.System_Int32:
                        return value is int;
                    case SpecialType.System_UInt32:
                        return value is uint;
                    case SpecialType.System_Int64:
                        return value is long;
                    case SpecialType.System_UInt64:
                        return value is ulong;
                    case SpecialType.System_Decimal:
                        return value is decimal;
                    case SpecialType.System_Single:
                        return value is float;
                    case SpecialType.System_Double:
                        return value is double;
                }
            }

            return false;
        }

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

        public static T GetReuseableSyntaxNodeForSymbol<T>(ISymbol symbol, CodeGenerationOptions options)
            where T : SyntaxNode
        {
            Contract.ThrowIfNull(symbol);

            return options != null && options.ReuseSyntax && symbol.DeclaringSyntaxReferences.Length == 1
                ? symbol.DeclaringSyntaxReferences[0].GetSyntax() as T
                : null;
        }

        public static T GetReuseableSyntaxNodeForAttribute<T>(AttributeData attribute, CodeGenerationOptions options)
            where T : SyntaxNode
        {
            Contract.ThrowIfNull(attribute);

            return options != null && options.ReuseSyntax && attribute.ApplicationSyntaxReference != null ?
                attribute.ApplicationSyntaxReference.GetSyntax() as T :
                null;
        }

        public static int GetInsertionIndex<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            IComparer<TDeclaration> comparerWithoutNameCheck,
            IComparer<TDeclaration> comparerWithNameCheck,
            Func<SyntaxList<TDeclaration>, TDeclaration> after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration> before = null)
            where TDeclaration : SyntaxNode
        {
            Contract.ThrowIfTrue(availableIndices != null && availableIndices.Count != declarationList.Count + 1);

            if (options != null)
            {
                // Try to strictly obey the after option by inserting immediately after the member containing the location
                if (options.AfterThisLocation != null)
                {
                    var afterMember = declarationList.LastOrDefault(m => m.SpanStart <= options.AfterThisLocation.SourceSpan.Start);
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
                if (options.BeforeThisLocation != null)
                {
                    var beforeMember = declarationList.FirstOrDefault(m => m.Span.End >= options.BeforeThisLocation.SourceSpan.End);
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

                if (options.AutoInsertionLocation)
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
            IList<bool> availableIndices,
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
            IList<bool> availableIndices,
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
