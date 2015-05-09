// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers.UI;
using Roslyn.Test.MetadataUtilities;

[assembly: DebuggerVisualizer(
    typeof(MetadataDeltaDebuggerVisualizer),
    Target = typeof(MetadataDelta),
    Description = "PDB Visualizer")]

namespace Roslyn.DebuggerVisualizers
{
    public sealed class MetadataDeltaDebuggerVisualizer : DialogDebuggerVisualizer
    {
        unsafe protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            var md = (MetadataDelta)objectProvider.GetObject();
            var writer = new StringWriter();
            fixed (byte* ptr = md.Bytes)
            {
                var reader = new MetadataReader(ptr, md.Bytes.Length, MetadataReaderOptions.ApplyWindowsRuntimeProjections);
                var visualizer = new MetadataVisualizer(reader, writer);
                visualizer.Visualize();
            }
            var viewer = new TextViewer(writer.ToString(), "Metadata");
            viewer.ShowDialog();
        }
    }
}
