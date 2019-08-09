// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider)), Shared]
    internal sealed class PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        public PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS8421");

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().First();
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var service = document.GetLanguageService<MakeLocalFunctionStaticService>();

            context.RegisterCodeFix(
                new MyCodeAction(c => service.CreateParameterSymbolAsync(context.Document, declaration, c)),
                diagnostic);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Pass_variable_explicitly_in_local_static_function, createChangedDocument, FeaturesResources.Pass_variable_explicitly_in_local_static_function)
            {
            }
        }
    }

}




