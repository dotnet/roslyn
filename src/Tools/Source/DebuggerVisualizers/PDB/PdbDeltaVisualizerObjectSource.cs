using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.Test.PdbUtilities;

namespace Roslyn.DebuggerVisualizers
{
    public sealed class PdbDeltaVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var pdbDelta = (PdbDelta)target;
            var text = PdbToXmlConverter.DeltaPdbToXml(pdbDelta.Stream, Enumerable.Range(0x06000001, 0xff));

            var writer = new StreamWriter(outgoingData);
            writer.Write(text);
            writer.Flush();
        }
    }
}
