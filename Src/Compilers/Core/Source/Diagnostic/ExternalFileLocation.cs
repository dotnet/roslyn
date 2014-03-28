using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in source code.
    /// </summary>
    [Serializable]
    internal class ExternalFileLocation : Location, IEquatable<ExternalFileLocation>
    {
        private readonly string filePath;
        private readonly TextSpan sourceSpan;
        private readonly FileLinePositionSpan lineSpan;

        public ExternalFileLocation(string filePath, TextSpan sourceSpan, LinePositionSpan lineSpan)
        {
            this.filePath = filePath;
            this.sourceSpan = sourceSpan;
            this.lineSpan = new FileLinePositionSpan(filePath, lineSpan);
        }

        public override string FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        public override TextSpan SourceSpan
        {
            get
            {
                return this.sourceSpan;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return this.lineSpan;
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            return this.lineSpan;
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.ExternalFile;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ExternalFileLocation);
        }

        public bool Equals(ExternalFileLocation obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return obj != null &&
                StringComparer.Ordinal.Equals(this.filePath, obj.filePath) &&
                this.sourceSpan == obj.sourceSpan &&
                this.lineSpan.Equals(obj.lineSpan);
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(this.lineSpan.GetHashCode(),
                Hash.Combine(this.sourceSpan.GetHashCode(), StringComparer.Ordinal.GetHashCode(this.filePath)));
        }
    }
}
