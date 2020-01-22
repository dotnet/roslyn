// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete (https://github.com/dotnet/roslyn/issues/35872)

using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptIndentationResultWrapper
    {
        private readonly IndentationResult _underlyingObject;

        public VSTypeScriptIndentationResultWrapper(IndentationResult underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }

        public int BasePosition => _underlyingObject.BasePosition;
        public int Offset => _underlyingObject.Offset;
    }
}
