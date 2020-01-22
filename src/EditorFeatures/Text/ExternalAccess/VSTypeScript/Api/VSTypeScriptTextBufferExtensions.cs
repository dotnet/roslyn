// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
