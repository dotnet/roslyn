using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Used so we can mock out logging in unit tests.
    /// </summary>
    internal interface IPackageSearchLogService
    {
        void LogException(Exception e, string text);
        void LogInfo(string text);
    }
}
