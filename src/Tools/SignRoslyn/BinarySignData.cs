using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal sealed class BinarySignData
    {
        /// <summary>
        /// The binary to be signed.
        /// </summary>
        internal BinaryName BinaryName { get; }

        internal FileSignData FileSignData { get; }

        /// <summary>
        /// The authenticode certificate which should be used to sign the binary.
        /// </summary>
        internal string Certificate => FileSignData.Certificate;

        /// <summary>
        /// This will be null in the case a strong name signing is not required.
        /// </summary>
        internal string StrongName => FileSignData.StrongName;

        internal BinarySignData(BinaryName name, FileSignData fileSignData)
        {
            Debug.Assert(name.IsAssembly || fileSignData.StrongName == null);

            BinaryName = name;
            FileSignData = fileSignData;
        }
    }
}
