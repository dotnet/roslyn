// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public class NoOpLspLogger : ILspLogger
{
    public static NoOpLspLogger Instance = new();

    public void LogError(string message, params object[] @params)
    {
    }

    public void LogException(Exception exception, string? message = null, params object[] @params)
    {
    }

    public void LogInformation(string message, params object[] @params)
    {
    }

    public void LogStartContext(string context, params object[] @params)
    {
    }

    public void LogEndContext(string context, params object[] @params)
    {
    }

    public void LogWarning(string message, params object[] @params)
    {
    }
}
