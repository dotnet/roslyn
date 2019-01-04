using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal struct ProjectKey : IEquatable<ProjectKey>
    {
        internal string FilePath { get; }

        internal string FileName => FilePath != null ? Path.GetFileName(FilePath) : "";

        internal ProjectKey(string filePath)
        {
            FilePath = Path.GetFullPath(filePath);
        }

        public static bool operator ==(ProjectKey left, ProjectKey right) => StringComparer.OrdinalIgnoreCase.Equals(left.FilePath, right.FilePath);
        public static bool operator !=(ProjectKey left, ProjectKey right) => !(left == right);
        public bool Equals(ProjectKey other) => other == this;
        public override bool Equals(object obj) => obj is ProjectKey && Equals((ProjectKey)obj);
        public override int GetHashCode() => FilePath?.GetHashCode() ?? 0;
        public override string ToString() => FileName;
    }
}
