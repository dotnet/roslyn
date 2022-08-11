// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

public interface ILspLogger
{
    Task LogStartContextAsync(string message, params object[] @params);
    Task LogEndContextAsync(string message, params object[] @params);
    Task LogInformationAsync(string message, params object[] @params);
    Task LogWarningAsync(string message, params object[] @params);
    Task LogErrorAsync(string message, params object[] @params);
    Task LogExceptionAsync(Exception exception, string? message = null, params object[] @params);
}
