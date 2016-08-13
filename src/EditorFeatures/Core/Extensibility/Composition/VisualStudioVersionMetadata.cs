// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.Composition
{
    internal sealed class VisualStudioVersionMetadata
    {
        public VisualStudioVersion Version { get; }

        public VisualStudioVersionMetadata(IDictionary<string, object> data)
        {
            Version = (VisualStudioVersion)data.GetValueOrDefault("Version");
        }
    }
}
