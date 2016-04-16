// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        public static TSyntaxNode AddCleanupAnnotationsTo<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
        {
            return node.WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static void CheckNodeType<TSyntaxNode1>(SyntaxNode node, string argumentName)
            where TSyntaxNode1 : SyntaxNode
        {
            if (node == null || node is TSyntaxNode1)
            {
                return;
            }

            throw new ArgumentException(WorkspacesResources.NodeIsOfTheWrongType, argumentName);
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
                for (int i = index; i < availableIndices.Count; i++)
                {
                    if (availableIndices[i])
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = index; i >= 0; i--)
                {
                    if (availableIndices[i])
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static bool TryGetDocumentationComment(ISymbol symbol, string commentToken, out string comment, CancellationToken cancellationToken = default(CancellationToken))
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

            return options != null && options.ReuseSyntax && symbol.DeclaringSyntaxReferences.Length == 1 ?
                symbol.DeclaringSyntaxReferences[0].GetSyntax() as T :
                null;
        }

        public static T GetReuseableSyntaxNodeForAttribute<T>(AttributeData attribute, CodeGenerationOptions options)
            where T : SyntaxNode
        {
            Contract.ThrowIfNull(attribute);

            return options != null && options.ReuseSyntax && attribute.ApplicationSyntaxReference != null ?
                attribute.ApplicationSyntaxReference.GetSyntax() as T :
                null;
        }
    }
}
