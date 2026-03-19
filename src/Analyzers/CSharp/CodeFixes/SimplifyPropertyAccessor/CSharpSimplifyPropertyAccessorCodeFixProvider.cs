// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyPropertyAccessor;

using static CSharpSyntaxTokens;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyPropertyAccessor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSimplifyPropertyAccessorCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.SimplifyPropertyAccessorDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Simplify_property_accessor, nameof(CSharpAnalyzersResources.Simplify_property_accessor));
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        using var _ = PooledHashSet<PropertyDeclarationSyntax>.GetInstance(out var seenPartialProperties);

        foreach (var diagnostic in diagnostics)
        {
            var accessor = (AccessorDeclarationSyntax)diagnostic.Location.FindNode(cancellationToken);

            // If this accessor belongs to a partial property remember that property.
            // If we later find another accessor of the same partial property, we reject the fix.
            // This is a case where both accessors of a partial property implementation part
            // are potentially simplifiable and we are performing a fix-all.
            // Analyzer reports both accessors since simplifying each individually won't break anything
            // but if we "fix" both the property won't be a valid partial implementation part anymore.
            // Therefore we fix only the first accessor we encounter
            if (accessor.Parent?.Parent is PropertyDeclarationSyntax containingProperty &&
                containingProperty.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                !seenPartialProperties.Add(containingProperty))
            {
                continue;
            }

            var fixedAccessor = accessor
                .WithBody(null)
                .WithExpressionBody(null);

            if (fixedAccessor.SemicolonToken == default)
            {
                fixedAccessor = fixedAccessor.WithSemicolonToken(
                    SemicolonToken.WithTrailingTrivia(accessor.GetTrailingTrivia()));
            }

            editor.ReplaceNode(accessor, fixedAccessor.WithAdditionalAnnotations(Formatter.Annotation));
        }
    }
}
