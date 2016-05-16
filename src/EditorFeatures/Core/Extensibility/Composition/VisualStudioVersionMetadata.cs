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
