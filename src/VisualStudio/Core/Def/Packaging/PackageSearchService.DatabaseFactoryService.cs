using System.IO;
using Elfie.Model;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class DatabaseFactoryService : IPackageSearchDatabaseFactoryService
        {
            public AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
            {
                using (var memoryStream = new MemoryStream(bytes))
                using (var streamReader = new StreamReader(memoryStream))
                {
                    var database = new AddReferenceDatabase();
                    database.ReadText(streamReader);
                    return database;
                }
            }
        }
    }
}
