using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public class NoHostComposition
    {
        private static readonly CompositionContainer composition;

        static NoHostComposition()
        {
            composition = CreateComposition();
        }

        public static CompositionContainer Composition
        {
            get
            {
                return composition;
            }
        }

        private static CompositionContainer CreateComposition()
        {
            return new CompositionContainer(CreateCatalog(), isThreadSafe: true);
        }

        private static ComposablePartCatalog CreateCatalog()
        {
            var catalogs = new List<ComposablePartCatalog>();

            // Build up assembly info based on our own assembly, since we use different info for
            // dev builds, official builds, builds for partners, etc.
            var thisAssemblyName = typeof(NoHostComposition).Assembly.GetName();
            var assemblyShortName = thisAssemblyName.Name;
            var assemblyVersion = thisAssemblyName.Version;
            var publicKeyToken = thisAssemblyName.GetPublicKeyToken().Aggregate(string.Empty, (s, b) => s + b.ToString("x2"));

            // add known Roslyn assemblies
            AddAssembly(catalogs, string.Format("{0}, Version={1}, Culture=neutral, PublicKeyToken={2}", assemblyShortName, assemblyVersion, publicKeyToken));

#if CODESENSE
            // Ugly hack. For the moment, we rename the binaries we produce for CodeSense.
            // When they start using the real Roslyn binaries, we can remove this.
            AddAssemblyIfExists(catalogs, string.Format("Microsoft.Alm.Services.CSharp, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));
            AddAssemblyIfExists(catalogs, string.Format("Microsoft.Alm.Services.VisualBasic, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));
#else
            AddAssemblyIfExists(catalogs, string.Format("Microsoft.CodeAnalysis.CSharp.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));
            AddAssemblyIfExists(catalogs, string.Format("Microsoft.CodeAnalysis.VisualBasic.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));
#endif

            return new AggregateCatalog(catalogs);
        }

        private static void AddAssemblyIfExists(List<ComposablePartCatalog> catalogs, string assemblyName)
        {
            try
            {
                AddAssembly(catalogs, assemblyName);
            }
            catch (Exception)
            {
            }
        }

        private static void AddAssembly(List<ComposablePartCatalog> catalogs, string assemblyName)
        {
            var loadedAssembly = Assembly.Load(assemblyName);
            var catalog = new AssemblyCatalog(loadedAssembly);
            catalogs.Add(catalog);
        }

        internal static AggregateCatalog GetDefaultCatalogWithTestOverrides(ComposablePartCatalog catalog)
        {
            return new AggregateCatalog(catalog, NoHostComposition.Composition.Catalog);
        }
    }
}
