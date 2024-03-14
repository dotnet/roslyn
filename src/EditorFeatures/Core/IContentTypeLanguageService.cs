// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

/// <summary>
/// Service to provide the default content type for a language.
/// </summary>
internal interface IContentTypeLanguageService : ILanguageService
{
    IContentType GetDefaultContentType();
}
