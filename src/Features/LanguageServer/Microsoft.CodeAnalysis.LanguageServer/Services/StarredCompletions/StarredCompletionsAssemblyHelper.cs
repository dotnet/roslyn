// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
internal static class StarredCompletionAssemblyHelper
{
    private static AsyncLazy<CompletionProvider>? _completionProviderLazy;

    private const string CompletionsDllName = "Microsoft.VisualStudio.IntelliCode.CSharp.dll";
    private const string ALCName = "IntelliCode-ALC";
    private const string CompletionHelperClassFullName = "PythiaCSDevKit.CSDevKitCompletionHelper";
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
            return; //no location provided means it wasn't passed through from C# Dev Kit
        }
        var logger = loggerFactory.CreateLogger(typeof(StarredCompletionAssemblyHelper));
        try
        {
            var alc = AssemblyLoadContextWrapper.TryCreate(ALCName, completionsAssemblyLocation, logger);
            if (alc is null)
            {
                return;
            }

            var createCompletionProviderMethodInfo = alc.GetMethodInfo(CompletionsDllName, CompletionHelperClassFullName, CreateCompletionProviderMethodName);
            _completionProviderLazy = new AsyncLazy<CompletionProvider>(c => CreateCompletionProviderAsync(
                    createCompletionProviderMethodInfo,
                    serviceBroker,
                    completionsAssemblyLocation,
                    logger
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not initialize {nameof(StarredCompletionAssemblyHelper)}. Starred completions will not be provided.");
        }
    }

    private static bool _completionProviderTaskFailed = false;
    internal static async Task<CompletionProvider?> GetCompletionProviderAsync(CancellationToken cancellationToken)
    {
        // early exit if async lazy is not initialized or if task has previously failed
        // this prevents us from seeing errors every time we try to get completions
        if (_completionProviderLazy == null || _completionProviderTaskFailed)
        {
            return null;
        }
        var completionProviderTask = _completionProviderLazy.GetValueAsync(cancellationToken);
        if (!completionProviderTask.IsCompleted)
        {
            return null;
        }
        _completionProviderTaskFailed = completionProviderTask.IsFaulted; //note if the task faulted so we don't await it again
        return await completionProviderTask;
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
