using System.Collections.Generic;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Rename
{
    internal interface IRenamePossibleNameConflictsLanguageService : ILanguageService
    {
         void TryAddPossibleNameConflicts(ISymbol symbol, string replacementText, List<string> possibleNameConflicts);
    }
}
