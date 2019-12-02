﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal readonly struct FSharpDebugLocationInfo
    {
        internal readonly DebugLocationInfo UnderlyingObject;

        public FSharpDebugLocationInfo(string name, int lineOffset)
            => UnderlyingObject = new DebugLocationInfo(name, lineOffset);

        public readonly string Name => UnderlyingObject.Name;
        public readonly int LineOffset => UnderlyingObject.LineOffset;
        internal bool IsDefault => UnderlyingObject.IsDefault;
    }
}
