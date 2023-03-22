// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FixIncorrectConstraint
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixIncorrectConstraint), Shared]
    internal class CSharpFixIncorrectConstraintCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS9010 = nameof(CS9010); // Keyword 'enum' cannot be used as a constraint.Did you mean 'struct, System.Enum'?	Net6 C:\github\repo_find_refs\Net6\Class1.cs 1	Active
        private const string CS9011 = nameof(CS9011); // 'delegate' cannot be used as a constraint.Did you mean 'System.Delegate'?	Net6 C:\github\repo_find_refs\Net6\Class1.cs 1	Active

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFixIncorrectConstraintCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS9010, CS9011);

        private static bool TryGetConstraint(
            Diagnostic diagnostic,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out TypeConstraintSyntax? constraint,
            out SyntaxToken enumOrDelegateKeyword)
        {
            enumOrDelegateKeyword = default;
            constraint = diagnostic.Location.FindNode(cancellationToken) as TypeConstraintSyntax;
            if (constraint == null)
                return false;

            if (constraint.Parent is not TypeParameterConstraintClauseSyntax)
                return false;

            if (constraint.Type is not IdentifierNameSyntax { Identifier.IsMissing: true } type)
                return false;

            var trailingTrivia = type.GetTrailingTrivia();
            if (trailingTrivia.Count == 0)
                return false;

            var firstTrivia = trailingTrivia[0];
            if (firstTrivia.Kind() != SyntaxKind.SkippedTokensTrivia)
                return false;

            var structure = (SkippedTokensTriviaSyntax)firstTrivia.GetStructure()!;
            if (structure.Tokens.Count != 1)
                return false;

            enumOrDelegateKeyword = structure.Tokens[0];
            return enumOrDelegateKeyword.Kind() is SyntaxKind.EnumKeyword or SyntaxKind.DelegateKeyword;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;

            var diagnostic = context.Diagnostics.First();
            if (TryGetConstraint(diagnostic, cancellationToken, out _, out _))
            {
                RegisterCodeFix(context, CSharpCodeFixesResources.Fix_constraint, nameof(CSharpFixIncorrectConstraintCodeFixProvider));
            }

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                if (TryGetConstraint(diagnostic, cancellationToken, out var constraintSyntax, out var enumOrDelegateKeyword))
                {
                    var isEnumConstraint = enumOrDelegateKeyword.Kind() is SyntaxKind.EnumKeyword;
                    var newType = generator.TypeExpression(compilation.GetSpecialType(
                        isEnumConstraint ? SpecialType.System_Enum : SpecialType.System_Delegate));

                    // Skip the first trailing trivia as that's the skipped enum/delegate keyword.
                    editor.ReplaceNode(
                        constraintSyntax.Type, newType
                        .WithLeadingTrivia(constraintSyntax.GetLeadingTrivia())
                        .WithTrailingTrivia(constraintSyntax.GetTrailingTrivia().Skip(1)));

                    // if they added the `enum` constraint, also add `struct` along with `System.Enum` to properly
                    // reflect what they meant (and what the diagnostic says).
                    if (isEnumConstraint)
                    {
                        editor.ReplaceNode(constraintSyntax.GetRequiredParent(), (parent, _) =>
                        {
                            var clause = (TypeParameterConstraintClauseSyntax)parent;
                            return clause.WithConstraints(
                                clause.Constraints.Insert(0, SyntaxFactory.ClassOrStructConstraint(
                                    SyntaxKind.StructConstraint)));
                        });
                    }
                }
            }
        }
    }
}
