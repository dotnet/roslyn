using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IAsyncFindReferencesPresenter
    {
        FindReferencesContext StartSearch();
    }
}
