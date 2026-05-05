// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.Logging;

[Export(typeof(ILoggerFactory))]
[method: ImportingConstructor]
internal sealed class VisualStudioLoggerFactory([ImportMany] IEnumerable<Lazy<ILoggerProvider, LoggerProviderMetadata>> providers)
    : AbstractLoggerFactory(providers.ToImmutableArray())
{
}
