// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

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

    internal SolutionServices SolutionServices { get; }

    /// <summary>
    /// Gets a workspace specific service provided by the host identified by the service type. 
    /// If the host does not provide the service, this method returns null.
    /// </summary>
    public abstract TWorkspaceService? GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService;

    protected HostWorkspaceServices()
    {
#pragma warning disable 618 // 'HostProjectServices.HostSolutionServices(HostLanguageServices)' is obsolete: 'Do not call directly.
        SolutionServices = new SolutionServices(this);
#pragma warning restore
    }

    /// <summary>
    /// Gets a workspace specific service provided by the host identified by the service type. 
    /// If the host does not provide the service, this method throws <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The host does not provide the service.</exception>
    public TWorkspaceService GetRequiredService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
    {
        var service = GetService<TWorkspaceService>();
        if (service == null)
        {
            throw new InvalidOperationException(string.Format(WorkspacesResources.Service_of_type_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_workspace, typeof(TWorkspaceService).FullName));
        }

        return service;
    }

    /// <summary>
    /// Obsolete.  Roslyn no longer supports a mechanism to perform arbitrary persistence of data.  If such functionality
    /// is needed, consumers are responsible for providing it themselves with whatever semantics are needed.
    /// </summary>
    [Obsolete("Roslyn no longer exports a mechanism to perform persistence.", error: true)]
    public virtual IPersistentStorageService PersistentStorage
    {
        get { return this.GetRequiredService<IPersistentStorageService>(); }
    }

    /// <summary>
    /// Obsolete.  Roslyn no longer supports a mechanism to store arbitrary data in-memory.  If such functionality
    /// is needed, consumers are responsible for providing it themselves with whatever semantics are needed.
    /// </summary>
    [Obsolete("Roslyn no longer exports a mechanism to store arbitrary data in-memory.")]
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
    public virtual IEnumerable<string> SupportedLanguages => SupportedLanguagesArray;

    internal virtual ImmutableArray<string> SupportedLanguagesArray => [];

    /// <summary>
    /// Returns true if the language is supported.
    /// </summary>
    public virtual bool IsSupported(string languageName)
        => false;

    /// <summary>
    /// Gets the <see cref="HostLanguageServices"/> for the language name.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the language isn't supported.</exception>
    public virtual HostLanguageServices GetLanguageServices(string languageName)
        => throw new NotSupportedException(string.Format(WorkspacesResources.The_language_0_is_not_supported, languageName));

    public delegate bool MetadataFilter(IReadOnlyDictionary<string, object> metadata);

    /// <summary>
    /// Finds all language services of the corresponding type across all supported languages that match the filter criteria.
    /// </summary>
    public abstract IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter);
}
