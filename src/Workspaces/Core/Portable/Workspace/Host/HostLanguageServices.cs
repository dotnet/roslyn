// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Per language services provided by the host environment.
    /// </summary>
    public abstract class HostLanguageServices
    {
        /// <summary>
        /// The <see cref="HostWorkspaceServices"/> that originated this language service.
        /// </summary>
        public abstract HostWorkspaceServices WorkspaceServices { get; }

        /// <summary>
        /// The name of the language
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets a language specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        [return: MaybeNull]
        public abstract TLanguageService GetService<TLanguageService>() where TLanguageService : ILanguageService;

        /// <summary>
        /// Gets a language specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns throws <see cref="InvalidOperationException"/>.
        /// </summary>
        [return: NotNull]
        public TLanguageService GetRequiredService<TLanguageService>() where TLanguageService : ILanguageService
        {
            var service = GetService<TLanguageService>()!;
            if (service == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.Service_of_type_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_workspace, typeof(TLanguageService)));
            }

            return service;
        }

        // common services

        /// <summary>
        /// A factory for creating compilations instances.
        /// </summary>
        internal virtual ICompilationFactoryService? CompilationFactory
        {
            get { return this.GetService<ICompilationFactoryService>(); }
        }

        // needs some work on the interface before it can be public
        internal virtual ISyntaxTreeFactoryService? SyntaxTreeFactory
        {
            get { return this.GetService<ISyntaxTreeFactoryService>(); }
        }
    }
}
