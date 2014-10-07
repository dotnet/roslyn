using System.Collections.Generic;

namespace Roslyn.Workspaces.LanguageServices
{
    public interface ILanguagesMetadata
    {
        IEnumerable<string> Languages { get; }
    }
}