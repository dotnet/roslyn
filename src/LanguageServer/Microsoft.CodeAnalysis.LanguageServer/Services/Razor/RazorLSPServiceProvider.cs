// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.Razor;

internal static class RazorLSPServiceProvider
{
    /// <summary>
    /// A gate to guard the actual creation of <see cref="s_razorServiceProvider"/>. This just prevents us from trying to create the provider more than once; once the field is set it
    /// won't change again.
    /// </summary>
    private static readonly SemaphoreSlim s_gate = new SemaphoreSlim(initialCount: 1);

    /// <summary>
    /// Null until an attempt to load the type is made, and then true if successful else false
    /// </summary>
    private static bool? s_loadStatus = null;

    /// <summary>
    /// The instance loaded from the assembly
    /// </summary>
    private static IRazorServiceProvider? s_razorServiceProvider;

    private static ExtensionAssemblyManager? s_extensionAssemblyManager;
    private static string? s_razorAssemblyLocation;

    internal static void Initialize(ExtensionAssemblyManager extensionAssemblyManager, string? razorAssemblyLocation)
    {
        if (string.IsNullOrEmpty(razorAssemblyLocation))
        {
            return;
        }

        s_extensionAssemblyManager = extensionAssemblyManager;
        s_razorAssemblyLocation = razorAssemblyLocation;
    }

    internal static Task<T?> TryGetServiceAsync<T>(CancellationToken cancellationToken)
    {
        if (s_razorServiceProvider is not null)
        {
            return Task.FromResult(s_razorServiceProvider.TryGetService<T>());
        }

        if (s_loadStatus is not true)
        {
            return Task.FromResult<T?>(default);
        }

        return TryInitializeAndGetCoreAsync<T>(cancellationToken);
    }

    internal static async Task<T> GetRequiredServiceAsync<T>(CancellationToken cancellationToken)
    {
        var service = await TryGetServiceAsync<T>(cancellationToken).ConfigureAwait(false);
        if (s_loadStatus is false)
        {
            throw new InvalidOperationException("Failed to load service provider to get service");
        }

        if (service is null)
        {
            throw new InvalidOperationException($"Unable to get service of type {typeof(T)}");
        }

        return service;
    }

    private static async Task<T?> TryInitializeAndGetCoreAsync<T>(CancellationToken cancellationToken)
    {
        if (s_loadStatus is not null)
        {
            return s_razorServiceProvider is null
                ? default
                : s_razorServiceProvider.TryGetService<T>();
        }

        using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (s_loadStatus is not null)
            {
                return s_razorServiceProvider is null
                    ? default
                    : s_razorServiceProvider.TryGetService<T>();
            }

            s_loadStatus = false;
            s_razorServiceProvider = TryCreateInitializer();
            s_loadStatus = s_razorServiceProvider is not null;
            return s_razorServiceProvider is null
                ? default
                : s_razorServiceProvider.TryGetService<T>();
        }
    }

    private static IRazorServiceProvider? TryCreateInitializer()
    {
        if (s_extensionAssemblyManager is null ||
            s_razorAssemblyLocation is null)
        {
            return null;
        }

        var assembly = s_extensionAssemblyManager.TryLoadAssemblyInExtensionContext(s_razorAssemblyLocation);
        if (assembly is null)
        {
            return null;
        }

        var initializer = assembly.GetTypes().FirstOrDefault(t => typeof(IRazorServiceProvider).IsAssignableFrom(t));
        if (initializer is null)
        {
            return null;
        }

        Debug.Assert(initializer.GetConstructor(Array.Empty<Type>()) is not null);
        return (IRazorServiceProvider?)Activator.CreateInstance(initializer);
    }
}
