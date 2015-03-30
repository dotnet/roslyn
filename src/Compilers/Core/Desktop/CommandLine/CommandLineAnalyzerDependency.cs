using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public struct CommandLineAnalyzerDependency : IEquatable<CommandLineAnalyzerDependency>
    {
        public CommandLineAnalyzerDependency(string path)
        {
            FilePath = path;
        }

        public string FilePath { get; }

        public override bool Equals(object obj)
        {
            return obj is CommandLineAnalyzerDependency && base.Equals((CommandLineAnalyzerDependency)obj);
        }

        public bool Equals(CommandLineAnalyzerDependency other)
        {
            return FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(FilePath, 0);
        }
    }
}
