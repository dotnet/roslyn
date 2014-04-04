// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UnitTests.Host.WorkspaceServices.Caching;
using Microsoft.CodeAnalysis.UnitTests.Persistence;
using Microsoft.CodeAnalysis.UnitTests.TemporaryStorage;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal class TestWorkspaceServiceProvider : IWorkspaceServiceProvider
    {
        private List<IWorkspaceService> services = new List<IWorkspaceService>();

        public TestWorkspaceServiceProvider()
        {
            var workspaceTaskSchedulerFactory = new WorkspaceTaskSchedulerFactory();
            var persistenceService = new TestPersistenceService();
            var languageServiceProviderFactory = new LanguageServiceProviderFactory(this, Features.All.LanguageServices);
            var syntaxTreeCacheService = new TestSyntaxTreeCacheService();
            var temporaryStorageService = new TestTemporaryStorageService();
            var textFactoryService = new TextFactoryServiceFactory.TextFactoryService();
            var metadataFileProviderService = new MetadataReferenceProviderServiceFactory().CreateService(this);

            Add(workspaceTaskSchedulerFactory,
                persistenceService,
                languageServiceProviderFactory,
                syntaxTreeCacheService,
                temporaryStorageService,
                textFactoryService,
                metadataFileProviderService);
        }

        public string Kind
        {
            get { return "Test"; }
        }

        public IWorkspaceServiceProviderFactory Factory
        {
            get { throw new NotImplementedException(); }
        }

        public void Add(params IWorkspaceService[] workspaceServices)
        {
            services.AddRange(workspaceServices);
        }

        public void ReplaceService<TWorkspaceService>(TWorkspaceService replacement)
            where TWorkspaceService : IWorkspaceService
        {
            services.RemoveAll(s => s is TWorkspaceService);
            Add(replacement);
        }

        public TWorkspaceService GetService<TWorkspaceService>()
            where TWorkspaceService : IWorkspaceService
        {
            return services.OfType<TWorkspaceService>().SingleOrDefault();
        }

#if !MEF
        public IEnumerable<Lazy<T>> GetServiceExtensions<T>() where T : class
        {
            return SpecializedCollections.EmptyEnumerable<Lazy<T>>();
        }

        public IEnumerable<Lazy<T, M>> GetServiceExtensions<T, M>() where T : class
        {
            return SpecializedCollections.EmptyEnumerable<Lazy<T, M>>();
        }
#endif
    }
}
