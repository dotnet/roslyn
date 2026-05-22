// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

/// <summary>
/// Specifies what LSP method the <see cref="MessageInterceptor"/> handles. May be applied multiple times.
/// </summary>
[ExcludeFromCodeCoverage]
internal class InterceptMethodAttribute : MultipleBaseMetadataAttribute
{
    public InterceptMethodAttribute(string interceptMethods)
    {
        InterceptMethods = interceptMethods;
    }

    // name must be kept in sync with IInterceptMethodMetadata
    public string InterceptMethods { get; }
}
