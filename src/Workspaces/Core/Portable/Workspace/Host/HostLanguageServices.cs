// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Host;

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
    /// Immutable snapshot of the host services.  Preferable to use instead of this <see
    /// cref="HostLanguageServices"/> when possible.
    /// </summary>
    public LanguageServices LanguageServices { get; }

    protected HostLanguageServices()
    {
#pragma warning disable 618 // 'HostProjectServices.HostProjectServices(HostLanguageServices)' is obsolete: 'Do not call directly.
        LanguageServices = new LanguageServices(this);
#pragma warning restore
    }

    /// <summary>
    /// Gets a language specific service provided by the host identified by the service type. 
    /// If the host does not provide the service, this method returns null.
    /// </summary>
    public abstract TLanguageService? GetService<TLanguageService>() where TLanguageService : ILanguageService;

    /// <summary>
    /// Gets a language specific service provided by the host identified by the service type. 
    /// If the host does not provide the service, this method returns throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public TLanguageService GetRequiredService<TLanguageService>() where TLanguageService : ILanguageService
    {
        var service = GetService<TLanguageService>();
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
