// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
#if MEF
using System.ComponentModel.Composition;
#endif
using System.Linq;
using Microsoft.CodeAnalysis.Composition;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
#if MEF
    [Export(typeof(IWorkspaceServiceProviderFactory))]
    internal class WorkspaceServiceProviderFactory : IWorkspaceServiceProviderFactory, IPartImportsSatisfiedNotification
#else
    internal class WorkspaceServiceProviderFactory : IWorkspaceServiceProviderFactory
#endif
    {
#if MEF
        [ImportMany]
#endif
        private IEnumerable<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>> workspaceServiceFactories = null;

#if MEF
        [ImportMany]
#endif
        private IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> workspaceServices = null;

        private Lazy<ImmutableList<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>>> unboundServices;

        private ExportSource exports;

        internal ExportSource Exports
        {
            get { return this.exports; }
        }

        internal void SetExports(ExportSource exports)
        {
            this.exports = exports;
        }

        public WorkspaceServiceProviderFactory()
        {
        }

#if MEF
        public void OnImportsSatisfied()
        {
            this.unboundServices = new Lazy<ImmutableList<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>>>(() =>
                    workspaceServices.Select(ws => new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(ws.Metadata, (wsp) => ws.Value))
                    .Concat(workspaceServiceFactories.Select(wsf => new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(wsf.Metadata, (wsp) => wsf.Value.CreateService(wsp)))).ToImmutableList());
        }
#else

        public WorkspaceServiceProviderFactory(ExportSource exports)
        {
            this.Exports = exports;
            this.workspaceServiceFactories = exports.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>();
            this.workspaceServices = exports.GetExports<IWorkspaceService, WorkspaceServiceMetadata>();

            this.unboundServices = new Lazy<ImmutableList<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>>>(() =>
                    workspaceServices.Select(ws => new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(ws.Metadata, (wsp) => ws.Value))
                    .Concat(workspaceServiceFactories.Select(wsf => new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(wsf.Metadata, (wsp) => wsf.Value.CreateService(wsp)))).ToImmutableList());
        }
#endif

        public IWorkspaceServiceProvider CreateWorkspaceServiceProvider(string workspaceKind)
        {
            return new WorkspaceServiceProvider(this, workspaceKind, GetUnboundServices(workspaceKind));
        }

        /// <summary>
        /// A map of WorkspaceKind -> (map of ServiceTypeAssemblyQualifiedName -> (function of IWorkspaceServiceProvider -> the service itself))
        /// </summary>
        private readonly ConcurrentDictionary<string, ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>>> unboundServicesByKindMap
            = new ConcurrentDictionary<string, ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>>>();

        private ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>> GetUnboundServices(string workspaceKind)
        {
            return this.unboundServicesByKindMap.GetOrAdd(workspaceKind, this.BuildServiceMap);
        }

        private ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>> BuildServiceMap(string workspaceKind)
        {
            return ImmutableDictionary.CreateRange<string, Func<IWorkspaceServiceProvider, IWorkspaceService>>(
                        this.unboundServices.Value.ToLookup(ws => ws.Key.ServiceTypeAssemblyQualifiedName)
                                .Select(grp =>
                                {
                                    var best = PickBestService(workspaceKind, grp);
                                    return new KeyValuePair<string, Func<IWorkspaceServiceProvider, IWorkspaceService>>(best.Key.ServiceTypeAssemblyQualifiedName, best.Value);
                                })
                                .Where(kvp => kvp.Value != null));
        }

        private static KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>> PickBestService(
            string workspaceKind, IEnumerable<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>> services)
        {
            var kind = workspaceKind == WorkspaceKind.Any ? WorkspaceKind.Host : workspaceKind;

            // first try exact match
            var kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == kind);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // host services override editor or default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Host);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // editor services override default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Editor);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // services marked as any are default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Any);
            if (kvp.Value != null)
            {
                return kvp;
            }

            return services.SingleOrDefault();
        }
    }
}