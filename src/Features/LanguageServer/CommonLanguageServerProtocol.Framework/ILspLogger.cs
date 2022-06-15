// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommonLanguageServerProtocol.Framework;

public interface ILspLogger
{
    void TraceInformation(string message);
    void TraceWarning(string message);
    void TraceError(string message);
    void TraceException(Exception exception);
    void TraceStart(string message);
    void TraceStop(string message);
}
