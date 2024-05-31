// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.DeclareAsNullable), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpDeclareAsNullableCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    // We want to distinguish different situations:
    // 1. local null assignments: `return null;`, `local = null;`, `parameter = null;` (high confidence that the null is introduced deliberately and the API should be updated)
    // 2. invocation with null: `M(null);`, or assigning null to field or property (test code might do this even though the API should remain not-nullable, so FixAll should be invoked with care)
    // 3. conditional: `return x?.ToString();`
    private const string AssigningNullLiteralLocallyEquivalenceKey = nameof(AssigningNullLiteralLocallyEquivalenceKey);
    private const string AssigningNullLiteralRemotelyEquivalenceKey = nameof(AssigningNullLiteralRemotelyEquivalenceKey);
    private const string ConditionalOperatorEquivalenceKey = nameof(ConditionalOperatorEquivalenceKey);

    // warning CS8603: Possible null reference return.
    // warning CS8600: Converting null literal or possible null value to non-nullable type.
    // warning CS8625: Cannot convert null literal to non-nullable reference type.
    // warning CS8618: Non-nullable property is uninitialized
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ["CS8603", "CS8600", "CS8625", "CS8618"];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;

        var node = context.Diagnostics.First().Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

        var declarationTypeToFix = await TryGetDeclarationTypeToFixAsync(
            context.Document, model: null, node, cancellationToken).ConfigureAwait(false);
        if (declarationTypeToFix == null)
            return;

        RegisterCodeFix(context, CSharpCodeFixesResources.Declare_as_nullable, GetEquivalenceKey(node));
    }

    private static string GetEquivalenceKey(SyntaxNode node)
    {
        // M(null) could be used in a test
        if (node.Parent is ArgumentSyntax)
            return AssigningNullLiteralRemotelyEquivalenceKey;

        // x.field could be used in a test
        if (node.Parent is AssignmentExpressionSyntax)
            return AssigningNullLiteralRemotelyEquivalenceKey;

        if (node.IsKind(SyntaxKind.ConditionalAccessExpression))
            return ConditionalOperatorEquivalenceKey;

        // Default for everything else.  Can create more categories here in the future if we need to.
        return AssigningNullLiteralLocallyEquivalenceKey;
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        // a method can have multiple `return null;` statements, but we should only fix its return type once
        using var _ = PooledHashSet<TypeSyntax>.GetInstance(out var alreadyHandled);

        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            await MakeDeclarationNullableAsync(
                document, model, editor, node, alreadyHandled, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
    {
        var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
        return equivalenceKey == GetEquivalenceKey(node);
    }

    private static async Task MakeDeclarationNullableAsync(
        Document document, SemanticModel model, SyntaxEditor editor, SyntaxNode node, HashSet<TypeSyntax> alreadyHandled, CancellationToken cancellationToken)
    {
        var declarationTypeToFix = await TryGetDeclarationTypeToFixAsync(
            document, model, node, cancellationToken).ConfigureAwait(false);
        if (declarationTypeToFix != null && alreadyHandled.Add(declarationTypeToFix))
        {
            var fixedDeclaration = SyntaxFactory.NullableType(declarationTypeToFix.WithoutTrivia()).WithTriviaFrom(declarationTypeToFix);
            editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
        }
    }

    private static async Task<TypeSyntax?> TryGetDeclarationTypeToFixAsync(
        Document document, SemanticModel? model, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (!IsExpressionSupported(node))
            return null;

        if (node.Parent is (kind: SyntaxKind.ReturnStatement or SyntaxKind.YieldReturnStatement))
        {
            var containingMember = node.GetAncestors().FirstOrDefault(
                a => a.Kind() is
                    SyntaxKind.MethodDeclaration or
                    SyntaxKind.PropertyDeclaration or
                    SyntaxKind.ParenthesizedLambdaExpression or
                    SyntaxKind.SimpleLambdaExpression or
                    SyntaxKind.LocalFunctionStatement or
                    SyntaxKind.AnonymousMethodExpression or
                    SyntaxKind.ConstructorDeclaration or
                    SyntaxKind.DestructorDeclaration or
                    SyntaxKind.OperatorDeclaration or
                    SyntaxKind.IndexerDeclaration or
                    SyntaxKind.EventDeclaration);

            if (containingMember == null)
                return null;

            var onYield = node.IsParentKind(SyntaxKind.YieldReturnStatement);

            return containingMember switch
            {
                MethodDeclarationSyntax method =>
                    // string M() { return null; }
                    // async Task<string> M() { return null; }
                    // IEnumerable<string> M() { yield return null; }
                    TryGetReturnType(method.ReturnType, method.Modifiers, onYield),

                LocalFunctionStatementSyntax localFunction =>
                    // string local() { return null; }
                    // async Task<string> local() { return null; }
                    // IEnumerable<string> local() { yield return null; }
                    TryGetReturnType(localFunction.ReturnType, localFunction.Modifiers, onYield),

                PropertyDeclarationSyntax property =>
                    // string x { get { return null; } }
                    // IEnumerable<string> Property { get { yield return null; } }
                    TryGetReturnType(property.Type, modifiers: default, onYield),

                _ => null,
            };
        }

        // string x = null;
        if (node.Parent?.Parent?.Parent is VariableDeclarationSyntax variableDeclaration)
        {
            // string x = null, y = null;
            return variableDeclaration.Variables.Count == 1 ? variableDeclaration.Type : null;
        }

        model ??= await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // x = null;
        if (node.Parent is AssignmentExpressionSyntax assignment)
        {
            var symbol = model.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
            if (symbol is ILocalSymbol { DeclaringSyntaxReferences.Length: > 0 } local)
            {
                var syntax = local.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (syntax is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1 } declaration })
                    return declaration.Type;
            }
            else if (symbol is IParameterSymbol parameter)
            {
                return TryGetParameterTypeSyntax(parameter, cancellationToken);
            }
            else if (symbol is IFieldSymbol { IsImplicitlyDeclared: false, DeclaringSyntaxReferences.Length: > 0 } field)
            {
                // implicitly declared fields don't have DeclaringSyntaxReferences so filter them out
                var syntax = field.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (syntax is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1 } declaration })
                    return declaration.Type;

                if (syntax is TupleElementSyntax tupleElement)
                    return tupleElement.Type;
            }
            else if (symbol is IFieldSymbol { CorrespondingTupleField: IFieldSymbol { Locations: [{ IsInSource: true } location] } })
            {
                // Assigning a tuple field, eg. foo.Item1 = null
                // The tupleField won't have DeclaringSyntaxReferences because it's implicitly declared, otherwise it
                // would have fallen into the branch above. We can use the Locations instead, if there is one and it's in source
                if (location.FindNode(cancellationToken) is TupleElementSyntax tupleElement)
                    return tupleElement.Type;
            }
            else if (symbol is IPropertySymbol { DeclaringSyntaxReferences.Length: > 0 } property)
            {
                var syntax = property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (syntax is PropertyDeclarationSyntax declaration)
                    return declaration.Type;
            }

            return null;
        }

        // Method(null)
        if (node.Parent is ArgumentSyntax argument && argument.Parent?.Parent is InvocationExpressionSyntax invocation)
        {
            var symbol = model.GetSymbolInfo(invocation.Expression, cancellationToken).Symbol;
            if (symbol is not IMethodSymbol method || method.PartialImplementationPart is not null)
            {
                // https://github.com/dotnet/roslyn/issues/73772: should we also bail out on a partial property?
                // We don't handle partial methods yet
                return null;
            }

            if (argument.NameColon?.Name is IdentifierNameSyntax { Identifier: var identifier })
            {
                var parameter = method.Parameters.Where(p => p.Name == identifier.Text).FirstOrDefault();
                return TryGetParameterTypeSyntax(parameter, cancellationToken);
            }

            var index = invocation.ArgumentList.Arguments.IndexOf(argument);
            if (index >= 0 && index < method.Parameters.Length)
            {
                var parameter = method.Parameters[index];
                return TryGetParameterTypeSyntax(parameter, cancellationToken);
            }

            return null;
        }

        // string x { get; set; } = null;
        if (node.Parent?.Parent is PropertyDeclarationSyntax propertyDeclaration)
            return propertyDeclaration.Type;

        // string x { get; }
        // Unassigned value that's not marked as null
        if (node is PropertyDeclarationSyntax propertyDeclarationSyntax)
            return propertyDeclarationSyntax.Type;

        // string x;
        // Unassigned value that's not marked as null
        if (node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax, Variables.Count: 1 } declarationSyntax })
            return declarationSyntax.Type;

        // void M(string x = null) { }
        if (node.Parent?.Parent is ParameterSyntax optionalParameter)
        {
            var parameterSymbol = model.GetDeclaredSymbol(optionalParameter, cancellationToken);
            return TryGetParameterTypeSyntax(parameterSymbol, cancellationToken);
        }

        // static string M() => null;
        if (node.IsParentKind(SyntaxKind.ArrowExpressionClause) &&
            node.Parent?.Parent is MethodDeclarationSyntax arrowMethod)
        {
            return arrowMethod.ReturnType;
        }

        return null;

        // local functions
        static TypeSyntax? TryGetReturnType(TypeSyntax returnType, SyntaxTokenList modifiers, bool onYield)
        {
            if (modifiers.Any(SyntaxKind.AsyncKeyword) || onYield)
            {
                // async Task<string> M() { return null; }
                // async IAsyncEnumerable<string> M() { yield return null; }
                // IEnumerable<string> M() { yield return null; }
                return TryGetSingleTypeArgument(returnType);
            }

            // string M() { return null; }
            return returnType;
        }

        static TypeSyntax? TryGetSingleTypeArgument(TypeSyntax type)
        {
            switch (type)
            {
                case QualifiedNameSyntax qualified:
                    return TryGetSingleTypeArgument(qualified.Right);

                case GenericNameSyntax generic:
                    var typeArguments = generic.TypeArgumentList.Arguments;
                    if (typeArguments.Count == 1)
                        return typeArguments[0];

                    break;
            }

            return null;
        }

        static TypeSyntax? TryGetParameterTypeSyntax(IParameterSymbol? parameterSymbol, CancellationToken cancellationToken)
        {
            if (parameterSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is ParameterSyntax parameterSyntax &&
                parameterSymbol.ContainingSymbol is IMethodSymbol method &&
                method.GetAllMethodSymbolsOfPartialParts().Length == 1)
            {
                return parameterSyntax.Type;
            }

            return null;
        }
    }

    private static bool IsExpressionSupported(SyntaxNode node)
        => node.Kind() is
            SyntaxKind.NullLiteralExpression or
            SyntaxKind.AsExpression or
            SyntaxKind.DefaultExpression or
            SyntaxKind.DefaultLiteralExpression or
            SyntaxKind.ConditionalExpression or
            SyntaxKind.ConditionalAccessExpression or
            SyntaxKind.PropertyDeclaration or
            SyntaxKind.VariableDeclarator;
}
