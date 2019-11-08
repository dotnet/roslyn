// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SemanticModelExtensions
    {
        public static IEnumerable<ITypeSymbol> LookupTypeRegardlessOfArity(
            this SemanticModel semanticModel,
            SyntaxToken name,
            CancellationToken cancellationToken)
        {
            if (name.Parent is ExpressionSyntax expression)
            {
                var results = semanticModel.LookupName(expression, namespacesAndTypesOnly: true, cancellationToken: cancellationToken);
                if (results.Length > 0)
                {
                    return results.OfType<ITypeSymbol>();
                }
            }

            return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
        }

        public static ImmutableArray<ISymbol> LookupName(
            this SemanticModel semanticModel,
            SyntaxToken name,
            bool namespacesAndTypesOnly,
            CancellationToken cancellationToken)
        {
            if (name.Parent is ExpressionSyntax expression)
            {
                return semanticModel.LookupName(expression, namespacesAndTypesOnly, cancellationToken);
            }

            return ImmutableArray.Create<ISymbol>();
        }

        /// <summary>
        /// Decomposes a name or member access expression into its component parts.
        /// </summary>
        /// <param name="expression">The name or member access expression.</param>
        /// <param name="qualifier">The qualifier (or left-hand-side) of the name expression. This may be null if there is no qualifier.</param>
        /// <param name="name">The name of the expression.</param>
        /// <param name="arity">The number of generic type parameters.</param>
        private static void DecomposeName(ExpressionSyntax expression, out ExpressionSyntax qualifier, out string name, out int arity)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    var max = (MemberAccessExpressionSyntax)expression;
                    qualifier = max.Expression;
                    name = max.Name.Identifier.ValueText;
                    arity = max.Name.Arity;
                    break;
                case SyntaxKind.QualifiedName:
                    var qn = (QualifiedNameSyntax)expression;
                    qualifier = qn.Left;
                    name = qn.Right.Identifier.ValueText;
                    arity = qn.Arity;
                    break;
                case SyntaxKind.AliasQualifiedName:
                    var aq = (AliasQualifiedNameSyntax)expression;
                    qualifier = aq.Alias;
                    name = aq.Name.Identifier.ValueText;
                    arity = aq.Name.Arity;
                    break;
                case SyntaxKind.GenericName:
                    var gx = (GenericNameSyntax)expression;
                    qualifier = null;
                    name = gx.Identifier.ValueText;
                    arity = gx.Arity;
                    break;
                case SyntaxKind.IdentifierName:
                    var nx = (IdentifierNameSyntax)expression;
                    qualifier = null;
                    name = nx.Identifier.ValueText;
                    arity = 0;
                    break;
                default:
                    qualifier = null;
                    name = null;
                    arity = 0;
                    break;
            }
        }

        public static ImmutableArray<ISymbol> LookupName(
            this SemanticModel semanticModel,
            ExpressionSyntax expression,
            bool namespacesAndTypesOnly,
            CancellationToken cancellationToken)
        {
            var expr = SyntaxFactory.GetStandaloneExpression(expression);
            DecomposeName(expr, out var qualifier, out var name, out var arity);

            INamespaceOrTypeSymbol symbol = null;
            if (qualifier != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(qualifier, cancellationToken);
                var symbolInfo = semanticModel.GetSymbolInfo(qualifier, cancellationToken);
                if (typeInfo.Type != null)
                {
                    symbol = typeInfo.Type;
                }
                else if (symbolInfo.Symbol != null)
                {
                    symbol = symbolInfo.Symbol as INamespaceOrTypeSymbol;
                }
            }

            return semanticModel.LookupSymbols(expr.SpanStart, container: symbol, name: name, includeReducedExtensionMethods: true);
        }

        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxToken token)
        {
            if (!CanBindToken(token))
            {
                return default;
            }

            switch (token.Parent)
            {
                case ExpressionSyntax expression:
                    return semanticModel.GetSymbolInfo(expression);
                case AttributeSyntax attribute:
                    return semanticModel.GetSymbolInfo(attribute);
                case ConstructorInitializerSyntax constructorInitializer:
                    return semanticModel.GetSymbolInfo(constructorInitializer);
            }

            return default;
        }

        private static bool CanBindToken(SyntaxToken token)
        {
            // Add more token kinds if necessary;
            switch (token.Kind())
            {
                case SyntaxKind.CommaToken:
                case SyntaxKind.DelegateKeyword:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Given an argument node, tries to generate an appropriate name that can be used for that
        /// argument.
        /// </summary>
        public static string GenerateNameForArgument(
            this SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            // If it named argument then we use the name provided.
            if (argument.NameColon != null)
            {
                return argument.NameColon.Name.Identifier.ValueText;
            }

            return semanticModel.GenerateNameForExpression(
                argument.Expression, capitalize: false, cancellationToken: cancellationToken);
        }

        public static string GenerateNameForArgument(
            this SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
        {
            // If it named argument then we use the name provided.
            if (argument.NameEquals != null)
            {
                return argument.NameEquals.Name.Identifier.ValueText;
            }

            return semanticModel.GenerateNameForExpression(
                argument.Expression, capitalize: false, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Given an expression node, tries to generate an appropriate name that can be used for
        /// that expression. 
        /// </summary>
        public static string GenerateNameForExpression(
            this SemanticModel semanticModel, ExpressionSyntax expression,
            bool capitalize, CancellationToken cancellationToken)
        {
            // Try to find a usable name node that we can use to name the
            // parameter.  If we have an expression that has a name as part of it
            // then we try to use that part.
            var current = expression;
            while (true)
            {
                current = current.WalkDownParentheses();

                if (current.Kind() == SyntaxKind.IdentifierName)
                {
                    return ((IdentifierNameSyntax)current).Identifier.ValueText.ToCamelCase();
                }
                else if (current is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.ValueText.ToCamelCase();
                }
                else if (current is MemberBindingExpressionSyntax memberBinding)
                {
                    return memberBinding.Name.Identifier.ValueText.ToCamelCase();
                }
                else if (current is ConditionalAccessExpressionSyntax conditionalAccess)
                {
                    current = conditionalAccess.WhenNotNull;
                }
                else if (current is CastExpressionSyntax castExpression)
                {
                    current = castExpression.Expression;
                }
                else if (current is DeclarationExpressionSyntax decl)
                {
                    if (!(decl.Designation is SingleVariableDesignationSyntax name))
                    {
                        break;
                    }

                    return name.Identifier.ValueText.ToCamelCase();
                }
                else
                {
                    break;
                }
            }

            // there was nothing in the expression to signify a name.  If we're in an argument
            // location, then try to choose a name based on the argument name.
            var argumentName = TryGenerateNameForArgumentExpression(
                semanticModel, expression, cancellationToken);
            if (argumentName != null)
            {
                return capitalize ? argumentName.ToPascalCase() : argumentName.ToCamelCase();
            }

            // Otherwise, figure out the type of the expression and generate a name from that
            // instead.
            var info = semanticModel.GetTypeInfo(expression, cancellationToken);

            // If we can't determine the type, then fallback to some placeholders.
            var type = info.Type;
            return type.CreateParameterName(capitalize);
        }

        private static string TryGenerateNameForArgumentExpression(
            SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var topExpression = expression.WalkUpParentheses();
            if (topExpression.IsParentKind(SyntaxKind.Argument))
            {
                var argument = (ArgumentSyntax)topExpression.Parent;
                if (argument.NameColon != null)
                {
                    return argument.NameColon.Name.Identifier.ValueText;
                }

                if (argument.Parent is BaseArgumentListSyntax argumentList)
                {
                    var index = argumentList.Arguments.IndexOf(argument);
                    if (semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken).Symbol is IMethodSymbol member && index < member.Parameters.Length)
                    {
                        var parameter = member.Parameters[index];
                        if (parameter.Type.OriginalDefinition.TypeKind != TypeKind.TypeParameter)
                        {
                            return parameter.Name;
                        }
                    }
                }
            }

            return null;
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            ArgumentListSyntax argumentList,
            CancellationToken cancellationToken)
        {
            return semanticModel.GenerateParameterNames(
                argumentList.Arguments, reservedNames: null, cancellationToken: cancellationToken);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            AttributeArgumentListSyntax argumentList,
            CancellationToken cancellationToken)
        {
            return semanticModel.GenerateParameterNames(
                argumentList.Arguments, reservedNames: null, cancellationToken: cancellationToken);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<ArgumentSyntax> arguments,
            IList<string> reservedNames,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameColon != null)).ToList();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToList();

            return GenerateNames(reservedNames, isFixed, parameterNames);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<ArgumentSyntax> arguments,
            IList<string> reservedNames,
            NamingRule parameterNamingRule,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameColon != null)).ToList();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToList();

            return GenerateNames(reservedNames, isFixed, parameterNames, parameterNamingRule);
        }

        private static ImmutableArray<ParameterName> GenerateNames(IList<string> reservedNames, List<bool> isFixed, List<string> parameterNames)
        {
            return NameGenerator.EnsureUniqueness(parameterNames, isFixed)
                                .Select((name, index) => new ParameterName(name, isFixed[index]))
                                .Skip(reservedNames.Count).ToImmutableArray();
        }

        private static ImmutableArray<ParameterName> GenerateNames(IList<string> reservedNames, List<bool> isFixed, List<string> parameterNames, NamingRule parameterNamingRule)
        {
            return NameGenerator.EnsureUniqueness(parameterNames, isFixed)
                .Select((name, index) => new ParameterName(name, isFixed[index], parameterNamingRule))
                .Skip(reservedNames.Count).ToImmutableArray();
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<AttributeArgumentSyntax> arguments,
            IList<string> reservedNames,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameEquals != null)).ToList();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToList();

            return GenerateNames(reservedNames, isFixed, parameterNames);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<AttributeArgumentSyntax> arguments,
            IList<string> reservedNames,
            NamingRule parameterNamingRule,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameEquals != null)).ToList();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToList();

            return GenerateNames(reservedNames, isFixed, parameterNames, parameterNamingRule);
        }

        public static ISet<INamespaceSymbol> GetUsingNamespacesInScope(this SemanticModel semanticModel, SyntaxNode location)
        {
            // Avoiding linq here for perf reasons. This is used heavily in the AddImport service
            var result = new HashSet<INamespaceSymbol>();

            foreach (var @using in location.GetEnclosingUsingDirectives())
            {
                if (@using.Alias == null)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(@using.Name);
                    if (symbolInfo is { Symbol: { Kind: SymbolKind.Namespace } })
                    {
                        result ??= new HashSet<INamespaceSymbol>();
                        result.Add((INamespaceSymbol)symbolInfo.Symbol);
                    }
                }
            }

            return result;
        }

        public static Accessibility DetermineAccessibilityConstraint(
            this SemanticModel semanticModel,
            TypeSyntax type,
            CancellationToken cancellationToken)
        {
            if (type == null)
            {
                return Accessibility.Private;
            }

            type = GetOutermostType(type);

            // Interesting cases based on 3.5.4 Accessibility constraints in the language spec.
            // If any of the below hold, then we will override the default accessibility if the
            // constraint wants the type to be more accessible. i.e. if by default we generate
            // 'internal', but a constraint makes us 'public', then be public.

            // 1) The direct base class of a class type must be at least as accessible as the
            //    class type itself.
            //
            // 2) The explicit base interfaces of an interface type must be at least as accessible
            //    as the interface type itself.
            if (type != null)
            {
                if (type.Parent is BaseTypeSyntax && type.Parent.IsParentKind(SyntaxKind.BaseList) && ((BaseTypeSyntax)type.Parent).Type == type)
                {
                    var containingType = semanticModel.GetDeclaredSymbol(type.GetAncestor<BaseTypeDeclarationSyntax>(), cancellationToken) as INamedTypeSymbol;
                    if (containingType != null && containingType.TypeKind == TypeKind.Interface)
                    {
                        return containingType.DeclaredAccessibility;
                    }
                    else if (((BaseListSyntax)type.Parent.Parent).Types[0] == type.Parent)
                    {
                        return containingType.DeclaredAccessibility;
                    }
                }
            }

            // 4) The type of a constant must be at least as accessible as the constant itself.
            // 5) The type of a field must be at least as accessible as the field itself.
            if (type.IsParentKind(SyntaxKind.VariableDeclaration) &&
                type.Parent.IsParentKind(SyntaxKind.FieldDeclaration))
            {
                var variableDeclaration = (VariableDeclarationSyntax)type.Parent;
                return semanticModel.GetDeclaredSymbol(
                    variableDeclaration.Variables[0], cancellationToken).DeclaredAccessibility;
            }

            // Also do the same check if we are in an object creation expression
            if (type.IsParentKind(SyntaxKind.ObjectCreationExpression) &&
                type.Parent.IsParentKind(SyntaxKind.EqualsValueClause) &&
                type.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
                type.Parent.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclaration) &&
                type.Parent.Parent.Parent.Parent.IsParentKind(SyntaxKind.FieldDeclaration))
            {
                var variableDeclaration = (VariableDeclarationSyntax)type.Parent.Parent.Parent.Parent;
                return semanticModel.GetDeclaredSymbol(
                    variableDeclaration.Variables[0], cancellationToken).DeclaredAccessibility;
            }

            // 3) The return type of a delegate type must be at least as accessible as the
            //    delegate type itself.
            // 6) The return type of a method must be at least as accessible as the method
            //    itself.
            // 7) The type of a property must be at least as accessible as the property itself.
            // 8) The type of an event must be at least as accessible as the event itself.
            // 9) The type of an indexer must be at least as accessible as the indexer itself.
            // 10) The return type of an operator must be at least as accessible as the operator
            //     itself.
            if (type.IsParentKind(SyntaxKind.DelegateDeclaration) ||
                type.IsParentKind(SyntaxKind.MethodDeclaration) ||
                type.IsParentKind(SyntaxKind.PropertyDeclaration) ||
                type.IsParentKind(SyntaxKind.EventDeclaration) ||
                type.IsParentKind(SyntaxKind.IndexerDeclaration) ||
                type.IsParentKind(SyntaxKind.OperatorDeclaration))
            {
                return semanticModel.GetDeclaredSymbol(
                    type.Parent, cancellationToken).DeclaredAccessibility;
            }

            // 3) The parameter types of a delegate type must be at least as accessible as the
            //    delegate type itself.
            // 6) The parameter types of a method must be at least as accessible as the method
            //    itself.
            // 9) The parameter types of an indexer must be at least as accessible as the
            //    indexer itself.
            // 10) The parameter types of an operator must be at least as accessible as the
            //     operator itself.
            // 11) The parameter types of an instance constructor must be at least as accessible
            //     as the instance constructor itself.
            if (type.IsParentKind(SyntaxKind.Parameter) && type.Parent.IsParentKind(SyntaxKind.ParameterList))
            {
                if (type.Parent.Parent.IsParentKind(SyntaxKind.DelegateDeclaration) ||
                    type.Parent.Parent.IsParentKind(SyntaxKind.MethodDeclaration) ||
                    type.Parent.Parent.IsParentKind(SyntaxKind.IndexerDeclaration) ||
                    type.Parent.Parent.IsParentKind(SyntaxKind.OperatorDeclaration))
                {
                    return semanticModel.GetDeclaredSymbol(
                        type.Parent.Parent.Parent, cancellationToken).DeclaredAccessibility;
                }

                if (type.Parent.Parent.IsParentKind(SyntaxKind.ConstructorDeclaration))
                {
                    var symbol = semanticModel.GetDeclaredSymbol(type.Parent.Parent.Parent, cancellationToken);
                    if (!symbol.IsStatic)
                    {
                        return symbol.DeclaredAccessibility;
                    }
                }
            }

            // 8) The type of an event must be at least as accessible as the event itself.
            if (type.IsParentKind(SyntaxKind.VariableDeclaration) &&
                type.Parent.IsParentKind(SyntaxKind.EventFieldDeclaration))
            {
                var variableDeclaration = (VariableDeclarationSyntax)type.Parent;
                var symbol = semanticModel.GetDeclaredSymbol(variableDeclaration.Variables[0], cancellationToken);
                if (symbol != null)
                {
                    return symbol.DeclaredAccessibility;
                }
            }

            // Type constraint must be at least as accessible as the declaring member (class, interface, delegate, method)
            if (type.IsParentKind(SyntaxKind.TypeConstraint))
            {
                return AllContainingTypesArePublicOrProtected(semanticModel, type, cancellationToken)
                    ? Accessibility.Public
                    : Accessibility.Internal;
            }

            return Accessibility.Private;
        }

        public static bool AllContainingTypesArePublicOrProtected(
            this SemanticModel semanticModel,
            TypeSyntax type,
            CancellationToken cancellationToken)
        {
            if (type == null)
            {
                return false;
            }

            var typeDeclarations = type.GetAncestors<TypeDeclarationSyntax>();

            foreach (var typeDeclaration in typeDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

                if (symbol.DeclaredAccessibility == Accessibility.Private ||
                    symbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal ||
                    symbol.DeclaredAccessibility == Accessibility.Internal)
                {
                    return false;
                }
            }

            return true;
        }

        private static TypeSyntax GetOutermostType(TypeSyntax type)
        {
            return type.GetAncestorsOrThis<TypeSyntax>().Last();
        }
    }
}
