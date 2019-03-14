using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    /// <summary>
    /// Service to provide the default content type for a language.
    /// </summary>
    internal interface IContentTypeLanguageService : ILanguageService
    {
        IContentType GetDefaultContentType();
    }
}
