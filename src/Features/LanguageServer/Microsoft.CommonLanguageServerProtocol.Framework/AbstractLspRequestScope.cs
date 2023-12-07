// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public abstract class AbstractLspRequestScope
{
    private readonly Stopwatch _stopwatch;

    protected string Name { get; }

    protected TimeSpan QueuedDuration { get; private set; }
    protected TimeSpan RequestDuration { get; private set; }

    protected AbstractLspRequestScope(string name)
    {
        _stopwatch = Stopwatch.StartNew();
        Name = name;
    }

    public abstract void RecordCancellation();
    public abstract void RecordException(Exception exception);
    public abstract void RecordWarning(string message);

    public void RecordExecutionStart()
    {
        QueuedDuration = _stopwatch.Elapsed;
    }

    public virtual void Dispose()
    {
        RequestDuration = _stopwatch.Elapsed;

        _stopwatch.Stop();
    }
}
