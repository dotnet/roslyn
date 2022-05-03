// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateEnumMember), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateConstructor)]
    internal class GenerateEnumMemberCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        private const string CS0117 = nameof(CS0117); // error CS0117: 'Color' does not contain a definition for 'Red'

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GenerateEnumMemberCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0117); }
        }

        protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CodeAndImportGenerationOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IGenerateEnumMemberService>();
            return service.GenerateEnumMemberAsync(document, node, fallbackOptions, cancellationToken);
        }

        protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
            => node is IdentifierNameSyntax;
    }
}
