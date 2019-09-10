// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNew), Shared]
    internal partial class HideBaseCodeFixProvider : CodeFixProvider
    {
        internal const string CS0108 = nameof(CS0108); // 'SomeClass.SomeMember' hides inherited member 'SomeClass.SomeMember'. Use the new keyword if hiding was intended.

        [ImportingConstructor]
        public HideBaseCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0108);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            SyntaxNode originalNode = token.GetAncestor<PropertyDeclarationSyntax>();

            if (originalNode == null)
            {
                originalNode = token.GetAncestor<MethodDeclarationSyntax>();
            }

            if (originalNode == null)
            {
                originalNode = token.GetAncestor<FieldDeclarationSyntax>();
            }

            if (originalNode == null)
            {
                return;
            }

            context.RegisterCodeFix(new AddNewKeywordAction(context.Document, originalNode), context.Diagnostics);
        }
    }
}
