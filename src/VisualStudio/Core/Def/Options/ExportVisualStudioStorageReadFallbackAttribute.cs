// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Options;

/// <summary>
/// Use this attribute to declare a custom fallback reader for an option stored in Visual Studio storage.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportVisualStudioStorageReadFallbackAttribute : ExportAttribute
{
    /// <summary>
    /// Option unique name. <see cref="OptionDefinition.ConfigName"/>.
    /// </summary>
    public string ConfigName { get; }

    public ExportVisualStudioStorageReadFallbackAttribute(string configName)
        : base(typeof(IVisualStudioStorageReadFallback))
    {
        ConfigName = configName;
    }
}

internal sealed class OptionNameMetadata
{
    public string ConfigName { get; }

    public OptionNameMetadata(IDictionary<string, object> data)
        => ConfigName = (string)data[nameof(ExportVisualStudioStorageReadFallbackAttribute.ConfigName)];

    public OptionNameMetadata(string language)
        => ConfigName = language;
}
