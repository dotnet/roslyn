// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Peek
{
    public interface IPeekableItemFactory
    {
        Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(ISymbol symbol, Project project, IPeekResultFactory peekResultFactory, CancellationToken cancellationToken);
    }
}
