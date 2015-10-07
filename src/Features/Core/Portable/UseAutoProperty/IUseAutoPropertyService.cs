using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    interface IUseAutoPropertyService : ILanguageService
    {
        SyntaxToken OnTokenRenamed(SyntaxToken oldToken, SyntaxToken newToken);
    }
}
