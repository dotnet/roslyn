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
    internal class FilePathLocation : Location, IEquatable<FilePathLocation>
    {
        private readonly string filePath;
        private readonly TextSpan sourceSpan;

        public FilePathLocation(string filePath, TextSpan sourceSpan)
        {
            this.filePath = filePath;
            this.sourceSpan = sourceSpan;
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

        public override bool IsInSource
        {
            get
            {
                return true;
            }
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.SourceFile;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FilePathLocation);
        }

        public bool Equals(FilePathLocation obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return obj != null &&
                StringComparer.OrdinalIgnoreCase.Equals(this.filePath, obj.filePath) &&
                this.sourceSpan == obj.sourceSpan;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                this.sourceSpan.GetHashCode(),
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.filePath));
        }
    }
}
