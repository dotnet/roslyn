using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    internal sealed class SerializedLocation : Location, IEquatable<SerializedLocation>
    {
        private readonly LocationKind kind;
        private readonly TextSpan sourceSpan;
        private readonly FileLinePositionSpan fileSpan;
        private readonly FileLinePositionSpan fileSpanUsingDirectives;

        private SerializedLocation(SerializationInfo info, StreamingContext context)
        {
            sourceSpan = (TextSpan)info.GetValue("sourceSpan", typeof(TextSpan));
            fileSpan = (FileLinePositionSpan)info.GetValue("fileSpan", typeof(FileLinePositionSpan));
            fileSpanUsingDirectives = (FileLinePositionSpan)info.GetValue("fileSpanUsingDirectives", typeof(FileLinePositionSpan));
            kind = (LocationKind)info.GetByte("kind");
        }

        internal static void GetObjectData(Location location, SerializationInfo info, StreamingContext context)
        {
            var fileSpan = location.GetLineSpan(usePreprocessorDirectives: false);
            var fileSpanUsingDirectives = location.GetLineSpan(usePreprocessorDirectives: true);
            info.AddValue("sourceSpan", location.SourceSpan, typeof(TextSpan));
            info.AddValue("fileSpan", fileSpan, typeof(FileLinePositionSpan));
            info.AddValue("fileSpanUsingDirectives", fileSpanUsingDirectives, typeof(FileLinePositionSpan));
            info.AddValue("kind", (byte)location.Kind);
        }

        public override LocationKind Kind
        {
            get { return kind; }
        }

        public override TextSpan SourceSpan
        {
            get { return sourceSpan; }
        }

        public override FileLinePositionSpan GetLineSpan(bool usePreprocessorDirectives)
        {
            return usePreprocessorDirectives ? fileSpanUsingDirectives : fileSpan;
        }

        public override int GetHashCode()
        {
            return fileSpan.GetHashCode();
        }

        public bool Equals(SerializedLocation other)
        {
            return other != null && fileSpan.Equals(other.fileSpan);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SerializedLocation);
        }
    }
}
