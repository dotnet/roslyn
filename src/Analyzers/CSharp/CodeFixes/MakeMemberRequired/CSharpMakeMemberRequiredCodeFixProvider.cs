// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeMemberRequired;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMemberRequired), Shared]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.DeclareAsNullable)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeMemberRequiredCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    private const string CS8618 = nameof(CS8618); // Non-nullable variable must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring it as nullable.

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [CS8618];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Required members are available in C# 11 or higher
        if (root.GetLanguageVersion() < LanguageVersion.CSharp11)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.RequiredMemberAttribute") is null)
        {
            // The attribute necessary to support required members is not present
            return;
        }

        // To ensure deterministic order (source order of members), we find all CS8618 diagnostics at this span.
        var allDiagnosticsAtLocation = semanticModel.GetDiagnostics(context.Span, cancellationToken)
                                                    .Where(d => d.Id == CS8618)
                                                    .ToImmutableArray();

        if (allDiagnosticsAtLocation.Length == 0) return;

        var diagnosticsAndInfo = new List<(Diagnostic diagnostic, SyntaxNode node, string title, string equivalenceKey)>();
        foreach (var diagnostic in allDiagnosticsAtLocation)
        {
            var memberNode = GetMemberNode(diagnostic, root);
            if (memberNode == null) continue;

            var symbol = semanticModel.GetDeclaredSymbol(memberNode, cancellationToken);
            if (IsFixableSymbol(symbol, out var title, out var equivalenceKey))
                diagnosticsAndInfo.Add((diagnostic, memberNode, title, equivalenceKey));
        }

        if (diagnosticsAndInfo.Count == 0) return;

        // Sort by the member's source position.
        diagnosticsAndInfo.Sort((d1, d2) => d1.node.SpanStart.CompareTo(d2.node.SpanStart));

        var firstNode = diagnosticsAndInfo[0].node;
        if (!context.Diagnostics.Any(d => GetMemberNode(d, root) == firstNode))
            return;

        foreach (var (diagnostic, _, title, equivalenceKey) in diagnosticsAndInfo)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    GetDocumentUpdater(context, diagnostic),
                    equivalenceKey),
                allDiagnosticsAtLocation);
        }
    }

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
        => equivalenceKey == nameof(CSharpMakeMemberRequiredCodeFixProvider);

    static bool IsFixableSymbol(ISymbol? symbol, out string title, out string equivalenceKey)
    {
        title = "";
        equivalenceKey = nameof(CSharpMakeMemberRequiredCodeFixProvider);
        if (symbol == null || symbol.IsStatic || symbol.Kind == SymbolKind.Event) return false;

        if (symbol is IPropertySymbol propertySymbol)
        {
            if (propertySymbol.IsOverride && !IsBaseRequired(propertySymbol)) return false;
            if (propertySymbol.SetMethod == null) return false;
            var visibility = propertySymbol.ContainingType.GetResultantVisibility();
            var setMethodAccessibility = propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
            var accessibility = (Accessibility)Math.Min((int)propertySymbol.DeclaredAccessibility, (int)setMethodAccessibility);
            if (!CanBeAccessed(visibility, accessibility)) return false;

            title = CSharpCodeFixesResources.Make_property_required;
            return true;
        }
        
        if (symbol is IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsReadOnly) return false;
            var visibility = fieldSymbol.ContainingType.GetResultantVisibility();
            if (!CanBeAccessed(visibility, fieldSymbol.DeclaredAccessibility)) return false;

            title = CSharpCodeFixesResources.Make_field_required;
            return true;
        }

        return false;
    }

    static bool IsBaseRequired(IPropertySymbol property)
    {
        var overridden = property.OverriddenProperty;
        while (overridden != null)
        {
            if (overridden.IsRequired) return true;
            overridden = overridden.OverriddenProperty;
        }
        return false;
    }

    // The `required` modifier cannot be used if a member is not accessible from outside of type.
    // For instance, having private required property in a public class leads to compiler error.
    // This function checks whether the member can be accessed by checking containing type visibility (which is computed already taking into account whether the type is nested and what is its visibility based on that fact)
    // against accessibility of member we are trying to make required
    static bool CanBeAccessed(SymbolVisibility containingTypeVisibility, Accessibility accessibility) => containingTypeVisibility switch
    {
        // Public is the highest accessibility. So in order to be accessible outside, member accessibility must be only public
        SymbolVisibility.Public => accessibility is Accessibility.Public,
        // In order to be accessible from an internal type, a member must have internal accessibility or higher
        SymbolVisibility.Internal => accessibility is >= Accessibility.Internal,
        // Private containing type visibility means it is nested in some other type.
        // In such case member must be accessible to the outer type of containing one.
        // This is possible with internal accessibility or higher
        SymbolVisibility.Private => accessibility is >= Accessibility.Internal,
        _ => false,
    };

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var generator = editor.Generator;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var visitedFieldDeclarations = new HashSet<FieldDeclarationSyntax>();

        foreach (var diagnostic in diagnostics)
        {
            var memberNode = GetMemberNode(diagnostic, root);
            if (memberNode == null)
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(memberNode, cancellationToken);
            if (!IsFixableSymbol(symbol, out _, out _)) continue;

            if (memberNode is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax fieldDecl })
                memberNode = fieldDecl;

            if (memberNode is FieldDeclarationSyntax fieldDeclaration)
            {
                if (!visitedFieldDeclarations.Add(fieldDeclaration))
                    continue;
            }

            if (memberNode is not (PropertyDeclarationSyntax or FieldDeclarationSyntax))
                continue;

            var declarationModifiers = generator.GetModifiers(memberNode);
            var newDeclarationModifiers = declarationModifiers.WithIsRequired(true);
            editor.ReplaceNode(memberNode, generator.WithModifiers(memberNode, newDeclarationModifiers));
        }
    }

    private static SyntaxNode? GetMemberNode(Diagnostic diagnostic, SyntaxNode root)
    {
        var location = diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[0] : diagnostic.Location;
        var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);
        return node.FirstAncestorOrSelf<SyntaxNode>(n => n is PropertyDeclarationSyntax or VariableDeclaratorSyntax);
    }
}
