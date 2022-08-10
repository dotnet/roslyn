// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

public interface ILspLogger
{
    Task LogStartContextAsync(string message);
    Task LogEndContextAsync(string message);
    Task LogInformationAsync(string message);
    Task LogWarningAsync(string message);
    Task LogErrorAsync(string message);
    Task LogExceptionAsync(Exception exception);
}
