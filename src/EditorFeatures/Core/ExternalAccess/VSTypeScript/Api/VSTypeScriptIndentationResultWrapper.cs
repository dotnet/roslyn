// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete (https://github.com/dotnet/roslyn/issues/35872)

using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptIndentationResultWrapper
    {
        private readonly IndentationResult _underlyingObject;

        public VSTypeScriptIndentationResultWrapper(IndentationResult underlyingObject)
            => _underlyingObject = underlyingObject;

        public int BasePosition => _underlyingObject.BasePosition;
        public int Offset => _underlyingObject.Offset;
    }
}
