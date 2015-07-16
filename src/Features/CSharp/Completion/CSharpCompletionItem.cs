// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    internal class CSharpCompletionItem : CompletionItem
    {
        public readonly Workspace Workspace;

        public CSharpCompletionItem(
            Workspace workspace,
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph,
            string sortText = null,
            string filterText = null,
            bool preselect = false,
            bool isBuilder = false,
            bool showsWarningIcon = false,
            bool shouldFormatOnCommit = false)
            : base(completionProvider,
                   displayText,
                   filterSpan,
                   descriptionFactory,
                   glyph,
                   sortText,
                   filterText,
                   preselect,
                   isBuilder,
                   showsWarningIcon,
                   shouldFormatOnCommit)
        {
            Contract.ThrowIfNull(workspace);

            this.Workspace = workspace;
        }
    }
}
