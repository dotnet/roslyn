// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using System.Composition;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CSharpSyntaxTokens;

[ExportLanguageService(typeof(SyntaxGenerator), LanguageNames.CSharp), Shared]
internal sealed class CSharpSyntaxGenerator : SyntaxGenerator
{
    // A bit hacky, but we need to actually run ParseToken on the "nameof" text as there's no
    // other way to get a token back that has the appropriate internal bit set that indicates
    // this has the .ContextualKind of SyntaxKind.NameOfKeyword.
    private static readonly IdentifierNameSyntax s_nameOfIdentifier =
        SyntaxFactory.IdentifierName(SyntaxFactory.ParseToken("nameof"));

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
    public CSharpSyntaxGenerator()
    {
    }

    internal override SyntaxTrivia ElasticMarker => SyntaxFactory.ElasticMarker;

    internal override SyntaxGeneratorInternal SyntaxGeneratorInternal
        => CSharpSyntaxGeneratorInternal.Instance;

    internal override SyntaxTrivia Whitespace(string text)
        => SyntaxFactory.Whitespace(text);

    internal override SeparatedSyntaxList<TElement> SeparatedList<TElement>(SyntaxNodeOrTokenList list)
        => SyntaxFactory.SeparatedList<TElement>(list);

    internal override SyntaxToken CreateInterpolatedStringStartToken(bool isVerbatim)
    {
        const string InterpolatedVerbatimText = "$@\"";

        return isVerbatim
            ? SyntaxFactory.Token(default, SyntaxKind.InterpolatedVerbatimStringStartToken, InterpolatedVerbatimText, InterpolatedVerbatimText, default)
            : InterpolatedStringStartToken;
    }

    internal override SyntaxToken CreateInterpolatedStringEndToken()
        => InterpolatedStringEndToken;

    internal override SeparatedSyntaxList<TElement> SeparatedList<TElement>(IEnumerable<TElement> nodes, IEnumerable<SyntaxToken> separators)
        => SyntaxFactory.SeparatedList(nodes, separators);

    internal override SyntaxTrivia Trivia(SyntaxNode node)
    {
        if (node is StructuredTriviaSyntax structuredTriviaSyntax)
        {
            return SyntaxFactory.Trivia(structuredTriviaSyntax);
        }

        throw ExceptionUtilities.UnexpectedValue(node.Kind());
    }

    internal override SyntaxNode DocumentationCommentTrivia(IEnumerable<SyntaxNode> nodes, SyntaxTriviaList trailingTrivia, string endOfLineString)
    {
        var docTrivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.MultiLineDocumentationCommentTrivia,
            (SyntaxList<XmlNodeSyntax>)SyntaxFactory.List(nodes),
            EndOfDocumentationCommentToken);

        docTrivia = docTrivia.WithLeadingTrivia(SyntaxFactory.DocumentationCommentExterior("/// "))
            .WithTrailingTrivia(trailingTrivia);

