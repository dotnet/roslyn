// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Peek;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Peek;

internal interface IXamlPeekableItemFactory
{
    Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(ISymbol symbol, Project project, IPeekResultFactory peekResultFactory, CancellationToken cancellationToken);
}

[Export(typeof(IXamlPeekableItemFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XamlPeekableItemFactory(
    IMetadataAsSourceFileService metadataAsSourceFileService,
    IGlobalOptionService globalOptions,
    IThreadingContext threadingContext)
    : PeekableItemFactory(metadataAsSourceFileService, globalOptions, threadingContext)
    , IXamlPeekableItemFactory;
