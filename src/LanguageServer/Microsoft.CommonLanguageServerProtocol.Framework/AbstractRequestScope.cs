// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;
internal abstract class AbstractRequestScope(string name) : IDisposable
{
    public string Name { get; } = name;

    public string? Language { get; private set; }

    public abstract void RecordCancellation();
    public abstract void RecordException(Exception exception);
    public abstract void RecordWarning(string message);

    public abstract void RecordExecutionStart();

    /// <summary>
    /// Updates the telemetry metrics for the request with the handler's language.
    /// </summary>
    public void RecordHandlerLanguage(string language)
    {
        Language = language;
    }

    public abstract void Dispose();
}
