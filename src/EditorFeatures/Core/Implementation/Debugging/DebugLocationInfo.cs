// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal readonly struct DebugLocationInfo
    {
        public readonly string Name;
        public readonly int LineOffset;

        public DebugLocationInfo(string name, int lineOffset)
        {
            Debug.Assert(name != null);
            this.Name = name;
            this.LineOffset = lineOffset;
        }

        public bool IsDefault
        {
            get { return Name == null; }
        }
    }
}
