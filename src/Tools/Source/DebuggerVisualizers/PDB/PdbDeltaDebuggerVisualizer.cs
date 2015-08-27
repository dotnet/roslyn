// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers.UI;
using Roslyn.Test.PdbUtilities;

[assembly: DebuggerVisualizer(
    typeof(PdbDeltaDebuggerVisualizer),
    typeof(PdbDeltaVisualizerObjectSource),
    Target = typeof(PdbDelta),
    Description = "PDB Visualizer")]

namespace Roslyn.DebuggerVisualizers
{
    public sealed class PdbDeltaDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            var stream = objectProvider.GetData();
            var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();

            var viewer = new TextViewer(text, "PDB");
            viewer.ShowDialog();
        }
    }
}
