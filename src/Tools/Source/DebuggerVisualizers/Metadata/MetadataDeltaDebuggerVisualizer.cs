// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers.UI;

[assembly: DebuggerVisualizer(
    typeof(MetadataDeltaDebuggerVisualizer),
    typeof(MetadataDeltaVisualizerObjectSource),
    Target = typeof(MetadataDelta),
    Description = "Metadata Visualizer")]

namespace Roslyn.DebuggerVisualizers
{
    public sealed class MetadataDeltaDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            var stream = objectProvider.GetData();
            var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();

            var viewer = new TextViewer(text, "Metadata");
            viewer.ShowDialog();
        }
    }
}
