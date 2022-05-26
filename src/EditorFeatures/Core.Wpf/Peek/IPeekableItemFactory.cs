// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
