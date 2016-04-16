// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Services.Editor.Implementation.Completion
{
    internal interface IStandardCompletionItemsService : ILanguageService
    {
        /// <summary>
        /// Returns valid completion items at the specified position in the document.
        /// </summary>
        IEnumerable<CompletionItem> GetItems(IDocument document, int position, CancellationToken cancellationToken = default(CancellationToken));
    }
}