        return docTrivia.WithTrailingTrivia(SyntaxFactory.EndOfLine(endOfLineString));
    }

    internal override SyntaxNode? DocumentationCommentTriviaWithUpdatedContent(SyntaxTrivia trivia, IEnumerable<SyntaxNode> content)
    {
        if (trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationCommentTrivia)
        {
            return SyntaxFactory.DocumentationCommentTrivia(documentationCommentTrivia.Kind(), (SyntaxList<XmlNodeSyntax>)SyntaxFactory.List(content), documentationCommentTrivia.EndOfComment);
        }

        return null;
    }

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
            ? [.. declarations.Select(this.AsUsingDirective).OfType<UsingDirectiveSyntax>()]
            : default;
    }

    private SyntaxNode? AsUsingDirective(SyntaxNode node)
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
            ? [.. declarations.Select(AsNamespaceMember).OfType<MemberDeclarationSyntax>()]
            : default;
    }

    private static SyntaxNode? AsNamespaceMember(SyntaxNode declaration)
    {
        switch (declaration.Kind())
        {
            case SyntaxKind.NamespaceDeclaration:
            case SyntaxKind.FileScopedNamespaceDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.DelegateDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.RecordStructDeclaration:
                return declaration;
            default:
                return null;
        }
    }

    public override SyntaxNode NamespaceImportDeclaration(SyntaxNode name)
        => SyntaxFactory.UsingDirective((NameSyntax)name);

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
        SyntaxNode? initializer)
    {
        // some constant types will also appear as readonly when read from metadata
        modifiers = modifiers.IsConst ? modifiers.WithIsReadOnly(false) : modifiers;

        return SyntaxFactory.FieldDeclaration(
            default,
            AsModifierList(accessibility, modifiers, SyntaxKind.FieldDeclaration),
            SyntaxFactory.VariableDeclaration(
                (TypeSyntax)type,
                [SyntaxFactory.VariableDeclarator(
                    name.ToIdentifierToken(),
                    null,
                    initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null)]));
    }

    private protected override SyntaxNode ParameterDeclaration(
        string name, SyntaxNode? type, SyntaxNode? initializer, RefKind refKind, bool isExtension, bool isParams, bool isScoped)
    {
        var modifiers = CSharpSyntaxGeneratorInternal.GetParameterModifiers(refKind);
        if (isScoped)
            modifiers = modifiers.Insert(0, ScopedKeyword);

        if (isExtension)
            modifiers = modifiers.Insert(0, ThisKeyword);

        if (isParams)
            modifiers = modifiers.Add(ParamsKeyword);

        return SyntaxFactory.Parameter(
            default,
            modifiers,
            (TypeSyntax?)type,
            name.ToIdentifierToken(),
            initializer != null ? SyntaxFactory.EqualsValueClause((ExpressionSyntax)initializer) : null);
    }

    internal static SyntaxToken GetArgumentModifiers(RefKind refKind)
    {
        switch (refKind)
        {
            case RefKind.None:
            case RefKind.In:
                return default;
            case RefKind.Out:
                return OutKeyword;
            case RefKind.Ref:
                return RefKeyword;
            case RefKind.RefReadOnlyParameter:
                return InKeyword;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    private protected override SyntaxNode MethodDeclaration(
        string name,
        IEnumerable<SyntaxNode>? parameters,
        IEnumerable<SyntaxNode>? typeParameters,
        SyntaxNode? returnType,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? statements)
    {
        var hasBody = !modifiers.IsAbstract && (!modifiers.IsPartial || statements != null);

        if (!hasBody)
            modifiers -= DeclarationModifiers.Async;

        name = StripExplicitInterfaceName(name);

        return SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: AsModifierList(accessibility, modifiers, SyntaxKind.MethodDeclaration),
            returnType: returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(VoidKeyword),
            explicitInterfaceSpecifier: null,
            identifier: name.ToIdentifierToken(),
            typeParameterList: AsTypeParameterList(typeParameters),
            parameterList: AsParameterList(parameters),
            constraintClauses: default,
            body: hasBody ? CreateBlock(statements) : null,
            expressionBody: null,
            semicolonToken: !hasBody ? SemicolonToken : default);
    }

    private static string StripExplicitInterfaceName(string name)
    {
        // Only keep what's after the dot (if this is an explicit impl).  The explicit impl part will be added by
        // the caller.
        return name.LastIndexOf('.') is var index && index >= 0 ? name[(index + 1)..] : name;
    }

    public override SyntaxNode OperatorDeclaration(OperatorKind kind, IEnumerable<SyntaxNode>? parameters = null, SyntaxNode? returnType = null, Accessibility accessibility = Accessibility.NotApplicable, DeclarationModifiers modifiers = default, IEnumerable<SyntaxNode>? statements = null)
    {
        return OperatorDeclaration(GetOperatorName(kind), isImplicitConversion: kind == OperatorKind.ImplicitConversion, parameters, returnType, accessibility, modifiers, statements);

    }

    private protected override SyntaxNode OperatorDeclaration(string operatorName, bool isImplicitConversion, IEnumerable<SyntaxNode>? parameters = null, SyntaxNode? returnType = null, Accessibility accessibility = Accessibility.NotApplicable, DeclarationModifiers modifiers = default, IEnumerable<SyntaxNode>? statements = null)
    {
        var hasBody = !modifiers.IsAbstract && (!modifiers.IsPartial || statements != null);
        var returnTypeNode = returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(VoidKeyword);
        var parameterList = AsParameterList(parameters);
        var body = hasBody ? CreateBlock(statements) : null;
        var semicolon = !hasBody ? SemicolonToken : default;
        var modifierList = AsModifierList(accessibility, modifiers, SyntaxKind.OperatorDeclaration);
        var attributes = default(SyntaxList<AttributeListSyntax>);

        if (operatorName is WellKnownMemberNames.ImplicitConversionName or WellKnownMemberNames.ExplicitConversionName)
        {
            var isImplicit = operatorName is WellKnownMemberNames.ImplicitConversionName;
            return SyntaxFactory.ConversionOperatorDeclaration(
                attributes, modifierList,
                isImplicit ? ImplicitKeyword : ExplicitKeyword,
                explicitInterfaceSpecifier: null,
                OperatorKeyword,
                checkedKeyword: default,
                returnTypeNode, parameterList, body, expressionBody: null, semicolon);
        }
        else
        {
            return SyntaxFactory.OperatorDeclaration(
                attributes, modifierList, returnTypeNode,
                explicitInterfaceSpecifier: null,
                OperatorKeyword,
                checkedKeyword: CSharp.SyntaxFacts.IsCheckedOperator(operatorName) ? CheckedKeyword : default,
                operatorToken: SyntaxFactory.Token(GetOperatorSyntaxKind(operatorName)),
                parameterList, body, expressionBody: null, semicolon);
        }
    }

    private static SyntaxKind GetOperatorSyntaxKind(string operatorName)
    {
        var operatorKind = CSharp.SyntaxFacts.GetOperatorKind(operatorName);
        if (operatorKind == SyntaxKind.None)
        {
            throw new ArgumentException("Unknown operator kind.");
        }

        return operatorKind;
    }

    private static string GetOperatorName(OperatorKind kind)
        => kind switch
        {
            OperatorKind.ImplicitConversion => WellKnownMemberNames.ImplicitConversionName,
            OperatorKind.ExplicitConversion => WellKnownMemberNames.ExplicitConversionName,
            OperatorKind.Addition => WellKnownMemberNames.AdditionOperatorName,
            OperatorKind.BitwiseAnd => WellKnownMemberNames.BitwiseAndOperatorName,
            OperatorKind.BitwiseOr => WellKnownMemberNames.BitwiseOrOperatorName,
            OperatorKind.Decrement => WellKnownMemberNames.DecrementOperatorName,
            OperatorKind.Division => WellKnownMemberNames.DivisionOperatorName,
            OperatorKind.Equality => WellKnownMemberNames.EqualityOperatorName,
            OperatorKind.ExclusiveOr => WellKnownMemberNames.ExclusiveOrOperatorName,
            OperatorKind.False => WellKnownMemberNames.FalseOperatorName,
            OperatorKind.GreaterThan => WellKnownMemberNames.GreaterThanOperatorName,
            OperatorKind.GreaterThanOrEqual => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
            OperatorKind.Increment => WellKnownMemberNames.IncrementOperatorName,
            OperatorKind.Inequality => WellKnownMemberNames.InequalityOperatorName,
            OperatorKind.LeftShift => WellKnownMemberNames.LeftShiftOperatorName,
            OperatorKind.LessThan => WellKnownMemberNames.LessThanOperatorName,
            OperatorKind.LessThanOrEqual => WellKnownMemberNames.LessThanOrEqualOperatorName,
            OperatorKind.LogicalNot => WellKnownMemberNames.LogicalNotOperatorName,
            OperatorKind.Modulus => WellKnownMemberNames.ModulusOperatorName,
            OperatorKind.Multiply => WellKnownMemberNames.MultiplyOperatorName,
            OperatorKind.OnesComplement => WellKnownMemberNames.OnesComplementOperatorName,
            OperatorKind.RightShift => WellKnownMemberNames.RightShiftOperatorName,
            OperatorKind.UnsignedRightShift => WellKnownMemberNames.UnsignedRightShiftOperatorName,
            OperatorKind.Subtraction => WellKnownMemberNames.SubtractionOperatorName,
            OperatorKind.True => WellKnownMemberNames.TrueOperatorName,
            OperatorKind.UnaryNegation => WellKnownMemberNames.UnaryNegationOperatorName,
            OperatorKind.UnaryPlus => WellKnownMemberNames.UnaryPlusOperatorName,
            _ => throw new ArgumentException("Unknown operator kind."),
        };

    private static ParameterListSyntax AsParameterList(IEnumerable<SyntaxNode>? parameters)
    {
        return parameters != null
            ? SyntaxFactory.ParameterList([.. parameters.Cast<ParameterSyntax>()])
            : SyntaxFactory.ParameterList();
    }

    public override SyntaxNode ConstructorDeclaration(
        string? name,
        IEnumerable<SyntaxNode>? parameters,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? baseConstructorArguments,
        IEnumerable<SyntaxNode>? statements)
    {
        return SyntaxFactory.ConstructorDeclaration(
            default,
            AsModifierList(accessibility, modifiers, SyntaxKind.ConstructorDeclaration),
            (name ?? "ctor").ToIdentifierToken(),
            AsParameterList(parameters),
            baseConstructorArguments != null ? SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList([.. baseConstructorArguments.Select(AsArgument)])) : null,
            CreateBlock(statements));
    }

    private protected override SyntaxNode DestructorDeclaration(IMethodSymbol destructorMethod)
        => SyntaxFactory.DestructorDeclaration(destructorMethod.ContainingType.Name).WithBody(SyntaxFactory.Block());

    public override SyntaxNode PropertyDeclaration(
        string name,
        SyntaxNode type,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? getAccessorStatements,
        IEnumerable<SyntaxNode>? setAccessorStatements)
    {
        SyntaxNode? getAccessor = null;
        SyntaxNode? setAccessor = null;

        if (!modifiers.IsWriteOnly)
            getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, modifiers.IsAbstract ? null : getAccessorStatements);

        if (!modifiers.IsReadOnly)
            setAccessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, modifiers.IsAbstract ? null : setAccessorStatements);

        return PropertyDeclaration(name, type, getAccessor, setAccessor, accessibility, modifiers);
    }

    private protected override SyntaxNode PropertyDeclaration(
        string name,
        SyntaxNode type,
        SyntaxNode? getAccessor,
        SyntaxNode? setAccessor,
        Accessibility accessibility,
        DeclarationModifiers modifiers)
    {
        var accessors = new List<AccessorDeclarationSyntax>();

        accessors.AddIfNotNull((AccessorDeclarationSyntax?)getAccessor);
        accessors.AddIfNotNull((AccessorDeclarationSyntax?)setAccessor);

        var actualModifiers = modifiers - (DeclarationModifiers.ReadOnly | DeclarationModifiers.WriteOnly);

        name = StripExplicitInterfaceName(name);

        return SyntaxFactory.PropertyDeclaration(
            attributeLists: default,
            AsModifierList(accessibility, actualModifiers, SyntaxKind.PropertyDeclaration),
            (TypeSyntax)type,
            explicitInterfaceSpecifier: null,
            name.ToIdentifierToken(),
            SyntaxFactory.AccessorList([.. accessors]));
    }

    public override SyntaxNode GetAccessorDeclaration(Accessibility accessibility, IEnumerable<SyntaxNode>? statements)
        => AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, accessibility, statements);

    private protected override SyntaxNode SetAccessorDeclaration(Accessibility accessibility, bool isInitOnly, IEnumerable<SyntaxNode>? statements)
        => AccessorDeclaration(isInitOnly ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration, accessibility, statements);

    private static SyntaxNode AccessorDeclaration(
        SyntaxKind kind, Accessibility accessibility, IEnumerable<SyntaxNode>? statements)
    {
        var accessor = SyntaxFactory.AccessorDeclaration(kind);
        accessor = accessor.WithModifiers(
            AsModifierList(accessibility, DeclarationModifiers.None, SyntaxKind.PropertyDeclaration));

        accessor = statements == null
            ? accessor.WithSemicolonToken(SemicolonToken)
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

    private static AccessorListSyntax CreateAccessorList(AccessorListSyntax? accessorList, IEnumerable<SyntaxNode> accessorDeclarations)
    {
        var list = SyntaxFactory.List(accessorDeclarations.Cast<AccessorDeclarationSyntax>());
        return accessorList == null
            ? SyntaxFactory.AccessorList(list)
            : accessorList.WithAccessors(list);
    }

    public override SyntaxNode IndexerDeclaration(
        IEnumerable<SyntaxNode> parameters,
        SyntaxNode type,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? getAccessorStatements,
        IEnumerable<SyntaxNode>? setAccessorStatements)
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
                getAccessorStatements = [];

            if (setAccessorStatements == null && hasSetter)
                setAccessorStatements = [];
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
            explicitInterfaceSpecifier: null,
            AsBracketedParameterList(parameters),
            SyntaxFactory.AccessorList([.. accessors]));
    }

    private static BracketedParameterListSyntax AsBracketedParameterList(IEnumerable<SyntaxNode> parameters)
    {
        return parameters != null
            ? SyntaxFactory.BracketedParameterList([.. parameters.Cast<ParameterSyntax>()])
            : SyntaxFactory.BracketedParameterList();
    }

    private static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, IEnumerable<SyntaxNode>? statements)
    {
        var ad = SyntaxFactory.AccessorDeclaration(
            kind,
            statements != null ? CreateBlock(statements) : null);

        if (statements == null)
        {
            ad = ad.WithSemicolonToken(SemicolonToken);
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
                [SyntaxFactory.VariableDeclarator(name)]));
    }

    public override SyntaxNode CustomEventDeclaration(
        string name,
        SyntaxNode type,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? parameters,
        IEnumerable<SyntaxNode>? addAccessorStatements,
        IEnumerable<SyntaxNode>? removeAccessorStatements)
    {
        var accessors = new List<AccessorDeclarationSyntax>();
        if (modifiers.IsAbstract)
        {
            addAccessorStatements = null;
            removeAccessorStatements = null;
        }
        else
        {
            addAccessorStatements ??= [];
            removeAccessorStatements ??= [];
        }

        accessors.Add(AccessorDeclaration(SyntaxKind.AddAccessorDeclaration, addAccessorStatements));
        accessors.Add(AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration, removeAccessorStatements));

        return SyntaxFactory.EventDeclaration(
            default,
            AsModifierList(accessibility, modifiers, SyntaxKind.EventDeclaration),
            (TypeSyntax)type,
            null,
            name.ToIdentifierToken(),
            SyntaxFactory.AccessorList([.. accessors]));
    }

    public override SyntaxNode? AsPublicInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceTypeName, string? interfaceMemberName)
    {
        // C# interface implementations are implicit/not-specified -- so they are just named the name as the interface member
        return PreserveTrivia(declaration, d =>
        {
            d = WithInterfaceSpecifier(d, specifier: null);
            d = this.AsImplementation(d, Accessibility.Public);

            if (interfaceMemberName != null)
            {
                d = this.WithName(d, interfaceMemberName);
            }

            return d;
        });
    }

    public override SyntaxNode? AsPrivateInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceTypeName, string? interfaceMemberName)
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
        if (declaration is MethodDeclarationSyntax method)
        {
            if (method.ConstraintClauses.Count > 0)
            {
                return RemoveNodes(method, method.ConstraintClauses);
            }
        }

        return declaration;
    }

    private static SyntaxNode WithInterfaceSpecifier(SyntaxNode declaration, ExplicitInterfaceSpecifierSyntax? specifier)
        => declaration.Kind() switch
        {
            SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
            SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
            SyntaxKind.OperatorDeclaration => ((OperatorDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
            SyntaxKind.ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax)declaration).WithExplicitInterfaceSpecifier(specifier),
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
            case SyntaxKind.ConversionOperatorDeclaration:
            case SyntaxKind.OperatorDeclaration:
                var method = (BaseMethodDeclarationSyntax)declaration;
                return (method.Body == null && method.ExpressionBody == null) ? method.WithSemicolonToken(default).WithBody(CreateBlock()) : method;

            case SyntaxKind.PropertyDeclaration:
                var prop = (PropertyDeclarationSyntax)declaration;
                return (prop.AccessorList != null) ? prop.WithAccessorList(WithBodies(prop.AccessorList)) : prop;

            case SyntaxKind.IndexerDeclaration:
                var ind = (IndexerDeclarationSyntax)declaration;
                return (ind.AccessorList != null) ? ind.WithAccessorList(WithBodies(ind.AccessorList)) : ind;

            case SyntaxKind.EventDeclaration:
                var ev = (EventDeclarationSyntax)declaration;
                return (ev.AccessorList != null) ? ev.WithAccessorList(WithBodies(ev.AccessorList)) : ev;
        }

        return declaration;
    }

    private static AccessorListSyntax WithBodies(AccessorListSyntax accessorList)
        => accessorList.WithAccessors([.. accessorList.Accessors.Select(WithBody)]);

    private static AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax accessor)
    {
        if (accessor.Body == null && accessor.ExpressionBody == null)
        {
            return accessor.WithSemicolonToken(default).WithBody(CreateBlock(null));
        }
        else
        {
            return accessor;
        }
    }

    private static AccessorListSyntax? WithoutBodies(AccessorListSyntax? accessorList)
        => accessorList?.WithAccessors([.. accessorList.Accessors.Select(WithoutBody)]);

    private static AccessorDeclarationSyntax WithoutBody(AccessorDeclarationSyntax accessor)
        => accessor.Body != null ? accessor.WithSemicolonToken(SemicolonToken).WithBody(null)
            : accessor.ExpressionBody != null ? accessor.WithExpressionBody(null)
            : accessor;

    private protected override SyntaxNode ClassDeclaration(
        bool isRecord,
        string name,
        IEnumerable<SyntaxNode>? typeParameters,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        SyntaxNode? baseType,
        IEnumerable<SyntaxNode>? interfaceTypes,
        IEnumerable<SyntaxNode>? members)
    {
        using var _ = ArrayBuilder<BaseTypeSyntax>.GetInstance(out var baseTypes);
        if (baseType != null || interfaceTypes != null)
        {
            if (baseType != null)
                baseTypes.Add(SyntaxFactory.SimpleBaseType((TypeSyntax)baseType));

            if (interfaceTypes != null)
                baseTypes.AddRange(interfaceTypes.Select(i => SyntaxFactory.SimpleBaseType((TypeSyntax)i)));
        }

        var kind = isRecord ? SyntaxKind.RecordDeclaration : SyntaxKind.ClassDeclaration;
        var modifierList = AsModifierList(accessibility, modifiers, kind);
        var nameToken = name.ToIdentifierToken();
        var typeParameterList = AsTypeParameterList(typeParameters);
        var baseTypeList = baseTypes.Count > 0 ? SyntaxFactory.BaseList([.. baseTypes]) : null;
        var typeMembers = this.AsClassMembers(name, members);

        return isRecord
            ? SyntaxFactory.RecordDeclaration(default, modifierList, RecordKeyword, nameToken, typeParameterList, null, baseTypeList, default, typeMembers)
            : SyntaxFactory.ClassDeclaration(default, modifierList, nameToken, typeParameterList, baseTypeList, default, typeMembers);
    }

    private SyntaxList<MemberDeclarationSyntax> AsClassMembers(string className, IEnumerable<SyntaxNode>? members)
    {
        return members != null
            ? [.. members.Select(m => AsClassMember(m, className)).WhereNotNull()]
            : default;
    }

    private MemberDeclarationSyntax? AsClassMember(SyntaxNode node, string className)
    {
        switch (node.Kind())
        {
            case SyntaxKind.ConstructorDeclaration:
                node = ((ConstructorDeclarationSyntax)node).WithIdentifier(className.ToIdentifierToken());
                break;

            case SyntaxKind.VariableDeclaration:
            case SyntaxKind.VariableDeclarator:
                node = AsIsolatedDeclaration(node);
                break;
        }

        return node as MemberDeclarationSyntax;
    }

    private protected override SyntaxNode StructDeclaration(
        bool isRecord,
        string name,
        IEnumerable<SyntaxNode>? typeParameters,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? interfaceTypes,
        IEnumerable<SyntaxNode>? members)
    {
        var itypes = interfaceTypes?.Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)i)).ToList();

        var kind = isRecord ? SyntaxKind.RecordStructDeclaration : SyntaxKind.StructDeclaration;
        var modifierList = AsModifierList(accessibility, modifiers, kind);
        var nameToken = name.ToIdentifierToken();
        var typeParameterList = AsTypeParameterList(typeParameters);
        var baseTypeList = itypes?.Count > 0 ? SyntaxFactory.BaseList([.. itypes]) : null;
        var structMembers = this.AsClassMembers(name, members);

        return isRecord
            ? SyntaxFactory.RecordDeclaration(default, modifierList, RecordKeyword, nameToken, typeParameterList, null, baseTypeList, default, structMembers).WithClassOrStructKeyword(StructKeyword)
            : SyntaxFactory.StructDeclaration(default, modifierList, nameToken, typeParameterList, baseTypeList, default, structMembers);
    }

    private protected override SyntaxNode InterfaceDeclaration(
        string name,
        IEnumerable<SyntaxNode>? typeParameters,
        Accessibility accessibility,
        IEnumerable<SyntaxNode>? interfaceTypes = null,
        IEnumerable<SyntaxNode>? members = null)
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
            itypes != null ? SyntaxFactory.BaseList([.. itypes]) : null,
            default,
            this.AsInterfaceMembers(members));
    }

    private SyntaxList<MemberDeclarationSyntax> AsInterfaceMembers(IEnumerable<SyntaxNode>? members)
    {
        return members != null
            ? [.. members.Select(AsInterfaceMember).OfType<MemberDeclarationSyntax>()]
            : default;
    }

    internal override SyntaxNode AsInterfaceMember(SyntaxNode m)
    {
        return Isolate(m, member =>
        {
            Accessibility acc;
            DeclarationModifiers modifiers;

            // return any nested member "as is" without any additional changes
            if (member is BaseTypeDeclarationSyntax)
                return member;

            switch (member.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)member)
                             .WithModifiers(default)
                             .WithSemicolonToken(SemicolonToken)
                             .WithBody(null)
                             .WithExpressionBody(null);

                case SyntaxKind.OperatorDeclaration:
                    var operatorDeclaration = (OperatorDeclarationSyntax)member;
                    var abstractVirtualModifiers = operatorDeclaration.Modifiers.Where(x =>
                        x.Kind() == SyntaxKind.AbstractKeyword
                        || x.Kind() == SyntaxKind.VirtualKeyword
                        || x.Kind() == SyntaxKind.PublicKeyword);
                    var modifiersToken = SyntaxFactory.TokenList(abstractVirtualModifiers);
                    modifiersToken = modifiersToken.Insert(0, StaticKeyword);
                    return operatorDeclaration
                        .WithModifiers(modifiersToken)
                        .WithSemicolonToken(SemicolonToken)
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
                    GetAccessibilityAndModifiers(f.Modifiers, out acc, out modifiers, out _);

                    var type = GetType(f);
                    Contract.ThrowIfNull(type);

                    return AsInterfaceMember(
                        PropertyDeclaration(GetName(f), ClearTrivia(type), acc, modifiers, getAccessorStatements: null, setAccessorStatements: null));

                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind());
            }
        });
    }

    public override SyntaxNode EnumDeclaration(
        string name,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        IEnumerable<SyntaxNode>? members)
    {
        return EnumDeclaration(name, underlyingType: null, accessibility, modifiers, members);
    }

    internal override SyntaxNode EnumDeclaration(string name, SyntaxNode? underlyingType, Accessibility accessibility = Accessibility.NotApplicable, DeclarationModifiers modifiers = default, IEnumerable<SyntaxNode>? members = null)
    {
        return SyntaxFactory.EnumDeclaration(
            default,
            AsModifierList(accessibility, modifiers, SyntaxKind.EnumDeclaration),
            name.ToIdentifierToken(),
            underlyingType != null ? SyntaxFactory.BaseList([SyntaxFactory.SimpleBaseType((TypeSyntax)underlyingType)]) : null,
            this.AsEnumMembers(members));
    }

    public override SyntaxNode EnumMember(string name, SyntaxNode? expression)
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
                    return (EnumMemberDeclarationSyntax)this.EnumMember(vd.Identifier.ToString(), vd.Initializer?.Value);
                }

                break;
        }

        return (EnumMemberDeclarationSyntax)node;
    }

    private SeparatedSyntaxList<EnumMemberDeclarationSyntax> AsEnumMembers(IEnumerable<SyntaxNode>? members)
        => members != null ? [.. members.Select(this.AsEnumMember)] : default;

    private protected override SyntaxNode DelegateDeclaration(
        string name,
        IEnumerable<SyntaxNode>? parameters,
        IEnumerable<SyntaxNode>? typeParameters,
        SyntaxNode? returnType,
        Accessibility accessibility = Accessibility.NotApplicable,
        DeclarationModifiers modifiers = default)
    {
        return SyntaxFactory.DelegateDeclaration(
            default,
            AsModifierList(accessibility, modifiers, SyntaxKind.DelegateDeclaration),
            returnType != null ? (TypeSyntax)returnType : SyntaxFactory.PredefinedType(VoidKeyword),
            name.ToIdentifierToken(),
            AsTypeParameterList(typeParameters),
            AsParameterList(parameters),
            default);
    }

    public override SyntaxNode Attribute(SyntaxNode name, IEnumerable<SyntaxNode>? attributeArguments)
        => AsAttributeList(SyntaxFactory.Attribute((NameSyntax)name, AsAttributeArgumentList(attributeArguments)));

    public override SyntaxNode AttributeArgument(string? name, SyntaxNode expression)
    {
        return name != null
            ? SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(name.ToIdentifierName()), nameColon: null, (ExpressionSyntax)expression)
            : SyntaxFactory.AttributeArgument((ExpressionSyntax)expression);
    }

    private static AttributeArgumentListSyntax? AsAttributeArgumentList(IEnumerable<SyntaxNode>? arguments)
        => arguments != null ? SyntaxFactory.AttributeArgumentList([.. arguments.Select(AsAttributeArgument)]) : null;

    private static AttributeArgumentSyntax AsAttributeArgument(SyntaxNode node)
    {
        if (node is ExpressionSyntax expr)
        {
            return SyntaxFactory.AttributeArgument(expr);
        }

        if (node is ArgumentSyntax arg && arg.Expression != null)
        {
            return SyntaxFactory.AttributeArgument(null, arg.NameColon, arg.Expression);
        }

        return (AttributeArgumentSyntax)node;
    }

    [return: MaybeNull, NotNullIfNotNull(nameof(node))]
    public override TNode ClearTrivia<TNode>([MaybeNull] TNode node)
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
        return attributes == null ? default : [.. attributes.Select(AsAttributeList)];
    }

    private static AttributeListSyntax AsAttributeList(SyntaxNode node)
    {
        return node is AttributeSyntax attr ? SyntaxFactory.AttributeList([attr]) : (AttributeListSyntax)node;
    }

    private static readonly ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>> s_declAttributes = new();

    public override IReadOnlyList<SyntaxNode> GetAttributes(SyntaxNode declaration)
    {
        if (!s_declAttributes.TryGetValue(declaration, out var attrs))
        {
            attrs = s_declAttributes.GetValue(declaration, declaration =>
                Flatten(declaration.GetAttributeLists().Where(al => !IsReturnAttribute(al))));
        }

        return attrs;
    }

    private static readonly ConditionalWeakTable<SyntaxNode, IReadOnlyList<SyntaxNode>> s_declReturnAttributes = new();

    public override IReadOnlyList<SyntaxNode> GetReturnAttributes(SyntaxNode declaration)
    {
        if (!s_declReturnAttributes.TryGetValue(declaration, out var attrs))
        {
            attrs = s_declReturnAttributes.GetValue(declaration, declaration =>
                Flatten(declaration.GetAttributeLists().Where(al => IsReturnAttribute(al))));
        }

        return attrs;
    }

    private static bool IsReturnAttribute(AttributeListSyntax list)
        => list.Target?.Identifier.IsKind(SyntaxKind.ReturnKeyword) ?? false;

    public override SyntaxNode InsertAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        => this.Isolate(declaration, d => this.InsertAttributesInternal(d, index, attributes));

    private SyntaxNode InsertAttributesInternal(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
    {
        var newAttributes = AsAttributeLists(attributes);

        var existingAttributes = this.GetAttributes(declaration);
        if (index >= 0 && index < existingAttributes.Count)
        {
            return this.InsertNodesBefore(declaration, existingAttributes[index], WithRequiredTargetSpecifier(newAttributes, declaration));
        }
        else if (existingAttributes.Count > 0)
        {
            return this.InsertNodesAfter(declaration, existingAttributes[existingAttributes.Count - 1], WithRequiredTargetSpecifier(newAttributes, declaration));
        }
        else
        {
            var lists = declaration.GetAttributeLists();
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
            var lists = d.GetAttributeLists();
            var newList = lists.AddRange(newAttributes);
            return WithAttributeLists(d, newList);
        }
    }

    private static IEnumerable<AttributeListSyntax> AsReturnAttributes(IEnumerable<SyntaxNode> attributes)
    {
        return AsAttributeLists(attributes)
            .Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(ReturnKeyword)));
    }

    private static SyntaxList<AttributeListSyntax> AsAssemblyAttributes(IEnumerable<AttributeListSyntax> attributes)
    {
        return [.. attributes.Select(list => list.WithTarget(SyntaxFactory.AttributeTargetSpecifier(AssemblyKeyword)))];
    }

    private static SyntaxList<AttributeListSyntax> WithRequiredTargetSpecifier(SyntaxList<AttributeListSyntax> attributes, SyntaxNode declaration)
    {
        if (!declaration.IsKind(SyntaxKind.CompilationUnit))
        {
            return attributes;
        }

        return AsAssemblyAttributes(attributes);
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

        return [];
    }

    public override SyntaxNode InsertAttributeArguments(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributeArguments)
        => this.Isolate(declaration, d => InsertAttributeArgumentsInternal(d, index, attributeArguments));

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

    private static AttributeArgumentListSyntax? GetAttributeArgumentList(SyntaxNode declaration)
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

    private static SyntaxNode WithAttributeArgumentList(SyntaxNode declaration, AttributeArgumentListSyntax? argList)
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
            StatementSyntax statement => statement.AttributeLists,
            _ => default,
        };

    private static SyntaxNode WithAttributeLists(SyntaxNode declaration, SyntaxList<AttributeListSyntax> attributeLists)
        => declaration switch
        {
            MemberDeclarationSyntax memberDecl => memberDecl.WithAttributeLists(attributeLists),
            AccessorDeclarationSyntax accessor => accessor.WithAttributeLists(attributeLists),
            ParameterSyntax parameter => parameter.WithAttributeLists(attributeLists),
            CompilationUnitSyntax compilationUnit => compilationUnit.WithAttributeLists(AsAssemblyAttributes(attributeLists)),
            StatementSyntax statement => statement.WithAttributeLists(attributeLists),
            TypeParameterSyntax typeParameter => typeParameter.WithAttributeLists(attributeLists),
            LambdaExpressionSyntax lambdaExpression => lambdaExpression.WithAttributeLists(attributeLists),
            _ => declaration,
        };

    internal override SyntaxNode? GetPrimaryConstructorParameterList(SyntaxNode declaration)
        => declaration is TypeDeclarationSyntax { ParameterList: { } parameterList } ? parameterList : null;

    internal override ImmutableArray<SyntaxNode> GetTypeInheritance(SyntaxNode declaration)
        => declaration is BaseTypeDeclarationSyntax baseType && baseType.BaseList != null
            ? [baseType.BaseList]
            : ImmutableArray<SyntaxNode>.Empty;

    public override IReadOnlyList<SyntaxNode> GetNamespaceImports(SyntaxNode declaration)
        => declaration switch
        {
            CompilationUnitSyntax compilationUnit => compilationUnit.Usings,
            BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Usings,
            _ => [],
        };

    public override SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports)
    {
        var result = PreserveTrivia(declaration, d => InsertNamespaceImportsInternal(d, index, imports));
        Contract.ThrowIfNull(result);
        return result;
    }

    private SyntaxNode InsertNamespaceImportsInternal(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports)
    {
        var usingsToInsert = this.AsUsingDirectives(imports);

        return declaration switch
        {
            CompilationUnitSyntax cu => cu.WithUsings(cu.Usings.InsertRange(index, usingsToInsert)),
            BaseNamespaceDeclarationSyntax nd => nd.WithUsings(nd.Usings.InsertRange(index, usingsToInsert)),
            _ => declaration,
        };
    }

    public override IReadOnlyList<SyntaxNode> GetMembers(SyntaxNode declaration)
        => Flatten(declaration switch
        {
            TypeDeclarationSyntax type => type.Members,
            EnumDeclarationSyntax @enum => @enum.Members,
            BaseNamespaceDeclarationSyntax @namespace => @namespace.Members,
            CompilationUnitSyntax compilationUnit => compilationUnit.Members,
            _ => [],
        });

    private static ImmutableArray<SyntaxNode> Flatten(IEnumerable<SyntaxNode> declarations)
    {
        var builder = ArrayBuilder<SyntaxNode>.GetInstance();

        foreach (var declaration in declarations)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.FieldDeclaration:
                    FlattenDeclaration(builder, declaration, ((FieldDeclarationSyntax)declaration).Declaration);
                    break;

                case SyntaxKind.EventFieldDeclaration:
                    FlattenDeclaration(builder, declaration, ((EventFieldDeclarationSyntax)declaration).Declaration);
                    break;

                case SyntaxKind.LocalDeclarationStatement:
                    FlattenDeclaration(builder, declaration, ((LocalDeclarationStatementSyntax)declaration).Declaration);
                    break;

                case SyntaxKind.VariableDeclaration:
                    FlattenDeclaration(builder, declaration, (VariableDeclarationSyntax)declaration);
                    break;

                case SyntaxKind.AttributeList:
                    var attrList = (AttributeListSyntax)declaration;
                    if (attrList.Attributes.Count > 1)
                    {
                        builder.AddRange(attrList.Attributes);
                    }
                    else
                    {
                        builder.Add(attrList);
                    }

                    break;

                default:
                    builder.Add(declaration);
                    break;
            }
        }

        return builder.ToImmutableAndFree();

        static void FlattenDeclaration(ArrayBuilder<SyntaxNode> builder, SyntaxNode declaration, VariableDeclarationSyntax variableDeclaration)
        {
            if (variableDeclaration.Variables.Count > 1)
            {
                builder.AddRange(variableDeclaration.Variables);
            }
            else
            {
                builder.Add(declaration);
            }
        }
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

    private static SyntaxNode EnsureTypeDeclarationHasBody(SyntaxNode declaration)
    {
        if (declaration is BaseTypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration
                .WithSemicolonToken(default)
                .WithOpenBraceToken(typeDeclaration.OpenBraceToken == default ? OpenBraceToken : typeDeclaration.OpenBraceToken)
                .WithCloseBraceToken(typeDeclaration.CloseBraceToken == default ? CloseBraceToken : typeDeclaration.CloseBraceToken);
        }

        return declaration;
    }

    public override SyntaxNode InsertMembers(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members)
    {
        declaration = EnsureTypeDeclarationHasBody(declaration);
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
            return declaration switch
            {
                TypeDeclarationSyntax type => type.WithMembers(type.Members.AddRange(newMembers)),
                EnumDeclarationSyntax @enum => @enum.WithMembers(@enum.Members.AddRange(newMembers.OfType<EnumMemberDeclarationSyntax>())),
                BaseNamespaceDeclarationSyntax @namespace => @namespace.WithMembers(@namespace.Members.AddRange(newMembers)),
                CompilationUnitSyntax compilationUnit => compilationUnit.WithMembers(compilationUnit.Members.AddRange(newMembers)),
                _ => declaration,
            };
        }
    }

    [return: NotNullIfNotNull(nameof(members))]
    private IEnumerable<MemberDeclarationSyntax>? AsMembersOf(SyntaxNode declaration, IEnumerable<SyntaxNode>? members)
        => members?.Select(m => AsMemberOf(declaration, m)).OfType<MemberDeclarationSyntax>();

    private SyntaxNode? AsMemberOf(SyntaxNode declaration, SyntaxNode member)
        => declaration switch
        {
            InterfaceDeclarationSyntax => AsInterfaceMember(member),
            TypeDeclarationSyntax typeDeclaration => AsClassMember(member, typeDeclaration.Identifier.Text),
            EnumDeclarationSyntax => AsEnumMember(member),
            BaseNamespaceDeclarationSyntax => AsNamespaceMember(member),
            CompilationUnitSyntax => AsNamespaceMember(member),
            _ => null,
        };

    private static bool CanHaveAccessibility(SyntaxNode declaration)
        // For certain declarations, the answer of CanHaveAccessibility depends on the modifiers.
        // For example, static constructors cannot have accessibility, but constructors in general can.
        // The same applies to file-local declarations (e.g, "file class C { }").
        // For such declarations, we want to return true. This is because we can be explicitly asked to put accessibility.
        // In such cases, we'll drop the modifier that prevents us from having accessibility.
        => CSharpAccessibilityFacts.Instance.CanHaveAccessibility(declaration, ignoreDeclarationModifiers: true);

    public override Accessibility GetAccessibility(SyntaxNode declaration)
        => CSharpAccessibilityFacts.Instance.GetAccessibility(declaration);

    private static void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers, out bool isDefault)
        => CSharpAccessibilityFacts.GetAccessibilityAndModifiers(modifierList, out accessibility, out modifiers, out isDefault);

    public override SyntaxNode WithAccessibility(SyntaxNode declaration, Accessibility accessibility)
    {
        if (!CanHaveAccessibility(declaration) &&
            accessibility != Accessibility.NotApplicable)
        {
            return declaration;
        }

        return this.Isolate(declaration, d =>
        {
            var tokens = GetModifierTokens(d);
            GetAccessibilityAndModifiers(tokens, out _, out var modifiers, out _);
            if (modifiers.IsFile && accessibility != Accessibility.NotApplicable)
            {
                // If user wants to set accessibility for a file-local declaration, we remove file.
                // Otherwise, code will be in error:
                // error CS9052: File-local type '{0}' cannot use accessibility modifiers.
                modifiers = modifiers.WithIsFile(false);
            }

            if (modifiers.IsStatic && declaration.IsKind(SyntaxKind.ConstructorDeclaration) && accessibility != Accessibility.NotApplicable)
            {
                // If user wants to add accessibility for a static constructor, we remove static modifier
                modifiers = modifiers.WithIsStatic(false);
            }

            var newTokens = Merge(tokens, AsModifierList(accessibility, modifiers));
            return SetModifierTokens(d, newTokens);
        });
    }

    private static readonly DeclarationModifiers s_fieldModifiers =
        DeclarationModifiers.Const |
        DeclarationModifiers.New |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Ref |
        DeclarationModifiers.Required |
        DeclarationModifiers.Static |
        DeclarationModifiers.Unsafe |
        DeclarationModifiers.Volatile;

    private static readonly DeclarationModifiers s_methodModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.Async |
        DeclarationModifiers.Extern |
        DeclarationModifiers.New |
        DeclarationModifiers.Override |
        DeclarationModifiers.Partial |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Static |
        DeclarationModifiers.Virtual |
        DeclarationModifiers.Unsafe;

    private static readonly DeclarationModifiers s_constructorModifiers =
        DeclarationModifiers.Extern |
        DeclarationModifiers.Static |
        DeclarationModifiers.Unsafe;

    private static readonly DeclarationModifiers s_destructorModifiers = DeclarationModifiers.Unsafe;
    private static readonly DeclarationModifiers s_propertyModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.Extern |
        DeclarationModifiers.New |
        DeclarationModifiers.Override |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Required |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Static |
        DeclarationModifiers.Virtual |
        DeclarationModifiers.Unsafe;

    private static readonly DeclarationModifiers s_eventModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.Extern |
        DeclarationModifiers.New |
        DeclarationModifiers.Override |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Static |
        DeclarationModifiers.Virtual |
        DeclarationModifiers.Unsafe;

    private static readonly DeclarationModifiers s_indexerModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.Extern |
        DeclarationModifiers.New |
        DeclarationModifiers.Override |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Static |
        DeclarationModifiers.Virtual |
        DeclarationModifiers.Unsafe;

    private static readonly DeclarationModifiers s_classModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.New |
        DeclarationModifiers.Partial |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Static |
        DeclarationModifiers.Unsafe |
        DeclarationModifiers.File;

    private static readonly DeclarationModifiers s_recordModifiers =
        DeclarationModifiers.Abstract |
        DeclarationModifiers.New |
        DeclarationModifiers.Partial |
        DeclarationModifiers.Sealed |
        DeclarationModifiers.Unsafe |
        DeclarationModifiers.File;

    private static readonly DeclarationModifiers s_structModifiers =
        DeclarationModifiers.New |
        DeclarationModifiers.Partial |
        DeclarationModifiers.ReadOnly |
        DeclarationModifiers.Ref |
        DeclarationModifiers.Unsafe |
        DeclarationModifiers.File;

    private static readonly DeclarationModifiers s_interfaceModifiers = DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.Unsafe | DeclarationModifiers.File;
    private static readonly DeclarationModifiers s_eventAccessorModifiers = DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Override | DeclarationModifiers.Virtual;
    private static readonly DeclarationModifiers s_propertyAccessorModifiers = s_eventAccessorModifiers | DeclarationModifiers.ReadOnly;

    private static readonly DeclarationModifiers s_localFunctionModifiers =
        DeclarationModifiers.Async |
        DeclarationModifiers.Static |
        DeclarationModifiers.Unsafe |
        DeclarationModifiers.Extern;

    private static readonly DeclarationModifiers s_lambdaModifiers =
        DeclarationModifiers.Async |
        DeclarationModifiers.Static;

    private static DeclarationModifiers GetAllowedModifiers(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.RecordDeclaration:
                return s_recordModifiers;
            case SyntaxKind.ClassDeclaration:
                return s_classModifiers;

            case SyntaxKind.EnumDeclaration:
                return DeclarationModifiers.New | DeclarationModifiers.File;

            case SyntaxKind.DelegateDeclaration:
                return DeclarationModifiers.New | DeclarationModifiers.Unsafe | DeclarationModifiers.File;

            case SyntaxKind.InterfaceDeclaration:
                return s_interfaceModifiers;

            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
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
            case SyntaxKind.EventDeclaration:
                return s_eventModifiers;

            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
                return s_propertyAccessorModifiers;

            case SyntaxKind.AddAccessorDeclaration:
            case SyntaxKind.RemoveAccessorDeclaration:
                return s_eventAccessorModifiers;

            case SyntaxKind.LocalFunctionStatement:
                return s_localFunctionModifiers;
            case SyntaxKind.ParenthesizedLambdaExpression:
            case SyntaxKind.SimpleLambdaExpression:
            case SyntaxKind.AnonymousMethodExpression:
                return s_lambdaModifiers;

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
        GetAccessibilityAndModifiers(modifierTokens, out _, out var modifiers, out _);
        return modifiers;
    }

    private static SyntaxTokenList GetModifierTokens(SyntaxNode declaration)
        => CSharpAccessibilityFacts.GetModifierTokens(declaration);

    public override SyntaxNode WithModifiers(SyntaxNode declaration, DeclarationModifiers modifiers)
        => this.Isolate(declaration, d => this.WithModifiersInternal(d, modifiers));

    private SyntaxNode WithModifiersInternal(SyntaxNode declaration, DeclarationModifiers modifiers)
    {
        modifiers &= GetAllowedModifiers(declaration.Kind());
        var existingModifiers = this.GetModifiers(declaration);

        if (modifiers != existingModifiers)
        {
            return this.Isolate(declaration, d =>
            {
                var tokens = GetModifierTokens(d);
                GetAccessibilityAndModifiers(tokens, out var accessibility, out var tmp, out _);
                if (accessibility != Accessibility.NotApplicable)
                {
                    if (modifiers.IsFile ||
                        (modifiers.IsStatic && declaration.IsKind(SyntaxKind.ConstructorDeclaration)))
                    {
                        // We remove the accessibility if the modifiers don't allow it.
                        accessibility = Accessibility.NotApplicable;
                    }
                }

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

    private static SyntaxNode SetModifierTokens(SyntaxNode declaration, SyntaxTokenList modifiers)
        => declaration switch
        {
            MemberDeclarationSyntax memberDecl => memberDecl.WithModifiers(modifiers),
            ParameterSyntax parameter => parameter.WithModifiers(modifiers),
            LocalDeclarationStatementSyntax localDecl => localDecl.WithModifiers(modifiers),
            LocalFunctionStatementSyntax localFunc => localFunc.WithModifiers(modifiers),
            AccessorDeclarationSyntax accessor => accessor.WithModifiers(modifiers),
            AnonymousFunctionExpressionSyntax anonymous => anonymous.WithModifiers(modifiers),
            _ => declaration,
        };

    private static SyntaxTokenList AsModifierList(Accessibility accessibility, DeclarationModifiers modifiers, SyntaxKind kind)
        => AsModifierList(accessibility, GetAllowedModifiers(kind) & modifiers);

    private static SyntaxTokenList AsModifierList(Accessibility accessibility, DeclarationModifiers modifiers)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var list);

        switch (accessibility)
        {
            case Accessibility.Internal:
                list.Add(InternalKeyword);
                break;
            case Accessibility.Public:
                list.Add(PublicKeyword);
                break;
            case Accessibility.Private:
                list.Add(PrivateKeyword);
                break;
            case Accessibility.Protected:
                list.Add(ProtectedKeyword);
                break;
            case Accessibility.ProtectedOrInternal:
                list.Add(ProtectedKeyword);
                list.Add(InternalKeyword);
                break;
            case Accessibility.ProtectedAndInternal:
                list.Add(PrivateKeyword);
                list.Add(ProtectedKeyword);
                break;
            case Accessibility.NotApplicable:
                break;
        }

        if (modifiers.IsFile)
            list.Add(FileKeyword);

        if (modifiers.IsAbstract)
            list.Add(AbstractKeyword);

        if (modifiers.IsNew)
            list.Add(NewKeyword);

        if (modifiers.IsSealed)
            list.Add(SealedKeyword);

        if (modifiers.IsOverride)
            list.Add(OverrideKeyword);

        if (modifiers.IsVirtual)
            list.Add(VirtualKeyword);

        if (modifiers.IsStatic)
            list.Add(StaticKeyword);

        if (modifiers.IsAsync)
            list.Add(AsyncKeyword);

        if (modifiers.IsConst)
            list.Add(ConstKeyword);

        if (modifiers.IsReadOnly)
            list.Add(ReadOnlyKeyword);

        if (modifiers.IsUnsafe)
            list.Add(UnsafeKeyword);

        if (modifiers.IsVolatile)
            list.Add(VolatileKeyword);

        if (modifiers.IsExtern)
            list.Add(ExternKeyword);

        if (modifiers.IsRequired)
            list.Add(RequiredKeyword);

        // partial and ref must be last
        if (modifiers.IsRef)
            list.Add(RefKeyword);

        if (modifiers.IsPartial)
            list.Add(PartialKeyword);

        return [.. list];
    }

    private protected override SyntaxNode TypeParameter(string name)
        => SyntaxFactory.TypeParameter(name);

    private protected override SyntaxNode TypeParameter(ITypeParameterSymbol typeParameter)
    {
        return SyntaxFactory.TypeParameter(
            attributeLists: default,
            varianceKeyword: typeParameter.Variance switch
            {
                VarianceKind.In => InKeyword,
                VarianceKind.Out => OutKeyword,
                _ => default,
            },
            SyntaxFactory.Identifier(typeParameter.Name));
    }

    private static TypeParameterListSyntax? AsTypeParameterList(IEnumerable<SyntaxNode>? typeParameterNodes)
    {
        var typeParameters = typeParameterNodes != null
            ? SyntaxFactory.TypeParameterList([.. typeParameterNodes.Cast<TypeParameterSyntax>()])
            : null;

        return typeParameters?.Parameters.Count > 0 ? typeParameters : null;
    }

    private protected override SyntaxNode WithTypeParameters(SyntaxNode declaration, IEnumerable<SyntaxNode> typeParameters)
    {
        var typeParameterList = AsTypeParameterList(typeParameters);

        return declaration switch
        {
            MethodDeclarationSyntax method => method.WithTypeParameterList(typeParameterList),
            TypeDeclarationSyntax type => type.WithTypeParameterList(typeParameterList),
            DelegateDeclarationSyntax @delegate => @delegate.WithTypeParameterList(typeParameterList),
            _ => declaration,
        };
    }

    internal override SyntaxNode WithExplicitInterfaceImplementations(
            SyntaxNode declaration,
            ImmutableArray<ISymbol> explicitInterfaceImplementations,
            bool removeDefaults)
        => WithAccessibility(declaration switch
        {
            MethodDeclarationSyntax method
                => WithoutConstraints(
                    method.ReplaceNodes(method.ParameterList.Parameters, (_, p) => RemoveDefaultValue(p, removeDefaults))
                          .WithExplicitInterfaceSpecifier(CreateExplicitInterfaceSpecifier(explicitInterfaceImplementations))),
            BasePropertyDeclarationSyntax member
                => member.WithExplicitInterfaceSpecifier(CreateExplicitInterfaceSpecifier(explicitInterfaceImplementations)),
            _ => declaration,
        }, Accessibility.NotApplicable);

    private static MethodDeclarationSyntax WithoutConstraints(MethodDeclarationSyntax method)
    {
        if (method.ConstraintClauses.Count == 0)
            return method;

        return method.WithConstraintClauses(default)
                     .WithParameterList(method.ParameterList.WithTrailingTrivia(
                        method.ParameterList.GetTrailingTrivia().Add(SyntaxFactory.ElasticMarker).AddRange(method.ConstraintClauses.Last().GetTrailingTrivia())));
    }

    private static SyntaxNode RemoveDefaultValue(ParameterSyntax parameter, bool removeDefaults)
    {
        if (!removeDefaults)
            return parameter;

        if (parameter.Default == null)
            return parameter;

        parameter = parameter.WithDefault(null);
        if (!parameter.Identifier.TrailingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
            parameter = parameter.WithIdentifier(parameter.Identifier.WithoutTrailingTrivia());

        return parameter;
    }

    private static ExplicitInterfaceSpecifierSyntax CreateExplicitInterfaceSpecifier(ImmutableArray<ISymbol> explicitInterfaceImplementations)
        => SyntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceImplementations[0].ContainingType.GenerateNameSyntax());

    private protected override SyntaxNode WithTypeConstraint(
        SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kinds, bool isUnmanagedType, IEnumerable<SyntaxNode>? types)
        => declaration switch
        {
            MethodDeclarationSyntax method => method.WithConstraintClauses(WithTypeConstraints(method.ConstraintClauses, typeParameterName, kinds, isUnmanagedType, types)),
            TypeDeclarationSyntax type => type.WithConstraintClauses(WithTypeConstraints(type.ConstraintClauses, typeParameterName, kinds, isUnmanagedType, types)),
            DelegateDeclarationSyntax @delegate => @delegate.WithConstraintClauses(WithTypeConstraints(@delegate.ConstraintClauses, typeParameterName, kinds, isUnmanagedType, types)),
            _ => declaration,
        };

    private protected override SyntaxNode WithDefaultConstraint(SyntaxNode declaration, string typeParameterName)
    {
        var method = (MethodDeclarationSyntax)declaration;
        return method.AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause(
            typeParameterName).AddConstraints(SyntaxFactory.DefaultConstraint()));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> WithTypeConstraints(
        SyntaxList<TypeParameterConstraintClauseSyntax> clauses,
        string typeParameterName,
        SpecialTypeConstraintKind kinds,
        bool isUnmanagedType,
        IEnumerable<SyntaxNode>? types)
    {
        var constraints = types != null
            ? SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>(types.Select(t => SyntaxFactory.TypeConstraint((TypeSyntax)t)))
            : [];

        if ((kinds & SpecialTypeConstraintKind.Constructor) != 0)
        {
            constraints = constraints.Add(SyntaxFactory.ConstructorConstraint());
        }

        var isReferenceType = (kinds & SpecialTypeConstraintKind.ReferenceType) != 0;
        var isValueType = (kinds & SpecialTypeConstraintKind.ValueType) != 0 && !isUnmanagedType;

        if (isReferenceType || isValueType)
        {
            constraints = constraints.Insert(0, SyntaxFactory.ClassOrStructConstraint(isReferenceType ? SyntaxKind.ClassConstraint : SyntaxKind.StructConstraint));
        }
        else if (isUnmanagedType)
        {
            constraints = constraints.Insert(0, SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("unmanaged")));
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
        => CSharpAccessibilityFacts.GetDeclarationKind(declaration);

    public override string GetName(SyntaxNode declaration)
        => declaration switch
        {
            BaseTypeDeclarationSyntax baseTypeDeclaration => baseTypeDeclaration.Identifier.ValueText,
            DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Identifier.ValueText,
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier.ValueText,
            BaseFieldDeclarationSyntax baseFieldDeclaration => this.GetName(baseFieldDeclaration.Declaration),
            PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier.ValueText,
            EnumMemberDeclarationSyntax enumMemberDeclaration => enumMemberDeclaration.Identifier.ValueText,
            EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier.ValueText,
            BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Name.ToString(),
            UsingDirectiveSyntax usingDirective => usingDirective.Name?.ToString() ?? string.Empty,
            ParameterSyntax parameter => parameter.Identifier.ValueText,
            LocalDeclarationStatementSyntax localDeclaration => this.GetName(localDeclaration.Declaration),
            VariableDeclarationSyntax variableDeclaration when variableDeclaration.Variables.Count == 1 => variableDeclaration.Variables[0].Identifier.ValueText,
            VariableDeclaratorSyntax variableDeclarator => variableDeclarator.Identifier.ValueText,
            TypeParameterSyntax typeParameter => typeParameter.Identifier.ValueText,
            AttributeListSyntax attributeList when attributeList.Attributes.Count == 1 => attributeList.Attributes[0].Name.ToString(),
            AttributeSyntax attribute => attribute.Name.ToString(),
            _ => string.Empty
        };

    public override SyntaxNode WithName(SyntaxNode declaration, string name)
        => this.Isolate(declaration, d => this.WithNameInternal(d, name));

    private SyntaxNode WithNameInternal(SyntaxNode declaration, string name)
    {
        var id = name.ToIdentifierToken();
        return declaration switch
        {
            BaseTypeDeclarationSyntax typeDeclaration => ReplaceWithTrivia(declaration, typeDeclaration.Identifier, id),
            DelegateDeclarationSyntax delegateDeclaration => ReplaceWithTrivia(declaration, delegateDeclaration.Identifier, id),
            MethodDeclarationSyntax methodDeclaration => ReplaceWithTrivia(declaration, methodDeclaration.Identifier, id),

            BaseFieldDeclarationSyntax fieldDeclaration when fieldDeclaration.Declaration.Variables.Count == 1 =>
                ReplaceWithTrivia(declaration, fieldDeclaration.Declaration.Variables[0].Identifier, id),

            PropertyDeclarationSyntax propertyDeclaration => ReplaceWithTrivia(declaration, propertyDeclaration.Identifier, id),
            EnumMemberDeclarationSyntax enumMemberDeclaration => ReplaceWithTrivia(declaration, enumMemberDeclaration.Identifier, id),
            EventDeclarationSyntax eventDeclaration => ReplaceWithTrivia(declaration, eventDeclaration.Identifier, id),
            BaseNamespaceDeclarationSyntax namespaceDeclaration => ReplaceWithTrivia(declaration, namespaceDeclaration.Name, this.DottedName(name)),
            UsingDirectiveSyntax usingDeclaration => ReplaceWithTrivia(declaration, usingDeclaration.NamespaceOrType, this.DottedName(name)),
            ParameterSyntax parameter => ReplaceWithTrivia(declaration, parameter.Identifier, id),

            LocalDeclarationStatementSyntax localDeclaration when localDeclaration.Declaration.Variables.Count == 1 =>
                ReplaceWithTrivia(declaration, localDeclaration.Declaration.Variables[0].Identifier, id),

            TypeParameterSyntax typeParameter => ReplaceWithTrivia(declaration, typeParameter.Identifier, id),

            AttributeListSyntax attributeList when attributeList.Attributes.Count == 1 =>
                ReplaceWithTrivia(declaration, attributeList.Attributes[0].Name, this.DottedName(name)),

            AttributeSyntax attribute => ReplaceWithTrivia(declaration, attribute.Name, this.DottedName(name)),

            VariableDeclarationSyntax variableDeclaration when variableDeclaration.Variables.Count == 1 =>
                ReplaceWithTrivia(declaration, variableDeclaration.Variables[0].Identifier, id),

            VariableDeclaratorSyntax variableDeclarator => ReplaceWithTrivia(declaration, variableDeclarator.Identifier, id),
            _ => declaration
        };
    }

    public override SyntaxNode? GetType(SyntaxNode declaration)
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

    private static TypeSyntax? NotVoid(TypeSyntax type)
        => type is PredefinedTypeSyntax pd && pd.Keyword.IsKind(SyntaxKind.VoidKeyword) ? null : type;

    public override SyntaxNode WithType(SyntaxNode declaration, SyntaxNode type)
        => Isolate(declaration, d => WithTypeInternal(d, type));

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
        => PreserveTrivia(AsIsolatedDeclaration(declaration), editor);

    private SyntaxNode AsIsolatedDeclaration(SyntaxNode declaration)
    {
        switch (declaration.Kind())
        {
            case SyntaxKind.VariableDeclaration:
                var vd = (VariableDeclarationSyntax)declaration;
                if (vd.Parent != null && vd.Variables.Count == 1)
                {
                    return AsIsolatedDeclaration(vd.Parent);
                }

                break;

            case SyntaxKind.VariableDeclarator:
                var v = (VariableDeclaratorSyntax)declaration;
                if (v.Parent != null && v.Parent.Parent != null)
                {
                    return ClearTrivia(WithVariable(v.Parent.Parent, v));
                }

                break;

            case SyntaxKind.Attribute:
                var attr = (AttributeSyntax)declaration;
                if (attr.Parent != null)
                {
                    var attrList = (AttributeListSyntax)attr.Parent;
                    return attrList.WithAttributes([attr]).WithTarget(null);
                }

                break;
        }

        return declaration;
    }

    private static SyntaxNode WithVariable(SyntaxNode declaration, VariableDeclaratorSyntax variable)
    {
        var vd = GetVariableDeclaration(declaration);
        if (vd != null)
        {
            return WithVariableDeclaration(declaration, vd.WithVariables([variable]));
        }

        return declaration;
    }

    private static VariableDeclarationSyntax? GetVariableDeclaration(SyntaxNode declaration)
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
                if (CSharpAccessibilityFacts.ParentIsFieldDeclaration(vd)
                    || CSharpAccessibilityFacts.ParentIsEventFieldDeclaration(vd)
                    || CSharpAccessibilityFacts.ParentIsLocalDeclarationStatement(vd))
                {
                    Contract.ThrowIfNull(vd.Parent);
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

    private SyntaxNode? AsNodeLike(SyntaxNode existingNode, SyntaxNode newNode)
    {
        switch (GetDeclarationKind(existingNode))
        {
            case DeclarationKind.Class:
            case DeclarationKind.Interface:
            case DeclarationKind.Struct:
            case DeclarationKind.Enum:
            case DeclarationKind.Namespace:
            case DeclarationKind.CompilationUnit:
                var container = GetDeclaration(existingNode.Parent);
                if (container != null)
                {
                    return AsMemberOf(container, newNode);
                }

                break;

            case DeclarationKind.Attribute:
                return AsAttributeList(newNode);
        }

        return newNode;
    }

    public override IReadOnlyList<SyntaxNode> GetParameters(SyntaxNode declaration)
    {
        var list = declaration.GetParameterList();
        return list != null
            ? list.Parameters
            : declaration is SimpleLambdaExpressionSyntax simpleLambda
                ? [simpleLambda.Parameter]
                : [];
    }

    public override SyntaxNode InsertParameters(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters)
    {
        var newParameters = AsParameterList(parameters);

        var currentList = declaration.GetParameterList();
        currentList ??= declaration.IsKind(SyntaxKind.IndexerDeclaration)
            ? SyntaxFactory.BracketedParameterList()
            : SyntaxFactory.ParameterList();

        var newList = currentList.WithParameters(currentList.Parameters.InsertRange(index, newParameters.Parameters));
        return WithParameterList(declaration, newList);
    }

    public override IReadOnlyList<SyntaxNode> GetSwitchSections(SyntaxNode switchStatement)
    {
        var statement = switchStatement as SwitchStatementSyntax;
        return statement?.Sections ?? [];
    }

    public override SyntaxNode InsertSwitchSections(SyntaxNode switchStatement, int index, IEnumerable<SyntaxNode> switchSections)
    {
        if (switchStatement is not SwitchStatementSyntax statement)
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
            => _recurse = recurse;

        [return: NotNullIfNotNull(nameof(node))]
        public override SyntaxNode? Visit(SyntaxNode? node)
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

    internal override SyntaxNode? GetParameterListNode(SyntaxNode declaration)
        => declaration.GetParameterList();

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
                var parameters = list.Parameters;
                if (parameters.Count == 1 && IsSimpleLambdaParameter(parameters[0]))
                {
                    return lambda.WithParameter(parameters[0]);
                }
                else
                {
                    return SyntaxFactory.ParenthesizedLambdaExpression(AsParameterList(parameters), lambda.Body)
                        .WithLeadingTrivia(lambda.GetLeadingTrivia())
                        .WithTrailingTrivia(lambda.GetTrailingTrivia());
                }
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.InterfaceDeclaration:
                return ((TypeDeclarationSyntax)declaration).WithParameterList((ParameterListSyntax)list);
            default:
                return declaration;
        }
    }

    public override SyntaxNode? GetExpression(SyntaxNode declaration)
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

            case SyntaxKind.MethodDeclaration:
                var method = (MethodDeclarationSyntax)declaration;
                if (method.ExpressionBody != null)
                {
                    return method.ExpressionBody.Expression;
                }

                goto default;

            case SyntaxKind.LocalFunctionStatement:
                var local = (LocalFunctionStatementSyntax)declaration;
                if (local.ExpressionBody != null)
                {
                    return local.ExpressionBody.Expression;
                }

                goto default;

            default:
                return GetEqualsValue(declaration)?.Value;
        }
    }

    public override SyntaxNode WithExpression(SyntaxNode declaration, SyntaxNode expression)
        => this.Isolate(declaration, d => WithExpressionInternal(d, expression));

    private static SyntaxNode WithExpressionInternal(SyntaxNode declaration, SyntaxNode expression)
    {
        var expr = (ExpressionSyntax)expression;

        switch (declaration.Kind())
        {
            case SyntaxKind.ParenthesizedLambdaExpression:
                return ((ParenthesizedLambdaExpressionSyntax)declaration).WithBody((CSharpSyntaxNode)expr ?? CreateBlock());

            case SyntaxKind.SimpleLambdaExpression:
                return ((SimpleLambdaExpressionSyntax)declaration).WithBody((CSharpSyntaxNode)expr ?? CreateBlock());

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

            case SyntaxKind.MethodDeclaration:
                var method = (MethodDeclarationSyntax)declaration;
                if (method.ExpressionBody != null)
                {
                    return ReplaceWithTrivia(method, method.ExpressionBody.Expression, expr);
                }

                goto default;

            case SyntaxKind.LocalFunctionStatement:
                var local = (LocalFunctionStatementSyntax)declaration;
                if (local.ExpressionBody != null)
                {
                    return ReplaceWithTrivia(local, local.ExpressionBody.Expression, expr);
                }

                goto default;

            default:
                var eq = GetEqualsValue(declaration);
                if (eq != null)
                {
                    if (expression == null)
                    {
                        return WithEqualsValue(declaration, initializer: null);
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

    private static EqualsValueClauseSyntax? GetEqualsValue(SyntaxNode declaration)
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

    private static SyntaxNode WithEqualsValue(SyntaxNode declaration, EqualsValueClauseSyntax? initializer)
    {
        switch (declaration.Kind())
        {
            case SyntaxKind.FieldDeclaration:
                var fd = (FieldDeclarationSyntax)declaration;
                if (fd.Declaration.Variables.Count == 1)
                {
                    return ReplaceWithTrivia(declaration, fd.Declaration.Variables[0], fd.Declaration.Variables[0].WithInitializer(initializer));
                }

                break;
            case SyntaxKind.PropertyDeclaration:
                var pd = (PropertyDeclarationSyntax)declaration;
                return pd.WithInitializer(initializer);
            case SyntaxKind.LocalDeclarationStatement:
                var ld = (LocalDeclarationStatementSyntax)declaration;
                if (ld.Declaration.Variables.Count == 1)
                {
                    return ReplaceWithTrivia(declaration, ld.Declaration.Variables[0], ld.Declaration.Variables[0].WithInitializer(initializer));
                }

                break;
            case SyntaxKind.VariableDeclaration:
                var vd = (VariableDeclarationSyntax)declaration;
                if (vd.Variables.Count == 1)
                {
                    return ReplaceWithTrivia(declaration, vd.Variables[0], vd.Variables[0].WithInitializer(initializer));
                }

                break;
            case SyntaxKind.VariableDeclarator:
                return ((VariableDeclaratorSyntax)declaration).WithInitializer(initializer);
            case SyntaxKind.Parameter:
                return ((ParameterSyntax)declaration).WithDefault(initializer);
        }

        return declaration;
    }

    public override IReadOnlyList<SyntaxNode> GetStatements(SyntaxNode declaration)
    {
        var result = declaration.Kind() switch
        {
            SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).Body?.Statements,
            SyntaxKind.OperatorDeclaration => ((OperatorDeclarationSyntax)declaration).Body?.Statements,
            SyntaxKind.ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax)declaration).Body?.Statements,
            SyntaxKind.ConstructorDeclaration => ((ConstructorDeclarationSyntax)declaration).Body?.Statements,
            SyntaxKind.DestructorDeclaration => ((DestructorDeclarationSyntax)declaration).Body?.Statements,
            SyntaxKind.LocalFunctionStatement => ((LocalFunctionStatementSyntax)declaration).Body?.Statements,
            SyntaxKind.AnonymousMethodExpression => (((AnonymousMethodExpressionSyntax)declaration).Body as BlockSyntax)?.Statements,
            SyntaxKind.ParenthesizedLambdaExpression => (((ParenthesizedLambdaExpressionSyntax)declaration).Body as BlockSyntax)?.Statements,
            SyntaxKind.SimpleLambdaExpression => (((SimpleLambdaExpressionSyntax)declaration).Body as BlockSyntax)?.Statements,
            SyntaxKind.GetAccessorDeclaration or
            SyntaxKind.SetAccessorDeclaration or
            SyntaxKind.AddAccessorDeclaration or
            SyntaxKind.RemoveAccessorDeclaration => ((AccessorDeclarationSyntax)declaration).Body?.Statements,
            _ => [],
        };
        return result ?? [];
    }

    public override SyntaxNode WithStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
    {
        var body = CreateBlock(statements);
        var somebody = statements != null ? body : null;
        var semicolon = statements == null ? SemicolonToken : default;

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
        return list?.Accessors ?? [];
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

    internal static AccessorListSyntax? GetAccessorList(SyntaxNode declaration)
        => (declaration as BasePropertyDeclarationSyntax)?.AccessorList;

    private static bool CanHaveAccessors(SyntaxNode declaration)
        => declaration.Kind() switch
        {
            SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)declaration).ExpressionBody == null,
            SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).ExpressionBody == null,
            SyntaxKind.EventDeclaration => true,
            _ => false,
        };

    private static SyntaxNode WithAccessorList(SyntaxNode declaration, AccessorListSyntax accessorList)
        => declaration switch
        {
            BasePropertyDeclarationSyntax baseProperty => baseProperty.WithAccessorList(accessorList),
            _ => declaration,
        };

    private static AccessorListSyntax AsAccessorList(IEnumerable<SyntaxNode> nodes, SyntaxKind parentKind)
        => SyntaxFactory.AccessorList([.. nodes.Select(n => AsAccessor(n, parentKind)).WhereNotNull()]);

    private static AccessorDeclarationSyntax? AsAccessor(SyntaxNode node, SyntaxKind parentKind)
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

    private static AccessorDeclarationSyntax? GetAccessor(SyntaxNode declaration, SyntaxKind kind)
    {
        var accessorList = GetAccessorList(declaration);
        return accessorList?.Accessors.FirstOrDefault(a => a.IsKind(kind));
    }

    private SyntaxNode WithAccessor(SyntaxNode declaration, SyntaxKind kind, AccessorDeclarationSyntax accessor)
        => WithAccessor(declaration, GetAccessorList(declaration), kind, accessor);

    private SyntaxNode WithAccessor(SyntaxNode declaration, AccessorListSyntax? accessorList, SyntaxKind kind, AccessorDeclarationSyntax? accessor)
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
        return accessor?.Body?.Statements ?? [];
    }

    public override IReadOnlyList<SyntaxNode> GetSetAccessorStatements(SyntaxNode declaration)
    {
        var accessor = GetAccessor(declaration, SyntaxKind.SetAccessorDeclaration);
        return accessor?.Body?.Statements ?? [];
    }

    public override SyntaxNode WithGetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        => this.WithAccessorStatements(declaration, SyntaxKind.GetAccessorDeclaration, statements);

    public override SyntaxNode WithSetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        => this.WithAccessorStatements(declaration, SyntaxKind.SetAccessorDeclaration, statements);

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
            return [];
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
            return AddBaseList(declaration, SyntaxFactory.BaseList([SyntaxFactory.SimpleBaseType((TypeSyntax)baseType)]));
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
            return AddBaseList(declaration, SyntaxFactory.BaseList([SyntaxFactory.SimpleBaseType((TypeSyntax)interfaceType)]));
        }
    }

    private static SyntaxNode AddBaseList(SyntaxNode declaration, BaseListSyntax baseList)
    {
        var newDecl = WithBaseList(declaration, baseList);

        // move trivia from type identifier to after base list
        return ShiftTrivia(newDecl, GetBaseList(newDecl)!);
    }

    private static BaseListSyntax? GetBaseList(SyntaxNode declaration)
        => declaration is TypeDeclarationSyntax typeDeclaration
            ? typeDeclaration.BaseList
            : null;

    private static SyntaxNode WithBaseList(SyntaxNode declaration, BaseListSyntax baseList)
        => declaration is TypeDeclarationSyntax typeDeclaration
            ? typeDeclaration.WithBaseList(baseList)
            : declaration;

    #endregion

    #region Remove, Replace, Insert

    public override SyntaxNode ReplaceNode(SyntaxNode root, SyntaxNode declaration, SyntaxNode? newDeclaration)
    {
        if (newDeclaration == null)
        {
            return RemoveNode(root, declaration);
        }

        newDeclaration = AsNodeLike(declaration, newDeclaration);
        if (newDeclaration == null)
        {
            return RemoveNode(root, declaration);
        }

        if (root.Span.Contains(declaration.Span))
        {
            var newFullDecl = AsIsolatedDeclaration(newDeclaration);
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
                var index = IndexOf(GetSubDeclarations(fullDecl), declaration);

                // replace declaration with multiple declarations
                return ReplaceRange(root, fullDecl, SplitAndReplace(fullDecl, index, [newDeclaration]));
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

    private static bool AreSimilarExceptForSubDeclarations(SyntaxNode? decl1, SyntaxNode? decl2)
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
        if (declaration.Parent.IsKind(SyntaxKind.GlobalStatement))
        {
            // Insert global statements before this global statement
            declaration = declaration.Parent;
            newDeclarations = newDeclarations.Select(declaration => declaration is StatementSyntax statement ? SyntaxFactory.GlobalStatement(statement) : declaration);
        }

        if (root.Span.Contains(declaration.Span))
        {
            return this.Isolate(root.TrackNodes(declaration), r => this.InsertNodesBeforeInternal(r, r.GetCurrentNode(declaration)!, newDeclarations));
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
        if (declaration.Parent.IsKind(SyntaxKind.GlobalStatement))
        {
            // Insert global statements before this global statement
            declaration = declaration.Parent;
            newDeclarations = newDeclarations.Select(declaration => declaration is StatementSyntax statement ? SyntaxFactory.GlobalStatement(statement) : declaration);
        }

        if (root.Span.Contains(declaration.Span))
        {
            return this.Isolate(root.TrackNodes(declaration), r => this.InsertNodesAfterInternal(r, r.GetCurrentNode(declaration)!, newDeclarations));
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
        var newNodes = new List<SyntaxNode>
        {
            this.WithSubDeclarationsRemoved(multiPartDeclaration, index, count - index).WithTrailingTrivia(SyntaxFactory.ElasticSpace)
        };
        newNodes.AddRange(newDeclarations);
        newNodes.Add(this.WithSubDeclarationsRemoved(multiPartDeclaration, 0, index).WithLeadingTrivia(SyntaxFactory.ElasticSpace));
        return newNodes;
    }

    private SyntaxNode WithSubDeclarationsRemoved(SyntaxNode declaration, int index, int count)
        => RemoveNodes(declaration, GetSubDeclarations(declaration).Skip(index).Take(count));

    private static IReadOnlyList<SyntaxNode> GetSubDeclarations(SyntaxNode declaration)
        => declaration.Kind() switch
        {
            SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax)declaration).Declaration.Variables,
            SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax)declaration).Declaration.Variables,
            SyntaxKind.LocalDeclarationStatement => ((LocalDeclarationStatementSyntax)declaration).Declaration.Variables,
            SyntaxKind.VariableDeclaration => ((VariableDeclarationSyntax)declaration).Variables,
            SyntaxKind.AttributeList => ((AttributeListSyntax)declaration).Attributes,
            _ => [],
        };

    public override SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node)
        => RemoveNode(root, node, DefaultRemoveOptions);

    public override SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node, SyntaxRemoveOptions options)
    {
        Contract.ThrowIfTrue(ReferenceEquals(root, node));

        if (node.Parent.IsKind(SyntaxKind.GlobalStatement))
        {
            // Remove the entire global statement as part of the edit
            node = node.Parent;
        }

        if (root.Span.Contains(node.Span))
        {
            // node exists within normal span of the root (not in trivia)
            return Isolate(root.TrackNodes(node), r => RemoveNodeInternal(r, r.GetCurrentNode(node)!, options));
        }
        else
        {
            return RemoveNodeInternal(root, node, options);
        }
    }

    private SyntaxNode RemoveNodeInternal(SyntaxNode root, SyntaxNode declaration, SyntaxRemoveOptions options)
    {
        Contract.ThrowIfTrue(ReferenceEquals(root, declaration));

        switch (declaration.Kind())
        {
            case SyntaxKind.Attribute:
                var attr = (AttributeSyntax)declaration;
                if (attr.Parent is AttributeListSyntax attrList && attrList.Attributes.Count == 1)
                {
                    // remove entire list if only one attribute
                    return RemoveNodeInternal(root, attrList, options);
                }

                break;

            case SyntaxKind.AttributeArgument:
                if (declaration.Parent != null && ((AttributeArgumentListSyntax)declaration.Parent).Arguments.Count == 1)
                {
                    // remove entire argument list if only one argument
                    return RemoveNodeInternal(root, declaration.Parent, options);
                }

                break;

            case SyntaxKind.VariableDeclarator:
                var full = GetFullDeclaration(declaration);
                if (full != declaration && GetDeclarationCount(full) == 1)
                {
                    // remove full declaration if only one declarator
                    return RemoveNodeInternal(root, full, options);
                }

                break;

            case SyntaxKind.SimpleBaseType:
                if (declaration.Parent is BaseListSyntax baseList && baseList.Types.Count == 1)
                {
                    // remove entire base list if this is the only base type.
                    return RemoveNodeInternal(root, baseList, options);
                }

                break;

            default:
                var parent = declaration.Parent;
                if (parent != null)
                {
                    switch (parent.Kind())
                    {
                        case SyntaxKind.SimpleBaseType:
                            return RemoveNodeInternal(root, parent, options);
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
                nodes: [node], computeReplacementNode: (o, r) => newNode,
                tokens: [previousToken], computeReplacementToken: (o, r) => newPreviousToken,
                trivia: null, computeReplacementTrivia: null);
        }

        return root;
    }

    internal override bool IsRegularOrDocComment(SyntaxTrivia trivia)
        => trivia.IsRegularOrDocComment();

    #endregion

    #region Statements and Expressions

    public override SyntaxNode AddEventHandler(SyntaxNode @event, SyntaxNode handler)
        => SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, (ExpressionSyntax)@event, (ExpressionSyntax)Parenthesize(handler));

    public override SyntaxNode RemoveEventHandler(SyntaxNode @event, SyntaxNode handler)
        => SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, (ExpressionSyntax)@event, (ExpressionSyntax)Parenthesize(handler));

    public override SyntaxNode AwaitExpression(SyntaxNode expression)
        => SyntaxFactory.AwaitExpression((ExpressionSyntax)expression);

    public override SyntaxNode NameOfExpression(SyntaxNode expression)
        => this.InvocationExpression(s_nameOfIdentifier, expression);

    public override SyntaxNode ReturnStatement(SyntaxNode? expression = null)
        => SyntaxFactory.ReturnStatement((ExpressionSyntax?)expression);

    public override SyntaxNode ThrowStatement(SyntaxNode? expression = null)
        => SyntaxFactory.ThrowStatement((ExpressionSyntax?)expression);

    public override SyntaxNode ThrowExpression(SyntaxNode expression)
        => SyntaxFactory.ThrowExpression((ExpressionSyntax)expression);

    public override SyntaxNode IfStatement(SyntaxNode condition, IEnumerable<SyntaxNode> trueStatements, IEnumerable<SyntaxNode>? falseStatements = null)
    {
        if (falseStatements == null)
        {
            return SyntaxFactory.IfStatement(
                (ExpressionSyntax)condition,
                CreateBlock(trueStatements));
        }
        else
        {
            var falseArray = falseStatements.ToList();

            // make else-if chain if false-statements contain only an if-statement
            return SyntaxFactory.IfStatement(
                (ExpressionSyntax)condition,
                CreateBlock(trueStatements),
                SyntaxFactory.ElseClause(
                    falseArray is [IfStatementSyntax ifStatement] ? ifStatement : CreateBlock(falseArray)));
        }
    }

    private static BlockSyntax CreateBlock(IEnumerable<SyntaxNode>? statements = null)
        => SyntaxFactory.Block(AsStatementList(statements)).WithAdditionalAnnotations(Simplifier.Annotation);

    private static SyntaxList<StatementSyntax> AsStatementList(IEnumerable<SyntaxNode>? nodes)
        => nodes == null ? default : [.. nodes.Select(AsStatement)];

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

    internal override SyntaxNode MemberAccessExpressionWorker(SyntaxNode? expression, SyntaxNode simpleName)
    {
        // can only be null in VB
        Contract.ThrowIfNull(expression);

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ParenthesizeLeft((ExpressionSyntax)expression),
            (SimpleNameSyntax)simpleName);
    }

    public override SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
        => SyntaxGeneratorInternal.ConditionalAccessExpression(expression, whenNotNull);

    public override SyntaxNode MemberBindingExpression(SyntaxNode name)
        => SyntaxGeneratorInternal.MemberBindingExpression(name);

    public override SyntaxNode ElementBindingExpression(IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.ElementBindingExpression(
            SyntaxFactory.BracketedArgumentList([.. arguments.Cast<ArgumentSyntax>()]));

    /// <summary>
    /// Parenthesize the left hand size of a member access, invocation or element access expression
    /// </summary>
    private static ExpressionSyntax ParenthesizeLeft(ExpressionSyntax expression)
    {
        if (expression is TypeSyntax ||
            expression.Kind()
                is SyntaxKind.ThisExpression
                or SyntaxKind.BaseExpression
                or SyntaxKind.ParenthesizedExpression
                or SyntaxKind.SimpleMemberAccessExpression
                or SyntaxKind.InvocationExpression
                or SyntaxKind.ElementAccessExpression
                or SyntaxKind.MemberBindingExpression)
        {
            return expression;
        }

        return (ExpressionSyntax)Parenthesize(expression);
    }

    private static SeparatedSyntaxList<ExpressionSyntax> AsExpressionList(IEnumerable<SyntaxNode> expressions)
        => [.. expressions.OfType<ExpressionSyntax>()];

    public override SyntaxNode ArrayCreationExpression(SyntaxNode elementType, SyntaxNode size)
    {
        var arrayType = SyntaxFactory.ArrayType((TypeSyntax)elementType, [SyntaxFactory.ArrayRankSpecifier([(ExpressionSyntax)size])]);
        return SyntaxFactory.ArrayCreationExpression(arrayType);
    }

    public override SyntaxNode ArrayCreationExpression(SyntaxNode elementType, IEnumerable<SyntaxNode> elements)
    {
        var arrayType = SyntaxFactory.ArrayType((TypeSyntax)elementType,
            [SyntaxFactory.ArrayRankSpecifier([SyntaxFactory.OmittedArraySizeExpression()])]);
        var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, AsExpressionList(elements));
        return SyntaxFactory.ArrayCreationExpression(arrayType, initializer);
    }

    public override SyntaxNode ObjectCreationExpression(SyntaxNode type, IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.ObjectCreationExpression((TypeSyntax)type, CreateArgumentList(arguments), null);

    internal override SyntaxNode ObjectCreationExpression(SyntaxNode type, SyntaxToken openParen, SeparatedSyntaxList<SyntaxNode> arguments, SyntaxToken closeParen)
        => SyntaxFactory.ObjectCreationExpression(
            (TypeSyntax)type,
            SyntaxFactory.ArgumentList(openParen, (SeparatedSyntaxList<ArgumentSyntax>)arguments, closeParen),
            initializer: null);

    private static ArgumentListSyntax CreateArgumentList(IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.ArgumentList(CreateArguments(arguments));

    private static SeparatedSyntaxList<ArgumentSyntax> CreateArguments(IEnumerable<SyntaxNode> arguments)
        => [.. arguments.Select(AsArgument)];

    private static ArgumentSyntax AsArgument(SyntaxNode argOrExpression)
        => argOrExpression as ArgumentSyntax ?? SyntaxFactory.Argument((ExpressionSyntax)argOrExpression);

    public override SyntaxNode InvocationExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.InvocationExpression(ParenthesizeLeft((ExpressionSyntax)expression), CreateArgumentList(arguments));

    public override SyntaxNode ElementAccessExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.ElementAccessExpression(ParenthesizeLeft((ExpressionSyntax)expression), SyntaxFactory.BracketedArgumentList(CreateArguments(arguments)));

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
            type is IPointerTypeSymbol ||
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

    private static SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        => CSharpSyntaxGeneratorInternal.Parenthesize(expression, includeElasticTrivia, addSimplifierAnnotation);

    public override SyntaxNode IsTypeExpression(SyntaxNode expression, SyntaxNode type)
        => SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, (ExpressionSyntax)Parenthesize(expression), (TypeSyntax)type);

    public override SyntaxNode TypeOfExpression(SyntaxNode type)
        => SyntaxFactory.TypeOfExpression((TypeSyntax)type);

    public override SyntaxNode TryCastExpression(SyntaxNode expression, SyntaxNode type)
        => SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, (ExpressionSyntax)Parenthesize(expression), (TypeSyntax)type);

    public override SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression)
        => SyntaxFactory.CastExpression((TypeSyntax)type, (ExpressionSyntax)Parenthesize(expression)).WithAdditionalAnnotations(Simplifier.Annotation);

    public override SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression)
        => SyntaxFactory.CastExpression((TypeSyntax)type, (ExpressionSyntax)Parenthesize(expression)).WithAdditionalAnnotations(Simplifier.Annotation);

    public override SyntaxNode AssignmentStatement(SyntaxNode left, SyntaxNode right)
        => SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)left, (ExpressionSyntax)Parenthesize(right));

    private static SyntaxNode CreateBinaryExpression(SyntaxKind syntaxKind, SyntaxNode left, SyntaxNode right)
        => SyntaxFactory.BinaryExpression(syntaxKind, (ExpressionSyntax)Parenthesize(left), (ExpressionSyntax)Parenthesize(right));

    public override SyntaxNode ValueEqualsExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);

    public override SyntaxNode ReferenceEqualsExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);

    public override SyntaxNode ValueNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);

    public override SyntaxNode ReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);

    public override SyntaxNode LessThanExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.LessThanExpression, left, right);

    public override SyntaxNode LessThanOrEqualExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.LessThanOrEqualExpression, left, right);

    public override SyntaxNode GreaterThanExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.GreaterThanExpression, left, right);

    public override SyntaxNode GreaterThanOrEqualExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, left, right);

    public override SyntaxNode NegateExpression(SyntaxNode expression)
        => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, (ExpressionSyntax)Parenthesize(expression));

    public override SyntaxNode AddExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.AddExpression, left, right);

    public override SyntaxNode SubtractExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.SubtractExpression, left, right);

    public override SyntaxNode MultiplyExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.MultiplyExpression, left, right);

    public override SyntaxNode DivideExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.DivideExpression, left, right);

    public override SyntaxNode ModuloExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.ModuloExpression, left, right);

    public override SyntaxNode BitwiseAndExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.BitwiseAndExpression, left, right);

    public override SyntaxNode BitwiseOrExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);

    public override SyntaxNode BitwiseNotExpression(SyntaxNode operand)
        => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.BitwiseNotExpression, (ExpressionSyntax)Parenthesize(operand));

    public override SyntaxNode LogicalAndExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.LogicalAndExpression, left, right);

    public override SyntaxNode LogicalOrExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.LogicalOrExpression, left, right);

    public override SyntaxNode LogicalNotExpression(SyntaxNode expression)
        => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, (ExpressionSyntax)Parenthesize(expression));

    public override SyntaxNode ConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse)
        => SyntaxFactory.ConditionalExpression((ExpressionSyntax)Parenthesize(condition), (ExpressionSyntax)Parenthesize(whenTrue), (ExpressionSyntax)Parenthesize(whenFalse));

    public override SyntaxNode CoalesceExpression(SyntaxNode left, SyntaxNode right)
        => CreateBinaryExpression(SyntaxKind.CoalesceExpression, left, right);

    public override SyntaxNode ThisExpression()
        => SyntaxFactory.ThisExpression();

    public override SyntaxNode BaseExpression()
        => SyntaxFactory.BaseExpression();

    public override SyntaxNode TypedConstantExpression(TypedConstant value)
        => ExpressionGenerator.GenerateExpression(this, value);

    private protected override SyntaxNode GenerateExpression(ITypeSymbol? type, object? value, bool canUseFieldReference)
        => ExpressionGenerator.GenerateExpression(this, type, value, canUseFieldReference);

    public override SyntaxNode IdentifierName(string identifier)
        => identifier.ToIdentifierName();

    public override SyntaxNode GenericName(string identifier, IEnumerable<SyntaxNode> typeArguments)
        => GenericName(identifier.ToIdentifierToken(), typeArguments);

    internal override SyntaxNode GenericName(SyntaxToken identifier, IEnumerable<SyntaxNode> typeArguments)
        => SyntaxFactory.GenericName(identifier,
            SyntaxFactory.TypeArgumentList([.. typeArguments.Cast<TypeSyntax>()]));

    public override SyntaxNode WithTypeArguments(SyntaxNode expression, IEnumerable<SyntaxNode> typeArguments)
    {
        switch (expression.Kind())
        {
            case SyntaxKind.IdentifierName:
                var sname = (SimpleNameSyntax)expression;
                return SyntaxFactory.GenericName(sname.Identifier, SyntaxFactory.TypeArgumentList([.. typeArguments.Cast<TypeSyntax>()]));

            case SyntaxKind.GenericName:
                var gname = (GenericNameSyntax)expression;
                return gname.WithTypeArgumentList(SyntaxFactory.TypeArgumentList([.. typeArguments.Cast<TypeSyntax>()]));

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
            SyntaxFactory.IdentifierName(GlobalKeyword),
            (SimpleNameSyntax)name);

    public override SyntaxNode NameExpression(INamespaceOrTypeSymbol namespaceOrTypeSymbol)
        => namespaceOrTypeSymbol.GenerateNameSyntax();

    private protected override SyntaxNode TypeExpression(ITypeSymbol typeSymbol, RefKind refKind)
    {
        var type = typeSymbol.GenerateTypeSyntax();
        return refKind switch
        {
            RefKind.Ref => SyntaxFactory.RefType(type),
            RefKind.RefReadOnly => SyntaxFactory.RefType(RefKeyword, ReadOnlyKeyword, type),
            _ => type,
        };
    }

    public override SyntaxNode TypeExpression(SpecialType specialType)
        => SyntaxFactory.PredefinedType(specialType switch
        {
            SpecialType.System_Boolean => BoolKeyword,
            SpecialType.System_Byte => ByteKeyword,
            SpecialType.System_Char => CharKeyword,
            SpecialType.System_Decimal => DecimalKeyword,
            SpecialType.System_Double => DoubleKeyword,
            SpecialType.System_Int16 => ShortKeyword,
            SpecialType.System_Int32 => IntKeyword,
            SpecialType.System_Int64 => LongKeyword,
            SpecialType.System_Object => ObjectKeyword,
            SpecialType.System_SByte => SByteKeyword,
            SpecialType.System_Single => FloatKeyword,
            SpecialType.System_String => StringKeyword,
            SpecialType.System_UInt16 => UShortKeyword,
            SpecialType.System_UInt32 => UIntKeyword,
            SpecialType.System_UInt64 => ULongKeyword,
            _ => throw new NotSupportedException("Unsupported SpecialType"),
        });

    public override SyntaxNode ArrayTypeExpression(SyntaxNode type)
        => SyntaxFactory.ArrayType((TypeSyntax)type, [SyntaxFactory.ArrayRankSpecifier()]);

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
        => SyntaxFactory.TupleType([.. elements.Cast<TupleElementSyntax>()]);

    public override SyntaxNode TupleElementExpression(SyntaxNode type, string? name = null)
        => SyntaxFactory.TupleElement((TypeSyntax)type, name?.ToIdentifierToken() ?? default);

    public override SyntaxNode Argument(string? name, RefKind refKind, SyntaxNode expression)
    {
        return SyntaxFactory.Argument(
            name == null ? null : SyntaxFactory.NameColon(name),
            GetArgumentModifiers(refKind),
            (ExpressionSyntax)expression);
    }

    public override SyntaxNode LocalDeclarationStatement(SyntaxNode? type, string name, SyntaxNode? initializer, bool isConst)
        => CSharpSyntaxGeneratorInternal.Instance.LocalDeclarationStatement(type, name.ToIdentifierToken(), initializer, isConst);

    public override SyntaxNode UsingStatement(SyntaxNode? type, string name, SyntaxNode expression, IEnumerable<SyntaxNode> statements)
    {
        return SyntaxFactory.UsingStatement(
            CSharpSyntaxGeneratorInternal.VariableDeclaration(type, name.ToIdentifierToken(), expression),
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

    public override SyntaxNode TryCatchStatement(IEnumerable<SyntaxNode>? tryStatements, IEnumerable<SyntaxNode>? catchClauses, IEnumerable<SyntaxNode>? finallyStatements = null)
    {
        return SyntaxFactory.TryStatement(
            CreateBlock(tryStatements),
            catchClauses != null ? [.. catchClauses.Cast<CatchClauseSyntax>()] : default,
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
        => SyntaxFactory.WhileStatement((ExpressionSyntax)condition, CreateBlock(statements));

    public override SyntaxNode SwitchStatement(SyntaxNode expression, IEnumerable<SyntaxNode> caseClauses)
    {
        if (expression is TupleExpressionSyntax)
        {
            return SyntaxFactory.SwitchStatement(
                (ExpressionSyntax)expression,
                [.. caseClauses.Cast<SwitchSectionSyntax>()]);
        }
        else
        {
            return SyntaxFactory.SwitchStatement(
                SwitchKeyword,
                OpenParenToken,
                (ExpressionSyntax)expression,
                CloseParenToken,
                OpenBraceToken,
                [.. caseClauses.Cast<SwitchSectionSyntax>()],
                CloseBraceToken);
        }
    }

    public override SyntaxNode SwitchSection(IEnumerable<SyntaxNode> expressions, IEnumerable<SyntaxNode> statements)
        => SyntaxFactory.SwitchSection(AsSwitchLabels(expressions), AsStatementList(statements));

    internal override SyntaxNode SwitchSectionFromLabels(IEnumerable<SyntaxNode> labels, IEnumerable<SyntaxNode> statements)
    {
        return SyntaxFactory.SwitchSection(
            [.. labels.Cast<SwitchLabelSyntax>()],
            AsStatementList(statements));
    }

    public override SyntaxNode DefaultSwitchSection(IEnumerable<SyntaxNode> statements)
        => SyntaxFactory.SwitchSection([SyntaxFactory.DefaultSwitchLabel()], AsStatementList(statements));

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
        => SyntaxFactory.BreakStatement();

    internal override SyntaxNode ScopeBlock(IEnumerable<SyntaxNode> statements)
        => SyntaxFactory.Block(statements.Cast<StatementSyntax>());

    internal override SyntaxNode GlobalStatement(SyntaxNode statement)
        => SyntaxFactory.GlobalStatement((StatementSyntax)statement);

    public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode>? parameterDeclarations, SyntaxNode expression)
    {
        var parameters = parameterDeclarations?.Cast<ParameterSyntax>().ToList();

        return parameters is [var parameter] && IsSimpleLambdaParameter(parameter)
            ? SyntaxFactory.SimpleLambdaExpression(parameter, (CSharpSyntaxNode)expression)
            : SyntaxFactory.ParenthesizedLambdaExpression(AsParameterList(parameters), (CSharpSyntaxNode)expression);
    }

    private static bool IsSimpleLambdaParameter(SyntaxNode node)
        => node is ParameterSyntax { Type: null, Default: null, Modifiers.Count: 0 };

    public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode>? lambdaParameters, SyntaxNode expression)
        => this.ValueReturningLambdaExpression(lambdaParameters, expression);

    public override SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode>? parameterDeclarations, IEnumerable<SyntaxNode> statements)
        => this.ValueReturningLambdaExpression(parameterDeclarations, CreateBlock(statements));

    public override SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode>? lambdaParameters, IEnumerable<SyntaxNode> statements)
        => this.ValueReturningLambdaExpression(lambdaParameters, statements);

    public override SyntaxNode LambdaParameter(string identifier, SyntaxNode? type = null)
        => this.ParameterDeclaration(identifier, type, initializer: null, RefKind.None);

    internal override SyntaxNode IdentifierName(SyntaxToken identifier)
        => SyntaxFactory.IdentifierName(identifier);

    internal override SyntaxNode NamedAnonymousObjectMemberDeclarator(SyntaxNode identifier, SyntaxNode expression)
    {
        return SyntaxFactory.AnonymousObjectMemberDeclarator(
            SyntaxFactory.NameEquals((IdentifierNameSyntax)identifier),
            (ExpressionSyntax)expression);
    }

    public override SyntaxNode TupleExpression(IEnumerable<SyntaxNode> arguments)
        => SyntaxFactory.TupleExpression([.. arguments.Select(AsArgument)]);

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

    internal override SyntaxNode ParseExpression(string stringToParse)
        => SyntaxFactory.ParseExpression(stringToParse);

    #endregion
}
