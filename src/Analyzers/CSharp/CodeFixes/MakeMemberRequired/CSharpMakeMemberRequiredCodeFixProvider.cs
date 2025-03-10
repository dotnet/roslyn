// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Required members are available in C# 11 or higher
        if (root.GetLanguageVersion() < LanguageVersion.CSharp11)
            return;

        var node = root.FindNode(span);

        // Supported cases:
        // public string [|MyProperty|] { get; set; }
        // public string [|_myField|];
        // public string [|_myField1|], [|_myField2|];
        if (node is not (PropertyDeclarationSyntax or VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax }))
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.RequiredMemberAttribute") is null)
        {
            // The attribute necessary to support required members is not present
            return;
        }

        var fieldOrPropertySymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
        if (fieldOrPropertySymbol is IPropertySymbol propertySymbol)
        {
            var setMethod = propertySymbol.SetMethod;

            // Property must have a `set` or `init` accessor in order to be able to be required
            if (setMethod is null)
                return;

            var containingTypeVisibility = propertySymbol.ContainingType.GetResultantVisibility();
            var minimalAccessibility = (Accessibility)Math.Min((int)propertySymbol.DeclaredAccessibility, (int)setMethod.DeclaredAccessibility);

            if (!CanBeAccessed(containingTypeVisibility, minimalAccessibility))
                return;

            RegisterCodeFix(context, CSharpCodeFixesResources.Make_property_required, nameof(CSharpCodeFixesResources.Make_property_required));
        }
        else if (fieldOrPropertySymbol is IFieldSymbol fieldSymbol)
        {
            var containingTypeVisibility = fieldSymbol.ContainingType.GetResultantVisibility();
            var accessibility = fieldSymbol.DeclaredAccessibility;

            if (!CanBeAccessed(containingTypeVisibility, accessibility))
                return;

            RegisterCodeFix(context, CSharpCodeFixesResources.Make_field_required, nameof(CSharpCodeFixesResources.Make_field_required));
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
            _ => throw ExceptionUtilities.Unreachable(),
        };
    }

    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var generator = editor.Generator;
        var visitedFieldDeclarations = new HashSet<FieldDeclarationSyntax>();

        foreach (var diagnostic in diagnostics)
        {
            var memberDeclarator = root.FindNode(diagnostic.Location.SourceSpan);

            // If we are fixing field, do not apply new declaration modifiers just to variable declarator, but to the whole field declaration.
            // This is observable when there are several variables in single filed declaration:
            // `public string _myField, _myField1;` -> `public required string _myField, _myField1;`
            // Without this branch the result would be:
            // ```
            // public required string _myField;
            // public required string _myField2;
            // ```
            if (memberDeclarator is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax fieldDeclaration })
            {
                // Skip field declarations we already visited to not try changing the same declaration twice.
                // Otherwise we get an exception in fix-all scenario like this:
                // `public string _myField, _myField1`
                // Here when visiting diagnostic for `_myField1` we already changed this field declaration.
                // Trying to change it again throws in `SyntaxEditor` later, because it cannot find the node.
                if (!visitedFieldDeclarations.Add(fieldDeclaration))
                    continue;

                memberDeclarator = fieldDeclaration;
            }

            var declarationModifiers = generator.GetModifiers(memberDeclarator);
            var newDeclarationModifiers = declarationModifiers.WithIsRequired(true);
            editor.ReplaceNode(memberDeclarator, generator.WithModifiers(memberDeclarator, newDeclarationModifiers));
        }

        return Task.CompletedTask;
    }
}
