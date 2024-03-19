// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStructFieldsWritable), Shared]
internal class CSharpMakeStructFieldsWritableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMakeStructFieldsWritableCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.MakeStructFieldsWritable];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Make_readonly_fields_writable, nameof(CSharpAnalyzersResources.Make_readonly_fields_writable));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            var diagnosticNode = diagnostic.Location.FindNode(cancellationToken);

            if (diagnosticNode is not StructDeclarationSyntax structDeclaration)
            {
                continue;
            }

            var fieldDeclarations = structDeclaration.Members
                .OfType<FieldDeclarationSyntax>();

            foreach (var fieldDeclaration in fieldDeclarations)
            {
                var fieldDeclarationModifiers = editor.Generator.GetModifiers(fieldDeclaration);
                var containsReadonlyModifier =
                    (fieldDeclarationModifiers & DeclarationModifiers.ReadOnly) == DeclarationModifiers.ReadOnly;

                if (containsReadonlyModifier)
                {
                    editor.SetModifiers(fieldDeclaration, fieldDeclarationModifiers.WithIsReadOnly(false));
                }
            }
        }

        return Task.CompletedTask;
    }
}
