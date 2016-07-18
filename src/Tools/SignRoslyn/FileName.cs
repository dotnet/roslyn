using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal struct FileName : IEquatable<FileName>
    {
        internal string Name { get; }
        internal string FullPath { get; }
        internal string RelativePath { get; }

        internal bool IsAssembly => PathUtil.IsAssembly(Name);
        internal bool IsVsix => PathUtil.IsVsix(Name);

        internal FileName(string rootBinaryPath, string relativePath)
        {
            Name = Path.GetFileName(relativePath);
            FullPath = Path.Combine(rootBinaryPath, relativePath);
            RelativePath = relativePath;
        }

        public static bool operator ==(FileName left, FileName right) => left.FullPath == right.FullPath;
        public static bool operator !=(FileName left, FileName right) => !(left == right);
        public bool Equals(FileName other) => this == other;
        public override int GetHashCode() => FullPath.GetHashCode();
        public override string ToString() => RelativePath;
        public override bool Equals(object obj) => obj is FileName && Equals((FileName)obj);
    }
}
