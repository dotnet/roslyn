﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptTextBufferExtensions
    {
        public static SourceTextContainer AsTextContainer(this ITextBuffer buffer)
            => Text.Extensions.TextBufferContainer.From(buffer);
    }
}
