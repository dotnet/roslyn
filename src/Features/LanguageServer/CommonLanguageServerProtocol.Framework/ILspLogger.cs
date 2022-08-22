// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

public interface ILspLogger
{
    Task LogStartContextAsync(string message, CancellationToken cancellationToken, params object[] @params);
    Task LogEndContextAsync(string message, CancellationToken cancellationToken, params object[] @params);
    Task LogInformationAsync(string message, CancellationToken cancellationToken, params object[] @params);
    Task LogWarningAsync(string message, CancellationToken cancellationToken, params object[] @params);
    Task LogErrorAsync(string message, CancellationToken cancellationToken, params object[] @params);
    Task LogExceptionAsync(Exception exception, string? message = null, CancellationToken? cancellationToken = null, params object[] @params);
}
