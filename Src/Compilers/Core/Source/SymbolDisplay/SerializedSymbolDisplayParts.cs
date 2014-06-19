using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    internal sealed class SerializedSymbolDisplayParts : ISerializable
    {
        private readonly SymbolDisplayPart[] DisplayParts;

        internal SerializedSymbolDisplayParts(ImmutableArray<SymbolDisplayPart> displayParts)
        {
            this.DisplayParts = displayParts.ToArray();
        }

        private SerializedSymbolDisplayParts(SerializationInfo info, StreamingContext context)
        {
            DisplayParts = (SymbolDisplayPart[])info.GetValue("parts", typeof(SymbolDisplayPart[]));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("parts", DisplayParts, typeof(SymbolDisplayPart[]));
        }

        public override string ToString()
        {
            return DisplayParts.AsImmutableOrNull().ToDisplayString();
        }
    }
}
