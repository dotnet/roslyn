// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Composition;
using Roslyn.Utilities;
using MEF = System.ComponentModel.Composition.Hosting;

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownFeatures
    {
        private static FeaturePack pack;

        public static FeaturePack Features
        {
            get
            {
                if (pack == null)
                {
                    Interlocked.CompareExchange(ref pack, ComputePack(), null);
                }

                return pack;
            }
        }

        // find feature packs dynamically, though ideally this would bind statically when/if this is moved up a layer.
        private static FeaturePack ComputePack()
        {
            // build a MEF composition using this assembly and the known VisualBasic/CSharp workspace assemblies.
            var assemblies = new List<Assembly>();
            var thisAssembly = typeof(WellKnownFeatures).Assembly;
            assemblies.Add(thisAssembly);

            var thisAssemblyName = thisAssembly.GetName();
            var assemblyShortName = thisAssemblyName.Name;
            var assemblyVersion = thisAssemblyName.Version;
            var publicKeyToken = thisAssemblyName.GetPublicKeyToken().Aggregate(string.Empty, (s, b) => s + b.ToString("x2"));

            LoadAssembly(assemblies,
                string.Format("Microsoft.CodeAnalysis.CSharp.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));

            LoadAssembly(assemblies,
                string.Format("Microsoft.CodeAnalysis.VisualBasic.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));

            var catalogs = assemblies.Select(a => new MEF.AssemblyCatalog(a));
            var pack = new MefExportPack(catalogs);

            return pack;
        }

        private static void LoadAssembly(List<Assembly> assemblies, string assemblyName)
        {
            try
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                assemblies.Add(loadedAssembly);
            }
            catch (Exception)
            {
            }
        }
    }
}