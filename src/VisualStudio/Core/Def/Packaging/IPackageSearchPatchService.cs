using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Used so we can mock out patching in unit tests.
    /// </summary>
    internal interface IPackageSearchPatchService
    {
        byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes);
    }
}
