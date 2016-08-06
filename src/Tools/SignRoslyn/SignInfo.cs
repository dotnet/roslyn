using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal struct SignInfo
    {
        /// <summary>
        /// The authenticode certificate which should be used to sign the binary.
        /// </summary>
        internal string Certificate { get; }

        /// <summary>
        /// This will be null in the case a strong name signing is not required.
        /// </summary>
        internal string StrongName { get; }

        internal SignInfo(string certificate, string strongName)
        {
            Certificate = certificate;
            StrongName = strongName;
        }
    }
}
