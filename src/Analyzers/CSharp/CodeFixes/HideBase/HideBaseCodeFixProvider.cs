// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNew), Shared]
    internal partial class HideBaseCodeFixProvider : CodeFixProvider
    {
        internal const string CS0108 = nameof(CS0108); // 'SomeClass.SomeMember' hides inherited member 'SomeClass.SomeMember'. Use the new keyword if hiding was intended.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public HideBaseCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0108);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);

            var originalNode = token.GetAncestor<PropertyDeclarationSyntax>() ??
                token.GetAncestor<MethodDeclarationSyntax>() ??
                (SyntaxNode?)token.GetAncestor<FieldDeclarationSyntax>();

            if (originalNode == null)
                return;

            context.RegisterCodeFix(new AddNewKeywordAction(context.Document, originalNode), context.Diagnostics);
        }
    }
}
