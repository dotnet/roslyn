// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers.UI;
using Roslyn.Test.PdbUtilities;

[assembly: DebuggerVisualizer(
    typeof(PdbDebuggerVisualizer),
    Target = typeof(PdbDelta),
    Description = "PDB Visualizer")]

namespace Roslyn.DebuggerVisualizers
{
    public sealed class PdbDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            StringBuilder sb = new StringBuilder();
            var pdb = (PdbDelta)objectProvider.GetObject();
            string xml = PdbToXmlConverter.DeltaPdbToXml(pdb.Stream, Enumerable.Range(0x06000001, 0xff));

            var viewer = new TextViewer(xml, "PDB");
            viewer.ShowDialog();
        }
    }
}
