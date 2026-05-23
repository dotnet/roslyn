// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.Logging;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportLoggerProviderAttribute : ExportAttribute
{
    public LogLevel? MinimumLogLevel { get; }

    public ExportLoggerProviderAttribute()
        : base(typeof(ILoggerProvider))
    {
        MinimumLogLevel = null;
    }

    public ExportLoggerProviderAttribute(LogLevel minimumLogLevel)
        : base(typeof(ILoggerProvider))
    {
        MinimumLogLevel = minimumLogLevel;
    }
}
