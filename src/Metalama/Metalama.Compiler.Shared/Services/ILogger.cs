// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace Metalama.Compiler.Services;

public interface ILogger
{
    /// <summary>
    /// Gets the <see cref="ILogWriter" /> for the <c>Trace</c> severity. Messages of this severity are written only in verbose mode.
    /// </summary>
    ILogWriter? Trace { get; }

    /// <summary>
    /// Gets the <see cref="ILogWriter" /> for the <c>Info</c> severity. Messages of this severity are written to the console output as normal text,
    /// or in logs even when the verbosity for the category is not set to verbose.
    /// </summary>
    ILogWriter? Info { get; }

    /// <summary>
    /// Gets the <see cref="ILogWriter" /> for the <c>Warning</c> severity. Messages of this severity are written to the console output as warnings,
    /// and to the logs.
    /// </summary>
    ILogWriter? Warning { get; }

    /// <summary>
    /// Gets the <see cref="ILogWriter" /> for the <c>Errors</c> severity. Messages of this severity are written to the console output as errors,
    /// and to the logs.
    /// </summary>
    ILogWriter? Error { get; }
}

public interface ILogWriter
{
    void Log(string message);
}
