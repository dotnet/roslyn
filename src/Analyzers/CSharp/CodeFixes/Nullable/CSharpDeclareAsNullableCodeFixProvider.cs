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

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.DeclareAsNullable), Shared]
    internal class CSharpDeclareAsNullableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // We want to distinguish different situations:
        // 1. local null assignments: `return null;`, `local = null;`, `parameter = null;` (high confidence that the null is introduced deliberately and the API should be updated)
        // 2. invocation with null: `M(null);`, or assigning null to field or property (test code might do this even though the API should remain not-nullable, so FixAll should be invoked with care)
        // 3. conditional: `return x?.ToString();`
        private const string AssigningNullLiteralLocallyEquivalenceKey = nameof(AssigningNullLiteralLocallyEquivalenceKey);
        private const string AssigningNullLiteralRemotelyEquivalenceKey = nameof(AssigningNullLiteralRemotelyEquivalenceKey);
        private const string ConditionalOperatorEquivalenceKey = nameof(ConditionalOperatorEquivalenceKey);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpDeclareAsNullableCodeFixProvider()
        {
        }

        // warning CS8603: Possible null reference return.
        // warning CS8600: Converting null literal or possible null value to non-nullable type.
        // warning CS8625: Cannot convert null literal to non-nullable reference type.
        // warning CS8618: Non-nullable property is uninitialized
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS8603", "CS8600", "CS8625", "CS8618");

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (model == null)
            {
                return;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            var declarationTypeToFix = TryGetDeclarationTypeToFix(model, node);
            if (declarationTypeToFix == null)
            {
                return;
            }

            RegisterCodeFix(context, CSharpCodeFixesResources.Declare_as_nullable, GetEquivalenceKey(node, model));
        }

        private static string GetEquivalenceKey(SyntaxNode node, SemanticModel model)
        {
            return IsRemoteApiUsage(node, model) ? AssigningNullLiteralRemotelyEquivalenceKey :
                node.IsKind(SyntaxKind.ConditionalAccessExpression) ? ConditionalOperatorEquivalenceKey :
                AssigningNullLiteralLocallyEquivalenceKey;

            static bool IsRemoteApiUsage(SyntaxNode node, SemanticModel model)
            {
                if (node.IsParentKind(SyntaxKind.Argument))
                {
                    // M(null) could be used in a test
                    return true;
                }

                if (node.Parent is AssignmentExpressionSyntax assignment)
                {
                    var symbol = model.GetSymbolInfo(assignment.Left).Symbol;
                    if (symbol is IFieldSymbol)
                    {
                        // x.field could be used in a test
                        return true;
                    }
                    else if (symbol is IPropertySymbol)
                    {
                        // x.Property could be used in a test
                        return true;
                    }
                }

                return false;
            }
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // a method can have multiple `return null;` statements, but we should only fix its return type once
            using var _ = PooledHashSet<TypeSyntax>.GetInstance(out var alreadyHandled);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (model != null)
            {
                foreach (var diagnostic in diagnostics)
                {
                    var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                    MakeDeclarationNullable(editor, model, node, alreadyHandled);
                }
            }
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, SemanticModel model, string? equivalenceKey, CancellationToken cancellationToken)
        {
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            return equivalenceKey == GetEquivalenceKey(node, model);
        }

        private static void MakeDeclarationNullable(SyntaxEditor editor, SemanticModel model, SyntaxNode node, HashSet<TypeSyntax> alreadyHandled)
        {
            var declarationTypeToFix = TryGetDeclarationTypeToFix(model, node);
            if (declarationTypeToFix != null && alreadyHandled.Add(declarationTypeToFix))
            {
                var fixedDeclaration = SyntaxFactory.NullableType(declarationTypeToFix.WithoutTrivia()).WithTriviaFrom(declarationTypeToFix);
                editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
            }
        }

        private static TypeSyntax? TryGetDeclarationTypeToFix(SemanticModel model, SyntaxNode node)
        {
            if (!IsExpressionSupported(node))
            {
                return null;
            }

            if (node.IsParentKind(SyntaxKind.ReturnStatement, SyntaxKind.YieldReturnStatement))
            {
                var containingMember = node.GetAncestors().FirstOrDefault(a => a.IsKind(
                    SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression,
                    SyntaxKind.LocalFunctionStatement, SyntaxKind.AnonymousMethodExpression, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
                    SyntaxKind.OperatorDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.EventDeclaration));

                if (containingMember == null)
                {
                    return null;
                }

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
            if (node.Parent?.Parent?.IsParentKind(SyntaxKind.VariableDeclaration) == true)
            {
                var variableDeclaration = (VariableDeclarationSyntax)node.Parent.Parent.Parent!;
                if (variableDeclaration.Variables.Count != 1)
                {
                    // string x = null, y = null;
                    return null;
                }

                return variableDeclaration.Type;
            }

            // x = null;
            if (node.Parent is AssignmentExpressionSyntax assignment)
            {
                var symbol = model.GetSymbolInfo(assignment.Left).Symbol;
                if (symbol is ILocalSymbol local)
                {
                    var syntax = local.DeclaringSyntaxReferences[0].GetSyntax();
                    if (syntax is VariableDeclaratorSyntax declarator &&
                        declarator.Parent is VariableDeclarationSyntax declaration &&
                        declaration.Variables.Count == 1)
                    {
                        return declaration.Type;
                    }
                }
                else if (symbol is IParameterSymbol parameter)
                {
                    return TryGetParameterTypeSyntax(parameter);
                }
                else if (symbol is IFieldSymbol { IsImplicitlyDeclared: false } field)
                {
                    // implicitly declared fields don't have DeclaringSyntaxReferences so filter them out
                    var syntax = field.DeclaringSyntaxReferences[0].GetSyntax();
                    if (syntax is VariableDeclaratorSyntax declarator &&
                       declarator.Parent is VariableDeclarationSyntax declaration &&
                       declaration.Variables.Count == 1)
                    {
                        return declaration.Type;
                    }
                    else if (syntax is TupleElementSyntax tupleElement)
                    {
                        return tupleElement.Type;
                    }
                }
                else if (symbol is IFieldSymbol { CorrespondingTupleField: IFieldSymbol tupleField })
                {
                    // Assigning a tuple field, eg. foo.Item1 = null
                    // The tupleField won't have DeclaringSyntaxReferences because it's implicitly declared, otherwise it
                    // would have fallen into the branch above. We can use the Locations instead, if there is one and it's in source
                    if (tupleField.Locations is { Length: 1 } &&
                        tupleField.Locations[0] is { IsInSource: true } location)
                    {
                        if (location.FindNode(default) is TupleElementSyntax tupleElement)
                        {
                            return tupleElement.Type;
                        }
                    }
                }
                else if (symbol is IPropertySymbol property)
                {
                    var syntax = property.DeclaringSyntaxReferences[0].GetSyntax();
                    if (syntax is PropertyDeclarationSyntax declaration)
                    {
                        return declaration.Type;
                    }
                }

                return null;
            }

            // Method(null)
            if (node.Parent is ArgumentSyntax argument && argument.Parent?.Parent is InvocationExpressionSyntax invocation)
            {
                var symbol = model.GetSymbolInfo(invocation.Expression).Symbol;
                if (symbol is not IMethodSymbol method || method.PartialImplementationPart is object)
                {
                    // We don't handle partial methods yet
                    return null;
                }

                if (argument.NameColon?.Name is IdentifierNameSyntax { Identifier: var identifier })
                {
                    var parameter = method.Parameters.Where(p => p.Name == identifier.Text).FirstOrDefault();
                    return TryGetParameterTypeSyntax(parameter);
                }

                var index = invocation.ArgumentList.Arguments.IndexOf(argument);
                if (index >= 0 && index < method.Parameters.Length)
                {
                    var parameter = method.Parameters[index];
                    return TryGetParameterTypeSyntax(parameter);
                }

                return null;
            }

            // string x { get; set; } = null;
            if (node.Parent.IsParentKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax? propertyDeclaration))
            {
                return propertyDeclaration.Type;
            }

            // string x { get; }
            // Unassigned value that's not marked as null
            if (node is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                return propertyDeclarationSyntax.Type;
            }

            // string x;
            // Unassigned value that's not marked as null
            if (node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax _ } declarationSyntax } &&
                declarationSyntax.Variables.Count == 1)
            {
                return declarationSyntax.Type;
            }

            // void M(string x = null) { }
            if (node.Parent.IsParentKind(SyntaxKind.Parameter, out ParameterSyntax? optionalParameter))
            {
                var parameterSymbol = model.GetDeclaredSymbol(optionalParameter);
                return TryGetParameterTypeSyntax(parameterSymbol);
            }

            // static string M() => null;
            if (node.IsParentKind(SyntaxKind.ArrowExpressionClause) &&
                node.Parent.IsParentKind(SyntaxKind.MethodDeclaration, out MethodDeclarationSyntax? arrowMethod))
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
                        {
                            return typeArguments[0];
                        }

                        break;
                }

                return null;
            }

            static TypeSyntax? TryGetParameterTypeSyntax(IParameterSymbol? parameterSymbol)
            {
                if (parameterSymbol is object &&
                    parameterSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax parameterSyntax &&
                    parameterSymbol.ContainingSymbol is IMethodSymbol method &&
                    method.GetAllMethodSymbolsOfPartialParts().Length == 1)
                {
                    return parameterSyntax.Type;
                }

                return null;
            }
        }

        private static bool IsExpressionSupported(SyntaxNode node)
        {
            return node.IsKind(
                SyntaxKind.NullLiteralExpression,
                SyntaxKind.AsExpression,
                SyntaxKind.DefaultExpression,
                SyntaxKind.DefaultLiteralExpression,
                SyntaxKind.ConditionalExpression,
                SyntaxKind.ConditionalAccessExpression,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.VariableDeclarator);
        }
    }
}
