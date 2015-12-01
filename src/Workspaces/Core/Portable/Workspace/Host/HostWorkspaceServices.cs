// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Per workspace services provided by the host environment.
    /// </summary>
    public abstract class HostWorkspaceServices
    {
        /// <summary>
        /// The host services this workspace services originated from.
        /// </summary>
        /// <returns></returns>
        public abstract HostServices HostServices { get; }

        /// <summary>
        /// The workspace corresponding to this workspace services instantiation
        /// </summary>
        public abstract Workspace Workspace { get; }

        /// <summary>
        /// Gets a workspace specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        public abstract TWorkspaceService GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService;

        /// <summary>
        /// Gets a workspace specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns <see cref="InvalidOperationException"/>.
        /// </summary>
        public TWorkspaceService GetRequiredService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
        {
            var service = GetService<TWorkspaceService>();
            if (service == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.WorkspaceServicesUnavailable, typeof(TWorkspaceService).FullName));
            }

            return service;
        }

        /// <summary>
        /// A service for storing information across that can be retrieved in a separate process.
        /// </summary>
        public virtual IPersistentStorageService PersistentStorage
        {
            get { return this.GetRequiredService<IPersistentStorageService>(); }
        }

        /// <summary>
        /// A service for storing information in a temporary location that only lasts for the duration of the process.
        /// </summary>
        public virtual ITemporaryStorageService TemporaryStorage
        {
            get { return this.GetRequiredService<ITemporaryStorageService>(); }
        }

        /// <summary>
        /// A factory that constructs <see cref="SourceText"/>.
        /// </summary>
        internal virtual ITextFactoryService TextFactory
        {
            get { return this.GetRequiredService<ITextFactoryService>(); }
        }

        /// <summary>
        /// A list of language names for supported language services.
        /// </summary>
        public virtual IEnumerable<string> SupportedLanguages
        {
            get { return SpecializedCollections.EmptyEnumerable<string>(); }
        }

        /// <summary>
        /// Returns true if the language is supported.
        /// </summary>
        public virtual bool IsSupported(string languageName)
        {
            return false;
        }

        /// <summary>
        /// Gets the <see cref="HostLanguageServices"/> for the language name.
        /// </summary>
        public virtual HostLanguageServices GetLanguageServices(string languageName)
        {
            throw new NotSupportedException(string.Format(WorkspacesResources.UnsupportedLanguage, languageName));
        }

        public delegate bool MetadataFilter(IReadOnlyDictionary<string, object> metadata);

        /// <summary>
        /// Finds all language services of the corresponding type across all supported languages that match the filter criteria.
        /// </summary>
        public abstract IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter);
    }
}
