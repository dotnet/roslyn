using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal sealed class VsixData
    {
        /// <summary>
        /// Name of the VSIX
        /// </summary>
        internal FileName Name { get; }

        /// <summary>
        /// The set of binaries nested inside this VSIX.
        /// </summary>
        internal ImmutableArray<VsixPart> NestedBinaryParts;

        /// <summary>
        /// The set of external binaries this VSIX depends on.
        /// </summary>
        internal ImmutableArray<string> NestedExternalNames { get; }

        internal VsixData(FileName name, ImmutableArray<VsixPart> nestedBinaryParts, ImmutableArray<string> nestedExternalNames)
        {
            Name = name;
            NestedBinaryParts = nestedBinaryParts;
            NestedExternalNames = nestedExternalNames;
        }

        internal VsixPart? GetNestedBinaryPart(string relativeName)
        {
            foreach (var part in NestedBinaryParts)
            {
                if (relativeName == part.RelativeName)
                {
                    return part;
                }
            }

            return null;
        }
    }

    internal struct VsixPart
    {
        internal string RelativeName { get; }
        internal FileName BinaryName { get; }

        internal VsixPart(string relativeName, FileName binaryName)
        {
            RelativeName = relativeName;
            BinaryName = binaryName;
        }

        public override string ToString() => $"{RelativeName} -> {BinaryName.RelativePath}";
    }
}
