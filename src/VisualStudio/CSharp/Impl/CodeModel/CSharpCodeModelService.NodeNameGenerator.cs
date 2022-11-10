// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal partial class CSharpCodeModelService
    {
        protected override AbstractNodeNameGenerator CreateNodeNameGenerator()
            => new NodeNameGenerator();

        private class NodeNameGenerator : AbstractNodeNameGenerator
        {
            protected override bool IsNameableNode(SyntaxNode node)
                => CSharpCodeModelService.IsNameableNode(node);

            private static void AppendName(StringBuilder builder, NameSyntax name)
            {
                if (name.Kind() == SyntaxKind.QualifiedName)
                {
                    AppendName(builder, ((QualifiedNameSyntax)name).Left);
                }

                switch (name.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        AppendDotIfNeeded(builder);
                        builder.Append(((IdentifierNameSyntax)name).Identifier.ValueText);
                        break;

                    case SyntaxKind.GenericName:
                        var genericName = (GenericNameSyntax)name;
                        AppendDotIfNeeded(builder);
                        builder.Append(genericName.Identifier.ValueText);
                        AppendArity(builder, genericName.Arity);
                        break;

                    case SyntaxKind.AliasQualifiedName:
                        var aliasQualifiedName = (AliasQualifiedNameSyntax)name;
                        AppendName(builder, aliasQualifiedName.Alias);
                        builder.Append("::");
                        AppendName(builder, aliasQualifiedName.Name);
                        break;

                    case SyntaxKind.QualifiedName:
                        AppendName(builder, ((QualifiedNameSyntax)name).Right);
                        break;
                }
            }

            private static void AppendTypeName(StringBuilder builder, TypeSyntax type)
            {
                if (type is NameSyntax name)
                {
                    AppendName(builder, name);
                }
                else
                {
                    switch (type.Kind())
                    {
                        case SyntaxKind.PredefinedType:
                            builder.Append(((PredefinedTypeSyntax)type).Keyword.ValueText);
                            break;

                        case SyntaxKind.ArrayType:
                            var arrayType = (ArrayTypeSyntax)type;
                            AppendTypeName(builder, arrayType.ElementType);

                            var specifiers = arrayType.RankSpecifiers;
                            for (var i = 0; i < specifiers.Count; i++)
                            {
                                builder.Append('[');

                                var specifier = specifiers[i];
                                if (specifier.Rank > 1)
                                {
                                    builder.Append(',', specifier.Rank - 1);
                                }

                                builder.Append(']');
                            }

                            break;

                        case SyntaxKind.PointerType:
                            AppendTypeName(builder, ((PointerTypeSyntax)type).ElementType);
                            builder.Append('*');
                            break;

                        case SyntaxKind.NullableType:
                            AppendTypeName(builder, ((NullableTypeSyntax)type).ElementType);
                            builder.Append('?');
                            break;
                    }
                }
            }

            private static void AppendParameterList(StringBuilder builder, BaseParameterListSyntax parameterList)
            {
                builder.Append(parameterList is BracketedParameterListSyntax ? '[' : '(');

                var firstSeen = false;

                foreach (var parameter in parameterList.Parameters)
                {
                    if (firstSeen)
                    {
                        builder.Append(",");
                    }

                    if (parameter.Modifiers.Any(SyntaxKind.RefKeyword))
                    {
                        builder.Append("ref ");
                    }
                    else if (parameter.Modifiers.Any(SyntaxKind.OutKeyword))
                    {
                        builder.Append("out ");
                    }
                    else if (parameter.Modifiers.Any(SyntaxKind.ParamsKeyword))
                    {
                        builder.Append("params ");
                    }

                    AppendTypeName(builder, parameter.Type);

                    firstSeen = true;
                }

                builder.Append(parameterList is BracketedParameterListSyntax ? ']' : ')');
            }

            private static void AppendOperatorName(StringBuilder builder, SyntaxKind kind)
            {
                var name = "#op_" + kind.ToString();
                if (name.EndsWith("Keyword", StringComparison.Ordinal))
                {
                    name = name[..^7];
                }
                else if (name.EndsWith("Token", StringComparison.Ordinal))
                {
                    name = name[..^5];
                }

                builder.Append(name);
            }

            protected override void AppendNodeName(StringBuilder builder, SyntaxNode node)
            {
                Debug.Assert(node != null);
                Debug.Assert(IsNameableNode(node));

                AppendDotIfNeeded(builder);

                switch (node.Kind())
                {
                    case SyntaxKind.NamespaceDeclaration:
                    case SyntaxKind.FileScopedNamespaceDeclaration:
                        var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)node;
                        AppendName(builder, namespaceDeclaration.Name);
                        break;

                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                        var typeDeclaration = (TypeDeclarationSyntax)node;
                        builder.Append(typeDeclaration.Identifier.ValueText);
                        AppendArity(builder, typeDeclaration.Arity);
                        break;

                    case SyntaxKind.EnumDeclaration:
                        var enumDeclaration = (EnumDeclarationSyntax)node;
                        builder.Append(enumDeclaration.Identifier.ValueText);
                        break;

                    case SyntaxKind.DelegateDeclaration:
                        var delegateDeclaration = (DelegateDeclarationSyntax)node;
                        builder.Append(delegateDeclaration.Identifier.ValueText);
                        AppendArity(builder, delegateDeclaration.Arity);
                        break;

                    case SyntaxKind.EnumMemberDeclaration:
                        var enumMemberDeclaration = (EnumMemberDeclarationSyntax)node;
                        builder.Append(enumMemberDeclaration.Identifier.ValueText);
                        break;

                    case SyntaxKind.VariableDeclarator:
                        var variableDeclarator = (VariableDeclaratorSyntax)node;
                        builder.Append(variableDeclarator.Identifier.ValueText);
                        break;

                    case SyntaxKind.MethodDeclaration:
                        var methodDeclaration = (MethodDeclarationSyntax)node;
                        builder.Append(methodDeclaration.Identifier.ValueText);
                        AppendArity(builder, methodDeclaration.Arity);
                        AppendParameterList(builder, methodDeclaration.ParameterList);
                        break;

                    case SyntaxKind.OperatorDeclaration:
                        var operatorDeclaration = (OperatorDeclarationSyntax)node;
                        AppendOperatorName(builder, operatorDeclaration.OperatorToken.Kind());
                        AppendParameterList(builder, operatorDeclaration.ParameterList);
                        break;

                    case SyntaxKind.ConversionOperatorDeclaration:
                        var conversionOperatorDeclaration = (ConversionOperatorDeclarationSyntax)node;
                        AppendOperatorName(builder, conversionOperatorDeclaration.ImplicitOrExplicitKeyword.Kind());
                        builder.Append('_');
                        AppendTypeName(builder, conversionOperatorDeclaration.Type);
                        AppendParameterList(builder, conversionOperatorDeclaration.ParameterList);
                        break;

                    case SyntaxKind.ConstructorDeclaration:
                        var constructorDeclaration = (ConstructorDeclarationSyntax)node;
                        builder.Append(constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ? "#sctor" : "#ctor");
                        AppendParameterList(builder, constructorDeclaration.ParameterList);
                        break;

                    case SyntaxKind.DestructorDeclaration:
                        builder.Append("#dtor()");
                        break;

                    case SyntaxKind.IndexerDeclaration:
                        var indexerDeclaration = (IndexerDeclarationSyntax)node;
                        builder.Append("#this");
                        AppendParameterList(builder, indexerDeclaration.ParameterList);
                        break;

                    case SyntaxKind.PropertyDeclaration:
                        var propertyDeclaration = (PropertyDeclarationSyntax)node;
                        builder.Append(propertyDeclaration.Identifier.ValueText);
                        break;

                    case SyntaxKind.EventDeclaration:
                        var eventDeclaration = (EventDeclarationSyntax)node;
                        builder.Append(eventDeclaration.Identifier.ValueText);
                        break;
                }
            }
        }
    }
}
