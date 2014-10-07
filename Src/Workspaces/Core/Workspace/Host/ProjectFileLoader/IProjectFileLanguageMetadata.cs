using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host.ProjectFileLoader
{
    internal interface IProjectFileLanguageMetadata : ILanguageMetadata
    {
        string ProjectType { get; }
        string ProjectFileExtension { get; }
    }
}