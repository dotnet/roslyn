// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal sealed class AspNetCoreCSharpVirtualCharService
    {
        private static readonly AspNetCoreCSharpVirtualCharService _instance =
            new AspNetCoreCSharpVirtualCharService(CSharpVirtualCharService.Instance);

        private readonly IVirtualCharService _virtualCharService;

        private AspNetCoreCSharpVirtualCharService(IVirtualCharService virtualCharService)
        {
            _virtualCharService = virtualCharService;
        }

        /// <inheritdoc cref="CSharpVirtualCharService.Instance"/>
        public static AspNetCoreCSharpVirtualCharService Instance => _instance;

        /// <inheritdoc cref="IVirtualCharService.TryConvertToVirtualChars"/>
        public AspNetCoreVirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)
            => new(_virtualCharService.TryConvertToVirtualChars(token));
    }
}
