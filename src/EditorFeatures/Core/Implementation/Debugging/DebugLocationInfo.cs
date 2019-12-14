// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal readonly struct DebugLocationInfo
    {
        public readonly string Name;
        public readonly int LineOffset;

        public DebugLocationInfo(string name, int lineOffset)
        {
            RoslynDebug.Assert(name != null);
            Name = name;
            LineOffset = lineOffset;
        }

        public bool IsDefault
            => Name == null;
    }
}
