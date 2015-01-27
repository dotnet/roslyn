// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.Providers;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeActions.ImplementAbstractClass
{
    [ExportCodeIssueProvider(PredefinedCodeActionProviderNames.ImplementAbstractClass, LanguageNames.CSharp)]
    [ExtensionOrder(After = PredefinedCodeActionProviderNames.GenerateType)]
    internal partial class ImplementAbstractClassCodeIssueProvider : AbstractCSharpCodeIssueProvider
    {
        public override IEnumerable<Type> SyntaxNodeTypes
        {
            get
            {
                yield return typeof(TypeSyntax);
            }
        }

        protected override async Task<CodeIssue> GetIssueAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return null;
            }

            var service = document.GetLanguageService<IImplementAbstractClassService>();
            var result = await service.ImplementAbstractClassAsync(document, node, cancellationToken).ConfigureAwait(false);

            var changes = await result.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            if (!changes.Any())
            {
                return null;
            }

            return new CodeIssue(
                CodeIssueKind.Error,
                node.Span,
                SpecializedCollections.SingletonEnumerable(new CodeAction(result)));
        }
    }
}
