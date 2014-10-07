// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(SyntaxGenerator), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxGenerator : SyntaxGenerator
    {
        public override SyntaxNode CompilationUnit(IEnumerable<SyntaxNode> declarations = null)
        {
            if (declarations != null)
            {
                var usings = declarations.OfType<UsingDirectiveSyntax>();
                var typesAndNamespaces = declarations.OfType<MemberDeclarationSyntax>();

                return SyntaxFactory.CompilationUnit()
                    .WithUsings(SyntaxFactory.List(usings))
                    .WithMembers(SyntaxFactory.List(typesAndNamespaces));
            }
            else
            {
                return SyntaxFactory.CompilationUnit();
            }
        }

        public override SyntaxNode NamespaceImportDeclaration(SyntaxNode name)
        {
            return SyntaxFactory.UsingDirective((NameSyntax)name);
        }

        public override SyntaxNode NamespaceDeclaration(SyntaxNode name, IEnumerable<SyntaxNode> declarations)
        {
            var usings = declarations.OfType<UsingDirectiveSyntax>();
            var typesAndNamespaces = declarations.OfType<MemberDeclarationSyntax>();

            return SyntaxFactory.NamespaceDeclaration(
                (NameSyntax)name,
                default(SyntaxList<ExternAliasDirectiveSyntax>),
                SyntaxFactory.List(usings),
                SyntaxFactory.List(typesAndNamespaces));
        }

        public override SyntaxNode FieldDeclaration(
            string identifier,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            SyntaxNode initializer)
        {
            return SyntaxFactory.FieldDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, modifiers & fieldModifiers),
                SyntaxFactory.VariableDeclaration(
                    (TypeSyntax)type,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(identifier),
                            default(BracketedArgumentListSyntax),
                            initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null))));
        }

        public override SyntaxNode ParameterDeclaration(string name, SyntaxNode type, SyntaxNode initializer, RefKind refKind)
        {
            return SyntaxFactory.Parameter(
                default(SyntaxList<AttributeListSyntax>),
                GetParameterModifiers(refKind),
                (TypeSyntax)type,
                SyntaxFactory.Identifier(name),
                initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null);
        }

        private SyntaxTokenList GetParameterModifiers(RefKind refKind)
        {
            switch (refKind)
            {
                case RefKind.None:
                default:
                    return default(SyntaxTokenList);
                case RefKind.Out:
                    return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword));
                case RefKind.Ref:
                    return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword));
            }
        }

        public override SyntaxNode MethodDeclaration(
            string identifier,
            IEnumerable<SyntaxNode> parameters,
            IEnumerable<string> typeParameters,
            SyntaxNode returnType,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> statements)
        {
            bool hasBody = !modifiers.IsAbstract;

            return SyntaxFactory.MethodDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, modifiers & methodModifiers),
                returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                default(ExplicitInterfaceSpecifierSyntax),
                SyntaxFactory.Identifier(identifier),
                GetTypeParameters(typeParameters),
                GetParameters(parameters),
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                hasBody ? CreateBlock(statements) : null,
                !hasBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default(SyntaxToken));
        }

        private ParameterListSyntax GetParameters(IEnumerable<SyntaxNode> parameters)
        {
            return parameters != null
                ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast<ParameterSyntax>()))
                : SyntaxFactory.ParameterList();
        }

        public override SyntaxNode ConstructorDeclaration(
            string name, 
            IEnumerable<SyntaxNode> parameters, 
            Accessibility accessibility, 
            DeclarationModifiers modifiers, 
            IEnumerable<SyntaxNode> baseConstructorArguments, 
            IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.ConstructorDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, modifiers & constructorModifers),
                SyntaxFactory.Identifier(name ?? "ctor"),
                SyntaxFactory.ParameterList(parameters != null ? SyntaxFactory.SeparatedList(parameters.Cast<ParameterSyntax>()) : default(SeparatedSyntaxList<ParameterSyntax>)),
                baseConstructorArguments != null ? SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(baseConstructorArguments.Select(AsArgument)))) : null,
                CreateBlock(statements));
        }

        public override SyntaxNode PropertyDeclaration(
            string identifier,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> getterStatements,
            IEnumerable<SyntaxNode> setterStatements)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            var hasSetter = !modifiers.IsReadOnly;

            if (modifiers.IsAbstract)
            {
                getterStatements = null;
                setterStatements = null;
            }
            else
            {
                if (getterStatements == null)
                {
                    getterStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                if (setterStatements == null && !modifiers.IsReadOnly)
                {
                    setterStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }
            }

            accessors.Add(GetAccessorDeclaration(getterStatements));

            if (hasSetter)
            {
                accessors.Add(SetAccessorDeclaration(setterStatements));
            }

            return SyntaxFactory.PropertyDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, (modifiers & propertyModifiers) - DeclarationModifiers.ReadOnly),
                (TypeSyntax)type,
                default(ExplicitInterfaceSpecifierSyntax),
                SyntaxFactory.Identifier(identifier),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        public override SyntaxNode IndexerDeclaration(
            IEnumerable<SyntaxNode> parameters,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> getterStatements,
            IEnumerable<SyntaxNode> setterStatements)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            var hasSetter = !modifiers.IsReadOnly;

            if (modifiers.IsAbstract)
            {
                getterStatements = null;
                setterStatements = null;
            }
            else
            {
                if (getterStatements == null)
                {
                    getterStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                if (setterStatements == null && !modifiers.IsReadOnly)
                {
                    setterStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }
            }

            accessors.Add(GetAccessorDeclaration(getterStatements));

            if (hasSetter)
            {
                accessors.Add(SetAccessorDeclaration(setterStatements));
            }

            return SyntaxFactory.IndexerDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, (modifiers & indexerModifiers) - DeclarationModifiers.ReadOnly),
                (TypeSyntax)type,
                default(ExplicitInterfaceSpecifierSyntax),
                SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters.Cast<ParameterSyntax>())),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        private AccessorDeclarationSyntax GetAccessorDeclaration(IEnumerable<SyntaxNode> statements)
        {
            var ad = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.GetAccessorDeclaration,
                statements != null ? CreateBlock(statements) : null);

            if (statements == null)
            {
                ad = ad.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            return ad;
        }

        private AccessorDeclarationSyntax SetAccessorDeclaration(IEnumerable<SyntaxNode> statements)
        {
            var ad = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.SetAccessorDeclaration,
                statements != null ? CreateBlock(statements) : null);

            if (statements == null)
            {
                ad = ad.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            return ad;
        }

        public override SyntaxNode AsPublicInterfaceImplementation(SyntaxNode declaration, SyntaxNode typeName)
        {
            // C# interface implementations are implicit/not-specified
            return AsImplementation(declaration, Accessibility.Public);
        }

        public override SyntaxNode AsPrivateInterfaceImplementation(SyntaxNode declaration, SyntaxNode typeName)
        {
            var specifier = SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)typeName);

            declaration = AsImplementation(declaration, Accessibility.NotApplicable);

            var method = declaration as MethodDeclarationSyntax;
            if (method != null)
            {
                return method.WithExplicitInterfaceSpecifier(specifier);
            }

            var property = declaration as PropertyDeclarationSyntax;
            if (property != null)
            {
                return property.WithExplicitInterfaceSpecifier(specifier);
            }

            var indexer = declaration as IndexerDeclarationSyntax;
            if (indexer != null)
            {
                return indexer.WithExplicitInterfaceSpecifier(specifier);
            }

            return declaration;
        }

        private SyntaxNode AsImplementation(SyntaxNode declaration, Accessibility requiredAccess)
        {
            Accessibility access;
            DeclarationModifiers modifiers;

            var method = declaration as MethodDeclarationSyntax;
            if (method != null)
            {
                this.GetAccessibilityAndModifiers(method.Modifiers, out access, out modifiers);
                if (modifiers.IsAbstract || access != requiredAccess)
                {
                    method = method.WithModifiers(GetModifiers(requiredAccess, modifiers - DeclarationModifiers.Abstract));
                }

                if (method.Body == null)
                {
                    method = method.WithSemicolonToken(default(SyntaxToken)).WithBody(CreateBlock(null));
                }

                return method;
            }

            var prop = declaration as PropertyDeclarationSyntax;
            if (prop != null)
            {
                this.GetAccessibilityAndModifiers(prop.Modifiers, out access, out modifiers);
                if (modifiers.IsAbstract || access != requiredAccess)
                {
                    prop = prop.WithModifiers(GetModifiers(requiredAccess, modifiers - DeclarationModifiers.Abstract));
                }

                if (prop.AccessorList.Accessors.Any(a => a.Body == null))
                {
                    prop = prop.WithAccessorList(prop.AccessorList.WithAccessors(SyntaxFactory.List(prop.AccessorList.Accessors.Select(a =>
                        a.WithSemicolonToken(default(SyntaxToken)).WithBody(CreateBlock(null))))));
                }

                return prop;
            }

            var indexer = declaration as IndexerDeclarationSyntax;
            if (indexer != null)
            {
                this.GetAccessibilityAndModifiers(indexer.Modifiers, out access, out modifiers);
                if (modifiers.IsAbstract || access != requiredAccess)
                {
                    indexer = indexer.WithModifiers(GetModifiers(requiredAccess, modifiers - DeclarationModifiers.Abstract));
                }

                if (indexer.AccessorList.Accessors.Any(a => a.Body == null))
                {
                    indexer = indexer.WithAccessorList(indexer.AccessorList.WithAccessors(SyntaxFactory.List(indexer.AccessorList.Accessors.Select(a =>
                        a.WithSemicolonToken(default(SyntaxToken)).WithBody(CreateBlock(null))))));
                }

                return indexer;
            }

            return declaration;
        }

        public override SyntaxNode ClassDeclaration(
            string name,
            IEnumerable<string> typeParameters,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            SyntaxNode baseType,
            IEnumerable<SyntaxNode> interfaceTypes,
            IEnumerable<SyntaxNode> members)
        {
            List<BaseTypeSyntax> baseTypes = null;
            if (baseType != null || interfaceTypes != null)
            {
                baseTypes = new List<BaseTypeSyntax>();

                if (baseType != null)
                {
                    baseTypes.Add(SyntaxFactory.SimpleBaseType((TypeSyntax)baseType));
                }

                if (interfaceTypes != null)
                {
                    baseTypes.AddRange(interfaceTypes.Select(i => SyntaxFactory.SimpleBaseType((TypeSyntax)i)));
                }

                if (baseTypes.Count == 0)
                {
                    baseTypes = null;
                }
            }

            return SyntaxFactory.ClassDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, modifiers & typeModifiers),
                SyntaxFactory.Identifier(name),
                GetTypeParameters(typeParameters),
                baseTypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes)) : null,
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                GetClassMembers(name, members));
        }

        private SyntaxList<MemberDeclarationSyntax> GetClassMembers(string className, IEnumerable<SyntaxNode> members)
        {
            return members != null
                ? SyntaxFactory.List(members.Select(m => AsClassMember(m, className)))
                : default(SyntaxList<MemberDeclarationSyntax>);
        }

        private MemberDeclarationSyntax AsClassMember(SyntaxNode node, string className)
        {
            var cons = node as ConstructorDeclarationSyntax;
            if (cons != null)
            {
                return cons.WithIdentifier(SyntaxFactory.Identifier(className));
            }

            return (MemberDeclarationSyntax)node;
        }

        public override SyntaxNode StructDeclaration(
            string name,
            IEnumerable<string> typeParameters,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> interfaceTypes,
            IEnumerable<SyntaxNode> members)
        {
            var itypes = interfaceTypes != null ? interfaceTypes.Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)i)).ToList() : null;
            if (itypes != null && itypes.Count == 0)
            {
                itypes = null;
            }

            return SyntaxFactory.StructDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, modifiers & typeModifiers),
                SyntaxFactory.Identifier(name),
                GetTypeParameters(typeParameters),
                itypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(itypes)) : null,
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                GetClassMembers(name, members));
        }

        public override SyntaxNode InterfaceDeclaration(
            string name,
            IEnumerable<string> typeParameters,
            Accessibility accessibility,
            IEnumerable<SyntaxNode> interfaceTypes = null,
            IEnumerable<SyntaxNode> members = null)
        {
            var itypes = interfaceTypes != null ? interfaceTypes.Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)i)).ToList() : null;
            if (itypes != null && itypes.Count == 0)
            {
                itypes = null;
            }

            return SyntaxFactory.InterfaceDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, DeclarationModifiers.None),
                SyntaxFactory.Identifier(name),
                GetTypeParameters(typeParameters),
                itypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(itypes)) : null,
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                GetInterfaceMembers(members));
        }

        private SyntaxList<MemberDeclarationSyntax> GetInterfaceMembers(IEnumerable<SyntaxNode> members)
        {
            return members != null
                ? SyntaxFactory.List(members.Cast<MemberDeclarationSyntax>().Select(AsInterfaceMember))
                : default(SyntaxList<MemberDeclarationSyntax>);
        }

        private MemberDeclarationSyntax AsInterfaceMember(MemberDeclarationSyntax member)
        {
            var method = member as MethodDeclarationSyntax;
            if (method != null)
            {
                // no modifiers or body
                return method.WithModifiers(default(SyntaxTokenList))
                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                             .WithBody(null);
            }

            var property = member as PropertyDeclarationSyntax;
            if (property != null)
            {
                return property.WithModifiers(default(SyntaxTokenList))
                               .WithAccessorList(property.AccessorList.WithAccessors(
                                   SyntaxFactory.List(property.AccessorList.Accessors.Select(a => a.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(null)))));
            }

            var indexer = member as IndexerDeclarationSyntax;
            if (indexer != null)
            {
                return indexer.WithModifiers(default(SyntaxTokenList))
                              .WithAccessorList(indexer.AccessorList.WithAccessors(
                                  SyntaxFactory.List(indexer.AccessorList.Accessors.Select(a => a.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(null)))));
            }

            throw new ArgumentException("Declaration is not valid interface member.");
        }

        public override SyntaxNode EnumDeclaration(
            string name, 
            Accessibility accessibility, 
            IEnumerable<SyntaxNode> members)
        {
            return SyntaxFactory.EnumDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                GetModifiers(accessibility, DeclarationModifiers.None),
                SyntaxFactory.Identifier(name),
                default(BaseListSyntax),
                members != null ? SyntaxFactory.SeparatedList(members.Select(AsEnumMember)) : default(SeparatedSyntaxList<EnumMemberDeclarationSyntax>));
        }

        public override SyntaxNode EnumMember(string identifier, SyntaxNode expression)
        {
            return SyntaxFactory.EnumMemberDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                SyntaxFactory.Identifier(identifier),
                expression != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression) : null);
        }

        private EnumMemberDeclarationSyntax AsEnumMember(SyntaxNode node)
        {
            var ident = node as IdentifierNameSyntax;
            if (ident != null)
            {
                return (EnumMemberDeclarationSyntax)EnumMember(ident.Identifier.ToString(), null);
            }

            var field = node as FieldDeclarationSyntax;
            if (field != null && field.Declaration.Variables.Count == 1)
            {
                var variable = field.Declaration.Variables[0];
                return (EnumMemberDeclarationSyntax)EnumMember(variable.Identifier.ToString(), variable.Initializer);
            }

            return (EnumMemberDeclarationSyntax)node;
        }

        private SyntaxList<AttributeListSyntax> GetAttributeLists(IEnumerable<SyntaxNode> attributes)
        {
            if (attributes != null)
            {
                return SyntaxFactory.List(attributes.Select(AsAttributeList));
            }
            else
            {
                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        private AttributeListSyntax AsAttributeList(SyntaxNode node)
        {
            var attr = node as AttributeSyntax;
            if (attr != null)
            {
                return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr));
            }
            else
            {
                return (AttributeListSyntax)node;
            }
        }

        public override SyntaxNode Attribute(SyntaxNode name, IEnumerable<SyntaxNode> attributeArguments)
        {
            return AsAttributeList(SyntaxFactory.Attribute((NameSyntax)name, GetAttributeArguments(attributeArguments)));
        }

        public override SyntaxNode AttributeArgument(string name, SyntaxNode expression)
        {
            return name != null
                ? SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)), default(NameColonSyntax), (ExpressionSyntax)expression)
                : SyntaxFactory.AttributeArgument((ExpressionSyntax)expression);
        }

        private AttributeArgumentListSyntax GetAttributeArguments(IEnumerable<SyntaxNode> arguments)
        {
            if (arguments != null)
            {
                return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments.Select(AsAttributeArgument)));
            }
            else
            {
                return null;
            }
        }

        private AttributeArgumentSyntax AsAttributeArgument(SyntaxNode node)
        {
            var expr = node as ExpressionSyntax;
            if (expr != null)
            {
                return SyntaxFactory.AttributeArgument(expr);
            }

            var arg = node as ArgumentSyntax;
            if (arg != null)
            {
                return SyntaxFactory.AttributeArgument(default(NameEqualsSyntax), arg.NameColon, arg.Expression);
            }

            return (AttributeArgumentSyntax)node;
        }

        public override SyntaxNode AddAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes)
        {
            var lists = GetAttributeLists(attributes);

            var compUnit = declaration as CompilationUnitSyntax;
            if (compUnit != null)
            {
                var attributesWithAssemblyTarget = lists
                    .Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword))));

                return compUnit.WithAttributeLists(compUnit.AttributeLists.AddRange(attributesWithAssemblyTarget));
            }

            var member = declaration as MemberDeclarationSyntax;
            if (member != null)
            {
                return member.AddAttributeLists(lists.ToArray());
            }

            var parameter = declaration as ParameterSyntax;
            if (parameter != null)
            {
                return parameter.AddAttributeLists(lists.ToArray());
            }

            throw new ArgumentException("declaration");
        }

        public override SyntaxNode AddReturnAttributes(SyntaxNode methodDeclaration, IEnumerable<SyntaxNode> attributes)
        {
            var method = methodDeclaration as MethodDeclarationSyntax;
            if (method == null)
            {
                throw new ArgumentException("methodDeclaration");
            }

            var attributesWithReturnTarget = GetAttributeLists(attributes)
                .Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword))));

            return method.WithAttributeLists(method.AttributeLists.AddRange(attributesWithReturnTarget));
        }

        private SyntaxTokenList GetModifiers(Accessibility accessibility, DeclarationModifiers modifiers)
        {
            SyntaxTokenList list = SyntaxFactory.TokenList();

            switch (accessibility)
            {
                case Accessibility.Internal:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.Public:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                case Accessibility.Private:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.Protected:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                               .Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.NotApplicable:
                    break;
                default:
                    throw new NotSupportedException(string.Format("Accessibility '{0}' not supported.", accessibility));
            }

            if (modifiers.IsAbstract)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            }

            if (modifiers.IsNew)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));
            }

            if (modifiers.IsOverride)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            }

            if (modifiers.IsVirtual)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
            }

            if (modifiers.IsStatic)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }

            if (modifiers.IsAsync)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }

            if (modifiers.IsConst)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword));
            }

            if (modifiers.IsReadOnly)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            }

            if (modifiers.IsSealed)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
            }

            if (modifiers.IsUnsafe)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            if (modifiers.IsWithEvents)
            {
                throw new NotSupportedException("Unsupported modifier");
            }

            // partial must be last
            if (modifiers.IsPartial)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }

            return list;
        }

        private void GetAccessibilityAndModifiers(SyntaxTokenList modifierTokens, out Accessibility accessibility, out DeclarationModifiers modifiers)
        { 
            accessibility = Accessibility.NotApplicable;
            modifiers = DeclarationModifiers.None;

            foreach (var token in modifierTokens)
            {
                switch (token.CSharpKind())
                {
                    case SyntaxKind.PublicKeyword:
                        accessibility = Accessibility.Public;
                        break;

                    case SyntaxKind.PrivateKeyword:
                        accessibility = Accessibility.Private;
                        break;

                    case SyntaxKind.InternalKeyword:
                        if (accessibility == Accessibility.Protected)
                        {
                            accessibility = Accessibility.ProtectedOrInternal;
                        }
                        else
                        {
                            accessibility = Accessibility.Internal;
                        }

                        break;

                    case SyntaxKind.ProtectedKeyword:
                        if (accessibility == Accessibility.Internal)
                        {
                            accessibility = Accessibility.ProtectedOrInternal;
                        }
                        else
                        {
                            accessibility = Accessibility.Protected;
                        }

                        break;

                    case SyntaxKind.AbstractKeyword:
                        modifiers = modifiers | DeclarationModifiers.Abstract;
                        break;

                    case SyntaxKind.NewKeyword:
                        modifiers = modifiers | DeclarationModifiers.New;
                        break;

                    case SyntaxKind.OverrideKeyword:
                        modifiers = modifiers | DeclarationModifiers.Override;
                        break;

                    case SyntaxKind.VirtualKeyword:
                        modifiers = modifiers | DeclarationModifiers.Virtual;
                        break;

                    case SyntaxKind.StaticKeyword:
                        modifiers = modifiers | DeclarationModifiers.Static;
                        break;

                    case SyntaxKind.AsyncKeyword:
                        modifiers = modifiers | DeclarationModifiers.Async;
                        break;

                    case SyntaxKind.ConstKeyword:
                        modifiers = modifiers | DeclarationModifiers.Const;
                        break;

                    case SyntaxKind.ReadOnlyKeyword:
                        modifiers = modifiers | DeclarationModifiers.ReadOnly;
                        break;

                    case SyntaxKind.SealedKeyword:
                        modifiers = modifiers | DeclarationModifiers.Sealed;
                        break;

                    case SyntaxKind.UnsafeKeyword:
                        modifiers = modifiers | DeclarationModifiers.Unsafe;
                        break;

                    case SyntaxKind.PartialKeyword:
                        modifiers = modifiers | DeclarationModifiers.Partial;
                        break;
                }
            }
        }

        private TypeParameterListSyntax GetTypeParameters(IEnumerable<string> typeParameterNames)
        { 
            var typeParameters = typeParameterNames != null
                ? SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParameterNames.Select(name => SyntaxFactory.TypeParameter(name))))
                : null;

            if (typeParameters != null && typeParameters.Parameters.Count == 0)
            {
                typeParameters = null;
            }

            return typeParameters;
        }

        public override SyntaxNode WithTypeParameters(SyntaxNode declaration, IEnumerable<string> typeParameterNames)
        {
            var typeParameters = this.GetTypeParameters(typeParameterNames);

            switch (declaration.CSharpKind())
            {
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).WithTypeParameterList(typeParameters);

                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)declaration).WithTypeParameterList(typeParameters);

                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)declaration).WithTypeParameterList(typeParameters);

                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)declaration).WithTypeParameterList(typeParameters);

                default:
                    return declaration;
            }
        }

        public override SyntaxNode WithTypeConstraint(SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kinds, IEnumerable<SyntaxNode> types)
        {
            switch (declaration.CSharpKind())
            {
                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)declaration;
                    return method.WithConstraintClauses(WithTypeConstraints(method.ConstraintClauses, typeParameterName, kinds, types));

                case SyntaxKind.ClassDeclaration:
                    var cls = (ClassDeclarationSyntax)declaration;
                    return cls.WithConstraintClauses(WithTypeConstraints(cls.ConstraintClauses, typeParameterName, kinds, types));

                case SyntaxKind.StructDeclaration:
                    var str = (StructDeclarationSyntax)declaration;
                    return str.WithConstraintClauses(WithTypeConstraints(str.ConstraintClauses, typeParameterName, kinds, types));

                case SyntaxKind.InterfaceDeclaration:
                    var iface = (InterfaceDeclarationSyntax)declaration;
                    return iface.WithConstraintClauses(WithTypeConstraints(iface.ConstraintClauses, typeParameterName, kinds, types));

                default:
                    return declaration;
            }
        }

        private SyntaxList<TypeParameterConstraintClauseSyntax> WithTypeConstraints(
            SyntaxList<TypeParameterConstraintClauseSyntax> clauses, string typeParameterName, SpecialTypeConstraintKind kinds, IEnumerable<SyntaxNode> types)
        {
            var constraints = types != null
                ? SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>(types.Select(t => SyntaxFactory.TypeConstraint((TypeSyntax)t)))
                : SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>();

            if ((kinds & SpecialTypeConstraintKind.Constructor) != 0)
            {
                constraints = constraints.Add(SyntaxFactory.ConstructorConstraint());
            }

            var isReferenceType = (kinds & SpecialTypeConstraintKind.ReferenceType) != 0;
            var isValueType = (kinds & SpecialTypeConstraintKind.ValueType) != 0;

            if (isReferenceType || isValueType)
            {
                constraints = constraints.Insert(0, SyntaxFactory.ClassOrStructConstraint(isReferenceType ? SyntaxKind.ClassConstraint : SyntaxKind.StructConstraint));
            }

            var clause = clauses.FirstOrDefault(c => c.Name.Identifier.ToString() == typeParameterName);

            if (clause == null)
            {
                if (constraints.Count > 0)
                {
                    return clauses.Add(SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(typeParameterName), constraints));
                }
                else
                {
                    return clauses;
                }
            }
            else if (constraints.Count == 0)
            {
                return clauses.Remove(clause);
            }
            else
            {
                return clauses.Replace(clause, clause.WithConstraints(constraints));
            }
        }

        public override SyntaxNode ReturnStatement(SyntaxNode expressionOpt = null)
        {
            return SyntaxFactory.ReturnStatement((ExpressionSyntax)expressionOpt);
        }

        public override SyntaxNode ThrowStatement(SyntaxNode expressionOpt = null)
        {
            return SyntaxFactory.ThrowStatement((ExpressionSyntax)expressionOpt);
        }

        public override SyntaxNode IfStatement(SyntaxNode condition, IEnumerable<SyntaxNode> trueStatements, IEnumerable<SyntaxNode> falseStatements = null)
        {
            if (falseStatements == null)
            {
                return SyntaxFactory.IfStatement(
                    (ExpressionSyntax)condition,
                    CreateBlock(trueStatements));
            }
            else
            {
                var falseArray = AsReadOnlyList(falseStatements);

                // make else-if chain if false-statements contain only an if-statement
                return SyntaxFactory.IfStatement(
                    (ExpressionSyntax)condition,
                    CreateBlock(trueStatements),
                    SyntaxFactory.ElseClause(
                        falseArray.Count == 1 && falseArray[0] is IfStatementSyntax ? (StatementSyntax)falseArray[0] : CreateBlock(falseArray)));
            }
        }

        private BlockSyntax CreateBlock(IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.Block(GetStatements(statements));
        }

        private SyntaxList<StatementSyntax> GetStatements(IEnumerable<SyntaxNode> nodes)
        {
            if (nodes == null)
            {
                return default(SyntaxList<StatementSyntax>);
            }
            else
            {
                return SyntaxFactory.List(nodes.Select(AsStatement));
            }
        }

        private StatementSyntax AsStatement(SyntaxNode node)
        {
            var expression = node as ExpressionSyntax;
            if (expression != null)
            {
                return SyntaxFactory.ExpressionStatement(expression);
            }

            return (StatementSyntax)node;
        }

        public override SyntaxNode ExpressionStatement(SyntaxNode expression)
        {
            return SyntaxFactory.ExpressionStatement((ExpressionSyntax)expression);
        }

        public override SyntaxNode MemberAccessExpression(SyntaxNode expression, SyntaxNode simpleName)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ParenthesizeLeft((ExpressionSyntax)expression), 
                (SimpleNameSyntax)simpleName);
        }

        // parenthesize the left hand size of a member access, invocation or element access expression
        private ExpressionSyntax ParenthesizeLeft(ExpressionSyntax expression)
        {
            if (expression is TypeSyntax
                || expression.IsKind(SyntaxKind.ThisExpression)
                || expression.IsKind(SyntaxKind.BaseExpression)
                || expression.IsKind(SyntaxKind.ParenthesizedExpression)
                || expression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                || expression.IsKind(SyntaxKind.InvocationExpression)
                || expression.IsKind(SyntaxKind.ElementAccessExpression))
            {
                return expression;
            }
            else
            {
                return this.Parenthesize(expression);
            }
        }

        public override SyntaxNode ObjectCreationExpression(SyntaxNode type, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ObjectCreationExpression((TypeSyntax)type, CreateArgumentList(arguments), null);
        }

        private ArgumentListSyntax CreateArgumentList(IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ArgumentList(CreateArguments(arguments));
        }

        private SeparatedSyntaxList<ArgumentSyntax> CreateArguments(IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.SeparatedList(arguments.Select(AsArgument).Cast<ArgumentSyntax>());
        }

        private ArgumentSyntax AsArgument(SyntaxNode argOrExpression)
        {
            var arg = argOrExpression as ArgumentSyntax;
            if (arg != null)
            {
                return arg;
            }
            else
            {
                return SyntaxFactory.Argument((ExpressionSyntax)argOrExpression);
            }
        }

        public override SyntaxNode InvocationExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.InvocationExpression(ParenthesizeLeft((ExpressionSyntax)expression), CreateArgumentList(arguments));
        }

        public override SyntaxNode ElementAccessExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ElementAccessExpression(ParenthesizeLeft((ExpressionSyntax)expression), SyntaxFactory.BracketedArgumentList(CreateArguments(arguments)));
        }

        public override SyntaxNode DefaultExpression(SyntaxNode type)
        {
            return SyntaxFactory.DefaultExpression((TypeSyntax)type);
        }

        public override SyntaxNode DefaultExpression(ITypeSymbol type)
        {
            // If it's just a reference type, then "null" is the default expression for it.  Note:
            // this counts for actual reference type, or a type parameter with a 'class' constraint.
            // Also, if it's a nullable type, then we can use "null".
            if (type.IsReferenceType ||
                type.IsPointerType() ||
                type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0", 0));
            }

            // Default to a "default(<typename>)" expression.
            return SyntaxFactory.DefaultExpression(type.GenerateTypeSyntax());
        }

        private ExpressionSyntax Parenthesize(SyntaxNode expression)
        {
            return ((ExpressionSyntax)expression).Parenthesize();
        }

        public override SyntaxNode IsExpression(SyntaxNode expression, SyntaxNode type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, Parenthesize(expression), (TypeSyntax)type);
        }

        public override SyntaxNode AsExpression(SyntaxNode expression, SyntaxNode type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, Parenthesize(expression), (TypeSyntax)type);
        }

        public override SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression)
        {
            return SyntaxFactory.CastExpression((TypeSyntax)type, Parenthesize(expression));
        }

        public override SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression)
        {
            return SyntaxFactory.CastExpression((TypeSyntax)type, Parenthesize(expression));
        }

        public override SyntaxNode AssignmentStatement(SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)left, Parenthesize(right));
        }

        private SyntaxNode CreateBinaryExpression(SyntaxKind syntaxKind, SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.BinaryExpression(syntaxKind, Parenthesize(left), Parenthesize(right));
        }

        public override SyntaxNode ValueEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);
        }

        public override SyntaxNode ReferenceEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);
        }

        public override SyntaxNode ValueNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);
        }

        public override SyntaxNode ReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);
        }

        public override SyntaxNode LessThanExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LessThanExpression, left, right);
        }

        public override SyntaxNode LessThanOrEqualExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LessThanOrEqualExpression, left, right);
        }

        public override SyntaxNode GreaterThanExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.GreaterThanExpression, left, right);
        }

        public override SyntaxNode GreaterThanOrEqualExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, left, right);
        }

        public override SyntaxNode NegateExpression(SyntaxNode expression)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, Parenthesize(expression));
        }

        public override SyntaxNode AddExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.AddExpression, left, right);
        }

        public override SyntaxNode SubtractExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.SubtractExpression, left, right);
        }

        public override SyntaxNode MultiplyExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.MultiplyExpression, left, right);
        }

        public override SyntaxNode DivideExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.DivideExpression, left, right);
        }

        public override SyntaxNode ModuloExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.ModuloExpression, left, right);
        }

        public override SyntaxNode BitwiseAndExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.BitwiseAndExpression, left, right);
        }

        public override SyntaxNode BitwiseOrExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);
        }

        public override SyntaxNode BitwiseNotExpression(SyntaxNode operand)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.BitwiseNotExpression, Parenthesize(operand));
        }

        public override SyntaxNode LogicalAndExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LogicalAndExpression, left, right);
        }

        public override SyntaxNode LogicalOrExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LogicalOrExpression, left, right);
        }

        public override SyntaxNode LogicalNotExpression(SyntaxNode expression)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Parenthesize(expression));
        }

        public override SyntaxNode ConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse)
        {
            return SyntaxFactory.ConditionalExpression(Parenthesize(condition), Parenthesize(whenTrue), Parenthesize(whenFalse));
        }

        public override SyntaxNode CoalesceExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.CoalesceExpression, left, right);
        }

        public override SyntaxNode ThisExpression()
        {
            return SyntaxFactory.ThisExpression();
        }

        public override SyntaxNode BaseExpression()
        {
            return SyntaxFactory.BaseExpression();
        }

        public override SyntaxNode LiteralExpression(object value)
        {
            return ExpressionGenerator.GenerateNonEnumValueExpression(null, value, canUseFieldReference: true);
        }

        public override SyntaxNode IdentifierName(string identifier)
        {
            return identifier.ToIdentifierName();
        }

        public override SyntaxNode GenericName(string identifier, IEnumerable<SyntaxNode> typeArguments)
        {
            return SyntaxFactory.GenericName(identifier.ToIdentifierToken(),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast<TypeSyntax>())));
        }

        public override SyntaxNode WithTypeArguments(SyntaxNode expression, IEnumerable<SyntaxNode> typeArguments)
        {
            switch (expression.CSharpKind())
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    var sname = (SimpleNameSyntax)expression;
                    return SyntaxFactory.GenericName(sname.Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast<TypeSyntax>())));

                case SyntaxKind.QualifiedName:
                    var qname = (QualifiedNameSyntax)expression;
                    return SyntaxFactory.QualifiedName(qname.Left, (SimpleNameSyntax)WithTypeArguments(qname.Right, typeArguments));

                case SyntaxKind.AliasQualifiedName:
                    var aname = (AliasQualifiedNameSyntax)expression;
                    return SyntaxFactory.AliasQualifiedName(aname.Alias, (SimpleNameSyntax)WithTypeArguments(aname.Name, typeArguments));

                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    var sma = (MemberAccessExpressionSyntax)expression;
                    return SyntaxFactory.MemberAccessExpression(expression.CSharpKind(), sma.Expression, (SimpleNameSyntax)WithTypeArguments(sma.Name, typeArguments));

                default:
                    return expression;
            }
        }

        public override SyntaxNode QualifiedName(SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.QualifiedName((NameSyntax)left, (SimpleNameSyntax)right);
        }

        public override SyntaxNode TypeExpression(ITypeSymbol typeSymbol)
        {
            return typeSymbol.GenerateTypeSyntax();
        }

        public override SyntaxNode TypeExpression(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
                case SpecialType.System_Byte:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword));
                case SpecialType.System_Char:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword));
                case SpecialType.System_Decimal:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
                case SpecialType.System_Double:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
                case SpecialType.System_Int16:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword));
                case SpecialType.System_Int32:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
                case SpecialType.System_Int64:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword));
                case SpecialType.System_Object:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
                case SpecialType.System_SByte:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword));
                case SpecialType.System_Single:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword));
                case SpecialType.System_String:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
                case SpecialType.System_UInt16:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword));
                case SpecialType.System_UInt32:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
                case SpecialType.System_UInt64:
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword));
                default:
                    throw new NotSupportedException("Unsupported SpecialType");
            }
        }

        public override SyntaxNode ArrayTypeExpression(SyntaxNode type)
        {
            return SyntaxFactory.ArrayType((TypeSyntax)type, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()));
        }

        public override SyntaxNode NullableTypeExpression(SyntaxNode type)
        {
            if (type is NullableTypeSyntax)
            {
                return type;
            }
            else
            {
                return SyntaxFactory.NullableType((TypeSyntax)type);
            }
        }

        public override SyntaxNode Argument(string nameOpt, RefKind refKind, SyntaxNode expression)
        {
            return SyntaxFactory.Argument(
                nameOpt == null ? null : SyntaxFactory.NameColon(nameOpt),
                refKind == RefKind.Ref ? SyntaxFactory.Token(SyntaxKind.RefKeyword) :
                refKind == RefKind.Out ? SyntaxFactory.Token(SyntaxKind.OutKeyword) : default(SyntaxToken),
                (ExpressionSyntax)expression);
        }

        public override SyntaxNode LocalDeclarationStatement(SyntaxNode type, string name, SyntaxNode initializer, bool isConst)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                isConst ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)) : default(SyntaxTokenList),
                this.VariableDeclaration(type, name, initializer));
        }

        private VariableDeclarationSyntax VariableDeclaration(SyntaxNode type, string name, SyntaxNode expression = null)
        {
            return SyntaxFactory.VariableDeclaration(
                type == null ? SyntaxFactory.IdentifierName("var") : (TypeSyntax)type,
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        name.ToIdentifierToken(),
                        null,
                        expression == null ? null : SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression))));
        }

        public override SyntaxNode UsingStatement(SyntaxNode type, string name, SyntaxNode expression, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.UsingStatement(
                VariableDeclaration(type, name, expression),
                expression: null,
                statement: CreateBlock(statements));
        }

        public override SyntaxNode UsingStatement(SyntaxNode expression, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.UsingStatement(
                declaration: null,
                expression: (ExpressionSyntax)expression,
                statement: CreateBlock(statements));
        }

        public override SyntaxNode TryCatchStatement(IEnumerable<SyntaxNode> tryStatements, IEnumerable<SyntaxNode> catchClauses, IEnumerable<SyntaxNode> finallyStatements = null)
        {
            return SyntaxFactory.TryStatement(
                CreateBlock(tryStatements),
                catchClauses != null ? SyntaxFactory.List(catchClauses.Cast<CatchClauseSyntax>()) : default(SyntaxList<CatchClauseSyntax>),
                finallyStatements != null ? SyntaxFactory.FinallyClause(CreateBlock(finallyStatements)) : null);
        }

        public override SyntaxNode CatchClause(SyntaxNode type, string identifier, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration((TypeSyntax)type, SyntaxFactory.Identifier(identifier)), 
                filter: null, 
                block: CreateBlock(statements));
        }

        public override SyntaxNode WhileStatement(SyntaxNode condition, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.WhileStatement((ExpressionSyntax)condition, CreateBlock(statements));
        }

        public override SyntaxNode SwitchStatement(SyntaxNode expression, IEnumerable<SyntaxNode> caseClauses)
        {
            return SyntaxFactory.SwitchStatement(
                (ExpressionSyntax)expression,
                caseClauses.Cast<SwitchSectionSyntax>().ToSyntaxList());
        }

        public override SyntaxNode SwitchSection(IEnumerable<SyntaxNode> expressions, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(GetSwitchLabels(expressions), GetStatements(statements));
        }

        public override SyntaxNode DefaultSwitchSection(IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(SyntaxFactory.SingletonList(SyntaxFactory.DefaultSwitchLabel() as SwitchLabelSyntax), GetStatements(statements));
        }

        private SyntaxList<SwitchLabelSyntax> GetSwitchLabels(IEnumerable<SyntaxNode> expressions)
        {
            var labels = default(SyntaxList<SwitchLabelSyntax>);

            if (expressions != null)
            {
                labels = labels.AddRange(expressions.Select(e => SyntaxFactory.CaseSwitchLabel((ExpressionSyntax)e)));
            }

            return labels;
        }

        public override SyntaxNode ExitSwitchStatement()
        {
            return SyntaxFactory.BreakStatement();
        }

        public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> parameterDeclarations, SyntaxNode expression)
        {
            var prms = AsReadOnlyList(parameterDeclarations.Cast<ParameterSyntax>());

            if (prms.Count == 1 && prms[0].Type == null)
            {
                return SyntaxFactory.SimpleLambdaExpression(
                    prms[0],
                    (CSharpSyntaxNode)expression);
            }
            else
            {
                return SyntaxFactory.ParenthesizedLambdaExpression(
                    SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(prms)),
                    (CSharpSyntaxNode)expression);
            }
        }

        public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, SyntaxNode expression)
        {
            return ValueReturningLambdaExpression(lambdaParameters, expression);
        }

        public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> parameterDeclarations, IEnumerable<SyntaxNode> statements)
        {
            return ValueReturningLambdaExpression(parameterDeclarations, CreateBlock(statements));
        }

        public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, IEnumerable<SyntaxNode> statements)
        {
            return ValueReturningLambdaExpression(lambdaParameters, statements);
        }

        public override SyntaxNode LambdaParameter(string identifier, SyntaxNode type = null)
        {
            return ParameterDeclaration(identifier, type, null, RefKind.None);
        }

        private IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> sequence)
        {
            var list = sequence as IReadOnlyList<T>;

            if (list == null)
            {
                list = sequence.ToImmutableReadOnlyListOrEmpty();
            }

            return list;
        }
    }
}
