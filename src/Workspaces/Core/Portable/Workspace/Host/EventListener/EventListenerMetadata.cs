// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// MEF metadata class used to find exports declared for a specific <see cref="IEventListener"/>.
/// </summary>
internal sealed class EventListenerMetadata(IDictionary<string, object> data)
{
    public string Service { get; } = (string)data[nameof(ExportEventListenerAttribute.Service)];
    public IReadOnlyList<string> WorkspaceKinds { get; } = (IReadOnlyList<string>)data[nameof(ExportEventListenerAttribute.WorkspaceKinds)];
}
