// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TestComposition
    {
        public readonly ImmutableHashSet<Assembly> Assemblies;
        public readonly ImmutableHashSet<Type> Types;

        private readonly bool _vsMef;
        private readonly Lazy<ContainerConfiguration> _lazyContainerConfiguration;
        private readonly Lazy<IExportProviderFactory> _exportProviderFactory;

        internal TestComposition(ImmutableHashSet<Assembly> assemblies, ImmutableHashSet<Type> types, bool vsMef)
        {
            Assemblies = assemblies;
            Types = types;

            _vsMef = vsMef;
            _lazyContainerConfiguration = new Lazy<ContainerConfiguration>(() => new ContainerConfiguration().WithAssemblies(Assemblies).WithParts(Types));
            _exportProviderFactory = new Lazy<IExportProviderFactory>(() => ExportProviderCache.GetOrCreateExportProviderFactory(GetCatalog()));
        }

        /// <summary>
        /// Returns a new instance of <see cref="HostServices"/> for the composition. This will either be a MEF composition or VS MEF composition host, 
        /// depending on what layer the composition is for. Editor Features and VS layers use VS MEF composition while anything else uses System.Composition.
        /// </summary>
        public HostServices GetHostServices()
            => _vsMef ? GetVisualStudioHostServices() : (HostServices)new MefHostServices(_lazyContainerConfiguration.Value.CreateContainer());

        internal VisualStudioMefHostServices GetVisualStudioHostServices()
            => VisualStudioMefHostServices.Create(ExportProviderFactory.CreateExportProvider());

        /// <summary>
        /// VS MEF <see cref="ExportProvider"/>.
        /// </summary>
        public IExportProviderFactory ExportProviderFactory => _exportProviderFactory.Value;

        private ComposableCatalog GetCatalog()
            => ExportProviderCache.GetOrCreateAssemblyCatalog(Assemblies, ExportProviderCache.CreateResolver()).WithParts(Types);

        public TestComposition WithAdditionalParts(params Type[]? types)
            => WithAdditionalParts(Array.Empty<Assembly>(), types ?? Array.Empty<Type>(), _vsMef);

        public TestComposition WithAdditionalParts(IEnumerable<Assembly> assemblies, IEnumerable<Type> types, bool vsMef = false)
        {
            var newAssemblies = Assemblies;
            foreach (var assembly in assemblies)
            {
                newAssemblies = newAssemblies.Add(assembly);
            }

            var newTypes = Types;
            foreach (var type in types)
            {
                newTypes = newTypes.Add(type);
            }

            if (newAssemblies == Assemblies && newTypes == Types && _vsMef == vsMef)
            {
                return this;
            }

            return new TestComposition(newAssemblies, newTypes, vsMef);
        }

        /// <summary>
        /// Use for VS MEF composition troubleshooting.
        /// </summary>
        /// <returns>All composition error messages.</returns>
        internal string GetCompositionErrorLog()
        {
            var configuration = CompositionConfiguration.Create(GetCatalog());

            var sb = new StringBuilder();
            foreach (var errorGroup in configuration.CompositionErrors)
            {
                foreach (var error in errorGroup)
                {
                    sb.Append(error.Message);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
