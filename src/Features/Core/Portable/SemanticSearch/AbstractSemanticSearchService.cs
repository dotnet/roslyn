// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal abstract partial class AbstractSemanticSearchService : ISemanticSearchService
{
    internal sealed class LoadContext() : AssemblyLoadContext("SemanticSearchLoadContext", isCollectible: true)
    {
        private readonly AssemblyLoadContext _current = GetLoadContext(typeof(LoadContext).Assembly)!;

        protected override Assembly? Load(AssemblyName assemblyName)
            => _current.LoadFromAssemblyName(assemblyName);

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            => IntPtr.Zero;
    }

    /// <summary>
    /// Mapping from the parameter type of the <c>Find</c> method to the <see cref="QueryKind"/> value.
    /// </summary>
    private static readonly ImmutableDictionary<Type, QueryKind> s_queryKindByParameterType = ImmutableDictionary<Type, QueryKind>.Empty
        .Add(typeof(Compilation), QueryKind.Compilation)
        .Add(typeof(INamespaceSymbol), QueryKind.Namespace)
        .Add(typeof(INamedTypeSymbol), QueryKind.NamedType)
        .Add(typeof(IMethodSymbol), QueryKind.Method)
        .Add(typeof(IFieldSymbol), QueryKind.Field)
        .Add(typeof(IPropertySymbol), QueryKind.Property)
        .Add(typeof(IEventSymbol), QueryKind.Event);

    protected abstract Compilation CreateCompilation(SourceText query, IEnumerable<MetadataReference> references, SolutionServices services, out SyntaxTree queryTree, CancellationToken cancellationToken);

    public async Task<ExecuteQueryResult> ExecuteQueryAsync(
        Solution solution,
        string query,
        string referenceAssembliesDir,
        ISemanticSearchResultsObserver observer,
        OptionsProvider<ClassificationOptions> classificationOptions,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        try
        {
            // add progress items - one for compilation, one for emit and one for each project:
            var remainingProgressItemCount = 2 + solution.ProjectIds.Count;
            await observer.AddItemsAsync(remainingProgressItemCount, cancellationToken).ConfigureAwait(false);

            var metadataService = solution.Services.GetRequiredService<IMetadataService>();
            var metadataReferences = SemanticSearchUtilities.GetMetadataReferences(metadataService, referenceAssembliesDir);
            var queryText = SemanticSearchUtilities.CreateSourceText(query);
            var queryCompilation = CreateCompilation(queryText, metadataReferences, solution.Services, out var queryTree, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // complete compilation progress item:
            remainingProgressItemCount--;
            await observer.ItemsCompletedAsync(1, cancellationToken).ConfigureAwait(false);

            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                instrumentationKinds: [InstrumentationKind.StackOverflowProbing, InstrumentationKind.ModuleCancellation]);

            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();

            var emitDifferenceTimer = SharedStopwatch.StartNew();
            var emitResult = queryCompilation.Emit(peStream, pdbStream, options: emitOptions, cancellationToken: cancellationToken);
            var emitTime = emitDifferenceTimer.Elapsed;

            var executionTime = TimeSpan.Zero;

            cancellationToken.ThrowIfCancellationRequested();

            // complete compilation progress item:
            remainingProgressItemCount--;
            await observer.ItemsCompletedAsync(1, cancellationToken).ConfigureAwait(false);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        traceSource.TraceInformation($"Semantic search query compilation failed: {diagnostic}");
                    }
                }

                var errors = emitResult.Diagnostics.SelectAsArray(
                        d => d.Severity == DiagnosticSeverity.Error,
                        d => new QueryCompilationError(d.Id, d.GetMessage(), (d.Location.SourceTree == queryTree) ? d.Location.SourceSpan : default));

                return CreateResult(errors, FeaturesResources.Semantic_search_query_failed_to_compile);
            }

            peStream.Position = 0;
            pdbStream.Position = 0;
            var loadContext = new LoadContext();
            try
            {
                var queryAssembly = loadContext.LoadFromStream(peStream, pdbStream);

                var pidType = queryAssembly.GetType("<PrivateImplementationDetails>", throwOnError: true);
                Contract.ThrowIfNull(pidType);
                var moduleCancellationTokenField = pidType.GetField("ModuleCancellationToken", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                Contract.ThrowIfNull(moduleCancellationTokenField);
                moduleCancellationTokenField.SetValue(null, cancellationToken);

                if (!TryGetFindMethod(queryAssembly, out var findMethod, out var queryKind, out var errorMessage, out var errorMessageArgs))
                {
                    traceSource.TraceInformation($"Semantic search failed: {errorMessage}");
                    return CreateResult(compilationErrors: [], errorMessage, errorMessageArgs);
                }

                var invocationContext = new QueryExecutionContext(queryText, findMethod, observer, classificationOptions, traceSource);
                try
                {
                    await invocationContext.InvokeAsync(solution, queryKind, cancellationToken).ConfigureAwait(false);

                    if (invocationContext.TerminatedWithException)
                    {
                        return CreateResult(compilationErrors: [], FeaturesResources.Semantic_search_query_terminated_with_exception);
                    }
                }
                finally
                {
                    executionTime = new TimeSpan(invocationContext.ExecutionTime);
                    remainingProgressItemCount -= invocationContext.ProcessedProjectCount;
                }
            }
            finally
            {
                loadContext.Unload();

                // complete the remaining items (in case the search gets interrupted)
                if (remainingProgressItemCount > 0)
                {
                    await observer.ItemsCompletedAsync(remainingProgressItemCount, cancellationToken).ConfigureAwait(false);
                }
            }

            return CreateResult(compilationErrors: [], errorMessage: null);

            ExecuteQueryResult CreateResult(ImmutableArray<QueryCompilationError> compilationErrors, string? errorMessage, params string[]? args)
                => new(compilationErrors, errorMessage, args, emitTime, executionTime);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static bool TryGetFindMethod(Assembly queryAssembly, [NotNullWhen(true)] out MethodInfo? method, out QueryKind queryKind, out string? error, out string[]? errorMessageArgs)
    {
        method = null;
        error = null;
        errorMessageArgs = null;
        queryKind = default;

        Type? program;
        try
        {
            program = queryAssembly.GetType(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, throwOnError: false);
        }
        catch (Exception e)
        {
            error = FeaturesResources.Unable_to_load_type_0_1;
            errorMessageArgs = [WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, e.Message];
            return false;
        }

        if (program != null)
        {
            try
            {
                (method, queryKind) = GetFindMethod(program, ref error);
            }
            catch
            {
            }
        }

        if (method != null)
        {
            return true;
        }

        error ??= string.Format(FeaturesResources.The_query_does_not_specify_0_top_level_function, SemanticSearchUtilities.FindMethodName);
        return false;
    }

    private static (MethodInfo? method, QueryKind queryKind) GetFindMethod(Type type, ref string? error)
    {
        try
        {
            using var _ = ArrayBuilder<MethodInfo>.GetInstance(out var candidates);

            foreach (var candidate in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (candidate.Name.StartsWith($"<{WellKnownMemberNames.TopLevelStatementsEntryPointMethodName}>g__{SemanticSearchUtilities.FindMethodName}|"))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates is [])
            {
                error = string.Format(FeaturesResources.The_query_does_not_specify_0_top_level_function, SemanticSearchUtilities.FindMethodName);
                return default;
            }

            candidates.RemoveAll(candidate => candidate.IsGenericMethod || !candidate.IsStatic);
            if (candidates is [])
            {
                error = string.Format(FeaturesResources.Method_0_must_be_static_and_non_generic, SemanticSearchUtilities.FindMethodName);
                return default;
            }

            if (candidates is not [var method])
            {
                error = string.Format(FeaturesResources.The_query_specifies_multiple_top_level_functions_1, SemanticSearchUtilities.FindMethodName);
                return default;
            }

            if (method.GetParameters() is not [var parameter])
            {
                error = string.Format(FeaturesResources.The_query_specifies_multiple_top_level_functions_1, SemanticSearchUtilities.FindMethodName);
                return default;
            }

            if (!s_queryKindByParameterType.TryGetValue(parameter.ParameterType, out var entity))
            {
                error = string.Format(
                    FeaturesResources.Type_0_is_not_among_supported_types_1,
                    SemanticSearchUtilities.FindMethodName,
                    string.Join(", ", s_queryKindByParameterType.Keys.Select(t => $"'{t.Name}'")));

                return default;
            }

            return (method, entity);
        }
        catch (Exception e)
        {
            error = e.Message;
            return default;
        }
    }
}
#endif
