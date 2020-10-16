﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.GenerateType
{
    [ExportLanguageService(typeof(IGenerateTypeService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateTypeService :
        AbstractGenerateTypeService<CSharpGenerateTypeService, SimpleNameSyntax, ObjectCreationExpressionSyntax, ExpressionSyntax, TypeDeclarationSyntax, ArgumentSyntax>
    {
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateTypeService()
        {
        }

        protected override string DefaultFileExtension => ".cs";

        protected override ExpressionSyntax GetLeftSideOfDot(SimpleNameSyntax simpleName)
            => simpleName.GetLeftSideOfDot();

        protected override bool IsInCatchDeclaration(ExpressionSyntax expression)
            => expression.IsParentKind(SyntaxKind.CatchDeclaration);

        protected override bool IsArrayElementType(ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.ArrayType) &&
                expression.Parent.IsParentKind(SyntaxKind.ArrayCreationExpression);
        }

        protected override bool IsInValueTypeConstraintContext(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            if (expression is TypeSyntax && expression.IsParentKind(SyntaxKind.TypeArgumentList, out TypeArgumentListSyntax typeArgumentList))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(typeArgumentList.Parent, cancellationToken);
                var symbol = symbolInfo.GetAnySymbol();
                if (symbol.IsConstructor())
                {
                    symbol = symbol.ContainingType;
                }

                var parameterIndex = typeArgumentList.Arguments.IndexOf((TypeSyntax)expression);
                if (symbol is INamedTypeSymbol type)
                {
                    type = type.OriginalDefinition;
                    var typeParameter = parameterIndex < type.TypeParameters.Length ? type.TypeParameters[parameterIndex] : null;
                    return typeParameter != null && typeParameter.HasValueTypeConstraint;
                }

                if (symbol is IMethodSymbol method)
                {
                    method = method.OriginalDefinition;
                    var typeParameter = parameterIndex < method.TypeParameters.Length ? method.TypeParameters[parameterIndex] : null;
                    return typeParameter != null && typeParameter.HasValueTypeConstraint;
                }
            }

            return false;
        }

        protected override bool IsInInterfaceList(ExpressionSyntax expression)
        {
            if (expression is TypeSyntax &&
                expression.Parent is BaseTypeSyntax baseType &&
                baseType.IsParentKind(SyntaxKind.BaseList, out BaseListSyntax baseList) &&
                baseType.Type == expression)
            {
                // If it's after the first item, then it's definitely an interface.
                if (baseList.Types[0] != expression.Parent)
                {
                    return true;
                }

                // If it's in the base list of an interface or struct, then it's definitely an
                // interface.
                return
                    baseList.IsParentKind(SyntaxKind.InterfaceDeclaration) ||
                    baseList.IsParentKind(SyntaxKind.StructDeclaration);
            }

            if (expression is TypeSyntax &&
                expression.IsParentKind(SyntaxKind.TypeConstraint, out TypeConstraintSyntax typeConstraint) &&
                typeConstraint.IsParentKind(SyntaxKind.TypeParameterConstraintClause, out TypeParameterConstraintClauseSyntax constraintClause))
            {
                var index = constraintClause.Constraints.IndexOf(typeConstraint);

                // If it's after the first item, then it's definitely an interface.
                return index > 0;
            }

            return false;
        }

        protected override bool TryGetNameParts(ExpressionSyntax expression, out IList<string> nameParts)
        {
            return expression.TryGetNameParts(out nameParts);
        }

        protected override bool TryInitializeState(
            SemanticDocument document,
            SimpleNameSyntax simpleName,
            CancellationToken cancellationToken,
            out GenerateTypeServiceStateOptions generateTypeServiceStateOptions)
        {
            generateTypeServiceStateOptions = new GenerateTypeServiceStateOptions();

            if (simpleName.IsVar)
            {
                return false;
            }

            if (SyntaxFacts.IsAliasQualifier(simpleName))
            {
                return false;
            }

            // Never offer if we're in a using directive, unless its a static using.  The feeling here is that it's highly
            // unlikely that this would be a location where a user would be wanting to generate
            // something.  They're really just trying to reference something that exists but
            // isn't available for some reason (i.e. a missing reference).
            var usingDirectiveSyntax = simpleName.GetAncestorOrThis<UsingDirectiveSyntax>();
            if (usingDirectiveSyntax != null && usingDirectiveSyntax.StaticKeyword.Kind() != SyntaxKind.StaticKeyword)
            {
                return false;
            }

            ExpressionSyntax nameOrMemberAccessExpression = null;
            if (simpleName.IsRightSideOfDot())
            {
                // This simplename comes from the cref
                if (simpleName.IsParentKind(SyntaxKind.NameMemberCref))
                {
                    return false;
                }

                nameOrMemberAccessExpression = generateTypeServiceStateOptions.NameOrMemberAccessExpression = (ExpressionSyntax)simpleName.Parent;

                // If we're on the right side of a dot, then the left side better be a name (and
                // not an arbitrary expression).
                var leftSideExpression = simpleName.GetLeftSideOfDot();
                if (!leftSideExpression.IsKind(
                    SyntaxKind.QualifiedName,
                    SyntaxKind.IdentifierName,
                    SyntaxKind.AliasQualifiedName,
                    SyntaxKind.GenericName,
                    SyntaxKind.SimpleMemberAccessExpression))
                {
                    return false;
                }
            }
            else
            {
                nameOrMemberAccessExpression = generateTypeServiceStateOptions.NameOrMemberAccessExpression = simpleName;
            }

            // BUG(5712): Don't offer generate type in an enum's base list.
            if (nameOrMemberAccessExpression.Parent is BaseTypeSyntax &&
                nameOrMemberAccessExpression.Parent.IsParentKind(SyntaxKind.BaseList) &&
                ((BaseTypeSyntax)nameOrMemberAccessExpression.Parent).Type == nameOrMemberAccessExpression &&
                nameOrMemberAccessExpression.Parent.Parent.IsParentKind(SyntaxKind.EnumDeclaration))
            {
                return false;
            }

            // If we can guarantee it's a type only context, great.  Otherwise, we may not want to
            // provide this here.
            var semanticModel = document.SemanticModel;
            if (!SyntaxFacts.IsInNamespaceOrTypeContext(nameOrMemberAccessExpression))
            {
                // Don't offer Generate Type in an expression context *unless* we're on the left
                // side of a dot.  In that case the user might be making a type that they're
                // accessing a static off of.
                var syntaxTree = semanticModel.SyntaxTree;
                var start = nameOrMemberAccessExpression.SpanStart;
                var tokenOnLeftOfStart = syntaxTree.FindTokenOnLeftOfPosition(start, cancellationToken);
                var isExpressionContext = syntaxTree.IsExpressionContext(start, tokenOnLeftOfStart, attributes: true, cancellationToken: cancellationToken, semanticModelOpt: semanticModel);
                var isStatementContext = syntaxTree.IsStatementContext(start, tokenOnLeftOfStart, cancellationToken);
                var isExpressionOrStatementContext = isExpressionContext || isStatementContext;

                // Delegate Type Creation is not allowed in Non Type Namespace Context
                generateTypeServiceStateOptions.IsDelegateAllowed = false;

                if (!isExpressionOrStatementContext)
                {
                    return false;
                }

                if (!simpleName.IsLeftSideOfDot() &&
                    !simpleName.IsInsideNameOfExpression(semanticModel, cancellationToken))
                {
                    if (nameOrMemberAccessExpression == null || !nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) || !simpleName.IsRightSideOfDot())
                    {
                        return false;
                    }

                    var leftSymbol = semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)nameOrMemberAccessExpression).Expression, cancellationToken).Symbol;
                    var token = simpleName.GetLastToken().GetNextToken();

                    // We let only the Namespace to be left of the Dot
                    if (leftSymbol == null ||
                        !leftSymbol.IsKind(SymbolKind.Namespace) ||
                        !token.IsKind(SyntaxKind.DotToken))
                    {
                        return false;
                    }
                    else
                    {
                        generateTypeServiceStateOptions.IsMembersWithModule = true;
                        generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess = true;
                    }
                }

                // Global Namespace 
                if (!generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess &&
                    !SyntaxFacts.IsInNamespaceOrTypeContext(simpleName))
                {
                    var token = simpleName.GetLastToken().GetNextToken();
                    if (token.IsKind(SyntaxKind.DotToken) &&
                            simpleName.Parent == token.Parent)
                    {
                        generateTypeServiceStateOptions.IsMembersWithModule = true;
                        generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess = true;
                    }
                }
            }

            var fieldDeclaration = simpleName.GetAncestor<FieldDeclarationSyntax>();
            if (fieldDeclaration != null &&
                fieldDeclaration.Parent is CompilationUnitSyntax &&
                document.Document.SourceCodeKind == SourceCodeKind.Regular)
            {
                return false;
            }

            // Check to see if Module could be an option in the Type Generation in Cross Language Generation
            var nextToken = simpleName.GetLastToken().GetNextToken();
            if (simpleName.IsLeftSideOfDot() ||
                nextToken.IsKind(SyntaxKind.DotToken))
            {
                if (simpleName.IsRightSideOfDot())
                {
                    if (simpleName.Parent is QualifiedNameSyntax parent)
                    {
                        var leftSymbol = semanticModel.GetSymbolInfo(parent.Left, cancellationToken).Symbol;

                        if (leftSymbol != null && leftSymbol.IsKind(SymbolKind.Namespace))
                        {
                            generateTypeServiceStateOptions.IsMembersWithModule = true;
                        }
                    }
                }
            }

            if (SyntaxFacts.IsInNamespaceOrTypeContext(nameOrMemberAccessExpression))
            {
                if (nextToken.IsKind(SyntaxKind.DotToken))
                {
                    // In Namespace or Type Context we cannot have Interface, Enum, Delegate as part of the Left Expression of a QualifiedName
                    generateTypeServiceStateOptions.IsDelegateAllowed = false;
                    generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext = true;
                    generateTypeServiceStateOptions.IsMembersWithModule = true;
                }

                // case: class Goo<T> where T: MyType
                if (nameOrMemberAccessExpression.GetAncestors<TypeConstraintSyntax>().Any())
                {
                    generateTypeServiceStateOptions.IsClassInterfaceTypes = true;
                    return true;
                }

                // Events
                if (nameOrMemberAccessExpression.GetAncestors<EventFieldDeclarationSyntax>().Any() ||
                    nameOrMemberAccessExpression.GetAncestors<EventDeclarationSyntax>().Any())
                {
                    // Case : event goo name11
                    // Only Delegate
                    if (simpleName.Parent != null && !(simpleName.Parent is QualifiedNameSyntax))
                    {
                        generateTypeServiceStateOptions.IsDelegateOnly = true;
                        return true;
                    }

                    // Case : event SomeSymbol.goo name11
                    if (nameOrMemberAccessExpression is QualifiedNameSyntax)
                    {
                        // Only Namespace, Class, Struct and Module are allowed to contain Delegate
                        // Case : event Something.Mytype.<Delegate> Identifier
                        if (nextToken.IsKind(SyntaxKind.DotToken))
                        {
                            if (nameOrMemberAccessExpression.Parent != null && nameOrMemberAccessExpression.Parent is QualifiedNameSyntax)
                            {
                                return true;
                            }

                            throw ExceptionUtilities.Unreachable;
                        }
                        else
                        {
                            // Case : event Something.<Delegate> Identifier
                            generateTypeServiceStateOptions.IsDelegateOnly = true;
                            return true;
                        }
                    }
                }
            }
            else
            {
                // MemberAccessExpression
                if ((nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) || (nameOrMemberAccessExpression.Parent != null && nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression)))
                    && nameOrMemberAccessExpression.IsLeftSideOfDot())
                {
                    // Check to see if the expression is part of Invocation Expression
                    ExpressionSyntax outerMostMemberAccessExpression = null;
                    if (nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        outerMostMemberAccessExpression = nameOrMemberAccessExpression;
                    }
                    else
                    {
                        Debug.Assert(nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression));
                        outerMostMemberAccessExpression = (ExpressionSyntax)nameOrMemberAccessExpression.Parent;
                    }

                    outerMostMemberAccessExpression = outerMostMemberAccessExpression.GetAncestorsOrThis<ExpressionSyntax>().SkipWhile(n => n != null && n.IsKind(SyntaxKind.SimpleMemberAccessExpression)).FirstOrDefault();
                    if (outerMostMemberAccessExpression != null && outerMostMemberAccessExpression is InvocationExpressionSyntax)
                    {
                        generateTypeServiceStateOptions.IsEnumNotAllowed = true;
                    }
                }
            }

            // Cases:
            // // 1 - Function Address
            // var s2 = new MyD2(goo);

            // // 2 - Delegate
            // MyD1 d = null;
            // var s1 = new MyD2(d);

            // // 3 - Action
            // Action action1 = null;
            // var s3 = new MyD2(action1);

            // // 4 - Func
            // Func<int> lambda = () => { return 0; };
            // var s4 = new MyD3(lambda);

            if (nameOrMemberAccessExpression.Parent is ObjectCreationExpressionSyntax)
            {
                var objectCreationExpressionOpt = generateTypeServiceStateOptions.ObjectCreationExpressionOpt = (ObjectCreationExpressionSyntax)nameOrMemberAccessExpression.Parent;

                // Enum and Interface not Allowed in Object Creation Expression
                generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext = true;

                if (objectCreationExpressionOpt.ArgumentList != null)
                {
                    if (objectCreationExpressionOpt.ArgumentList.CloseParenToken.IsMissing)
                    {
                        return false;
                    }

                    // Get the Method symbol for the Delegate to be created
                    if (generateTypeServiceStateOptions.IsDelegateAllowed &&
                        objectCreationExpressionOpt.ArgumentList.Arguments.Count == 1 &&
                        objectCreationExpressionOpt.ArgumentList.Arguments[0].Expression.Kind() != SyntaxKind.DeclarationExpression)
                    {
                        generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMethodSymbolIfPresent(semanticModel, objectCreationExpressionOpt.ArgumentList.Arguments[0].Expression, cancellationToken);
                    }
                    else
                    {
                        generateTypeServiceStateOptions.IsDelegateAllowed = false;
                    }
                }

                if (objectCreationExpressionOpt.Initializer != null)
                {
                    foreach (var expression in objectCreationExpressionOpt.Initializer.Expressions)
                    {
                        if (!(expression is AssignmentExpressionSyntax simpleAssignmentExpression))
                        {
                            continue;
                        }

                        if (!(simpleAssignmentExpression.Left is SimpleNameSyntax name))
                        {
                            continue;
                        }

                        generateTypeServiceStateOptions.PropertiesToGenerate.Add(name);
                    }
                }
            }

            if (generateTypeServiceStateOptions.IsDelegateAllowed)
            {
                // MyD1 z1 = goo;
                if (nameOrMemberAccessExpression.Parent.IsKind(SyntaxKind.VariableDeclaration, out VariableDeclarationSyntax variableDeclaration) &&
                    variableDeclaration.Variables.Count != 0)
                {
                    var firstVarDeclWithInitializer = variableDeclaration.Variables.FirstOrDefault(var => var.Initializer != null && var.Initializer.Value != null);
                    if (firstVarDeclWithInitializer != null && firstVarDeclWithInitializer.Initializer != null && firstVarDeclWithInitializer.Initializer.Value != null)
                    {
                        generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMethodSymbolIfPresent(semanticModel, firstVarDeclWithInitializer.Initializer.Value, cancellationToken);
                    }
                }

                // var w1 = (MyD1)goo;
                if (nameOrMemberAccessExpression.Parent.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression) &&
                    castExpression.Expression != null)
                {
                    generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMethodSymbolIfPresent(semanticModel, castExpression.Expression, cancellationToken);
                }
            }

            return true;
        }

        private static IMethodSymbol GetMethodSymbolIfPresent(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            if (expression == null)
            {
                return null;
            }

            var memberGroup = semanticModel.GetMemberGroup(expression, cancellationToken);
            if (memberGroup.Length != 0)
            {
                return memberGroup.ElementAt(0).IsKind(SymbolKind.Method) ? (IMethodSymbol)memberGroup.ElementAt(0) : null;
            }

            var expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (expressionType.IsDelegateType())
            {
                return ((INamedTypeSymbol)expressionType).DelegateInvokeMethod;
            }

            var expressionSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (expressionSymbol.IsKind(SymbolKind.Method))
            {
                return (IMethodSymbol)expressionSymbol;
            }

            return null;
        }

        private static Accessibility DetermineAccessibilityConstraint(
            State state,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return semanticModel.DetermineAccessibilityConstraint(
                state.NameOrMemberAccessExpression as TypeSyntax, cancellationToken);
        }

        private static bool AllContainingTypesArePublicOrProtected(
            State state,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return semanticModel.AllContainingTypesArePublicOrProtected(
                state.NameOrMemberAccessExpression as TypeSyntax, cancellationToken);
        }

        protected override ImmutableArray<ITypeParameterSymbol> GetTypeParameters(
            State state,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (state.SimpleName is GenericNameSyntax)
            {
                var genericName = (GenericNameSyntax)state.SimpleName;
                var typeArguments = state.SimpleName.Arity == genericName.TypeArgumentList.Arguments.Count
                    ? genericName.TypeArgumentList.Arguments.OfType<SyntaxNode>().ToList()
                    : Enumerable.Repeat<SyntaxNode>(null, state.SimpleName.Arity);
                return GetTypeParameters(state, semanticModel, typeArguments, cancellationToken);
            }

            return ImmutableArray<ITypeParameterSymbol>.Empty;
        }

        protected override bool TryGetArgumentList(ObjectCreationExpressionSyntax objectCreationExpression, out IList<ArgumentSyntax> argumentList)
        {
            if (objectCreationExpression != null && objectCreationExpression.ArgumentList != null)
            {
                argumentList = objectCreationExpression.ArgumentList.Arguments.ToList();
                return true;
            }

            argumentList = null;
            return false;
        }

        protected override IList<ParameterName> GenerateParameterNames(
            SemanticModel semanticModel, IList<ArgumentSyntax> arguments, CancellationToken cancellationToken)
        {
            return semanticModel.GenerateParameterNames(arguments, reservedNames: null, cancellationToken: cancellationToken);
        }

        public override string GetRootNamespace(CompilationOptions options)
            => string.Empty;

        protected override bool IsInVariableTypeContext(ExpressionSyntax expression)
            => false;

        protected override INamedTypeSymbol DetermineTypeToGenerateIn(SemanticModel semanticModel, SimpleNameSyntax simpleName, CancellationToken cancellationToken)
            => semanticModel.GetEnclosingNamedType(simpleName.SpanStart, cancellationToken);

        protected override Accessibility GetAccessibility(State state, SemanticModel semanticModel, bool intoNamespace, CancellationToken cancellationToken)
        {
            var accessibility = DetermineDefaultAccessibility(state, semanticModel, intoNamespace, cancellationToken);
            if (!state.IsTypeGeneratedIntoNamespaceFromMemberAccess)
            {
                var accessibilityConstraint = DetermineAccessibilityConstraint(state, semanticModel, cancellationToken);

                if (accessibilityConstraint == Accessibility.Public ||
                    accessibilityConstraint == Accessibility.Internal)
                {
                    accessibility = accessibilityConstraint;
                }
                else if (accessibilityConstraint == Accessibility.Protected ||
                         accessibilityConstraint == Accessibility.ProtectedOrInternal)
                {
                    // If nested type is declared in public type then we should generate public type instead of internal
                    accessibility = AllContainingTypesArePublicOrProtected(state, semanticModel, cancellationToken)
                        ? Accessibility.Public
                        : Accessibility.Internal;
                }
            }

            return accessibility;
        }

        protected override ITypeSymbol DetermineArgumentType(SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
            => argument.DetermineParameterType(semanticModel, cancellationToken);

        protected override bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
            => compilation.ClassifyConversion(sourceType, targetType).IsImplicit;

        public override async Task<(INamespaceSymbol, INamespaceOrTypeSymbol, Location)> GetOrGenerateEnclosingNamespaceSymbolAsync(
            INamedTypeSymbol namedTypeSymbol, string[] containers, Document selectedDocument, SyntaxNode selectedDocumentRoot, CancellationToken cancellationToken)
        {
            var compilationUnit = (CompilationUnitSyntax)selectedDocumentRoot;
            var semanticModel = await selectedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (containers.Length != 0)
            {
                // Search the NS declaration in the root
                var containerList = new List<string>(containers);
                var enclosingNamespace = FindNamespaceInMemberDeclarations(compilationUnit.Members, indexDone: 0, containerList);
                if (enclosingNamespace != null)
                {
                    var enclosingNamespaceSymbol = semanticModel.GetSymbolInfo(enclosingNamespace.Name, cancellationToken);
                    if (enclosingNamespaceSymbol.Symbol != null)
                    {
                        return ((INamespaceSymbol)enclosingNamespaceSymbol.Symbol,
                                namedTypeSymbol,
                                enclosingNamespace.CloseBraceToken.GetLocation());
                    }
                }
            }

            var globalNamespace = semanticModel.GetEnclosingNamespace(0, cancellationToken);
            var rootNamespaceOrType = namedTypeSymbol.GenerateRootNamespaceOrType(containers);
            var lastMember = compilationUnit.Members.LastOrDefault();
            var afterThisLocation = lastMember != null
                ? semanticModel.SyntaxTree.GetLocation(new TextSpan(lastMember.Span.End, 0))
                : semanticModel.SyntaxTree.GetLocation(new TextSpan());

            return (globalNamespace, rootNamespaceOrType, afterThisLocation);
        }

        private NamespaceDeclarationSyntax FindNamespaceInMemberDeclarations(SyntaxList<MemberDeclarationSyntax> members, int indexDone, List<string> containers)
        {
            foreach (var member in members)
            {
                if (member is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    var found = FindNamespaceInNamespace(namespaceDeclaration, indexDone, containers);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        private NamespaceDeclarationSyntax FindNamespaceInNamespace(NamespaceDeclarationSyntax namespaceDecl, int indexDone, List<string> containers)
        {
            if (namespaceDecl.Name is AliasQualifiedNameSyntax)
                return null;

            var namespaceContainers = new List<string>();
            GetNamespaceContainers(namespaceDecl.Name, namespaceContainers);

            if (namespaceContainers.Count + indexDone > containers.Count ||
                !IdentifierMatches(indexDone, namespaceContainers, containers))
            {
                return null;
            }

            indexDone += namespaceContainers.Count;
            if (indexDone == containers.Count)
                return namespaceDecl;

            return FindNamespaceInMemberDeclarations(namespaceDecl.Members, indexDone, containers);
        }

        private static bool IdentifierMatches(int indexDone, List<string> namespaceContainers, List<string> containers)
        {
            for (var i = 0; i < namespaceContainers.Count; ++i)
            {
                if (namespaceContainers[i] != containers[indexDone + i])
                {
                    return false;
                }
            }

            return true;
        }

        private void GetNamespaceContainers(NameSyntax name, List<string> namespaceContainers)
        {
            if (name is QualifiedNameSyntax qualifiedName)
            {
                GetNamespaceContainers(qualifiedName.Left, namespaceContainers);
                namespaceContainers.Add(qualifiedName.Right.Identifier.ValueText);
            }
            else
            {
                Debug.Assert(name is SimpleNameSyntax);
                namespaceContainers.Add(((SimpleNameSyntax)name).Identifier.ValueText);
            }
        }

        internal override bool TryGetBaseList(ExpressionSyntax expression, out TypeKindOptions typeKindValue)
        {
            typeKindValue = TypeKindOptions.AllOptions;

            if (expression == null)
            {
                return false;
            }

            var node = expression as SyntaxNode;

            while (node != null)
            {
                if (node is BaseListSyntax)
                {
                    if (node.Parent != null && (node.Parent is InterfaceDeclarationSyntax || node.Parent is StructDeclarationSyntax))
                    {
                        typeKindValue = TypeKindOptions.Interface;
                        return true;
                    }

                    typeKindValue = TypeKindOptions.BaseList;
                    return true;
                }

                node = node.Parent;
            }

            return false;
        }

        internal override bool IsPublicOnlyAccessibility(ExpressionSyntax expression, Project project)
        {
            if (expression == null)
            {
                return false;
            }

            if (GeneratedTypesMustBePublic(project))
            {
                return true;
            }

            var node = expression as SyntaxNode;
            SyntaxNode previousNode = null;

            while (node != null)
            {
                // Types in BaseList, Type Constraint or Member Types cannot be of more restricted accessibility than the declaring type
                if ((node is BaseListSyntax || node is TypeParameterConstraintClauseSyntax) &&
                    node.Parent != null &&
                    node.Parent is TypeDeclarationSyntax)
                {
                    if (node.Parent is TypeDeclarationSyntax typeDecl)
                    {
                        if (typeDecl.GetModifiers().Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                        {
                            return IsAllContainingTypeDeclsPublic(typeDecl);
                        }
                        else
                        {
                            // The Type Decl which contains the BaseList does not contain Public
                            return false;
                        }
                    }

                    throw ExceptionUtilities.Unreachable;
                }

                if ((node is EventDeclarationSyntax || node is EventFieldDeclarationSyntax) &&
                    node.Parent != null &&
                    node.Parent is TypeDeclarationSyntax)
                {
                    // Make sure the GFU is not inside the Accessors
                    if (previousNode != null && previousNode is AccessorListSyntax)
                    {
                        return false;
                    }

                    // Make sure that Event Declaration themselves are Public in the first place
                    if (!node.GetModifiers().Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                    {
                        return false;
                    }

                    return IsAllContainingTypeDeclsPublic(node);
                }

                previousNode = node;
                node = node.Parent;
            }

            return false;
        }

        private static bool IsAllContainingTypeDeclsPublic(SyntaxNode node)
        {
            // Make sure that all the containing Type Declarations are also Public
            var containingTypeDeclarations = node.GetAncestors<TypeDeclarationSyntax>();
            if (containingTypeDeclarations.Count() == 0)
            {
                return true;
            }
            else
            {
                return containingTypeDeclarations.All(typedecl => typedecl.GetModifiers().Any(m => m.Kind() == SyntaxKind.PublicKeyword));
            }
        }

        internal override bool IsGenericName(SimpleNameSyntax simpleName)
            => simpleName is GenericNameSyntax;

        internal override bool IsSimpleName(ExpressionSyntax expression)
            => expression is SimpleNameSyntax;

        internal override async Task<Solution> TryAddUsingsOrImportToDocumentAsync(Solution updatedSolution, SyntaxNode modifiedRoot, Document document, SimpleNameSyntax simpleName, string includeUsingsOrImports, CancellationToken cancellationToken)
        {
            // Nothing to include
            if (string.IsNullOrWhiteSpace(includeUsingsOrImports))
            {
                return updatedSolution;
            }

            SyntaxNode root = null;
            if (modifiedRoot == null)
            {
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                root = modifiedRoot;
            }

            if (root is CompilationUnitSyntax compilationRoot)
            {
                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(includeUsingsOrImports));

                // Check if the usings is already present
                if (compilationRoot.Usings.Where(n => n != null && n.Alias == null)
                                          .Select(n => n.Name.ToString())
                                          .Any(n => n.Equals(includeUsingsOrImports)))
                {
                    return updatedSolution;
                }

                // Check if the GFU is triggered from the namespace same as the usings namespace
                if (await IsWithinTheImportingNamespaceAsync(document, simpleName.SpanStart, includeUsingsOrImports, cancellationToken).ConfigureAwait(false))
                {
                    return updatedSolution;
                }

                var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);
                var addedCompilationRoot = compilationRoot.AddUsingDirectives(new[] { usingDirective }, placeSystemNamespaceFirst, Formatter.Annotation);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, addedCompilationRoot, PreservationMode.PreserveIdentity);
            }

            return updatedSolution;
        }

        private static ITypeSymbol GetPropertyType(
            SimpleNameSyntax propertyName,
            SemanticModel semanticModel,
            ITypeInferenceService typeInference,
            CancellationToken cancellationToken)
        {
            if (propertyName.Parent is AssignmentExpressionSyntax parentAssignment)
            {
                return typeInference.InferType(
                    semanticModel, parentAssignment.Left, objectAsDefault: true, cancellationToken: cancellationToken);
            }

            if (propertyName.Parent is IsPatternExpressionSyntax isPatternExpression)
            {
                return typeInference.InferType(
                    semanticModel, isPatternExpression.Expression, objectAsDefault: true, cancellationToken: cancellationToken);
            }

            return null;
        }

        private static IPropertySymbol CreatePropertySymbol(
            SimpleNameSyntax propertyName, ITypeSymbol propertyType)
        {
            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes: ImmutableArray<AttributeData>.Empty,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                explicitInterfaceImplementations: default,
                name: propertyName.Identifier.ValueText,
                type: propertyType,
                refKind: RefKind.None,
                parameters: default,
                getMethod: s_accessor,
                setMethod: s_accessor,
                isIndexer: false);
        }

        private static readonly IMethodSymbol s_accessor = CodeGenerationSymbolFactory.CreateAccessorSymbol(
            attributes: default,
            accessibility: Accessibility.Public,
            statements: default);

        internal override bool TryGenerateProperty(
            SimpleNameSyntax propertyName,
            SemanticModel semanticModel,
            ITypeInferenceService typeInference,
            CancellationToken cancellationToken,
            out IPropertySymbol property)
        {
            var propertyType = GetPropertyType(propertyName, semanticModel, typeInference, cancellationToken);
            if (propertyType == null || propertyType is IErrorTypeSymbol)
            {
                property = CreatePropertySymbol(propertyName, semanticModel.Compilation.ObjectType);
                return property != null;
            }

            property = CreatePropertySymbol(propertyName, propertyType);
            return property != null;
        }

        internal override IMethodSymbol GetDelegatingConstructor(
            SemanticDocument document,
            ObjectCreationExpressionSyntax objectCreation,
            INamedTypeSymbol namedType,
            ISet<IMethodSymbol> candidates,
            CancellationToken cancellationToken)
        {
            var model = document.SemanticModel;

            var oldNode = objectCreation
                    .AncestorsAndSelf(ascendOutOfTrivia: false)
                    .Where(node => SpeculationAnalyzer.CanSpeculateOnNode(node))
                    .LastOrDefault();

            var typeNameToReplace = objectCreation.Type;
            var newTypeName = namedType.GenerateTypeSyntax();
            var newObjectCreation = objectCreation.WithType(newTypeName).WithAdditionalAnnotations(s_annotation);
            var newNode = oldNode.ReplaceNode(objectCreation, newObjectCreation);

            var speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(oldNode, newNode, model);
            if (speculativeModel != null)
            {
                newObjectCreation = (ObjectCreationExpressionSyntax)newNode.GetAnnotatedNodes(s_annotation).Single();
                var symbolInfo = speculativeModel.GetSymbolInfo(newObjectCreation, cancellationToken);
                var parameterTypes = newObjectCreation.ArgumentList.Arguments.Select(
                    a => a.DetermineParameterType(speculativeModel, cancellationToken)).ToList();

                return GenerateConstructorHelpers.GetDelegatingConstructor(
                    document, symbolInfo, candidates, namedType, parameterTypes);
            }

            return null;
        }
    }
}
