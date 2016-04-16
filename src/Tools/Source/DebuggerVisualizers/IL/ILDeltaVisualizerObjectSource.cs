// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.Test.MetadataUtilities;

namespace Roslyn.DebuggerVisualizers
{
    public sealed class ILDeltaVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var ilDelta = (ILDelta)target;
            var text = ImmutableArray.Create(ilDelta.Value).GetMethodIL();

            var writer = new StreamWriter(outgoingData);
            writer.Write(text);
            writer.Flush();
        }
    }
}
