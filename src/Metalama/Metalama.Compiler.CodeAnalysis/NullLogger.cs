// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Metalama.Compiler.Services;

namespace Metalama.Compiler;

internal class NullLogger : ILogger
{
    private NullLogger()
    {
    }

    public static NullLogger Instance { get; } = new();

    public ILogWriter? Trace => null;

    public ILogWriter? Info => null;

    public ILogWriter? Warning => null;

    public ILogWriter? Error => null;
}
