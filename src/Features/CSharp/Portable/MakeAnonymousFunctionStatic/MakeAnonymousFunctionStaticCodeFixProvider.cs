// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeAnonymousFunctionStatic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeAnonymousFunctionStaticCodeFixProvider)), Shared]
    internal class MakeAnonymousFunctionStaticCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public MakeAnonymousFunctionStaticCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.MakeAnonymousFunctionStaticDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Make_anonymous_function_static, nameof(CSharpAnalyzersResources.Make_anonymous_function_static));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var anonymousFunctions = diagnostics.SelectAsArray(d => d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));
            foreach (var anonymousFunction in anonymousFunctions)
            {
                editor.ReplaceNode(
                    anonymousFunction,
                    (current, generator) =>
                    {
                        var currentAnonymousFunction = (AnonymousFunctionExpressionSyntax)current;
                        return generator.WithModifiers(
                            currentAnonymousFunction,
                            generator.GetModifiers(currentAnonymousFunction).WithIsStatic(true));
                    });
            }

            return Task.CompletedTask;
        }
    }
}
