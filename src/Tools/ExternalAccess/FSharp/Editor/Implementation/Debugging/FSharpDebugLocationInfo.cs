// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal readonly struct FSharpDebugLocationInfo
    {
        public readonly string Name;
        public readonly int LineOffset;

        public FSharpDebugLocationInfo(string name, int lineOffset)
        {
            Debug.Assert(name != null);
            Name = name;
            LineOffset = lineOffset;
        }

        public bool IsDefault
        {
            get { return Name == null; }
        }
    }
}
