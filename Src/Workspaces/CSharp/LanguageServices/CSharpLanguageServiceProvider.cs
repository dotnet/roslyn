using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Services.LanguageServices;

namespace Roslyn.Services.CSharp
{
    [ExportLanguageServiceProvider(LanguageNames.CSharp)]
    internal class CSharpLanguageServiceProvider : AbstractLanguageServiceProvider
    {
        [ImportingConstructor]
        public CSharpLanguageServiceProvider(
            [ImportMany] IEnumerable<Lazy<ILanguageService, ILanguageServiceMetadata>> languageServices)
            : base(LanguageNames.CSharp, languageServices)
        {
        }
    }
}
