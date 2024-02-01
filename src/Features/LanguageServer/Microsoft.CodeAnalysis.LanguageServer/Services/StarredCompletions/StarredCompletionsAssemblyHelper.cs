// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
internal static class StarredCompletionAssemblyHelper
{
    private const string CompletionsDllName = "Microsoft.VisualStudio.IntelliCode.CSharp.dll";
    private const string CompletionHelperClassFullName = "PythiaCSDevKit.CSDevKitCompletionHelper";
    private const string CreateCompletionProviderMethodName = "CreateCompletionProviderAsync";

    // The following fields are only set as a part of the call to InitializeInstance, which is only called once for the lifetime of the process. Thus, it is safe to assume that once
    // set, they will never change again.
    private static string? s_completionsAssemblyLocation;
    private static ILogger? s_logger;
    private static ServiceBrokerFactory? s_serviceBrokerFactory;
    private static ExtensionAssemblyManager? s_extensionAssemblyManager;

    /// <summary>
    /// A gate to guard the actual creation of <see cref="s_completionProvider"/>. This just prevents us from trying to create the provider more than once; once the field is set it
    /// won't change again.
    /// </summary>
    private static readonly SemaphoreSlim s_gate = new SemaphoreSlim(initialCount: 1);
    private static bool s_previousCreationFailed = false;
    private static CompletionProvider? s_completionProvider;

    /// <summary>
    /// Initializes CompletionsAssemblyHelper singleton
    /// </summary>
    /// <param name="completionsAssemblyLocation">Location of dll for starred completion</param>
    /// <param name="loggerFactory">Factory for creating new logger</param>
    /// <param name="serviceBrokerFactory">Service broker with access to necessary remote services</param>
    internal static void InitializeInstance(string? completionsAssemblyLocation, ExtensionAssemblyManager extensionAssemblyManager, ILoggerFactory loggerFactory, ServiceBrokerFactory serviceBrokerFactory)
    {
        // No location provided means it wasn't passed through from C# Dev Kit, so we don't need to initialize anything further
        if (string.IsNullOrEmpty(completionsAssemblyLocation))
        {
            return;
        }

        // C# Dev Kit must be installed, so we should be able to provide this; however we may not yet have a connection to the Dev Kit service broker, so we need to defer the actual creation
        // until that point.
        s_completionsAssemblyLocation = completionsAssemblyLocation;
        s_logger = loggerFactory.CreateLogger(typeof(StarredCompletionAssemblyHelper));
        s_serviceBrokerFactory = serviceBrokerFactory;
        s_extensionAssemblyManager = extensionAssemblyManager;
    }

    internal static string GetStarredCompletionAssemblyPath(string starredCompletionComponentPath)
    {
        return Path.Combine(starredCompletionComponentPath, CompletionsDllName);
    }

    internal static async Task<CompletionProvider?> GetCompletionProviderAsync(CancellationToken cancellationToken)
    {
        // Short cut: if we already have a provider, return it
        if (s_completionProvider is CompletionProvider completionProvider)
            return completionProvider;

        // If we don't have one because we previously failed to create one, then just return failure
        if (s_previousCreationFailed)
            return null;

        // If we were never initialized with any information from Dev Kit, we can't create one
        if (s_completionsAssemblyLocation is null
            || s_logger is null
            || s_serviceBrokerFactory is null
            || s_extensionAssemblyManager is null)
        {
            return null;
        }

        // If we don't have a connection to a service broker yet, we also can't create one
        var serviceBroker = s_serviceBrokerFactory.TryGetFullAccessServiceBroker();
        if (serviceBroker is null)
            return null;

        // At this point, we have everything we need to go and create the provider, so let's do it
        using (await s_gate.DisposableWaitAsync(cancellationToken))
        {
            // Re-check this inside the lock, since we could have had a success between the earlier check and now
            if (s_completionProvider is CompletionProvider completionProviderInsideLock)
                return completionProviderInsideLock;

            // Re-check this inside the lock, since we could have had a failure between the earlier check and now
            if (s_previousCreationFailed)
                return null;

            try
            {
                var completionsDllPath = GetStarredCompletionAssemblyPath(s_completionsAssemblyLocation);
                s_logger.LogTrace("trying to load intellicode provider");
                var starredCompletionsAssembly = s_extensionAssemblyManager.TryLoadAssemblyInExtensionContext(completionsDllPath);
                if (starredCompletionsAssembly is null)
                {
                    s_logger.LogTrace("failed to load intellicode provider");
                    s_previousCreationFailed = true;
                    return null;
                }

                var createCompletionProviderMethodInfo = GetMethodInfo(starredCompletionsAssembly, CompletionHelperClassFullName, CreateCompletionProviderMethodName);

                s_completionProvider = await CreateCompletionProviderAsync(createCompletionProviderMethodInfo, serviceBroker, s_completionsAssemblyLocation, s_logger);
                return s_completionProvider;
            }
            catch (Exception ex)
            {
                s_previousCreationFailed = true;
                s_logger.LogError(ex, "Unable to create the StarredCompletionProvider.");
                throw;
            }
        }
    }

    private static MethodInfo GetMethodInfo(Assembly assembly, string className, string methodName)
    {
        var completionHelperType = assembly.GetType(className);
        if (completionHelperType == null)
        {
            throw new ArgumentException($"{assembly.FullName} assembly did not contain {className} class");
        }
        var createCompletionProviderMethodInto = completionHelperType?.GetMethod(methodName);
        if (createCompletionProviderMethodInto == null)
        {
            throw new ArgumentException($"{className} from {assembly.FullName} assembly did not contain {methodName} method");
        }
        return createCompletionProviderMethodInto;
    }

    private static async Task<CompletionProvider> CreateCompletionProviderAsync(MethodInfo createCompletionProviderMethodInfo, IServiceBroker serviceBroker, string modelBasePath, ILogger logger)
    {
        var completionProviderObj = createCompletionProviderMethodInfo.Invoke(null, new object[4] { serviceBroker, BrokeredServices.Services.Descriptors.RemoteModelService, modelBasePath, logger });
        if (completionProviderObj == null)
        {
            throw new NotSupportedException($"{createCompletionProviderMethodInfo.Name} method could not be invoked");
        }
        var completionProvider = (Task<CompletionProvider>)completionProviderObj;
        return await completionProvider;
    }
}
