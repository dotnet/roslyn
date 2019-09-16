// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassInCapturedVariablesAsArgumentsCodeFixProvider)), Shared]
    internal sealed class PassInCapturedVariablesAsArgumentsCodeFixProvider : CodeFixProvider
    {
        //  "CS8421: A static local function can't contain a reference to <variable>."
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS8421");

        public override FixAllProvider? GetFixAllProvider()
        {
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

            // Even when the language version doesn't support staic local function, the compiler will still
            // generate this error. So we need to check to make sure we don't provide incorrect fix.
            if (!MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(root.SyntaxTree))
            {
                return;
            }

            var localFunction = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().First();
            if (localFunction == null)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;

            if (MakeLocalFunctionStaticHelper.TryGetCaputuredSymbols(localFunction, semanticModel, out var captures) &&
                captures.Length > 0 &&
                !captures.Any(s => s.IsThisParameter()))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => MakeLocalFunctionStaticHelper.MakeLocalFunctionStaticAsync(document, semanticModel, localFunction, captures, c)),
                    diagnostic);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Pass_in_captured_variables_as_arguments, createChangedDocument, CSharpFeaturesResources.Pass_in_captured_variables_as_arguments)
            {
            }
        }
    }
}
