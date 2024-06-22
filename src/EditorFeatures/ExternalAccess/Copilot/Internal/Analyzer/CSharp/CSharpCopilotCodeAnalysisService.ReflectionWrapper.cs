// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

using AnalyzeDocumentAsyncDelegateType = Func<Document, TextSpan?, string, CancellationToken, Task<ImmutableArray<Diagnostic>>>;
using GetAvailablePromptTitlesAsyncDelegateType = Func<Document, CancellationToken, Task<ImmutableArray<string>>>;
using GetCachedDiagnosticsAsyncDelegateType = Func<Document, string, CancellationToken, Task<ImmutableArray<Diagnostic>>>;
using IsAvailableAsyncDelegateType = Func<CancellationToken, Task<bool>>;
using StartRefinementSessionAsyncDelegateType = Func<Document, Document, Diagnostic?, CancellationToken, Task>;
using GetOnTheFlyDocsAsyncDelegateType = Func<string, ImmutableArray<string>, string, CancellationToken, Task<string>>;
using IsAnyExclusionAsyncDelegateType = Func<CancellationToken, Task<bool>>;

internal sealed partial class CSharpCopilotCodeAnalysisService
{
    // A temporary helper to get access to the implementation of IExternalCSharpCopilotCodeAnalysisService, until it can be MEF exported.
    private sealed class ReflectionWrapper : IExternalCSharpCopilotCodeAnalysisService
    {
        private const string CopilotRoslynDllName = "Microsoft.VisualStudio.Copilot.Roslyn, Version=0.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private const string InternalCSharpCopilotAnalyzerTypeFullName = "Microsoft.VisualStudio.Copilot.Roslyn.Analyzer.InternalCSharpCopilotAnalyzer";

        private const string IsAvailableAsyncMethodName = "IsAvailableAsync";
        private const string GetAvailablePromptTitlesAsyncMethodName = "GetAvailablePromptTitlesAsync";
        private const string AnalyzeDocumentAsyncMethodName = "AnalyzeDocumentAsync";
        private const string GetCachedDiagnosticsAsyncMethodName = "GetCachedDiagnosticsAsync";
        private const string StartRefinementSessionAsyncMethodName = "StartRefinementSessionAsync";
        private const string GetOnTheFlyDocsAsyncMethodName = "GetOnTheFlyDocsAsync";
        private const string IsAnyExclusionAsyncMethodName = "IsAnyExclusionAsync";

        // Create and cache closed delegate to ensure we use a singleton object and with better performance.
        private readonly Type? _analyzerType;
        private readonly object? _analyzerInstance;
        private readonly Lazy<IsAvailableAsyncDelegateType?> _lazyIsAvailableAsyncDelegate;
        private readonly Lazy<GetAvailablePromptTitlesAsyncDelegateType?> _lazyGetAvailablePromptTitlesAsyncDelegate;
        private readonly Lazy<AnalyzeDocumentAsyncDelegateType?> _lazyAnalyzeDocumentAsyncDelegate;
        private readonly Lazy<GetCachedDiagnosticsAsyncDelegateType?> _lazyGetCachedDiagnosticsAsyncDelegate;
        private readonly Lazy<StartRefinementSessionAsyncDelegateType?> _lazyStartRefinementSessionAsyncDelegate;
        private readonly Lazy<GetOnTheFlyDocsAsyncDelegateType?> _lazyGetOnTheFlyDocsAsyncDelegate;
        private readonly Lazy<IsAnyExclusionAsyncDelegateType?> _lazyIsAnyExclusionAsyncDelegate;

