using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Roslyn.Test.MetadataUtilities;

namespace Roslyn.DebuggerVisualizers
{
    public sealed class MetadataDeltaVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var metadataDelta = (MetadataDelta)target;
            var text = GetMetadataText(metadataDelta);

            var writer = new StreamWriter(outgoingData);
            writer.Write(text);
            writer.Flush();
        }

        private static unsafe string GetMetadataText(MetadataDelta metadataDelta)
        {
            var writer = new StringWriter();

            fixed (byte* ptr = metadataDelta.Bytes)
            {
                var reader = new MetadataReader(ptr, metadataDelta.Bytes.Length, MetadataReaderOptions.ApplyWindowsRuntimeProjections);
                var visualizer = new MetadataVisualizer(reader, writer);
                visualizer.Visualize();
            }

            return writer.ToString();
        }
    }
}
