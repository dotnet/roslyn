// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

internal static class ExportProviderExtensions
{
    public static TCommandHandler GetCommandHandler<TCommandHandler>(this ExportProvider exportProvider, string name)
        where TCommandHandler : ICommandHandler
    {
        var lazyCommandHandlers = exportProvider.GetExports<ICommandHandler, OrderableMetadata>();
        return Assert.IsType<TCommandHandler>(lazyCommandHandlers.Single(lazyCommandHandler => lazyCommandHandler.Metadata.Name == name).Value);
    }

    public static TCommandHandler GetCommandHandler<TCommandHandler>(this ExportProvider exportProvider, string name, string contentType)
        where TCommandHandler : ICommandHandler
    {
        var lazyCommandHandlers = exportProvider.GetExports<ICommandHandler, OrderableContentTypeMetadata>();
        return Assert.IsType<TCommandHandler>(lazyCommandHandlers.Single(lazyCommandHandler => lazyCommandHandler.Metadata.Name == name && lazyCommandHandler.Metadata.ContentTypes.Contains(contentType)).Value);
    }
}
