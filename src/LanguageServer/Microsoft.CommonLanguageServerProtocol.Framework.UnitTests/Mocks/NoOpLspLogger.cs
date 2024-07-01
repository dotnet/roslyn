// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal sealed class NoOpLspLogger : AbstractLspLogger, ILspLogger
{
    public static NoOpLspLogger Instance = new();

    public override void LogDebug(string message, params object[] @params)
    {
    }

    public override void LogError(string message, params object[] @params)
    {
    }

    public override void LogException(Exception exception, string? message = null, params object[] @params)
    {
    }

    public override void LogInformation(string message, params object[] @params)
    {
    }

    public override void LogStartContext(string context, params object[] @params)
    {
    }

    public override void LogEndContext(string context, params object[] @params)
    {
    }

    public override void LogWarning(string message, params object[] @params)
    {
    }
}
