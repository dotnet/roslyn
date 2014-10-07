using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface IFormattingService : ILanguageService
    {
        /// <summary>
        /// Returns the text changes necessary to format the document.  If "textSpan" is provided,
        /// only the text changes necessary to format that span are needed.
        /// </summary>
        Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, CancellationToken cancellationToken);
    }
}