        public ReflectionWrapper(IServiceProvider serviceProvider, IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer)
        {
            try
            {
                var assembly = Assembly.Load(CopilotRoslynDllName);
                var analyzerType = assembly.GetType(InternalCSharpCopilotAnalyzerTypeFullName);
                if (analyzerType is not null)
                {
                    var analyzerInstance = Activator.CreateInstance(analyzerType, serviceProvider, brokeredServiceContainer);
                    if (analyzerInstance is not null)
                    {
                        _analyzerType = analyzerType;
                        _analyzerInstance = analyzerInstance;
                    }
                }
            }
            catch
            {
                // Catch all here since failure is expected if user has no copilot chat or an older version of it installed.
            }

            _lazyIsAvailableAsyncDelegate = new(CreateIsAvailableAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyGetAvailablePromptTitlesAsyncDelegate = new(CreateGetAvailablePromptTitlesAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyAnalyzeDocumentAsyncDelegate = new(CreateAnalyzeDocumentAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyGetCachedDiagnosticsAsyncDelegate = new(CreateGetCachedDiagnosticsAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyStartRefinementSessionAsyncDelegate = new(CreateStartRefinementSessionAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyGetOnTheFlyDocsAsyncDelegate = new(CreateGetOnTheFlyDocsAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
            _lazyIsAnyExclusionAsyncDelegate = new(CreateIsAnyExclusionAsyncDelegate, LazyThreadSafetyMode.PublicationOnly);
        }

        private T? CreateDelegate<T>(string methodName, Type[] types) where T : Delegate
        {
            try
            {
                if (_analyzerInstance is null || _analyzerType is null)
                    return null;

                if (_analyzerType.GetMethod(methodName, types) is MethodInfo methodInfo)
                    return (T)Delegate.CreateDelegate(typeof(T), _analyzerInstance, methodInfo);
            }
            catch
            {
                // Catch all here since failure is expected if user has no copilot chat or an older version of it installed
            }

            return null;
        }

        private IsAvailableAsyncDelegateType? CreateIsAvailableAsyncDelegate()
            => CreateDelegate<IsAvailableAsyncDelegateType>(IsAvailableAsyncMethodName, [typeof(CancellationToken)]);

        private GetAvailablePromptTitlesAsyncDelegateType? CreateGetAvailablePromptTitlesAsyncDelegate()
            => CreateDelegate<GetAvailablePromptTitlesAsyncDelegateType>(GetAvailablePromptTitlesAsyncMethodName, [typeof(Document), typeof(CancellationToken)]);

        private AnalyzeDocumentAsyncDelegateType? CreateAnalyzeDocumentAsyncDelegate()
            => CreateDelegate<AnalyzeDocumentAsyncDelegateType>(AnalyzeDocumentAsyncMethodName, [typeof(Document), typeof(TextSpan?), typeof(string), typeof(CancellationToken)]);

        private GetCachedDiagnosticsAsyncDelegateType? CreateGetCachedDiagnosticsAsyncDelegate()
            => CreateDelegate<GetCachedDiagnosticsAsyncDelegateType>(GetCachedDiagnosticsAsyncMethodName, [typeof(Document), typeof(string), typeof(CancellationToken)]);

        private StartRefinementSessionAsyncDelegateType? CreateStartRefinementSessionAsyncDelegate()
            => CreateDelegate<StartRefinementSessionAsyncDelegateType>(StartRefinementSessionAsyncMethodName, [typeof(Document), typeof(Document), typeof(Diagnostic), typeof(CancellationToken)]);

        private GetOnTheFlyDocsAsyncDelegateType? CreateGetOnTheFlyDocsAsyncDelegate()
            => CreateDelegate<GetOnTheFlyDocsAsyncDelegateType>(GetOnTheFlyDocsAsyncMethodName, [typeof(string), typeof(ImmutableArray<string>), typeof(string), typeof(CancellationToken)]);

        private IsAnyExclusionAsyncDelegateType? CreateIsAnyExclusionAsyncDelegate()
            => CreateDelegate<IsAnyExclusionAsyncDelegateType>(IsAnyExclusionAsyncMethodName, [typeof(CancellationToken)]);

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            if (_lazyIsAvailableAsyncDelegate.Value is null)
                return false;

            return await _lazyIsAvailableAsyncDelegate.Value(cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
        {
            if (_lazyGetAvailablePromptTitlesAsyncDelegate.Value is null)
                return [];

            return await _lazyGetAvailablePromptTitlesAsyncDelegate.Value(document, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<Diagnostic>> AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
        {
            if (_lazyAnalyzeDocumentAsyncDelegate.Value is null)
                return [];

            return await _lazyAnalyzeDocumentAsyncDelegate.Value(document, span, promptTitle, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsAsync(Document document, string promptTitle, CancellationToken cancellationToken)
        {
            if (_lazyGetCachedDiagnosticsAsyncDelegate.Value is null)
                return [];

            return await _lazyGetCachedDiagnosticsAsyncDelegate.Value(document, promptTitle, cancellationToken).ConfigureAwait(false);
        }

        public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
        {
            if (_lazyStartRefinementSessionAsyncDelegate.Value is null)
                return Task.CompletedTask;

            return _lazyStartRefinementSessionAsyncDelegate.Value(oldDocument, newDocument, primaryDiagnostic, cancellationToken);
        }

        public async Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
        {
            if (_lazyGetOnTheFlyDocsAsyncDelegate.Value is null)
                return string.Empty;

            return await _lazyGetOnTheFlyDocsAsyncDelegate.Value(symbolSignature, declarationCode, language, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsAnyExclusionAsync(CancellationToken cancellationToken)
        {
            if (_lazyIsAnyExclusionAsyncDelegate.Value is null)
                return false;

            return await _lazyIsAnyExclusionAsyncDelegate.Value(cancellationToken).ConfigureAwait(false);
        }
    }
}
