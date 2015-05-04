// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementInterface
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementInterface), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementAbstractClass)]
    internal class ImplementInterfaceCodeFixProvider : CodeFixProvider
    {
        private readonly Func<TypeSyntax, bool> _interfaceName = n => n.Parent is BaseTypeSyntax && n.Parent.Parent is BaseListSyntax && ((BaseTypeSyntax)n.Parent).Type == n;
        private readonly Func<IEnumerable<CodeAction>, bool> _codeActionAvailable = actions => actions != null && actions.Any();

        internal const string CS0535 = "CS0535"; // 'Program' does not implement interface member 'System.Collections.IEnumerable.GetEnumerator()'
        internal const string CS0737 = "CS0737"; // 'Class' does not implement interface member 'IInterface.M()'. 'Class.M()' cannot implement an interface member because it is not public.
        internal const string CS0738 = "CS0738"; // 'C' does not implement interface member 'I.Method1()'. 'B.Method1()' cannot implement 'I.Method1()' because it does not have the matching return type of 'void'.

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0535, CS0737, CS0738); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return;
            }

            var service = document.GetLanguageService<IImplementInterfaceService>();
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var actions = token.Parent.GetAncestorsOrThis<TypeSyntax>()
                                      .Where(_interfaceName)
                                      .Select(n => service.GetCodeActions(document, model, n, cancellationToken))
                                      .FirstOrDefault(_codeActionAvailable);

            if (_codeActionAvailable(actions))
            {
                context.RegisterFixes(actions, context.Diagnostics);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
