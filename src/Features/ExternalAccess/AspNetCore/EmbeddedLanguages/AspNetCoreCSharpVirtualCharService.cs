// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

internal sealed class AspNetCoreCSharpVirtualCharService
{
    private readonly IVirtualCharService _virtualCharService;

    private AspNetCoreCSharpVirtualCharService(IVirtualCharService virtualCharService)
    {
        _virtualCharService = virtualCharService;
    }

    /// <inheritdoc cref="CSharpVirtualCharService.Instance"/>
    public static AspNetCoreCSharpVirtualCharService Instance { get; } = new AspNetCoreCSharpVirtualCharService(CSharpVirtualCharService.Instance);

    /// <inheritdoc cref="IVirtualCharService.TryConvertToVirtualChars"/>
    public AspNetCoreVirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)
        => new(_virtualCharService.TryConvertToVirtualChars(token));
}
