using Elfie.Model;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal interface IPackageSearchDatabaseFactoryService
    {
        AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes);
    }
}
