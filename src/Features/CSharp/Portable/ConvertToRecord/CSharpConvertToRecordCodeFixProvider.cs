// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToRecord), Shared]
    internal class CSharpConvertToRecordCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToRecordCodeFixProvider()
        {
        }

        public override CodeAnalysis.CodeFixes.FixAllProvider? GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Only records may inherit from records.
        /// </summary>
        private const string CS8865 = nameof(CS8865);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS8865);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;
            // get the class declaration. The span should be on the base type in the base list
            var codeRefactoringHelper = document.GetRequiredLanguageService<IRefactoringHelpersService>();
            var baseSyntax = await codeRefactoringHelper.GetRelevantNodesAsync<BaseTypeSyntax>(
                document, span, cancellationToken).ConfigureAwait(false);

            var action = await ConvertToRecordCommon.GetCodeActionAsync(
                document,
                baseSyntax.FirstOrDefault()?.GetAncestor<TypeDeclarationSyntax>(),
                cancellationToken)
                .ConfigureAwait(false);

            if (action != null)
            {
                context.RegisterCodeFix(action, context.Diagnostics);
            }
        }
    }
}
