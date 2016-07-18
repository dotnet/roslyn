using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal sealed class FileSignInfo
    {
        /// <summary>
        /// The binary to be signed.
        /// </summary>
        internal FileName FileName { get; }

        internal SignInfo FileSignData { get; }

        /// <summary>
        /// The authenticode certificate which should be used to sign the binary.
        /// </summary>
        internal string Certificate => FileSignData.Certificate;

        /// <summary>
        /// This will be null in the case a strong name signing is not required.
        /// </summary>
        internal string StrongName => FileSignData.StrongName;

        internal FileSignInfo(FileName name, SignInfo fileSignData)
        {
            Debug.Assert(name.IsAssembly || fileSignData.StrongName == null);

            FileName = name;
            FileSignData = fileSignData;
        }
    }
}
