// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    internal class WorkspaceServiceProvider : IWorkspaceServiceProvider
    {
        private readonly WorkspaceServiceProviderFactory factory;
        private readonly string workspaceKind;
        private readonly ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>> unboundServiceMap;
        private readonly ConcurrentDictionary<Type, IWorkspaceService> boundServiceMap = new ConcurrentDictionary<Type, IWorkspaceService>();

        public WorkspaceServiceProvider(WorkspaceServiceProviderFactory factory, string workspaceKind, ImmutableDictionary<string, Func<IWorkspaceServiceProvider, IWorkspaceService>> unboundServiceMap)
        {
            this.factory = factory;
            this.workspaceKind = workspaceKind;
            this.unboundServiceMap = unboundServiceMap;
            this.constructService = this.ConstructService;
        }

        private readonly Func<Type, IWorkspaceService> constructService;

        private IWorkspaceService ConstructService(Type type)
        {
            Func<IWorkspaceServiceProvider, IWorkspaceService> serviceBinder;
            if (this.unboundServiceMap.TryGetValue(type.AssemblyQualifiedName, out serviceBinder))
            {
                return serviceBinder(this);
            }

            // no such service for this type
            return null;
        }

        public string Kind
        {
            get { return this.workspaceKind; }
        }

        public IWorkspaceServiceProviderFactory Factory
        {
            get { return this.factory; }
        }

        public TWorkspaceService GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
        {
            return (TWorkspaceService)this.boundServiceMap.GetOrAdd(typeof(TWorkspaceService), this.constructService);
        }

        public IEnumerable<Lazy<T>> GetServiceExtensions<T>() where T : class
        {
            return this.factory.Exports.GetExports<T>();
        }

        public IEnumerable<Lazy<T, M>> GetServiceExtensions<T, M>() where T : class
        {
            return this.factory.Exports.GetExports<T, M>();
        }
    }
}