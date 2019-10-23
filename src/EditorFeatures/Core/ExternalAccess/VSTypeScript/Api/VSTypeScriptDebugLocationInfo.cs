// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal struct VSTypeScriptDebugLocationInfo
    {
        public readonly string Name;
        public readonly int LineOffset;

        public VSTypeScriptDebugLocationInfo(string name, int lineOffset)
        {
            Debug.Assert(name != null);
            Name = name;
            LineOffset = lineOffset;
        }

        internal bool IsDefault => Name == null;
    }
}
