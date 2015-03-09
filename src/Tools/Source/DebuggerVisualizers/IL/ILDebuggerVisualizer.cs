// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers;
using Roslyn.DebuggerVisualizers.UI;
using Roslyn.Test.MetadataUtilities;

[assembly: DebuggerVisualizer(typeof(ILDebuggerVisualizer), Target = typeof(ILDelta), Description = "IL Visualizer")]

namespace Roslyn.DebuggerVisualizers
{
    public sealed class ILDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            StringBuilder sb = new StringBuilder();
            var ilBytes = ((ILDelta)objectProvider.GetObject()).Value;
            var viewer = new TextViewer(ilBytes.GetMethodIL(), "IL");
            viewer.ShowDialog();
        }
    }
}
