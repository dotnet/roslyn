// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

internal sealed class RemoveSinkWhenDisposed : IDisposable
{
    private readonly List<ITableDataSink> _tableSinks;
    private readonly ITableDataSink _sink;

    public RemoveSinkWhenDisposed(List<ITableDataSink> tableSinks, ITableDataSink sink)
    {
        _tableSinks = tableSinks;
        _sink = sink;
    }

    public void Dispose()
    {
        // whoever subscribed is no longer interested in my data.
        // Remove them from the list of sinks
        _ = _tableSinks.Remove(_sink);
    }
}
