// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Peek;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Peek;

internal interface IPeekableItemFactory
{
    Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(ISymbol symbol, Project project, IPeekResultFactory peekResultFactory, CancellationToken cancellationToken);
}

/// <summary>
/// Legacy export for xaml.  They should move to IXamlPeekableItemFactory once this is inserted.
/// </summary>
[Export(typeof(IPeekableItemFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorPeekableItemFactory(
    IMetadataAsSourceFileService metadataAsSourceFileService,
    IGlobalOptionService globalOptions,
    IThreadingContext threadingContext) : PeekableItemFactory(metadataAsSourceFileService, globalOptions, threadingContext);
