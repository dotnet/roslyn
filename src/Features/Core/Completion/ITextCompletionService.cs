// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal interface ITextCompletionService : ICompletionService
    {
        Task<CompletionList> GetCompletionListAsync(SourceText text, int position, CompletionTriggerInfo triggerInfo, IEnumerable<CompletionListProvider> completionProviders, OptionSet options, CancellationToken cancellationToken);
    }
}
