// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
internal class StarredCompletionAssemblyHelper
{
    internal static StarredCompletionAssemblyHelper? Instance { get; private set; }

    internal readonly AsyncLazy<CompletionProvider> _completionProviderLazy;
    internal readonly ILogger _logger;

    private const string CompletionsDllName = "Microsoft.VisualStudio.IntelliCode.CSharp.dll";
    private const string ALCName = "IntelliCode-ALC";
    private const string CompletionHelperClassFullName = "PythiaVSGreen.VSGreenCompletionHelper";
    private const string CreateCompletionProviderMethodName = "CreateCompletionProviderAsync";

    /// <summary>
    /// Initializes CompletionsAssemblyHelper singleton
    /// </summary>
    /// <param name="completionsAssemblyLocation">Location of dll for starred completion</param>
    /// <param name="loggerFactory">Factory for creating new logger</param>
    /// <param name="serviceBroker">Service broker with access to necessary remote services</param>
    internal static void InitializeInstance(string? completionsAssemblyLocation, ILoggerFactory loggerFactory, IServiceBroker serviceBroker)
    {
        if (string.IsNullOrEmpty(completionsAssemblyLocation))
        {
            return; //no location provided means it wasn't passed through from green
        }
        var logger = loggerFactory.CreateLogger<StarredCompletionAssemblyHelper>();
        try
        {
            var starredCompletionsALC = new AssemblyLoadContext(ALCName);
            var starredCompletionsAssembly = LoadSuggestionsAssemblyAndDependencies(starredCompletionsALC, completionsAssemblyLocation, logger);
            var createCompletionProviderMethodInfo = GetMethodInfo(starredCompletionsAssembly, CompletionHelperClassFullName, CreateCompletionProviderMethodName);
            var completionProviderLazy = new AsyncLazy<CompletionProvider>(c => CreateCompletionProviderAsync(
                    createCompletionProviderMethodInfo,
                    serviceBroker,
                    completionsAssemblyLocation,
                    logger
                ), cacheResult: true);
            Instance = new StarredCompletionAssemblyHelper(completionProviderLazy, logger);
        }
        catch (Exception ex)
        {
            logger.LogError($"Could not initialize {nameof(StarredCompletionAssemblyHelper)}. Starred completions will not be provided. Error: {ex}");
        }
    }

    internal static async Task<CompletionProvider?> GetCompletionProviderAsync(CancellationToken cancellationToken)
    {
        if (Instance != null)
        {
            try
            {
                return await Instance._completionProviderLazy.GetValueAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Instance._logger.LogError($"Could not initialize {nameof(CompletionProvider)}. Starred completions will not be provided. Error: {ex}");
            }
        }
        return null;
    }

    /// <summary>
    /// Constructor is private as class should only be initialized via static <see cref="InitializeInstance"/> method
    /// </summary>
    private StarredCompletionAssemblyHelper(AsyncLazy<CompletionProvider> completionProviderLazy, ILogger logger)
    {
        _completionProviderLazy = completionProviderLazy;
        _logger = logger;
    }

    private static Assembly LoadSuggestionsAssemblyAndDependencies(AssemblyLoadContext alc, string assemblyLocation, ILogger logger)
    {
        Assembly? starredSuggestionsAssembly = null;
        var directory = new DirectoryInfo(assemblyLocation);
        foreach (var file in directory.EnumerateFiles("*.dll"))
        {
            var assembly = alc.LoadFromAssemblyPath(file.FullName);
            if (file.Name == CompletionsDllName)
            {
                starredSuggestionsAssembly = assembly;
            }
        }
        if (starredSuggestionsAssembly == null)
        {
            throw new FileNotFoundException($"Required assembly {CompletionsDllName} could not be found");
        }
        return starredSuggestionsAssembly;
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
        var completionProviderObj = createCompletionProviderMethodInfo.Invoke(null, new object[4] { serviceBroker, Descriptors.RemoteModelService, modelBasePath, logger });
        if (completionProviderObj == null)
        {
            throw new NotSupportedException($"{createCompletionProviderMethodInfo.Name} method could not be invoked");
        }
        if (completionProviderObj.GetType().BaseType != typeof(Task<CompletionProvider>))
        {
            throw new NotSupportedException($"{createCompletionProviderMethodInfo.Name} method did not return object of type {nameof(Task<CompletionProvider>)}");
        }
        var completionProvider = (Task<CompletionProvider>)completionProviderObj;
        return await completionProvider;
    }
}
