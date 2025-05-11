// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

internal sealed class ContentTypeLanguageMetadata(IDictionary<string, object> data) : ILanguageMetadata
{
    public string Language { get; } = (string)data[nameof(ExportLanguageServiceAttribute.Language)];
    public string? DefaultContentType { get; } = (string?)data.GetValueOrDefault(nameof(ExportContentTypeLanguageServiceAttribute.DefaultContentType));
}
