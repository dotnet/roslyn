// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UpgradeProject
{
    internal abstract partial class AbstractUpgradeProjectCodeFixProvider : CodeFixProvider
    {
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var diagnostics = context.Diagnostics;

            context.RegisterFixes(GetUpgradeProjectCodeActionsAsync(context), diagnostics);
            return Task.CompletedTask;
        }

        protected abstract ImmutableArray<CodeAction> GetUpgradeProjectCodeActionsAsync(CodeFixContext context);
    }
}