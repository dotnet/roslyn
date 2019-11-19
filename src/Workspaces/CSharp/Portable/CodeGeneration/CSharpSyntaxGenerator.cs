// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(SyntaxGenerator), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxGenerator : SyntaxGenerator
    {
        [ImportingConstructor]
        public CSharpSyntaxGenerator()
        {
        }

        internal override SyntaxTrivia ElasticCarriageReturnLineFeed => SyntaxFactory.ElasticCarriageReturnLineFeed;
        internal override SyntaxTrivia CarriageReturnLineFeed => SyntaxFactory.CarriageReturnLineFeed;

        internal override bool RequiresExplicitImplementationForInterfaceMembers => false;

        internal override ISyntaxFactsService SyntaxFacts => CSharpSyntaxFactsService.Instance;

        internal override SyntaxTrivia EndOfLine(string text)
            => SyntaxFactory.EndOfLine(text);

        internal override SyntaxTrivia Whitespace(string text)
            => SyntaxFactory.Whitespace(text);

        internal override SeparatedSyntaxList<TElement> SeparatedList<TElement>(SyntaxNodeOrTokenList list)
            => SyntaxFactory.SeparatedList<TElement>(list);

        public static readonly SyntaxGenerator Instance = new CSharpSyntaxGenerator();

        #region Declarations
        public override SyntaxNode CompilationUnit(IEnumerable<SyntaxNode> declarations)
        {
            return SyntaxFactory.CompilationUnit()
                .WithUsings(this.AsUsingDirectives(declarations))
                .WithMembers(AsNamespaceMembers(declarations));
        }

        private SyntaxList<UsingDirectiveSyntax> AsUsingDirectives(IEnumerable<SyntaxNode> declarations)
        {
            return declarations != null
                ? SyntaxFactory.List(declarations.Select(this.AsUsingDirective).OfType<UsingDirectiveSyntax>())
                : default;
        }

        private SyntaxNode AsUsingDirective(SyntaxNode node)
        {
            if (node is NameSyntax name)
            {
                return this.NamespaceImportDeclaration(name);
            }

            return node as UsingDirectiveSyntax;
        }

        private static SyntaxList<MemberDeclarationSyntax> AsNamespaceMembers(IEnumerable<SyntaxNode> declarations)
        {
            return declarations != null
                ? SyntaxFactory.List(declarations.Select(AsNamespaceMember).OfType<MemberDeclarationSyntax>())
                : default;
        }

        private static SyntaxNode AsNamespaceMember(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                    return declaration;
                default:
                    return null;
            }
        }

        public override SyntaxNode NamespaceImportDeclaration(SyntaxNode name)
        {
            return SyntaxFactory.UsingDirective((NameSyntax)name);
        }

        public override SyntaxNode AliasImportDeclaration(string aliasIdentifierName, SyntaxNode name)
            => SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals(aliasIdentifierName), (NameSyntax)name);

        public override SyntaxNode NamespaceDeclaration(SyntaxNode name, IEnumerable<SyntaxNode> declarations)
        {
            return SyntaxFactory.NamespaceDeclaration(
                (NameSyntax)name,
                default,
                this.AsUsingDirectives(declarations),
                AsNamespaceMembers(declarations));
        }

        public override SyntaxNode FieldDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            SyntaxNode initializer)
        {
            return SyntaxFactory.FieldDeclaration(
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.FieldDeclaration),
                SyntaxFactory.VariableDeclaration(
                    (TypeSyntax)type,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            name.ToIdentifierToken(),
                            default,
                            initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null))));
        }

        public override SyntaxNode ParameterDeclaration(string name, SyntaxNode type, SyntaxNode initializer, RefKind refKind)
        {
            return SyntaxFactory.Parameter(
                default,
                GetParameterModifiers(refKind),
                (TypeSyntax)type,
                name.ToIdentifierToken(),
                initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null);
        }

        internal static SyntaxTokenList GetParameterModifiers(RefKind refKind)
            => refKind switch
            {
                RefKind.None => new SyntaxTokenList(),
                RefKind.Out => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
                RefKind.Ref => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
                RefKind.In => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)),
                _ => throw ExceptionUtilities.UnexpectedValue(refKind),
            };

        internal static SyntaxToken GetArgumentModifiers(RefKind refKind)
        {
            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    return default;
                case RefKind.Out:
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                case RefKind.Ref:
                    return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }
        }

        public override SyntaxNode MethodDeclaration(
            string name,
            IEnumerable<SyntaxNode> parameters,
            IEnumerable<string> typeParameters,
            SyntaxNode returnType,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> statements)
        {
            var hasBody = !modifiers.IsAbstract && (!modifiers.IsPartial || statements != null);

            return SyntaxFactory.MethodDeclaration(
                attributeLists: default,
                modifiers: AsModifierList(accessibility, modifiers, SyntaxKind.MethodDeclaration),
                returnType: returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                explicitInterfaceSpecifier: default,
                identifier: name.ToIdentifierToken(),
                typeParameterList: AsTypeParameterList(typeParameters),
                parameterList: AsParameterList(parameters),
                constraintClauses: default,
                body: hasBody ? CreateBlock(statements) : null,
                expressionBody: null,
                semicolonToken: !hasBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default);
        }

        public override SyntaxNode OperatorDeclaration(OperatorKind kind, IEnumerable<SyntaxNode> parameters = null, SyntaxNode returnType = null, Accessibility accessibility = Accessibility.NotApplicable, DeclarationModifiers modifiers = default, IEnumerable<SyntaxNode> statements = null)
        {
            var hasBody = !modifiers.IsAbstract && (!modifiers.IsPartial || statements != null);
            var returnTypeNode = returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var parameterList = AsParameterList(parameters);
            var body = hasBody ? CreateBlock(statements) : null;
            var semicolon = !hasBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default;
            var modifierList = AsModifierList(accessibility, modifiers, SyntaxKind.OperatorDeclaration);
            var attributes = default(SyntaxList<AttributeListSyntax>);

            if (kind == OperatorKind.ImplicitConversion || kind == OperatorKind.ExplicitConversion)
            {
                return SyntaxFactory.ConversionOperatorDeclaration(
                    attributes, modifierList, SyntaxFactory.Token(GetTokenKind(kind)),
                    SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                    returnTypeNode, parameterList, body, semicolon);
            }
            else
            {
                return SyntaxFactory.OperatorDeclaration(
                    attributes, modifierList, returnTypeNode,
                    SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                    SyntaxFactory.Token(GetTokenKind(kind)),
                    parameterList, body, semicolon);
            }
        }

        private static SyntaxKind GetTokenKind(OperatorKind kind)
            => kind switch
            {
                OperatorKind.ImplicitConversion => SyntaxKind.ImplicitKeyword,
                OperatorKind.ExplicitConversion => SyntaxKind.ExplicitKeyword,
                OperatorKind.Addition => SyntaxKind.PlusToken,
                OperatorKind.BitwiseAnd => SyntaxKind.AmpersandToken,
                OperatorKind.BitwiseOr => SyntaxKind.BarToken,
                OperatorKind.Decrement => SyntaxKind.MinusMinusToken,
                OperatorKind.Division => SyntaxKind.SlashToken,
                OperatorKind.Equality => SyntaxKind.EqualsEqualsToken,
                OperatorKind.ExclusiveOr => SyntaxKind.CaretToken,
                OperatorKind.False => SyntaxKind.FalseKeyword,
                OperatorKind.GreaterThan => SyntaxKind.GreaterThanToken,
                OperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanEqualsToken,
                OperatorKind.Increment => SyntaxKind.PlusPlusToken,
                OperatorKind.Inequality => SyntaxKind.ExclamationEqualsToken,
                OperatorKind.LeftShift => SyntaxKind.LessThanLessThanToken,
                OperatorKind.LessThan => SyntaxKind.LessThanToken,
                OperatorKind.LessThanOrEqual => SyntaxKind.LessThanEqualsToken,
                OperatorKind.LogicalNot => SyntaxKind.ExclamationToken,
                OperatorKind.Modulus => SyntaxKind.PercentToken,
                OperatorKind.Multiply => SyntaxKind.AsteriskToken,
                OperatorKind.OnesComplement => SyntaxKind.TildeToken,
                OperatorKind.RightShift => SyntaxKind.GreaterThanGreaterThanToken,
                OperatorKind.Subtraction => SyntaxKind.MinusToken,
                OperatorKind.True => SyntaxKind.TrueKeyword,
                OperatorKind.UnaryNegation => SyntaxKind.MinusToken,
                OperatorKind.UnaryPlus => SyntaxKind.PlusToken,
                _ => throw new ArgumentException("Unknown operator kind."),
            };

        private static ParameterListSyntax AsParameterList(IEnumerable<SyntaxNode> parameters)
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
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.ConstructorDeclaration),
                (name ?? "ctor").ToIdentifierToken(),
                AsParameterList(parameters),
                baseConstructorArguments != null ? SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(baseConstructorArguments.Select(AsArgument)))) : null,
                CreateBlock(statements));
        }

        public override SyntaxNode PropertyDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> getAccessorStatements,
            IEnumerable<SyntaxNode> setAccessorStatements)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            var hasGetter = !modifiers.IsWriteOnly;
            var hasSetter = !modifiers.IsReadOnly;

            if (modifiers.IsAbstract)
            {
                getAccessorStatements = null;
                setAccessorStatements = null;
            }
            else
            {
                if (getAccessorStatements == null && hasGetter)
                {
                    getAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                if (setAccessorStatements == null && hasSetter)
                {
                    setAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }
            }

            if (hasGetter)
            {
                accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getAccessorStatements));
            }

            if (hasSetter)
            {
                accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setAccessorStatements));
            }

            var actualModifiers = modifiers - (DeclarationModifiers.ReadOnly | DeclarationModifiers.WriteOnly);

            return SyntaxFactory.PropertyDeclaration(
                default,
                AsModifierList(accessibility, actualModifiers, SyntaxKind.PropertyDeclaration),
                (TypeSyntax)type,
                default,
                name.ToIdentifierToken(),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        public override SyntaxNode GetAccessorDeclaration(Accessibility accessibility, IEnumerable<SyntaxNode> statements)
            => AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, accessibility, statements);

        public override SyntaxNode SetAccessorDeclaration(Accessibility accessibility, IEnumerable<SyntaxNode> statements)
            => AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, accessibility, statements);

        private static SyntaxNode AccessorDeclaration(
            SyntaxKind kind, Accessibility accessibility, IEnumerable<SyntaxNode> statements)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(kind);
            accessor = accessor.WithModifiers(
                AsModifierList(accessibility, DeclarationModifiers.None, SyntaxKind.PropertyDeclaration));

            accessor = statements == null
                ? accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                : accessor.WithBody(CreateBlock(statements));

            return accessor;
        }

        public override SyntaxNode WithAccessorDeclarations(SyntaxNode declaration, IEnumerable<SyntaxNode> accessorDeclarations)
            => declaration switch
            {
                PropertyDeclarationSyntax property => property.WithAccessorList(CreateAccessorList(property.AccessorList, accessorDeclarations))
                                  .WithExpressionBody(null)
                                  .WithSemicolonToken(default),

                IndexerDeclarationSyntax indexer => indexer.WithAccessorList(CreateAccessorList(indexer.AccessorList, accessorDeclarations))
                                  .WithExpressionBody(null)
                                  .WithSemicolonToken(default),

                _ => declaration,
            };

        private AccessorListSyntax CreateAccessorList(AccessorListSyntax accessorListOpt, IEnumerable<SyntaxNode> accessorDeclarations)
        {
            var list = SyntaxFactory.List(accessorDeclarations.Cast<AccessorDeclarationSyntax>());
            return accessorListOpt == null
                ? SyntaxFactory.AccessorList(list)
                : accessorListOpt.WithAccessors(list);
        }

        public override SyntaxNode IndexerDeclaration(
            IEnumerable<SyntaxNode> parameters,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> getAccessorStatements,
            IEnumerable<SyntaxNode> setAccessorStatements)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            var hasGetter = !modifiers.IsWriteOnly;
            var hasSetter = !modifiers.IsReadOnly;

            if (modifiers.IsAbstract)
            {
                getAccessorStatements = null;
                setAccessorStatements = null;
            }
            else
            {
                if (getAccessorStatements == null && hasGetter)
                {
                    getAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                if (setAccessorStatements == null && hasSetter)
                {
                    setAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }
            }

            if (hasGetter)
            {
                accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getAccessorStatements));
            }

            if (hasSetter)
            {
                accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setAccessorStatements));
            }

            var actualModifiers = modifiers - (DeclarationModifiers.ReadOnly | DeclarationModifiers.WriteOnly);

            return SyntaxFactory.IndexerDeclaration(
                default,
                AsModifierList(accessibility, actualModifiers, SyntaxKind.IndexerDeclaration),
                (TypeSyntax)type,
                default,
                AsBracketedParameterList(parameters),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        private static BracketedParameterListSyntax AsBracketedParameterList(IEnumerable<SyntaxNode> parameters)
        {
            return parameters != null
                ? SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameters.Cast<ParameterSyntax>()))
                : SyntaxFactory.BracketedParameterList();
        }

        private static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, IEnumerable<SyntaxNode> statements)
        {
            var ad = SyntaxFactory.AccessorDeclaration(
                kind,
                statements != null ? CreateBlock(statements) : null);

            if (statements == null)
            {
                ad = ad.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            return ad;
        }

        public override SyntaxNode EventDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers)
        {
            return SyntaxFactory.EventFieldDeclaration(
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.EventFieldDeclaration),
                SyntaxFactory.VariableDeclaration(
                    (TypeSyntax)type,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(name))));
        }

        public override SyntaxNode CustomEventDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> parameters,
            IEnumerable<SyntaxNode> addAccessorStatements,
            IEnumerable<SyntaxNode> removeAccessorStatements)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            if (modifiers.IsAbstract)
            {
                addAccessorStatements = null;
                removeAccessorStatements = null;
            }
            else
            {
                if (addAccessorStatements == null)
                {
                    addAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                if (removeAccessorStatements == null)
                {
                    removeAccessorStatements = SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }
            }

            accessors.Add(AccessorDeclaration(SyntaxKind.AddAccessorDeclaration, addAccessorStatements));
            accessors.Add(AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration, removeAccessorStatements));

            return SyntaxFactory.EventDeclaration(
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.EventDeclaration),
                (TypeSyntax)type,
                default,
                name.ToIdentifierToken(),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        public override SyntaxNode AsPublicInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceTypeName, string interfaceMemberName)
        {
            // C# interface implementations are implicit/not-specified -- so they are just named the name as the interface member
            return PreserveTrivia(declaration, d =>
            {
                d = WithInterfaceSpecifier(d, null);
                d = this.AsImplementation(d, Accessibility.Public);

                if (interfaceMemberName != null)
                {
                    d = this.WithName(d, interfaceMemberName);
                }

                return d;
            });
        }

        public override SyntaxNode AsPrivateInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceTypeName, string interfaceMemberName)
        {
            return PreserveTrivia(declaration, d =>
            {
                d = this.AsImplementation(d, Accessibility.NotApplicable);
                d = this.WithoutConstraints(d);

                if (interfaceMemberName != null)
                {
                    d = this.WithName(d, interfaceMemberName);
                }

                return WithInterfaceSpecifier(d, SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)interfaceTypeName));
            });
        }

        private SyntaxNode WithoutConstraints(SyntaxNode declaration)
        {
            if (declaration.IsKind(SyntaxKind.MethodDeclaration))
            {
                var method = (MethodDeclarationSyntax)declaration;
                if (method.ConstraintClauses.Count > 0)
                {
                    return this.RemoveNodes(method, method.ConstraintClauses);
                }
            }

            return declaration;
        }

        private static SyntaxNode WithInterfaceSpecifier(SyntaxNode declaration, ExplicitInterfaceSpecifierSyntax specifier)
            => declaration.Kind() switch
            {
                SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
                SyntaxKind.EventDeclaration => ((EventDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
                _ => declaration,
            };

        private SyntaxNode AsImplementation(SyntaxNode declaration, Accessibility requiredAccess)
        {
            declaration = this.WithAccessibility(declaration, requiredAccess);
            declaration = this.WithModifiers(declaration, this.GetModifiers(declaration) - DeclarationModifiers.Abstract);
            declaration = WithBodies(declaration);
            return declaration;
        }

        private static SyntaxNode WithBodies(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)declaration;
                    return method.Body == null ? method.WithSemicolonToken(default).WithBody(CreateBlock(null)) : method;
                case SyntaxKind.OperatorDeclaration:
                    var op = (OperatorDeclarationSyntax)declaration;
                    return op.Body == null ? op.WithSemicolonToken(default).WithBody(CreateBlock(null)) : op;
                case SyntaxKind.ConversionOperatorDeclaration:
                    var cop = (ConversionOperatorDeclarationSyntax)declaration;
                    return cop.Body == null ? cop.WithSemicolonToken(default).WithBody(CreateBlock(null)) : cop;
                case SyntaxKind.PropertyDeclaration:
                    var prop = (PropertyDeclarationSyntax)declaration;
                    return prop.WithAccessorList(WithBodies(prop.AccessorList));
                case SyntaxKind.IndexerDeclaration:
                    var ind = (IndexerDeclarationSyntax)declaration;
                    return ind.WithAccessorList(WithBodies(ind.AccessorList));
                case SyntaxKind.EventDeclaration:
                    var ev = (EventDeclarationSyntax)declaration;
                    return ev.WithAccessorList(WithBodies(ev.AccessorList));
            }

            return declaration;
        }

        private static AccessorListSyntax WithBodies(AccessorListSyntax accessorList)
        {
            return accessorList.WithAccessors(SyntaxFactory.List(accessorList.Accessors.Select(x => WithBody(x))));
        }

        private static AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax accessor)
        {
            if (accessor.Body == null)
            {
                return accessor.WithSemicolonToken(default).WithBody(CreateBlock(null));
            }
            else
            {
                return accessor;
            }
        }

        private static AccessorListSyntax WithoutBodies(AccessorListSyntax accessorList)
        {
            return accessorList.WithAccessors(SyntaxFactory.List(accessorList.Accessors.Select(WithoutBody)));
        }

        private static AccessorDeclarationSyntax WithoutBody(AccessorDeclarationSyntax accessor)
        {
            return accessor.Body != null ? accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(null) : accessor;
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
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.ClassDeclaration),
                name.ToIdentifierToken(),
                AsTypeParameterList(typeParameters),
                baseTypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes)) : null,
                default,
                this.AsClassMembers(name, members));
        }

        private SyntaxList<MemberDeclarationSyntax> AsClassMembers(string className, IEnumerable<SyntaxNode> members)
        {
            return members != null
                ? SyntaxFactory.List(members.Select(m => this.AsClassMember(m, className)).Where(m => m != null))
                : default;
        }

        private MemberDeclarationSyntax AsClassMember(SyntaxNode node, string className)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    node = ((ConstructorDeclarationSyntax)node).WithIdentifier(className.ToIdentifierToken());
                    break;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    node = this.AsIsolatedDeclaration(node);
                    break;
            }

            return node as MemberDeclarationSyntax;
        }

        public override SyntaxNode StructDeclaration(
            string name,
            IEnumerable<string> typeParameters,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> interfaceTypes,
            IEnumerable<SyntaxNode> members)
        {
            var itypes = interfaceTypes?.Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)i)).ToList();
            if (itypes?.Count == 0)
            {
                itypes = null;
            }

            return SyntaxFactory.StructDeclaration(
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.StructDeclaration),
                name.ToIdentifierToken(),
                AsTypeParameterList(typeParameters),
                itypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(itypes)) : null,
                default,
                this.AsClassMembers(name, members));
        }

        public override SyntaxNode InterfaceDeclaration(
            string name,
            IEnumerable<string> typeParameters,
            Accessibility accessibility,
            IEnumerable<SyntaxNode> interfaceTypes = null,
            IEnumerable<SyntaxNode> members = null)
        {
            var itypes = interfaceTypes?.Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)i)).ToList();
            if (itypes?.Count == 0)
            {
                itypes = null;
            }

            return SyntaxFactory.InterfaceDeclaration(
                default,
                AsModifierList(accessibility, DeclarationModifiers.None),
                name.ToIdentifierToken(),
                AsTypeParameterList(typeParameters),
                itypes != null ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(itypes)) : null,
                default,
                this.AsInterfaceMembers(members));
        }

        private SyntaxList<MemberDeclarationSyntax> AsInterfaceMembers(IEnumerable<SyntaxNode> members)
        {
            return members != null
                ? SyntaxFactory.List(members.Select(this.AsInterfaceMember).OfType<MemberDeclarationSyntax>())
                : default;
        }

        internal override SyntaxNode AsInterfaceMember(SyntaxNode m)
        {
            return this.Isolate(m, member =>
            {
                Accessibility acc;
                DeclarationModifiers modifiers;

                switch (member.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member)
                                 .WithModifiers(default)
                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                 .WithBody(null);

                    case SyntaxKind.PropertyDeclaration:
                        var property = (PropertyDeclarationSyntax)member;
                        return property
                            .WithModifiers(default)
                            .WithAccessorList(WithoutBodies(property.AccessorList));

                    case SyntaxKind.IndexerDeclaration:
                        var indexer = (IndexerDeclarationSyntax)member;
                        return indexer
                            .WithModifiers(default)
                            .WithAccessorList(WithoutBodies(indexer.AccessorList));

                    // convert event into field event
                    case SyntaxKind.EventDeclaration:
                        var ev = (EventDeclarationSyntax)member;
                        return this.EventDeclaration(
                            ev.Identifier.ValueText,
                            ev.Type,
                            Accessibility.NotApplicable,
                            DeclarationModifiers.None);

                    case SyntaxKind.EventFieldDeclaration:
                        var ef = (EventFieldDeclarationSyntax)member;
                        return ef.WithModifiers(default);

                    // convert field into property
                    case SyntaxKind.FieldDeclaration:
                        var f = (FieldDeclarationSyntax)member;
                        GetAccessibilityAndModifiers(f.Modifiers, out acc, out modifiers);
                        return this.AsInterfaceMember(
                            this.PropertyDeclaration(this.GetName(f), this.ClearTrivia(this.GetType(f)), acc, modifiers, getAccessorStatements: null, setAccessorStatements: null));

                    default:
                        return null;
                }
            });
        }

        public override SyntaxNode EnumDeclaration(
            string name,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            IEnumerable<SyntaxNode> members)
        {
            return SyntaxFactory.EnumDeclaration(
                default,
                AsModifierList(accessibility, modifiers, SyntaxKind.EnumDeclaration),
                name.ToIdentifierToken(),
                default,
                this.AsEnumMembers(members));
        }

        public override SyntaxNode EnumMember(string name, SyntaxNode expression)
        {
            return SyntaxFactory.EnumMemberDeclaration(
                default,
                name.ToIdentifierToken(),
                expression != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression) : null);
        }

        private EnumMemberDeclarationSyntax AsEnumMember(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                    var id = (IdentifierNameSyntax)node;
                    return (EnumMemberDeclarationSyntax)this.EnumMember(id.Identifier.ToString(), null);

                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)node;
                    if (fd.Declaration.Variables.Count == 1)
                    {
                        var vd = fd.Declaration.Variables[0];
                        return (EnumMemberDeclarationSyntax)this.EnumMember(vd.Identifier.ToString(), vd.Initializer);
                    }
                    break;
            }

            return (EnumMemberDeclarationSyntax)node;
        }

        private SeparatedSyntaxList<EnumMemberDeclarationSyntax> AsEnumMembers(IEnumerable<SyntaxNode> members)
        {
            return members != null ? SyntaxFactory.SeparatedList(members.Select(this.AsEnumMember)) : default;
        }

        public override SyntaxNode DelegateDeclaration(
            string name,
            IEnumerable<SyntaxNode> parameters,
            IEnumerable<string> typeParameters,
            SyntaxNode returnType,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default)
        {
            return SyntaxFactory.DelegateDeclaration(
                default,
                AsModifierList(accessibility, modifiers),
                returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                name.ToIdentifierToken(),
                AsTypeParameterList(typeParameters),
                AsParameterList(parameters),
                default);
        }

        public override SyntaxNode Attribute(SyntaxNode name, IEnumerable<SyntaxNode> attributeArguments)
        {
            return AsAttributeList(SyntaxFactory.Attribute((NameSyntax)name, AsAttributeArgumentList(attributeArguments)));
        }

        public override SyntaxNode AttributeArgument(string name, SyntaxNode expression)
        {
            return name != null
                ? SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(name.ToIdentifierName()), default, (ExpressionSyntax)expression)
                : SyntaxFactory.AttributeArgument((ExpressionSyntax)expression);
        }

        private static AttributeArgumentListSyntax AsAttributeArgumentList(IEnumerable<SyntaxNode> arguments)
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

        private static AttributeArgumentSyntax AsAttributeArgument(SyntaxNode node)
        {
            if (node is ExpressionSyntax expr)
            {
                return SyntaxFactory.AttributeArgument(expr);
            }

            if (node is ArgumentSyntax arg && arg.Expression != null)
            {
                return SyntaxFactory.AttributeArgument(default, arg.NameColon, arg.Expression);
            }

            return (AttributeArgumentSyntax)node;
        }

        public override TNode ClearTrivia<TNode>(TNode node)
        {
            if (node != null)
            {
                return node.WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                           .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            }
            else
            {
                return null;
            }
        }

        private static SyntaxList<AttributeListSyntax> AsAttributeLists(IEnumerable<SyntaxNode> attributes)
        {
            if (attributes != null)
            {
                return SyntaxFactory.List(attributes.Select(AsAttributeList));
            }
            else
            {
                return default;
            }
        }

        private static AttributeListSyntax AsAttributeList(SyntaxNode node)
        {
            if (node is AttributeSyntax attr)
            {
                return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr));
            }
            else
            {
                return (AttributeListSyntax)node;
            }
        }

        private static readonly ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>> s_declAttributes
            = new ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>>();

        public override IReadOnlyList<SyntaxNode> GetAttributes(SyntaxNode declaration)
        {
            if (!s_declAttributes.TryGetValue(declaration, out var attrs))
            {
                var tmp = Flatten(GetAttributeLists(declaration).Where(al => !IsReturnAttribute(al)).ToImmutableReadOnlyListOrEmpty());
                attrs = s_declAttributes.GetValue(declaration, _d => tmp);
            }

            return attrs;
        }

        private static readonly ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>> s_declReturnAttributes
            = new ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>>();

        public override IReadOnlyList<SyntaxNode> GetReturnAttributes(SyntaxNode declaration)
        {
            if (!s_declReturnAttributes.TryGetValue(declaration, out var attrs))
            {
                var tmp = Flatten(GetAttributeLists(declaration).Where(al => IsReturnAttribute(al)).ToImmutableReadOnlyListOrEmpty());
                attrs = s_declReturnAttributes.GetValue(declaration, _d => tmp);
            }

            return attrs;
        }

        private static bool IsReturnAttribute(AttributeListSyntax list)
        {
            return list.Target?.Identifier.IsKind(SyntaxKind.ReturnKeyword) ?? false;
        }

        public override SyntaxNode InsertAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        {
            return this.Isolate(declaration, d => this.InsertAttributesInternal(d, index, attributes));
        }

        private SyntaxNode InsertAttributesInternal(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        {
            var newAttributes = AsAttributeLists(attributes);

            var existingAttributes = this.GetAttributes(declaration);
            if (index >= 0 && index < existingAttributes.Count)
            {
                return this.InsertNodesBefore(declaration, existingAttributes[index], newAttributes);
            }
            else if (existingAttributes.Count > 0)
            {
                return this.InsertNodesAfter(declaration, existingAttributes[existingAttributes.Count - 1], newAttributes);
            }
            else
            {
                var lists = GetAttributeLists(declaration);
                var newList = lists.AddRange(newAttributes);
                return WithAttributeLists(declaration, newList);
            }
        }

        public override SyntaxNode InsertReturnAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DelegateDeclaration:
                    return this.Isolate(declaration, d => this.InsertReturnAttributesInternal(d, index, attributes));
                default:
                    return declaration;
            }
        }

        private SyntaxNode InsertReturnAttributesInternal(SyntaxNode d, int index, IEnumerable<SyntaxNode> attributes)
        {
            var newAttributes = AsReturnAttributes(attributes);

            var existingAttributes = this.GetReturnAttributes(d);
            if (index >= 0 && index < existingAttributes.Count)
            {
                return this.InsertNodesBefore(d, existingAttributes[index], newAttributes);
            }
            else if (existingAttributes.Count > 0)
            {
                return this.InsertNodesAfter(d, existingAttributes[existingAttributes.Count - 1], newAttributes);
            }
            else
            {
                var lists = GetAttributeLists(d);
                var newList = lists.AddRange(newAttributes);
                return WithAttributeLists(d, newList);
            }
        }

        private static IEnumerable<AttributeListSyntax> AsReturnAttributes(IEnumerable<SyntaxNode> attributes)
        {
            return AsAttributeLists(attributes)
                .Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword))));
        }

        private static SyntaxList<AttributeListSyntax> AsAssemblyAttributes(IEnumerable<AttributeListSyntax> attributes)
        {
            return SyntaxFactory.List(
                    attributes.Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)))));
        }

        public override IReadOnlyList<SyntaxNode> GetAttributeArguments(SyntaxNode attributeDeclaration)
        {
            switch (attributeDeclaration.Kind())
            {
                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)attributeDeclaration;
                    if (list.Attributes.Count == 1)
                    {
                        return this.GetAttributeArguments(list.Attributes[0]);
                    }
                    break;
                case SyntaxKind.Attribute:
                    var attr = (AttributeSyntax)attributeDeclaration;
                    if (attr.ArgumentList != null)
                    {
                        return attr.ArgumentList.Arguments;
                    }
                    break;
            }

            return SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
        }

        public override SyntaxNode InsertAttributeArguments(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributeArguments)
        {
            return this.Isolate(declaration, d => InsertAttributeArgumentsInternal(d, index, attributeArguments));
        }

        private static SyntaxNode InsertAttributeArgumentsInternal(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributeArguments)
        {
            var newArgumentList = AsAttributeArgumentList(attributeArguments);

            var existingArgumentList = GetAttributeArgumentList(declaration);

            if (existingArgumentList == null)
            {
                return WithAttributeArgumentList(declaration, newArgumentList);
            }
            else if (newArgumentList != null)
            {
                return WithAttributeArgumentList(declaration, existingArgumentList.WithArguments(existingArgumentList.Arguments.InsertRange(index, newArgumentList.Arguments)));
            }
            else
            {
                return declaration;
            }
        }

        private static AttributeArgumentListSyntax GetAttributeArgumentList(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)declaration;
                    if (list.Attributes.Count == 1)
                    {
                        return list.Attributes[0].ArgumentList;
                    }
                    break;
                case SyntaxKind.Attribute:
                    var attr = (AttributeSyntax)declaration;
                    return attr.ArgumentList;
            }

            return null;
        }

        private static SyntaxNode WithAttributeArgumentList(SyntaxNode declaration, AttributeArgumentListSyntax argList)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)declaration;
                    if (list.Attributes.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, list.Attributes[0], list.Attributes[0].WithArgumentList(argList));
                    }
                    break;
                case SyntaxKind.Attribute:
                    var attr = (AttributeSyntax)declaration;
                    return attr.WithArgumentList(argList);
            }

            return declaration;
        }

        internal static SyntaxList<AttributeListSyntax> GetAttributeLists(SyntaxNode declaration)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.AttributeLists,
                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                ParameterSyntax parameter => parameter.AttributeLists,
                CompilationUnitSyntax compilationUnit => compilationUnit.AttributeLists,
                _ => default,
            };

        private static SyntaxNode WithAttributeLists(SyntaxNode declaration, SyntaxList<AttributeListSyntax> attributeLists)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.WithAttributeLists(attributeLists),
                AccessorDeclarationSyntax accessor => accessor.WithAttributeLists(attributeLists),
                ParameterSyntax parameter => parameter.WithAttributeLists(attributeLists),
                CompilationUnitSyntax compilationUnit => compilationUnit.WithAttributeLists(AsAssemblyAttributes(attributeLists)),
                _ => declaration,
            };

        internal override ImmutableArray<SyntaxNode> GetTypeInheritance(SyntaxNode declaration)
            => declaration is BaseTypeDeclarationSyntax baseType && baseType.BaseList != null
                ? ImmutableArray.Create<SyntaxNode>(baseType.BaseList)
                : ImmutableArray<SyntaxNode>.Empty;

        public override IReadOnlyList<SyntaxNode> GetNamespaceImports(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.CompilationUnit => ((CompilationUnitSyntax)declaration).Usings,
                SyntaxKind.NamespaceDeclaration => ((NamespaceDeclarationSyntax)declaration).Usings,
                _ => SpecializedCollections.EmptyReadOnlyList<SyntaxNode>(),
            };

        public override SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports)
        {
            return PreserveTrivia(declaration, d => this.InsertNamespaceImportsInternal(d, index, imports));
        }

        private SyntaxNode InsertNamespaceImportsInternal(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports)
        {
            var usingsToInsert = this.AsUsingDirectives(imports);

            switch (declaration.Kind())
            {
                case SyntaxKind.CompilationUnit:
                    var cu = (CompilationUnitSyntax)declaration;
                    return cu.WithUsings(cu.Usings.InsertRange(index, usingsToInsert));
                case SyntaxKind.NamespaceDeclaration:
                    var nd = (NamespaceDeclarationSyntax)declaration;
                    return nd.WithUsings(nd.Usings.InsertRange(index, usingsToInsert));
                default:
                    return declaration;
            }
        }

        public override IReadOnlyList<SyntaxNode> GetMembers(SyntaxNode declaration)
        {
            return Flatten(GetUnflattenedMembers(declaration));
        }

        private static IReadOnlyList<SyntaxNode> GetUnflattenedMembers(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntax)declaration).Members,
                SyntaxKind.StructDeclaration => ((StructDeclarationSyntax)declaration).Members,
                SyntaxKind.InterfaceDeclaration => ((InterfaceDeclarationSyntax)declaration).Members,
                SyntaxKind.EnumDeclaration => ((EnumDeclarationSyntax)declaration).Members,
                SyntaxKind.NamespaceDeclaration => ((NamespaceDeclarationSyntax)declaration).Members,
                SyntaxKind.CompilationUnit => ((CompilationUnitSyntax)declaration).Members,
                _ => SpecializedCollections.EmptyReadOnlyList<SyntaxNode>(),
            };

        private static IReadOnlyList<SyntaxNode> Flatten(IReadOnlyList<SyntaxNode> members)
        {
            if (members.Count == 0 || !members.Any(m => GetDeclarationCount(m) > 1))
            {
                return members;
            }

            var list = new List<SyntaxNode>();

            foreach (var declaration in members)
            {
                switch (declaration.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        Flatten(declaration, ((FieldDeclarationSyntax)declaration).Declaration, list);
                        break;
                    case SyntaxKind.EventFieldDeclaration:
                        Flatten(declaration, ((EventFieldDeclarationSyntax)declaration).Declaration, list);
                        break;
                    case SyntaxKind.LocalDeclarationStatement:
                        Flatten(declaration, ((LocalDeclarationStatementSyntax)declaration).Declaration, list);
                        break;
                    case SyntaxKind.VariableDeclaration:
                        Flatten(declaration, (VariableDeclarationSyntax)declaration, list);
                        break;
                    case SyntaxKind.AttributeList:
                        var attrList = (AttributeListSyntax)declaration;
                        if (attrList.Attributes.Count > 1)
                        {
                            list.AddRange(attrList.Attributes);
                        }
                        else
                        {
                            list.Add(attrList);
                        }
                        break;
                    default:
                        list.Add(declaration);
                        break;
                }
            }

            return list.ToImmutableReadOnlyListOrEmpty();
        }

        private static int GetDeclarationCount(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).Declaration.Variables.Count,
                SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).Declaration.Variables.Count,
                SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).Declaration.Variables.Count,
                SyntaxKind.VariableDeclaration => ((VariableDeclarationSyntax)declaration).Variables.Count,
                SyntaxKind.AttributeList => ((AttributeListSyntax)declaration).Attributes.Count,
                _ => 1,
            };

        private static void Flatten(SyntaxNode declaration, VariableDeclarationSyntax vd, List<SyntaxNode> flat)
        {
            if (vd.Variables.Count > 1)
            {
                flat.AddRange(vd.Variables);
            }
            else
            {
                flat.Add(declaration);
            }
        }

        public override SyntaxNode InsertMembers(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members)
        {
            var newMembers = this.AsMembersOf(declaration, members);

            var existingMembers = this.GetMembers(declaration);
            if (index >= 0 && index < existingMembers.Count)
            {
                return this.InsertNodesBefore(declaration, existingMembers[index], newMembers);
            }
            else if (existingMembers.Count > 0)
            {
                return this.InsertNodesAfter(declaration, existingMembers[existingMembers.Count - 1], newMembers);
            }
            else
            {
                switch (declaration.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                        var cd = (ClassDeclarationSyntax)declaration;
                        return cd.WithMembers(cd.Members.AddRange(newMembers));
                    case SyntaxKind.StructDeclaration:
                        var sd = (StructDeclarationSyntax)declaration;
                        return sd.WithMembers(sd.Members.AddRange(newMembers));
                    case SyntaxKind.InterfaceDeclaration:
                        var id = (InterfaceDeclarationSyntax)declaration;
                        return id.WithMembers(id.Members.AddRange(newMembers));
                    case SyntaxKind.EnumDeclaration:
                        var ed = (EnumDeclarationSyntax)declaration;
                        return ed.WithMembers(ed.Members.AddRange(newMembers.OfType<EnumMemberDeclarationSyntax>()));
                    case SyntaxKind.NamespaceDeclaration:
                        var nd = (NamespaceDeclarationSyntax)declaration;
                        return nd.WithMembers(nd.Members.AddRange(newMembers));
                    case SyntaxKind.CompilationUnit:
                        var cu = (CompilationUnitSyntax)declaration;
                        return cu.WithMembers(cu.Members.AddRange(newMembers));
                    default:
                        return declaration;
                }
            }
        }

        private IEnumerable<MemberDeclarationSyntax> AsMembersOf(SyntaxNode declaration, IEnumerable<SyntaxNode> members)
        {
            return members?.Select(m => this.AsMemberOf(declaration, m)).OfType<MemberDeclarationSyntax>();
        }

        private SyntaxNode AsMemberOf(SyntaxNode declaration, SyntaxNode member)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    var cd = (ClassDeclarationSyntax)declaration;
                    return this.AsClassMember(member, cd.Identifier.Text);
                case SyntaxKind.StructDeclaration:
                    var sd = (StructDeclarationSyntax)declaration;
                    return this.AsClassMember(member, sd.Identifier.Text);
                case SyntaxKind.InterfaceDeclaration:
                    return this.AsInterfaceMember(member);
                case SyntaxKind.EnumDeclaration:
                    return this.AsEnumMember(member);
                case SyntaxKind.NamespaceDeclaration:
                    return AsNamespaceMember(member);
                case SyntaxKind.CompilationUnit:
                    return AsNamespaceMember(member);
                default:
                    return null;
            }
        }

        internal override bool CanHaveAccessibility(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    var declarationKind = this.GetDeclarationKind(declaration);
                    return declarationKind == DeclarationKind.Field || declarationKind == DeclarationKind.Event;

                case SyntaxKind.ConstructorDeclaration:
                    // Static constructor can't have accessibility
                    return !((ConstructorDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)declaration;
                    if (method.ExplicitInterfaceSpecifier != null)
                    {
                        // explicit interface methods can't have accessibility.
                        return false;
                    }

                    if (method.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        // partial methods can't have accessibility modifiers.
                        return false;
                    }

                    return true;

                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                default:
                    return false;
            }
        }

        public override Accessibility GetAccessibility(SyntaxNode declaration)
        {
            if (!this.CanHaveAccessibility(declaration))
            {
                return Accessibility.NotApplicable;
            }

            var modifierTokens = GetModifierTokens(declaration);
            GetAccessibilityAndModifiers(modifierTokens, out var accessibility, out _);
            return accessibility;
        }

        public override SyntaxNode WithAccessibility(SyntaxNode declaration, Accessibility accessibility)
        {
            if (!this.CanHaveAccessibility(declaration) &&
                accessibility != Accessibility.NotApplicable)
            {
                return declaration;
            }

            return this.Isolate(declaration, d =>
            {
                var tokens = GetModifierTokens(d);
                GetAccessibilityAndModifiers(tokens, out _, out var modifiers);
                var newTokens = Merge(tokens, AsModifierList(accessibility, modifiers));
                return SetModifierTokens(d, newTokens);
            });
        }

        private static readonly DeclarationModifiers s_fieldModifiers = DeclarationModifiers.Const | DeclarationModifiers.New | DeclarationModifiers.ReadOnly | DeclarationModifiers.Static | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_methodModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.Async | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.Partial | DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Virtual | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_constructorModifiers = DeclarationModifiers.Static | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_destructorModifiers = DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_propertyModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.ReadOnly | DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Virtual | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_eventModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Virtual | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_eventFieldModifiers = DeclarationModifiers.New | DeclarationModifiers.Static | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_indexerModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.ReadOnly | DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Virtual | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_classModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_structModifiers = DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.ReadOnly | DeclarationModifiers.Ref | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_interfaceModifiers = DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
        private static readonly DeclarationModifiers s_accessorModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.Virtual;
        private static readonly DeclarationModifiers s_localFunctionModifiers = DeclarationModifiers.Async | DeclarationModifiers.Static;

        private static DeclarationModifiers GetAllowedModifiers(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                    return s_classModifiers;

                case SyntaxKind.EnumDeclaration:
                    return DeclarationModifiers.New;

                case SyntaxKind.DelegateDeclaration:
                    return DeclarationModifiers.New | DeclarationModifiers.Unsafe;

                case SyntaxKind.InterfaceDeclaration:
                    return s_interfaceModifiers;

                case SyntaxKind.StructDeclaration:
                    return s_structModifiers;

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    return s_methodModifiers;

                case SyntaxKind.ConstructorDeclaration:
                    return s_constructorModifiers;

                case SyntaxKind.DestructorDeclaration:
                    return s_destructorModifiers;

                case SyntaxKind.FieldDeclaration:
                    return s_fieldModifiers;

                case SyntaxKind.PropertyDeclaration:
                    return s_propertyModifiers;

                case SyntaxKind.IndexerDeclaration:
                    return s_indexerModifiers;

                case SyntaxKind.EventFieldDeclaration:
                    return s_eventFieldModifiers;

                case SyntaxKind.EventDeclaration:
                    return s_eventModifiers;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return s_accessorModifiers;

                case SyntaxKind.LocalFunctionStatement:
                    return s_localFunctionModifiers;

                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.Parameter:
                case SyntaxKind.LocalDeclarationStatement:
                default:
                    return DeclarationModifiers.None;
            }
        }

        public override DeclarationModifiers GetModifiers(SyntaxNode declaration)
        {
            var modifierTokens = GetModifierTokens(declaration);
            GetAccessibilityAndModifiers(modifierTokens, out _, out var modifiers);
            return modifiers;
        }

        public override SyntaxNode WithModifiers(SyntaxNode declaration, DeclarationModifiers modifiers)
        {
            return this.Isolate(declaration, d => this.WithModifiersInternal(d, modifiers));
        }

        private SyntaxNode WithModifiersInternal(SyntaxNode declaration, DeclarationModifiers modifiers)
        {
            modifiers &= GetAllowedModifiers(declaration.Kind());
            var existingModifiers = this.GetModifiers(declaration);

            if (modifiers != existingModifiers)
            {
                return this.Isolate(declaration, d =>
                {
                    var tokens = GetModifierTokens(d);
                    GetAccessibilityAndModifiers(tokens, out var accessibility, out var tmp);
                    var newTokens = Merge(tokens, AsModifierList(accessibility, modifiers));
                    return SetModifierTokens(d, newTokens);
                });
            }
            else
            {
                // no change
                return declaration;
            }
        }

        private static SyntaxTokenList GetModifierTokens(SyntaxNode declaration)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.Modifiers,
                ParameterSyntax parameter => parameter.Modifiers,
                LocalDeclarationStatementSyntax localDecl => localDecl.Modifiers,
                LocalFunctionStatementSyntax localFunc => localFunc.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                VariableDeclarationSyntax varDecl => GetModifierTokens(varDecl.Parent),
                VariableDeclaratorSyntax varDecl => GetModifierTokens(varDecl.Parent),
                _ => default,
            };

        private static SyntaxNode SetModifierTokens(SyntaxNode declaration, SyntaxTokenList modifiers)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.WithModifiers(modifiers),
                ParameterSyntax parameter => parameter.WithModifiers(modifiers),
                LocalDeclarationStatementSyntax localDecl => localDecl.WithModifiers(modifiers),
                LocalFunctionStatementSyntax localFunc => localFunc.WithModifiers(modifiers),
                AccessorDeclarationSyntax accessor => accessor.WithModifiers(modifiers),
                _ => declaration,
            };

        private static SyntaxTokenList AsModifierList(Accessibility accessibility, DeclarationModifiers modifiers, SyntaxKind kind)
        {
            return AsModifierList(accessibility, GetAllowedModifiers(kind) & modifiers);
        }

        private static SyntaxTokenList AsModifierList(Accessibility accessibility, DeclarationModifiers modifiers)
        {
            var list = SyntaxFactory.TokenList();

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
                case Accessibility.ProtectedAndInternal:
                    list = list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                               .Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.NotApplicable:
                    break;
            }

            if (modifiers.IsAbstract)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            }

            if (modifiers.IsNew)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));
            }

            if (modifiers.IsSealed)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
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

            if (modifiers.IsUnsafe)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            // partial and ref must be last
            if (modifiers.IsRef)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword));
            }

            if (modifiers.IsPartial)
            {
                list = list.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }

            return list;
        }

        private static void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers)
        {
            accessibility = Accessibility.NotApplicable;
            modifiers = DeclarationModifiers.None;

            foreach (var token in modifierList)
            {
                accessibility = (token.Kind(), accessibility) switch
                {
                    (SyntaxKind.PublicKeyword, _) => Accessibility.Public,

                    (SyntaxKind.PrivateKeyword, Accessibility.Protected) => Accessibility.ProtectedAndInternal,
                    (SyntaxKind.PrivateKeyword, _) => Accessibility.Private,

                    (SyntaxKind.InternalKeyword, Accessibility.Protected) => Accessibility.ProtectedOrInternal,
                    (SyntaxKind.InternalKeyword, _) => Accessibility.Internal,

                    (SyntaxKind.ProtectedKeyword, Accessibility.Private) => Accessibility.ProtectedAndInternal,
                    (SyntaxKind.ProtectedKeyword, Accessibility.Internal) => Accessibility.ProtectedOrInternal,
                    (SyntaxKind.ProtectedKeyword, _) => Accessibility.Protected,

                    _ => accessibility,
                };

                modifiers |= token.Kind() switch
                {
                    SyntaxKind.AbstractKeyword => DeclarationModifiers.Abstract,
                    SyntaxKind.NewKeyword => DeclarationModifiers.New,
                    SyntaxKind.OverrideKeyword => DeclarationModifiers.Override,
                    SyntaxKind.VirtualKeyword => DeclarationModifiers.Virtual,
                    SyntaxKind.StaticKeyword => DeclarationModifiers.Static,
                    SyntaxKind.AsyncKeyword => DeclarationModifiers.Async,
                    SyntaxKind.ConstKeyword => DeclarationModifiers.Const,
                    SyntaxKind.ReadOnlyKeyword => DeclarationModifiers.ReadOnly,
                    SyntaxKind.SealedKeyword => DeclarationModifiers.Sealed,
                    SyntaxKind.UnsafeKeyword => DeclarationModifiers.Unsafe,
                    SyntaxKind.PartialKeyword => DeclarationModifiers.Partial,
                    SyntaxKind.RefKeyword => DeclarationModifiers.Ref,
                    _ => DeclarationModifiers.None,
                };
            }
        }

        private static TypeParameterListSyntax AsTypeParameterList(IEnumerable<string> typeParameterNames)
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
            var typeParameters = AsTypeParameterList(typeParameterNames);

            return declaration.Kind() switch
            {
                SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).WithTypeParameterList(typeParameters),
                SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntax)declaration).WithTypeParameterList(typeParameters),
                SyntaxKind.StructDeclaration => ((StructDeclarationSyntax)declaration).WithTypeParameterList(typeParameters),
                SyntaxKind.InterfaceDeclaration => ((InterfaceDeclarationSyntax)declaration).WithTypeParameterList(typeParameters),
                SyntaxKind.DelegateDeclaration => ((DelegateDeclarationSyntax)declaration).WithTypeParameterList(typeParameters),
                _ => declaration,
            };
        }

        internal override SyntaxNode WithExplicitInterfaceImplementations(SyntaxNode declaration, ImmutableArray<ISymbol> explicitInterfaceImplementations)
            => WithAccessibility(declaration switch
            {
                MethodDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(CreateExplicitInterfaceSpecifier(explicitInterfaceImplementations)),
                PropertyDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(CreateExplicitInterfaceSpecifier(explicitInterfaceImplementations)),
                EventDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(CreateExplicitInterfaceSpecifier(explicitInterfaceImplementations)),
                _ => declaration,
            }, Accessibility.NotApplicable);

        private ExplicitInterfaceSpecifierSyntax CreateExplicitInterfaceSpecifier(ImmutableArray<ISymbol> explicitInterfaceImplementations)
            => SyntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceImplementations[0].ContainingType.GenerateNameSyntax());

        public override SyntaxNode WithTypeConstraint(SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kinds, IEnumerable<SyntaxNode> types)
        {
            switch (declaration.Kind())
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

                case SyntaxKind.DelegateDeclaration:
                    var del = (DelegateDeclarationSyntax)declaration;
                    return del.WithConstraintClauses(WithTypeConstraints(del.ConstraintClauses, typeParameterName, kinds, types));
                default:
                    return declaration;
            }
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> WithTypeConstraints(
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
                    return clauses.Add(SyntaxFactory.TypeParameterConstraintClause(typeParameterName.ToIdentifierName(), constraints));
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

        public override DeclarationKind GetDeclarationKind(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return DeclarationKind.Class;
                case SyntaxKind.StructDeclaration:
                    return DeclarationKind.Struct;
                case SyntaxKind.InterfaceDeclaration:
                    return DeclarationKind.Interface;
                case SyntaxKind.EnumDeclaration:
                    return DeclarationKind.Enum;
                case SyntaxKind.DelegateDeclaration:
                    return DeclarationKind.Delegate;

                case SyntaxKind.MethodDeclaration:
                    return DeclarationKind.Method;
                case SyntaxKind.OperatorDeclaration:
                    return DeclarationKind.Operator;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return DeclarationKind.ConversionOperator;
                case SyntaxKind.ConstructorDeclaration:
                    return DeclarationKind.Constructor;
                case SyntaxKind.DestructorDeclaration:
                    return DeclarationKind.Destructor;

                case SyntaxKind.PropertyDeclaration:
                    return DeclarationKind.Property;
                case SyntaxKind.IndexerDeclaration:
                    return DeclarationKind.Indexer;
                case SyntaxKind.EventDeclaration:
                    return DeclarationKind.CustomEvent;
                case SyntaxKind.EnumMemberDeclaration:
                    return DeclarationKind.EnumMember;
                case SyntaxKind.CompilationUnit:
                    return DeclarationKind.CompilationUnit;
                case SyntaxKind.NamespaceDeclaration:
                    return DeclarationKind.Namespace;
                case SyntaxKind.UsingDirective:
                    return DeclarationKind.NamespaceImport;
                case SyntaxKind.Parameter:
                    return DeclarationKind.Parameter;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return DeclarationKind.LambdaExpression;

                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration != null && fd.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Field;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.EventFieldDeclaration:
                    var ef = (EventFieldDeclarationSyntax)declaration;
                    if (ef.Declaration != null && ef.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Event;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration != null && ld.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Variable;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.VariableDeclaration:
                    {
                        var vd = (VariableDeclarationSyntax)declaration;
                        if (vd.Variables.Count == 1 && vd.Parent == null)
                        {
                            // this node is the declaration if it contains only one variable and has no parent.
                            return DeclarationKind.Variable;
                        }
                        else
                        {
                            return DeclarationKind.None;
                        }
                    }

                case SyntaxKind.VariableDeclarator:
                    {
                        var vd = declaration.Parent as VariableDeclarationSyntax;

                        // this node is considered the declaration if it is one among many, or it has no parent
                        if (vd == null || vd.Variables.Count > 1)
                        {
                            if (ParentIsFieldDeclaration(vd))
                            {
                                return DeclarationKind.Field;
                            }
                            else if (ParentIsEventFieldDeclaration(vd))
                            {
                                return DeclarationKind.Event;
                            }
                            else
                            {
                                return DeclarationKind.Variable;
                            }
                        }
                        break;
                    }

                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)declaration;
                    if (list.Attributes.Count == 1)
                    {
                        return DeclarationKind.Attribute;
                    }
                    break;

                case SyntaxKind.Attribute:
                    if (!(declaration.Parent is AttributeListSyntax parentList) || parentList.Attributes.Count > 1)
                    {
                        return DeclarationKind.Attribute;
                    }
                    break;

                case SyntaxKind.GetAccessorDeclaration:
                    return DeclarationKind.GetAccessor;
                case SyntaxKind.SetAccessorDeclaration:
                    return DeclarationKind.SetAccessor;
                case SyntaxKind.AddAccessorDeclaration:
                    return DeclarationKind.AddAccessor;
                case SyntaxKind.RemoveAccessorDeclaration:
                    return DeclarationKind.RemoveAccessor;
            }

            return DeclarationKind.None;
        }

        private static bool ParentIsFieldDeclaration(SyntaxNode node)
        {
            return node?.Parent.IsKind(SyntaxKind.FieldDeclaration) ?? false;
        }

        private static bool ParentIsEventFieldDeclaration(SyntaxNode node)
        {
            return node?.Parent.IsKind(SyntaxKind.EventFieldDeclaration) ?? false;
        }

        private static bool ParentIsLocalDeclarationStatement(SyntaxNode node)
        {
            return node?.Parent.IsKind(SyntaxKind.LocalDeclarationStatement) ?? false;
        }

        public override string GetName(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.FieldDeclaration:
                    return this.GetName(((FieldDeclarationSyntax)declaration).Declaration);
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.EnumMemberDeclaration:
                    return ((EnumMemberDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.EventFieldDeclaration:
                    return this.GetName(((EventFieldDeclarationSyntax)declaration).Declaration);
                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.NamespaceDeclaration:
                    return ((NamespaceDeclarationSyntax)declaration).Name.ToString();
                case SyntaxKind.UsingDirective:
                    return ((UsingDirectiveSyntax)declaration).Name.ToString();
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.LocalDeclarationStatement:
                    return this.GetName(((LocalDeclarationStatementSyntax)declaration).Declaration);
                case SyntaxKind.VariableDeclaration:
                    var vd = (VariableDeclarationSyntax)declaration;
                    if (vd.Variables.Count == 1)
                    {
                        return vd.Variables[0].Identifier.ValueText;
                    }
                    break;
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.TypeParameter:
                    return ((TypeParameterSyntax)declaration).Identifier.ValueText;
                case SyntaxKind.AttributeList:
                    var al = (AttributeListSyntax)declaration;
                    if (al.Attributes.Count == 1)
                    {
                        return al.Attributes[0].Name.ToString();
                    }
                    break;
                case SyntaxKind.Attribute:
                    return ((AttributeSyntax)declaration).Name.ToString();
            }

            return string.Empty;
        }

        public override SyntaxNode WithName(SyntaxNode declaration, string name)
        {
            return this.Isolate(declaration, d => this.WithNameInternal(d, name));
        }

        private SyntaxNode WithNameInternal(SyntaxNode declaration, string name)
        {
            var id = name.ToIdentifierToken();

            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ReplaceWithTrivia(declaration, ((ClassDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.StructDeclaration:
                    return ReplaceWithTrivia(declaration, ((StructDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.InterfaceDeclaration:
                    return ReplaceWithTrivia(declaration, ((InterfaceDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.EnumDeclaration:
                    return ReplaceWithTrivia(declaration, ((EnumDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.DelegateDeclaration:
                    return ReplaceWithTrivia(declaration, ((DelegateDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.MethodDeclaration:
                    return ReplaceWithTrivia(declaration, ((MethodDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, fd.Declaration.Variables[0].Identifier, id);
                    }
                    break;
                case SyntaxKind.PropertyDeclaration:
                    return ReplaceWithTrivia(declaration, ((PropertyDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.EnumMemberDeclaration:
                    return ReplaceWithTrivia(declaration, ((EnumMemberDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.EventFieldDeclaration:
                    var efd = (EventFieldDeclarationSyntax)declaration;
                    if (efd.Declaration.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, efd.Declaration.Variables[0].Identifier, id);
                    }
                    break;
                case SyntaxKind.EventDeclaration:
                    return ReplaceWithTrivia(declaration, ((EventDeclarationSyntax)declaration).Identifier, id);
                case SyntaxKind.NamespaceDeclaration:
                    return ReplaceWithTrivia(declaration, ((NamespaceDeclarationSyntax)declaration).Name, this.DottedName(name));
                case SyntaxKind.UsingDirective:
                    return ReplaceWithTrivia(declaration, ((UsingDirectiveSyntax)declaration).Name, this.DottedName(name));
                case SyntaxKind.Parameter:
                    return ReplaceWithTrivia(declaration, ((ParameterSyntax)declaration).Identifier, id);
                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, ld.Declaration.Variables[0].Identifier, id);
                    }
                    break;
                case SyntaxKind.TypeParameter:
                    return ReplaceWithTrivia(declaration, ((TypeParameterSyntax)declaration).Identifier, id);
                case SyntaxKind.AttributeList:
                    var al = (AttributeListSyntax)declaration;
                    if (al.Attributes.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, al.Attributes[0].Name, this.DottedName(name));
                    }
                    break;
                case SyntaxKind.Attribute:
                    return ReplaceWithTrivia(declaration, ((AttributeSyntax)declaration).Name, this.DottedName(name));
                case SyntaxKind.VariableDeclaration:
                    var vd = (VariableDeclarationSyntax)declaration;
                    if (vd.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, vd.Variables[0].Identifier, id);
                    }
                    break;
                case SyntaxKind.VariableDeclarator:
                    return ReplaceWithTrivia(declaration, ((VariableDeclaratorSyntax)declaration).Identifier, id);
            }

            return declaration;
        }

        public override SyntaxNode GetType(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.DelegateDeclaration:
                    return NotVoid(((DelegateDeclarationSyntax)declaration).ReturnType);
                case SyntaxKind.MethodDeclaration:
                    return NotVoid(((MethodDeclarationSyntax)declaration).ReturnType);
                case SyntaxKind.FieldDeclaration:
                    return ((FieldDeclarationSyntax)declaration).Declaration.Type;
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)declaration).Type;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)declaration).Type;
                case SyntaxKind.EventFieldDeclaration:
                    return ((EventFieldDeclarationSyntax)declaration).Declaration.Type;
                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)declaration).Type;
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)declaration).Type;
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)declaration).Declaration.Type;
                case SyntaxKind.VariableDeclaration:
                    return ((VariableDeclarationSyntax)declaration).Type;
                case SyntaxKind.VariableDeclarator:
                    if (declaration.Parent != null)
                    {
                        return this.GetType(declaration.Parent);
                    }
                    break;
            }

            return null;
        }

        private static TypeSyntax NotVoid(TypeSyntax type)
        {
            return type is PredefinedTypeSyntax pd && pd.Keyword.IsKind(SyntaxKind.VoidKeyword) ? null : type;
        }

        public override SyntaxNode WithType(SyntaxNode declaration, SyntaxNode type)
        {
            return this.Isolate(declaration, d => WithTypeInternal(d, type));
        }

        private static SyntaxNode WithTypeInternal(SyntaxNode declaration, SyntaxNode type)
            => declaration.Kind() switch
            {
                SyntaxKind.DelegateDeclaration => ((DelegateDeclarationSyntax)declaration).WithReturnType((TypeSyntax)type),
                SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).WithReturnType((TypeSyntax)type),
                SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).WithDeclaration(((FieldDeclarationSyntax)declaration).Declaration.WithType((TypeSyntax)type)),
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).WithType((TypeSyntax)type),
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).WithType((TypeSyntax)type),
                SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).WithDeclaration(((EventFieldDeclarationSyntax)declaration).Declaration.WithType((TypeSyntax)type)),
                SyntaxKind.EventDeclaration => ((EventDeclarationSyntax)declaration).WithType((TypeSyntax)type),
                SyntaxKind.Parameter => ((ParameterSyntax)declaration).WithType((TypeSyntax)type),
                SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).WithDeclaration(((LocalDeclarationStatementSyntax)declaration).Declaration.WithType((TypeSyntax)type)),
                SyntaxKind.VariableDeclaration => ((VariableDeclarationSyntax)declaration).WithType((TypeSyntax)type),
                _ => declaration,
            };

        private SyntaxNode Isolate(SyntaxNode declaration, Func<SyntaxNode, SyntaxNode> editor)
        {
            var isolated = this.AsIsolatedDeclaration(declaration);

            return PreserveTrivia(isolated, editor);
        }

        private SyntaxNode AsIsolatedDeclaration(SyntaxNode declaration)
        {
            if (declaration != null)
            {
                switch (declaration.Kind())
                {
                    case SyntaxKind.VariableDeclaration:
                        var vd = (VariableDeclarationSyntax)declaration;
                        if (vd.Parent != null && vd.Variables.Count == 1)
                        {
                            return this.AsIsolatedDeclaration(vd.Parent);
                        }
                        break;

                    case SyntaxKind.VariableDeclarator:
                        var v = (VariableDeclaratorSyntax)declaration;
                        if (v.Parent != null && v.Parent.Parent != null)
                        {
                            return this.ClearTrivia(WithVariable(v.Parent.Parent, v));
                        }
                        break;

                    case SyntaxKind.Attribute:
                        var attr = (AttributeSyntax)declaration;
                        if (attr.Parent != null)
                        {
                            var attrList = (AttributeListSyntax)attr.Parent;
                            return attrList.WithAttributes(SyntaxFactory.SingletonSeparatedList(attr)).WithTarget(null);
                        }
                        break;
                }
            }

            return declaration;
        }

        private static SyntaxNode WithVariable(SyntaxNode declaration, VariableDeclaratorSyntax variable)
        {
            var vd = GetVariableDeclaration(declaration);
            if (vd != null)
            {
                return WithVariableDeclaration(declaration, vd.WithVariables(SyntaxFactory.SingletonSeparatedList(variable)));
            }

            return declaration;
        }

        private static VariableDeclarationSyntax GetVariableDeclaration(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).Declaration,
                SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).Declaration,
                SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).Declaration,
                _ => null,
            };

        private static SyntaxNode WithVariableDeclaration(SyntaxNode declaration, VariableDeclarationSyntax variables)
            => declaration.Kind() switch
            {
                SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).WithDeclaration(variables),
                SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).WithDeclaration(variables),
                SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).WithDeclaration(variables),
                _ => declaration,
            };

        private static SyntaxNode GetFullDeclaration(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.VariableDeclaration:
                    var vd = (VariableDeclarationSyntax)declaration;
                    if (ParentIsFieldDeclaration(vd)
                        || ParentIsEventFieldDeclaration(vd)
                        || ParentIsLocalDeclarationStatement(vd))
                    {
                        return vd.Parent;
                    }
                    else
                    {
                        return vd;
                    }

                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.Attribute:
                    if (declaration.Parent != null)
                    {
                        return GetFullDeclaration(declaration.Parent);
                    }
                    break;
            }

            return declaration;
        }

        private SyntaxNode AsNodeLike(SyntaxNode existingNode, SyntaxNode newNode)
        {
            switch (this.GetDeclarationKind(existingNode))
            {
                case DeclarationKind.Class:
                case DeclarationKind.Interface:
                case DeclarationKind.Struct:
                case DeclarationKind.Enum:
                case DeclarationKind.Namespace:
                case DeclarationKind.CompilationUnit:
                    var container = this.GetDeclaration(existingNode.Parent);
                    if (container != null)
                    {
                        return this.AsMemberOf(container, newNode);
                    }
                    break;

                case DeclarationKind.Attribute:
                    return AsAttributeList(newNode);
            }

            return newNode;
        }

        public override IReadOnlyList<SyntaxNode> GetParameters(SyntaxNode declaration)
        {
            var list = GetParameterList(declaration);
            return list != null
                ? list.Parameters
                : declaration is SimpleLambdaExpressionSyntax simpleLambda
                    ? new[] { simpleLambda.Parameter }
                    : SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
        }

        public override SyntaxNode InsertParameters(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters)
        {
            var newParameters = AsParameterList(parameters);

            var currentList = GetParameterList(declaration);
            if (currentList == null)
            {
                currentList = declaration.IsKind(SyntaxKind.IndexerDeclaration)
                    ? SyntaxFactory.BracketedParameterList()
                    : (BaseParameterListSyntax)SyntaxFactory.ParameterList();
            }

            var newList = currentList.WithParameters(currentList.Parameters.InsertRange(index, newParameters.Parameters));
            return WithParameterList(declaration, newList);
        }

        public override IReadOnlyList<SyntaxNode> GetSwitchSections(SyntaxNode switchStatement)
        {
            var statement = switchStatement as SwitchStatementSyntax;
            return statement?.Sections ?? SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
        }

        public override SyntaxNode InsertSwitchSections(SyntaxNode switchStatement, int index, IEnumerable<SyntaxNode> switchSections)
        {
            if (!(switchStatement is SwitchStatementSyntax statement))
            {
                return switchStatement;
            }

            var newSections = statement.Sections.InsertRange(index, switchSections.Cast<SwitchSectionSyntax>());
            return AddMissingTokens(statement, recurse: false).WithSections(newSections);
        }

        private static TNode AddMissingTokens<TNode>(TNode node, bool recurse)
            where TNode : CSharpSyntaxNode
        {
            var rewriter = new AddMissingTokensRewriter(recurse);
            return (TNode)rewriter.Visit(node);
        }

        private class AddMissingTokensRewriter : CSharpSyntaxRewriter
        {
            private readonly bool _recurse;
            private bool _firstVisit = true;

            public AddMissingTokensRewriter(bool recurse)
            {
                _recurse = recurse;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (!_recurse && !_firstVisit)
                {
                    return node;
                }

                _firstVisit = false;
                return base.Visit(node);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var rewrittenToken = base.VisitToken(token);
                if (!rewrittenToken.IsMissing || !CSharp.SyntaxFacts.IsPunctuationOrKeyword(token.Kind()))
                {
                    return rewrittenToken;
                }

                return SyntaxFactory.Token(token.Kind()).WithTriviaFrom(rewrittenToken);
            }
        }

        internal override SyntaxNode GetParameterListNode(SyntaxNode declaration)
            => GetParameterList(declaration);

        internal static BaseParameterListSyntax GetParameterList(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.DelegateDeclaration => ((DelegateDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.OperatorDeclaration => ((OperatorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ConstructorDeclaration => ((ConstructorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.DestructorDeclaration => ((DestructorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ParenthesizedLambdaExpression => ((ParenthesizedLambdaExpressionSyntax)declaration).ParameterList,
                SyntaxKind.LocalFunctionStatement => ((LocalFunctionStatementSyntax)declaration).ParameterList,
                SyntaxKind.AnonymousMethodExpression => ((AnonymousMethodExpressionSyntax)declaration).ParameterList,
                _ => (BaseParameterListSyntax)null,
            };

        private static SyntaxNode WithParameterList(SyntaxNode declaration, BaseParameterListSyntax list)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)declaration).WithParameterList(list);
                case SyntaxKind.LocalFunctionStatement:
                    return ((LocalFunctionStatementSyntax)declaration).WithParameterList((ParameterListSyntax)list);
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)declaration).WithParameterList((ParameterListSyntax)list);
                case SyntaxKind.SimpleLambdaExpression:
                    var lambda = (SimpleLambdaExpressionSyntax)declaration;
                    var roList = AsReadOnlyList(list.Parameters);
                    if (roList.Count == 1 && IsSimpleLambdaParameter(roList[0]))
                    {
                        return lambda.WithParameter(roList[0]);
                    }
                    else
                    {
                        return SyntaxFactory.ParenthesizedLambdaExpression(AsParameterList(roList), lambda.Body)
                            .WithLeadingTrivia(lambda.GetLeadingTrivia())
                            .WithTrailingTrivia(lambda.GetTrailingTrivia());
                    }
                default:
                    return declaration;
            }
        }

        public override SyntaxNode GetExpression(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)declaration).Body as ExpressionSyntax;
                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)declaration).Body as ExpressionSyntax;

                case SyntaxKind.PropertyDeclaration:
                    var pd = (PropertyDeclarationSyntax)declaration;
                    if (pd.ExpressionBody != null)
                    {
                        return pd.ExpressionBody.Expression;
                    }
                    goto default;

                case SyntaxKind.IndexerDeclaration:
                    var id = (IndexerDeclarationSyntax)declaration;
                    if (id.ExpressionBody != null)
                    {
                        return id.ExpressionBody.Expression;
                    }
                    goto default;

                default:
                    return GetEqualsValue(declaration)?.Value;
            }
        }

        public override SyntaxNode WithExpression(SyntaxNode declaration, SyntaxNode expression)
        {
            return this.Isolate(declaration, d => WithExpressionInternal(d, expression));
        }

        private static SyntaxNode WithExpressionInternal(SyntaxNode declaration, SyntaxNode expression)
        {
            var expr = (ExpressionSyntax)expression;

            switch (declaration.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)declaration).WithBody((CSharpSyntaxNode)expr ?? CreateBlock(null));

                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)declaration).WithBody((CSharpSyntaxNode)expr ?? CreateBlock(null));

                case SyntaxKind.PropertyDeclaration:
                    var pd = (PropertyDeclarationSyntax)declaration;
                    if (pd.ExpressionBody != null)
                    {
                        return ReplaceWithTrivia(pd, pd.ExpressionBody.Expression, expr);
                    }
                    goto default;

                case SyntaxKind.IndexerDeclaration:
                    var id = (IndexerDeclarationSyntax)declaration;
                    if (id.ExpressionBody != null)
                    {
                        return ReplaceWithTrivia(id, id.ExpressionBody.Expression, expr);
                    }
                    goto default;

                default:
                    var eq = GetEqualsValue(declaration);
                    if (eq != null)
                    {
                        if (expression == null)
                        {
                            return WithEqualsValue(declaration, null);
                        }
                        else
                        {
                            // use replace so we only change the value part.
                            return ReplaceWithTrivia(declaration, eq.Value, expr);
                        }
                    }
                    else if (expression != null)
                    {
                        return WithEqualsValue(declaration, SyntaxFactory.EqualsValueClause(expr));
                    }
                    else
                    {
                        return declaration;
                    }
            }
        }

        private static EqualsValueClauseSyntax GetEqualsValue(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration.Variables.Count == 1)
                    {
                        return fd.Declaration.Variables[0].Initializer;
                    }
                    break;
                case SyntaxKind.PropertyDeclaration:
                    var pd = (PropertyDeclarationSyntax)declaration;
                    return pd.Initializer;
                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration.Variables.Count == 1)
                    {
                        return ld.Declaration.Variables[0].Initializer;
                    }
                    break;
                case SyntaxKind.VariableDeclaration:
                    var vd = (VariableDeclarationSyntax)declaration;
                    if (vd.Variables.Count == 1)
                    {
                        return vd.Variables[0].Initializer;
                    }
                    break;
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)declaration).Initializer;
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)declaration).Default;
            }

            return null;
        }

        private static SyntaxNode WithEqualsValue(SyntaxNode declaration, EqualsValueClauseSyntax eq)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, fd.Declaration.Variables[0], fd.Declaration.Variables[0].WithInitializer(eq));
                    }
                    break;
                case SyntaxKind.PropertyDeclaration:
                    var pd = (PropertyDeclarationSyntax)declaration;
                    return pd.WithInitializer(eq);
                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, ld.Declaration.Variables[0], ld.Declaration.Variables[0].WithInitializer(eq));
                    }
                    break;
                case SyntaxKind.VariableDeclaration:
                    var vd = (VariableDeclarationSyntax)declaration;
                    if (vd.Variables.Count == 1)
                    {
                        return ReplaceWithTrivia(declaration, vd.Variables[0], vd.Variables[0].WithInitializer(eq));
                    }
                    break;
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)declaration).WithInitializer(eq);
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)declaration).WithDefault(eq);
            }

            return declaration;
        }

        private static readonly IReadOnlyList<SyntaxNode> s_EmptyList = SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();

        public override IReadOnlyList<SyntaxNode> GetStatements(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.LocalFunctionStatement:
                    return ((LocalFunctionStatementSyntax)declaration).Body?.Statements ?? s_EmptyList;
                case SyntaxKind.AnonymousMethodExpression:
                    return (((AnonymousMethodExpressionSyntax)declaration).Body as BlockSyntax)?.Statements ?? s_EmptyList;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return (((ParenthesizedLambdaExpressionSyntax)declaration).Body as BlockSyntax)?.Statements ?? s_EmptyList;
                case SyntaxKind.SimpleLambdaExpression:
                    return (((SimpleLambdaExpressionSyntax)declaration).Body as BlockSyntax)?.Statements ?? s_EmptyList;
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)declaration).Body?.Statements ?? s_EmptyList;
                default:
                    return s_EmptyList;
            }
        }

        public override SyntaxNode WithStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        {
            var body = CreateBlock(statements);
            var somebody = statements != null ? body : null;
            var semicolon = statements == null ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default;

            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.LocalFunctionStatement:
                    return ((LocalFunctionStatementSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                case SyntaxKind.AnonymousMethodExpression:
                    return ((AnonymousMethodExpressionSyntax)declaration).WithBody(body);
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)declaration).WithBody(body);
                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)declaration).WithBody(body);
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)declaration).WithBody(somebody).WithSemicolonToken(semicolon).WithExpressionBody(null);
                default:
                    return declaration;
            }
        }

        public override IReadOnlyList<SyntaxNode> GetAccessors(SyntaxNode declaration)
        {
            var list = GetAccessorList(declaration);
            return list?.Accessors ?? s_EmptyList;
        }

        public override SyntaxNode InsertAccessors(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> accessors)
        {
            var newAccessors = AsAccessorList(accessors, declaration.Kind());

            var currentList = GetAccessorList(declaration);
            if (currentList == null)
            {
                if (CanHaveAccessors(declaration))
                {
                    currentList = SyntaxFactory.AccessorList();
                }
                else
                {
                    return declaration;
                }
            }

            var newList = currentList.WithAccessors(currentList.Accessors.InsertRange(index, newAccessors.Accessors));
            return WithAccessorList(declaration, newList);
        }

        private static AccessorListSyntax GetAccessorList(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).AccessorList,
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).AccessorList,
                SyntaxKind.EventDeclaration => ((EventDeclarationSyntax)declaration).AccessorList,
                _ => null,
            };

        private static bool CanHaveAccessors(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).ExpressionBody == null,
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).ExpressionBody == null,
                SyntaxKind.EventDeclaration => true,
                _ => false,
            };

        private static SyntaxNode WithAccessorList(SyntaxNode declaration, AccessorListSyntax accessorList)
            => declaration.Kind() switch
            {
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).WithAccessorList(accessorList),
                SyntaxKind.IndexerDeclaration => ((PropertyDeclarationSyntax)declaration).WithAccessorList(accessorList),
                SyntaxKind.EventDeclaration => ((EventDeclarationSyntax)declaration).WithAccessorList(accessorList),
                _ => declaration,
            };

        private static AccessorListSyntax AsAccessorList(IEnumerable<SyntaxNode> nodes, SyntaxKind parentKind)
        {
            return SyntaxFactory.AccessorList(
                SyntaxFactory.List(nodes.Select(n => AsAccessor(n, parentKind)).Where(n => n != null)));
        }

        private static AccessorDeclarationSyntax AsAccessor(SyntaxNode node, SyntaxKind parentKind)
        {
            switch (parentKind)
            {
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    switch (node.Kind())
                    {
                        case SyntaxKind.GetAccessorDeclaration:
                        case SyntaxKind.SetAccessorDeclaration:
                            return (AccessorDeclarationSyntax)node;
                    }
                    break;
                case SyntaxKind.EventDeclaration:
                    switch (node.Kind())
                    {
                        case SyntaxKind.AddAccessorDeclaration:
                        case SyntaxKind.RemoveAccessorDeclaration:
                            return (AccessorDeclarationSyntax)node;
                    }
                    break;
            }

            return null;
        }

        private static AccessorDeclarationSyntax GetAccessor(SyntaxNode declaration, SyntaxKind kind)
        {
            var accessorList = GetAccessorList(declaration);
            return accessorList?.Accessors.FirstOrDefault(a => a.IsKind(kind));
        }

        private SyntaxNode WithAccessor(SyntaxNode declaration, SyntaxKind kind, AccessorDeclarationSyntax accessor)
        {
            return this.WithAccessor(declaration, GetAccessorList(declaration), kind, accessor);
        }

        private SyntaxNode WithAccessor(SyntaxNode declaration, AccessorListSyntax accessorList, SyntaxKind kind, AccessorDeclarationSyntax accessor)
        {
            if (accessorList != null)
            {
                var acc = accessorList.Accessors.FirstOrDefault(a => a.IsKind(kind));
                if (acc != null)
                {
                    return this.ReplaceNode(declaration, acc, accessor);
                }
                else if (accessor != null)
                {
                    return this.ReplaceNode(declaration, accessorList, accessorList.AddAccessors(accessor));
                }
            }

            return declaration;
        }

        public override IReadOnlyList<SyntaxNode> GetGetAccessorStatements(SyntaxNode declaration)
        {
            var accessor = GetAccessor(declaration, SyntaxKind.GetAccessorDeclaration);
            return accessor?.Body?.Statements ?? s_EmptyList;
        }

        public override IReadOnlyList<SyntaxNode> GetSetAccessorStatements(SyntaxNode declaration)
        {
            var accessor = GetAccessor(declaration, SyntaxKind.SetAccessorDeclaration);
            return accessor?.Body?.Statements ?? s_EmptyList;
        }

        public override SyntaxNode WithGetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        {
            return this.WithAccessorStatements(declaration, SyntaxKind.GetAccessorDeclaration, statements);
        }

        public override SyntaxNode WithSetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        {
            return this.WithAccessorStatements(declaration, SyntaxKind.SetAccessorDeclaration, statements);
        }

        private SyntaxNode WithAccessorStatements(SyntaxNode declaration, SyntaxKind kind, IEnumerable<SyntaxNode> statements)
        {
            var accessor = GetAccessor(declaration, kind);
            if (accessor == null)
            {
                accessor = AccessorDeclaration(kind, statements);
                return this.WithAccessor(declaration, kind, accessor);
            }
            else
            {
                return this.WithAccessor(declaration, kind, (AccessorDeclarationSyntax)this.WithStatements(accessor, statements));
            }
        }

        public override IReadOnlyList<SyntaxNode> GetBaseAndInterfaceTypes(SyntaxNode declaration)
        {
            var baseList = GetBaseList(declaration);
            if (baseList != null)
            {
                return baseList.Types.OfType<SimpleBaseTypeSyntax>().Select(bt => bt.Type).ToReadOnlyCollection();
            }
            else
            {
                return SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
            }
        }

        public override SyntaxNode AddBaseType(SyntaxNode declaration, SyntaxNode baseType)
        {
            var baseList = GetBaseList(declaration);

            if (baseList != null)
            {
                return WithBaseList(declaration, baseList.WithTypes(baseList.Types.Insert(0, SyntaxFactory.SimpleBaseType((TypeSyntax)baseType))));
            }
            else
            {
                return AddBaseList(declaration, SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType((TypeSyntax)baseType))));
            }
        }

        public override SyntaxNode AddInterfaceType(SyntaxNode declaration, SyntaxNode interfaceType)
        {
            var baseList = GetBaseList(declaration);

            if (baseList != null)
            {
                return WithBaseList(declaration, baseList.WithTypes(baseList.Types.Insert(baseList.Types.Count, SyntaxFactory.SimpleBaseType((TypeSyntax)interfaceType))));
            }
            else
            {
                return AddBaseList(declaration, SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType((TypeSyntax)interfaceType))));
            }
        }

        private static SyntaxNode AddBaseList(SyntaxNode declaration, BaseListSyntax baseList)
        {
            var newDecl = WithBaseList(declaration, baseList);

            // move trivia from type identifier to after base list
            return ShiftTrivia(newDecl, GetBaseList(newDecl));
        }

        private static BaseListSyntax GetBaseList(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntax)declaration).BaseList,
                SyntaxKind.StructDeclaration => ((StructDeclarationSyntax)declaration).BaseList,
                SyntaxKind.InterfaceDeclaration => ((InterfaceDeclarationSyntax)declaration).BaseList,
                _ => null,
            };

        private static SyntaxNode WithBaseList(SyntaxNode declaration, BaseListSyntax baseList)
            => declaration.Kind() switch
            {
                SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntax)declaration).WithBaseList(baseList),
                SyntaxKind.StructDeclaration => ((StructDeclarationSyntax)declaration).WithBaseList(baseList),
                SyntaxKind.InterfaceDeclaration => ((InterfaceDeclarationSyntax)declaration).WithBaseList(baseList),
                _ => declaration,
            };

        #endregion

        #region Remove, Replace, Insert

        public override SyntaxNode ReplaceNode(SyntaxNode root, SyntaxNode declaration, SyntaxNode newDeclaration)
        {
            newDeclaration = this.AsNodeLike(declaration, newDeclaration);

            if (newDeclaration == null)
            {
                return this.RemoveNode(root, declaration);
            }

            if (root.Span.Contains(declaration.Span))
            {
                var newFullDecl = this.AsIsolatedDeclaration(newDeclaration);
                var fullDecl = GetFullDeclaration(declaration);

                // special handling for replacing at location of sub-declaration
                if (fullDecl != declaration && fullDecl.IsKind(newFullDecl.Kind()))
                {
                    // try to replace inline if possible
                    if (GetDeclarationCount(newFullDecl) == 1)
                    {
                        var newSubDecl = GetSubDeclarations(newFullDecl)[0];
                        if (AreInlineReplaceableSubDeclarations(declaration, newSubDecl))
                        {
                            return base.ReplaceNode(root, declaration, newSubDecl);
                        }
                    }

                    // replace sub declaration by splitting full declaration and inserting between
                    var index = this.IndexOf(GetSubDeclarations(fullDecl), declaration);

                    // replace declaration with multiple declarations
                    return ReplaceRange(root, fullDecl, this.SplitAndReplace(fullDecl, index, new[] { newDeclaration }));
                }

                // attempt normal replace
                return base.ReplaceNode(root, declaration, newFullDecl);
            }
            else
            {
                return base.ReplaceNode(root, declaration, newDeclaration);
            }
        }

        // returns true if one sub-declaration can be replaced inline with another sub-declaration
        private static bool AreInlineReplaceableSubDeclarations(SyntaxNode decl1, SyntaxNode decl2)
        {
            var kind = decl1.Kind();
            if (decl2.IsKind(kind))
            {
                switch (kind)
                {
                    case SyntaxKind.Attribute:
                    case SyntaxKind.VariableDeclarator:
                        return AreSimilarExceptForSubDeclarations(decl1.Parent, decl2.Parent);
                }
            }

            return false;
        }

        private static bool AreSimilarExceptForSubDeclarations(SyntaxNode decl1, SyntaxNode decl2)
        {
            if (decl1 == decl2)
            {
                return true;
            }

            if (decl1 == null || decl2 == null)
            {
                return false;
            }

            var kind = decl1.Kind();
            if (decl2.IsKind(kind))
            {
                switch (kind)
                {
                    case SyntaxKind.FieldDeclaration:
                        var fd1 = (FieldDeclarationSyntax)decl1;
                        var fd2 = (FieldDeclarationSyntax)decl2;
                        return SyntaxFactory.AreEquivalent(fd1.Modifiers, fd2.Modifiers)
                            && SyntaxFactory.AreEquivalent(fd1.AttributeLists, fd2.AttributeLists);

                    case SyntaxKind.EventFieldDeclaration:
                        var efd1 = (EventFieldDeclarationSyntax)decl1;
                        var efd2 = (EventFieldDeclarationSyntax)decl2;
                        return SyntaxFactory.AreEquivalent(efd1.Modifiers, efd2.Modifiers)
                            && SyntaxFactory.AreEquivalent(efd1.AttributeLists, efd2.AttributeLists);

                    case SyntaxKind.LocalDeclarationStatement:
                        var ld1 = (LocalDeclarationStatementSyntax)decl1;
                        var ld2 = (LocalDeclarationStatementSyntax)decl2;
                        return SyntaxFactory.AreEquivalent(ld1.Modifiers, ld2.Modifiers);

                    case SyntaxKind.AttributeList:
                        // don't compare targets, since aren't part of the abstraction
                        return true;

                    case SyntaxKind.VariableDeclaration:
                        var vd1 = (VariableDeclarationSyntax)decl1;
                        var vd2 = (VariableDeclarationSyntax)decl2;
                        return SyntaxFactory.AreEquivalent(vd1.Type, vd2.Type) && AreSimilarExceptForSubDeclarations(vd1.Parent, vd2.Parent);
                }
            }

            return false;
        }

        // replaces sub-declaration by splitting multi-part declaration first
        private IEnumerable<SyntaxNode> SplitAndReplace(SyntaxNode multiPartDeclaration, int index, IEnumerable<SyntaxNode> newDeclarations)
        {
            var count = GetDeclarationCount(multiPartDeclaration);

            if (index >= 0 && index < count)
            {
                var newNodes = new List<SyntaxNode>();

                if (index > 0)
                {
                    // make a single declaration with only sub-declarations before the sub-declaration being replaced
                    newNodes.Add(this.WithSubDeclarationsRemoved(multiPartDeclaration, index, count - index).WithTrailingTrivia(SyntaxFactory.ElasticSpace));
                }

                newNodes.AddRange(newDeclarations);

                if (index < count - 1)
                {
                    // make a single declaration with only the sub-declarations after the sub-declaration being replaced
                    newNodes.Add(this.WithSubDeclarationsRemoved(multiPartDeclaration, 0, index + 1).WithLeadingTrivia(SyntaxFactory.ElasticSpace));
                }

                return newNodes;
            }
            else
            {
                return newDeclarations;
            }
        }

        public override SyntaxNode InsertNodesBefore(SyntaxNode root, SyntaxNode declaration, IEnumerable<SyntaxNode> newDeclarations)
        {
            if (root.Span.Contains(declaration.Span))
            {
                return this.Isolate(root.TrackNodes(declaration), r => this.InsertNodesBeforeInternal(r, r.GetCurrentNode(declaration), newDeclarations));
            }
            else
            {
                return base.InsertNodesBefore(root, declaration, newDeclarations);
            }
        }

        private SyntaxNode InsertNodesBeforeInternal(SyntaxNode root, SyntaxNode declaration, IEnumerable<SyntaxNode> newDeclarations)
        {
            var fullDecl = GetFullDeclaration(declaration);
            if (fullDecl == declaration || GetDeclarationCount(fullDecl) == 1)
            {
                return base.InsertNodesBefore(root, fullDecl, newDeclarations);
            }

            var subDecls = GetSubDeclarations(fullDecl);
            var index = this.IndexOf(subDecls, declaration);

            // insert new declaration between full declaration split into two
            if (index > 0)
            {
                return ReplaceRange(root, fullDecl, this.SplitAndInsert(fullDecl, index, newDeclarations));
            }

            return base.InsertNodesBefore(root, fullDecl, newDeclarations);
        }

        public override SyntaxNode InsertNodesAfter(SyntaxNode root, SyntaxNode declaration, IEnumerable<SyntaxNode> newDeclarations)
        {
            if (root.Span.Contains(declaration.Span))
            {
                return this.Isolate(root.TrackNodes(declaration), r => this.InsertNodesAfterInternal(r, r.GetCurrentNode(declaration), newDeclarations));
            }
            else
            {
                return base.InsertNodesAfter(root, declaration, newDeclarations);
            }
        }

        private SyntaxNode InsertNodesAfterInternal(SyntaxNode root, SyntaxNode declaration, IEnumerable<SyntaxNode> newDeclarations)
        {
            var fullDecl = GetFullDeclaration(declaration);
            if (fullDecl == declaration || GetDeclarationCount(fullDecl) == 1)
            {
                return base.InsertNodesAfter(root, fullDecl, newDeclarations);
            }

            var subDecls = GetSubDeclarations(fullDecl);
            var count = subDecls.Count;
            var index = this.IndexOf(subDecls, declaration);

            // insert new declaration between full declaration split into two
            if (index >= 0 && index < count - 1)
            {
                return ReplaceRange(root, fullDecl, this.SplitAndInsert(fullDecl, index + 1, newDeclarations));
            }

            return base.InsertNodesAfter(root, fullDecl, newDeclarations);
        }

        private IEnumerable<SyntaxNode> SplitAndInsert(SyntaxNode multiPartDeclaration, int index, IEnumerable<SyntaxNode> newDeclarations)
        {
            var count = GetDeclarationCount(multiPartDeclaration);
            var newNodes = new List<SyntaxNode>();
            newNodes.Add(this.WithSubDeclarationsRemoved(multiPartDeclaration, index, count - index).WithTrailingTrivia(SyntaxFactory.ElasticSpace));
            newNodes.AddRange(newDeclarations);
            newNodes.Add(this.WithSubDeclarationsRemoved(multiPartDeclaration, 0, index).WithLeadingTrivia(SyntaxFactory.ElasticSpace));
            return newNodes;
        }

        private SyntaxNode WithSubDeclarationsRemoved(SyntaxNode declaration, int index, int count)
        {
            return this.RemoveNodes(declaration, GetSubDeclarations(declaration).Skip(index).Take(count));
        }

        private static IReadOnlyList<SyntaxNode> GetSubDeclarations(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).Declaration.Variables,
                SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).Declaration.Variables,
                SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).Declaration.Variables,
                SyntaxKind.VariableDeclaration => ((VariableDeclarationSyntax)declaration).Variables,
                SyntaxKind.AttributeList => ((AttributeListSyntax)declaration).Attributes,
                _ => SpecializedCollections.EmptyReadOnlyList<SyntaxNode>(),
            };

        public override SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node)
        {
            return this.RemoveNode(root, node, DefaultRemoveOptions);
        }

        public override SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node, SyntaxRemoveOptions options)
        {
            if (root.Span.Contains(node.Span))
            {
                // node exists within normal span of the root (not in trivia)
                return this.Isolate(root.TrackNodes(node), r => this.RemoveNodeInternal(r, r.GetCurrentNode(node), options));
            }
            else
            {
                return this.RemoveNodeInternal(root, node, options);
            }
        }

        private SyntaxNode RemoveNodeInternal(SyntaxNode root, SyntaxNode declaration, SyntaxRemoveOptions options)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.Attribute:
                    var attr = (AttributeSyntax)declaration;
                    if (attr.Parent is AttributeListSyntax attrList && attrList.Attributes.Count == 1)
                    {
                        // remove entire list if only one attribute
                        return this.RemoveNodeInternal(root, attrList, options);
                    }
                    break;

                case SyntaxKind.AttributeArgument:
                    if (declaration.Parent != null && ((AttributeArgumentListSyntax)declaration.Parent).Arguments.Count == 1)
                    {
                        // remove entire argument list if only one argument
                        return this.RemoveNodeInternal(root, declaration.Parent, options);
                    }
                    break;

                case SyntaxKind.VariableDeclarator:
                    var full = GetFullDeclaration(declaration);
                    if (full != declaration && GetDeclarationCount(full) == 1)
                    {
                        // remove full declaration if only one declarator
                        return this.RemoveNodeInternal(root, full, options);
                    }
                    break;

                case SyntaxKind.SimpleBaseType:
                    if (declaration.Parent is BaseListSyntax baseList && baseList.Types.Count == 1)
                    {
                        // remove entire base list if this is the only base type.
                        return this.RemoveNodeInternal(root, baseList, options);
                    }
                    break;

                default:
                    var parent = declaration.Parent;
                    if (parent != null)
                    {
                        switch (parent.Kind())
                        {
                            case SyntaxKind.SimpleBaseType:
                                return this.RemoveNodeInternal(root, parent, options);
                        }
                    }
                    break;
            }

            return base.RemoveNode(root, declaration, options);
        }

        /// <summary>
        /// Moves the trailing trivia from the node's previous token to the end of the node
        /// </summary>
        private static SyntaxNode ShiftTrivia(SyntaxNode root, SyntaxNode node)
        {
            var firstToken = node.GetFirstToken();
            var previousToken = firstToken.GetPreviousToken();
            if (previousToken != default && root.Contains(previousToken.Parent))
            {
                var newNode = node.WithTrailingTrivia(node.GetTrailingTrivia().AddRange(previousToken.TrailingTrivia));
                var newPreviousToken = previousToken.WithTrailingTrivia(default(SyntaxTriviaList));
                return root.ReplaceSyntax(
                    nodes: new[] { node }, computeReplacementNode: (o, r) => newNode,
                    tokens: new[] { previousToken }, computeReplacementToken: (o, r) => newPreviousToken,
                    trivia: null, computeReplacementTrivia: null);
            }

            return root;
        }

        internal override bool IsRegularOrDocComment(SyntaxTrivia trivia)
            => trivia.IsRegularOrDocComment();

        #endregion

        #region Statements and Expressions

        public override SyntaxNode AddEventHandler(SyntaxNode @event, SyntaxNode handler)
        {
            return SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, (ExpressionSyntax)@event, Parenthesize(handler));
        }

        public override SyntaxNode RemoveEventHandler(SyntaxNode @event, SyntaxNode handler)
        {
            return SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, (ExpressionSyntax)@event, Parenthesize(handler));
        }

        public override SyntaxNode AwaitExpression(SyntaxNode expression)
        {
            return SyntaxFactory.AwaitExpression((ExpressionSyntax)expression);
        }

        public override SyntaxNode NameOfExpression(SyntaxNode expression)
        {
            return this.InvocationExpression(
                this.IdentifierName(CSharp.SyntaxFacts.GetText(SyntaxKind.NameOfKeyword)),
                expression);
        }

        public override SyntaxNode ReturnStatement(SyntaxNode expressionOpt = null)
            => SyntaxFactory.ReturnStatement((ExpressionSyntax)expressionOpt);

        internal override SyntaxNode YieldReturnStatement(SyntaxNode expressionOpt = null)
            => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)expressionOpt);

        public override SyntaxNode ThrowStatement(SyntaxNode expressionOpt = null)
            => SyntaxFactory.ThrowStatement((ExpressionSyntax)expressionOpt);

        public override SyntaxNode ThrowExpression(SyntaxNode expression)
        {
            return SyntaxFactory.ThrowExpression((ExpressionSyntax)expression);
        }

        internal override bool SupportsThrowExpression() => true;

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

        private static BlockSyntax CreateBlock(IEnumerable<SyntaxNode> statements)
            => SyntaxFactory.Block(AsStatementList(statements)).WithAdditionalAnnotations(Simplifier.Annotation);

        private static SyntaxList<StatementSyntax> AsStatementList(IEnumerable<SyntaxNode> nodes)
        {
            return nodes == null ? default : SyntaxFactory.List(nodes.Select(AsStatement));
        }

        private static StatementSyntax AsStatement(SyntaxNode node)
        {
            if (node is ExpressionSyntax expression)
            {
                return SyntaxFactory.ExpressionStatement(expression);
            }

            return (StatementSyntax)node;
        }

        public override SyntaxNode ExpressionStatement(SyntaxNode expression)
            => SyntaxFactory.ExpressionStatement((ExpressionSyntax)expression);

        internal override SyntaxNode MemberAccessExpressionWorker(SyntaxNode expression, SyntaxNode simpleName)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ParenthesizeLeft((ExpressionSyntax)expression),
                (SimpleNameSyntax)simpleName);
        }

        internal override SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
            => SyntaxFactory.ConditionalAccessExpression((ExpressionSyntax)expression, (ExpressionSyntax)whenNotNull);

        internal override SyntaxNode MemberBindingExpression(SyntaxNode name)
            => SyntaxFactory.MemberBindingExpression((SimpleNameSyntax)name);

        internal override SyntaxNode ElementBindingExpression(SyntaxNode argumentList)
            => SyntaxFactory.ElementBindingExpression((BracketedArgumentListSyntax)argumentList);

        /// <summary>
        /// Parenthesize the left hand size of a member access, invocation or element access expression
        /// </summary>
        private static ExpressionSyntax ParenthesizeLeft(ExpressionSyntax expression)
        {
            if (expression is TypeSyntax ||
                expression.IsKind(SyntaxKind.ThisExpression) ||
                expression.IsKind(SyntaxKind.BaseExpression) ||
                expression.IsKind(SyntaxKind.ParenthesizedExpression) ||
                expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                expression.IsKind(SyntaxKind.InvocationExpression) ||
                expression.IsKind(SyntaxKind.ElementAccessExpression) ||
                expression.IsKind(SyntaxKind.MemberBindingExpression))
            {
                return expression;
            }

            return Parenthesize(expression);
        }

        private static SeparatedSyntaxList<ExpressionSyntax> AsExpressionList(IEnumerable<SyntaxNode> expressions)
        {
            return SyntaxFactory.SeparatedList(expressions.OfType<ExpressionSyntax>());
        }

        public override SyntaxNode ArrayCreationExpression(SyntaxNode elementType, SyntaxNode size)
        {
            var arrayType = SyntaxFactory.ArrayType((TypeSyntax)elementType, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList((ExpressionSyntax)size))));
            return SyntaxFactory.ArrayCreationExpression(arrayType);
        }

        public override SyntaxNode ArrayCreationExpression(SyntaxNode elementType, IEnumerable<SyntaxNode> elements)
        {
            var arrayType = SyntaxFactory.ArrayType((TypeSyntax)elementType, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()));
            var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, AsExpressionList(elements));
            return SyntaxFactory.ArrayCreationExpression(arrayType, initializer);
        }

        public override SyntaxNode ObjectCreationExpression(SyntaxNode type, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ObjectCreationExpression((TypeSyntax)type, CreateArgumentList(arguments), null);
        }

        private static ArgumentListSyntax CreateArgumentList(IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ArgumentList(CreateArguments(arguments));
        }

        private static SeparatedSyntaxList<ArgumentSyntax> CreateArguments(IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.SeparatedList(arguments.Select(AsArgument));
        }

        private static ArgumentSyntax AsArgument(SyntaxNode argOrExpression)
            => argOrExpression as ArgumentSyntax ?? SyntaxFactory.Argument((ExpressionSyntax)argOrExpression);

        public override SyntaxNode InvocationExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.InvocationExpression(ParenthesizeLeft((ExpressionSyntax)expression), CreateArgumentList(arguments));
        }

        public override SyntaxNode ElementAccessExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory.ElementAccessExpression(ParenthesizeLeft((ExpressionSyntax)expression), SyntaxFactory.BracketedArgumentList(CreateArguments(arguments)));
        }

        internal override SyntaxNode InterpolatedStringExpression(SyntaxToken startToken, IEnumerable<SyntaxNode> content, SyntaxToken endToken)
            => SyntaxFactory.InterpolatedStringExpression(startToken, SyntaxFactory.List(content.Cast<InterpolatedStringContentSyntax>()), endToken);

        internal override SyntaxNode InterpolatedStringText(SyntaxToken textToken)
            => SyntaxFactory.InterpolatedStringText(textToken);

        internal override SyntaxToken InterpolatedStringTextToken(string content)
            => SyntaxFactory.Token(
                SyntaxFactory.TriviaList(),
                SyntaxKind.InterpolatedStringTextToken,
                content, "",
                SyntaxFactory.TriviaList());

        internal override SyntaxNode Interpolation(SyntaxNode syntaxNode)
            => SyntaxFactory.Interpolation((ExpressionSyntax)syntaxNode);

        internal override SyntaxToken NumericLiteralToken(string text, ulong value)
            => SyntaxFactory.Literal(text, value);

        public override SyntaxNode DefaultExpression(SyntaxNode type)
            => SyntaxFactory.DefaultExpression((TypeSyntax)type).WithAdditionalAnnotations(Simplifier.Annotation);

        public override SyntaxNode DefaultExpression(ITypeSymbol type)
        {
            // If it's just a reference type, then "null" is the default expression for it.  Note:
            // this counts for actual reference type, or a type parameter with a 'class' constraint.
            // Also, if it's a nullable type, then we can use "null".
            if (type.IsReferenceType ||
                type.IsPointerType() ||
                type.IsNullable())
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
            return DefaultExpression(type.GenerateTypeSyntax());
        }

        internal override SyntaxNode AddParentheses(SyntaxNode expression)
        {
            return Parenthesize(expression);
        }

        private static ExpressionSyntax Parenthesize(SyntaxNode expression)
        {
            return ((ExpressionSyntax)expression).Parenthesize();
        }

        public override SyntaxNode IsTypeExpression(SyntaxNode expression, SyntaxNode type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, Parenthesize(expression), (TypeSyntax)type);
        }

        public override SyntaxNode TypeOfExpression(SyntaxNode type)
        {
            return SyntaxFactory.TypeOfExpression((TypeSyntax)type);
        }

        public override SyntaxNode TryCastExpression(SyntaxNode expression, SyntaxNode type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, Parenthesize(expression), (TypeSyntax)type);
        }

        public override SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression)
            => SyntaxFactory.CastExpression((TypeSyntax)type, Parenthesize(expression)).WithAdditionalAnnotations(Simplifier.Annotation);

        public override SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression)
        {
            return SyntaxFactory.CastExpression((TypeSyntax)type, Parenthesize(expression)).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public override SyntaxNode AssignmentStatement(SyntaxNode left, SyntaxNode right)
            => SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)left, Parenthesize(right));

        private static SyntaxNode CreateBinaryExpression(SyntaxKind syntaxKind, SyntaxNode left, SyntaxNode right)
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

        public override SyntaxNode TypedConstantExpression(TypedConstant value)
        {
            return ExpressionGenerator.GenerateExpression(value);
        }

        public override SyntaxNode IdentifierName(string identifier)
            => identifier.ToIdentifierName();

        public override SyntaxNode GenericName(string identifier, IEnumerable<SyntaxNode> typeArguments)
            => GenericName(identifier.ToIdentifierToken(), typeArguments);

        internal override SyntaxNode GenericName(SyntaxToken identifier, IEnumerable<SyntaxNode> typeArguments)
            => SyntaxFactory.GenericName(identifier,
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast<TypeSyntax>())));

        public override SyntaxNode WithTypeArguments(SyntaxNode expression, IEnumerable<SyntaxNode> typeArguments)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                    var sname = (SimpleNameSyntax)expression;
                    return SyntaxFactory.GenericName(sname.Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast<TypeSyntax>())));

                case SyntaxKind.GenericName:
                    var gname = (GenericNameSyntax)expression;
                    return gname.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast<TypeSyntax>())));

                case SyntaxKind.QualifiedName:
                    var qname = (QualifiedNameSyntax)expression;
                    return qname.WithRight((SimpleNameSyntax)this.WithTypeArguments(qname.Right, typeArguments));

                case SyntaxKind.AliasQualifiedName:
                    var aname = (AliasQualifiedNameSyntax)expression;
                    return aname.WithName((SimpleNameSyntax)this.WithTypeArguments(aname.Name, typeArguments));

                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    var sma = (MemberAccessExpressionSyntax)expression;
                    return sma.WithName((SimpleNameSyntax)this.WithTypeArguments(sma.Name, typeArguments));

                default:
                    return expression;
            }
        }

        public override SyntaxNode QualifiedName(SyntaxNode left, SyntaxNode right)
            => SyntaxFactory.QualifiedName((NameSyntax)left, (SimpleNameSyntax)right).WithAdditionalAnnotations(Simplifier.Annotation);

        internal override SyntaxNode GlobalAliasedName(SyntaxNode name)
            => SyntaxFactory.AliasQualifiedName(
                SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                (SimpleNameSyntax)name);

        public override SyntaxNode NameExpression(INamespaceOrTypeSymbol namespaceOrTypeSymbol)
            => namespaceOrTypeSymbol.GenerateNameSyntax();

        public override SyntaxNode TypeExpression(ITypeSymbol typeSymbol)
        {
            return typeSymbol.GenerateTypeSyntax();
        }

        public override SyntaxNode TypeExpression(SpecialType specialType)
            => specialType switch
            {
                SpecialType.System_Boolean => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SpecialType.System_Byte => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword)),
                SpecialType.System_Char => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword)),
                SpecialType.System_Decimal => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword)),
                SpecialType.System_Double => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)),
                SpecialType.System_Int16 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword)),
                SpecialType.System_Int32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SpecialType.System_Int64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)),
                SpecialType.System_Object => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                SpecialType.System_SByte => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword)),
                SpecialType.System_Single => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                SpecialType.System_String => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SpecialType.System_UInt16 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword)),
                SpecialType.System_UInt32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                SpecialType.System_UInt64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword)),
                _ => throw new NotSupportedException("Unsupported SpecialType"),
            };

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

        internal override SyntaxNode CreateTupleType(IEnumerable<SyntaxNode> elements)
            => SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements.Cast<TupleElementSyntax>()));

        public override SyntaxNode TupleElementExpression(SyntaxNode type, string name = null)
            => SyntaxFactory.TupleElement((TypeSyntax)type, name?.ToIdentifierToken() ?? default);

        public override SyntaxNode Argument(string nameOpt, RefKind refKind, SyntaxNode expression)
        {
            return SyntaxFactory.Argument(
                nameOpt == null ? null : SyntaxFactory.NameColon(nameOpt),
                GetArgumentModifiers(refKind),
                (ExpressionSyntax)expression);
        }

        public override SyntaxNode LocalDeclarationStatement(SyntaxNode type, string name, SyntaxNode initializer, bool isConst)
            => LocalDeclarationStatement(type, name.ToIdentifierToken(), initializer, isConst);

        internal override SyntaxNode LocalDeclarationStatement(SyntaxNode type, SyntaxToken name, SyntaxNode initializer, bool isConst)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                isConst ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)) : default,
                VariableDeclaration(type, name, initializer));
        }

        internal override SyntaxNode WithInitializer(SyntaxNode variableDeclarator, SyntaxNode initializer)
            => ((VariableDeclaratorSyntax)variableDeclarator).WithInitializer((EqualsValueClauseSyntax)initializer);

        internal override SyntaxNode EqualsValueClause(SyntaxToken operatorToken, SyntaxNode value)
            => SyntaxFactory.EqualsValueClause(operatorToken, (ExpressionSyntax)value);

        private static VariableDeclarationSyntax VariableDeclaration(SyntaxNode type, SyntaxToken name, SyntaxNode expression)
        {
            return SyntaxFactory.VariableDeclaration(
                type == null ? SyntaxFactory.IdentifierName("var") : (TypeSyntax)type,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            name, argumentList: null,
                            expression == null ? null : SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression))));
        }

        public override SyntaxNode UsingStatement(SyntaxNode type, string name, SyntaxNode expression, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.UsingStatement(
                VariableDeclaration(type, name.ToIdentifierToken(), expression),
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

        public override SyntaxNode LockStatement(SyntaxNode expression, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.LockStatement(
                expression: (ExpressionSyntax)expression,
                statement: CreateBlock(statements));
        }

        public override SyntaxNode TryCatchStatement(IEnumerable<SyntaxNode> tryStatements, IEnumerable<SyntaxNode> catchClauses, IEnumerable<SyntaxNode> finallyStatements = null)
        {
            return SyntaxFactory.TryStatement(
                CreateBlock(tryStatements),
                catchClauses != null ? SyntaxFactory.List(catchClauses.Cast<CatchClauseSyntax>()) : default,
                finallyStatements != null ? SyntaxFactory.FinallyClause(CreateBlock(finallyStatements)) : null);
        }

        public override SyntaxNode CatchClause(SyntaxNode type, string name, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration((TypeSyntax)type, name.ToIdentifierToken()),
                filter: null,
                block: CreateBlock(statements));
        }

        public override SyntaxNode WhileStatement(SyntaxNode condition, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.WhileStatement((ExpressionSyntax)condition, CreateBlock(statements));
        }

        public override SyntaxNode SwitchStatement(SyntaxNode expression, IEnumerable<SyntaxNode> caseClauses)
        {
            if (expression is TupleExpressionSyntax)
            {
                return SyntaxFactory.SwitchStatement(
                    (ExpressionSyntax)expression,
                    caseClauses.Cast<SwitchSectionSyntax>().ToSyntaxList());
            }
            else
            {
                return SyntaxFactory.SwitchStatement(
                    SyntaxFactory.Token(SyntaxKind.SwitchKeyword),
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    (ExpressionSyntax)expression,
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                    caseClauses.Cast<SwitchSectionSyntax>().ToSyntaxList(),
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
            }
        }

        public override SyntaxNode SwitchSection(IEnumerable<SyntaxNode> expressions, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(AsSwitchLabels(expressions), AsStatementList(statements));
        }

        internal override SyntaxNode SwitchSectionFromLabels(IEnumerable<SyntaxNode> labels, IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(
                labels.Cast<SwitchLabelSyntax>().ToSyntaxList(),
                AsStatementList(statements));
        }

        public override SyntaxNode DefaultSwitchSection(IEnumerable<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(SyntaxFactory.SingletonList(SyntaxFactory.DefaultSwitchLabel() as SwitchLabelSyntax), AsStatementList(statements));
        }

        private static SyntaxList<SwitchLabelSyntax> AsSwitchLabels(IEnumerable<SyntaxNode> expressions)
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

        internal override SyntaxNode ScopeBlock(IEnumerable<SyntaxNode> statements)
            => SyntaxFactory.Block(statements.Cast<StatementSyntax>());

        public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> parameterDeclarations, SyntaxNode expression)
        {
            var prms = AsReadOnlyList(parameterDeclarations?.Cast<ParameterSyntax>());

            if (prms.Count == 1 && IsSimpleLambdaParameter(prms[0]))
            {
                return SyntaxFactory.SimpleLambdaExpression(prms[0], (CSharpSyntaxNode)expression);
            }
            else
            {
                return SyntaxFactory.ParenthesizedLambdaExpression(AsParameterList(prms), (CSharpSyntaxNode)expression);
            }
        }

        private static bool IsSimpleLambdaParameter(SyntaxNode node)
            => node is ParameterSyntax p && p.Type == null && p.Default == null && p.Modifiers.Count == 0;

        public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, SyntaxNode expression)
        {
            return this.ValueReturningLambdaExpression(lambdaParameters, expression);
        }

        public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> parameterDeclarations, IEnumerable<SyntaxNode> statements)
        {
            return this.ValueReturningLambdaExpression(parameterDeclarations, CreateBlock(statements));
        }

        public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, IEnumerable<SyntaxNode> statements)
        {
            return this.ValueReturningLambdaExpression(lambdaParameters, statements);
        }

        public override SyntaxNode LambdaParameter(string identifier, SyntaxNode type = null)
        {
            return this.ParameterDeclaration(identifier, type, null, RefKind.None);
        }

        private static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> sequence)
        {
            return sequence as IReadOnlyList<T> ?? sequence.ToImmutableReadOnlyListOrEmpty();
        }

        internal override SyntaxNode IdentifierName(SyntaxToken identifier)
            => SyntaxFactory.IdentifierName(identifier);

        internal override SyntaxToken Identifier(string identifier)
            => SyntaxFactory.Identifier(identifier);

        internal override SyntaxNode NamedAnonymousObjectMemberDeclarator(SyntaxNode identifier, SyntaxNode expression)
        {
            return SyntaxFactory.AnonymousObjectMemberDeclarator(
                SyntaxFactory.NameEquals((IdentifierNameSyntax)identifier),
                (ExpressionSyntax)expression);
        }

        internal override SyntaxNode RefExpression(SyntaxNode expression)
            => SyntaxFactory.RefExpression((ExpressionSyntax)expression);

        public override SyntaxNode TupleExpression(IEnumerable<SyntaxNode> arguments)
            => SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(arguments.Select(AsArgument)));

        internal override SyntaxNode RemoveAllComments(SyntaxNode node)
        {
            var modifiedNode = RemoveLeadingAndTrailingComments(node);

            if (modifiedNode is TypeDeclarationSyntax declarationSyntax)
            {
                return declarationSyntax.WithOpenBraceToken(RemoveLeadingAndTrailingComments(declarationSyntax.OpenBraceToken))
                    .WithCloseBraceToken(RemoveLeadingAndTrailingComments(declarationSyntax.CloseBraceToken));
            }

            return modifiedNode;
        }

        internal override SyntaxTriviaList RemoveCommentLines(SyntaxTriviaList syntaxTriviaList)
        {
            static IEnumerable<IEnumerable<SyntaxTrivia>> splitIntoLines(SyntaxTriviaList triviaList)
            {
                var index = 0;
                for (var i = 0; i < triviaList.Count; i++)
                {
                    if (triviaList[i].IsEndOfLine())
                    {
                        yield return triviaList.TakeRange(index, i);
                        index = i + 1;
                    }
                }

                if (index < triviaList.Count)
                {
                    yield return triviaList.TakeRange(index, triviaList.Count - 1);
                }
            }

            var syntaxWithoutComments = splitIntoLines(syntaxTriviaList)
                .Where(trivia => !trivia.Any(t => t.IsRegularOrDocComment()))
                .SelectMany(t => t);

            return new SyntaxTriviaList(syntaxWithoutComments);
        }
        #endregion

        #region Patterns

        internal override bool SupportsPatterns(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        internal override SyntaxNode IsPatternExpression(SyntaxNode expression, SyntaxNode pattern)
            => SyntaxFactory.IsPatternExpression((ExpressionSyntax)expression, (PatternSyntax)pattern);

        internal override SyntaxNode ConstantPattern(SyntaxNode expression)
            => SyntaxFactory.ConstantPattern((ExpressionSyntax)expression);

        internal override SyntaxNode DeclarationPattern(INamedTypeSymbol type, string name)
            => SyntaxFactory.DeclarationPattern(
                type.GenerateTypeSyntax(),
                SyntaxFactory.SingleVariableDesignation(name.ToIdentifierToken()));

        #endregion
    }
}
