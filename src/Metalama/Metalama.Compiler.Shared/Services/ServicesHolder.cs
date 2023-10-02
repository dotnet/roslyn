// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler.Services;

public abstract class ServicesHolder
{
    protected ServicesHolder(ILogger? logger, IExceptionReporter? exceptionReporter)
    {
        Logger = logger;
        ExceptionReporter = exceptionReporter;
    }

    public ILogger? Logger { get; }

    public IExceptionReporter? ExceptionReporter { get; }

    public abstract void DisposeServices(Action<Diagnostic> reportDiagnostic);
}
