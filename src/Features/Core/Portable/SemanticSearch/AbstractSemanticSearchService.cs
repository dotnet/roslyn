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
using System.Threading.Channels;
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
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
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

    private readonly struct CompiledQuery(MemoryStream peStream, MemoryStream pdbStream, SourceText text) : IDisposable
    {
        public MemoryStream PEStream { get; } = peStream;
        public MemoryStream PdbStream { get; } = pdbStream;
        public SourceText Text { get; } = text;

        public void Dispose()
        {
            PEStream.Dispose();
            PdbStream.Dispose();
        }
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

    private ImmutableDictionary<CompiledQueryId, CompiledQuery> _compiledQueries = ImmutableDictionary<CompiledQueryId, CompiledQuery>.Empty;

    protected abstract Compilation CreateCompilation(SourceText query, IEnumerable<MetadataReference> references, SolutionServices services, out SyntaxTree queryTree, CancellationToken cancellationToken);

    public CompileQueryResult CompileQuery(
        SolutionServices services,
        string query,
        string referenceAssembliesDir,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        var metadataService = services.GetRequiredService<IMetadataService>();
        var metadataReferences = SemanticSearchUtilities.GetMetadataReferences(metadataService, referenceAssembliesDir);
        var queryText = SemanticSearchUtilities.CreateSourceText(query);
        var queryCompilation = CreateCompilation(queryText, metadataReferences, services, out var queryTree, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var emitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.PortablePdb,
            instrumentationKinds: [InstrumentationKind.StackOverflowProbing, InstrumentationKind.ModuleCancellation]);

        var peStream = new MemoryStream();
        var pdbStream = new MemoryStream();

        var emitDifferenceTimer = SharedStopwatch.StartNew();
        var emitResult = queryCompilation.Emit(peStream, pdbStream, options: emitOptions, cancellationToken: cancellationToken);
        var emitTime = emitDifferenceTimer.Elapsed;

        CompiledQueryId queryId;
        ImmutableArray<QueryCompilationError> errors;
        if (emitResult.Success)
        {
            queryId = CompiledQueryId.Create(queryCompilation.Language);
            Contract.ThrowIfFalse(ImmutableInterlocked.TryAdd(ref _compiledQueries, queryId, new CompiledQuery(peStream, pdbStream, queryText)));

            errors = [];
        }
        else
        {
            queryId = default;

            foreach (var diagnostic in emitResult.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    traceSource.TraceInformation($"Semantic search query compilation failed: {diagnostic}");
                }
            }

            errors = emitResult.Diagnostics.SelectAsArray(
                d => d.Severity == DiagnosticSeverity.Error,
                d => new QueryCompilationError(d.Id, d.GetMessage(), (d.Location.SourceTree == queryTree) ? d.Location.SourceSpan : default));
        }

        return new CompileQueryResult(queryId, errors, emitTime);
    }

    public void DiscardQuery(CompiledQueryId queryId)
    {
        Contract.ThrowIfFalse(ImmutableInterlocked.TryRemove(ref _compiledQueries, queryId, out var compiledQuery));
        compiledQuery.Dispose();
    }

    public async Task<ExecuteQueryResult> ExecuteQueryAsync(
        Solution solution,
        CompiledQueryId queryId,
        ISemanticSearchResultsObserver observer,
        OptionsProvider<ClassificationOptions> classificationOptions,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(ImmutableInterlocked.TryRemove(ref _compiledQueries, queryId, out var query));

        try
        {
            var executionTime = TimeSpan.Zero;

            var remainingProgressItemCount = solution.ProjectIds.Count;
            await observer.AddItemsAsync(remainingProgressItemCount, cancellationToken).ConfigureAwait(false);

            query.PEStream.Position = 0;
            query.PdbStream.Position = 0;
            var loadContext = new LoadContext();
            try
            {
                var queryAssembly = loadContext.LoadFromStream(query.PEStream, query.PdbStream);
                SetModuleCancellationToken(queryAssembly, cancellationToken);

                SetToolImplementations(
                    queryAssembly,
                    new ReferencingSyntaxFinder(solution, cancellationToken),
                    new SemanticModelGetter(solution, cancellationToken));

                if (!TryGetQueryFunctions(queryAssembly, out var functions, out var queryKind, out var errorMessage, out var errorMessageArgs))
                {
                    traceSource.TraceInformation($"Semantic search failed: {errorMessage}");
                    return CreateResult(errorMessage, errorMessageArgs);
                }

                var invocationContext = new QueryExecutionContext(solution, query.Text, functions, observer, classificationOptions, traceSource);
                try
                {
                    await invocationContext.InvokeAsync(solution, queryKind, cancellationToken).ConfigureAwait(false);

                    if (invocationContext.TerminatedWithException)
                    {
                        return CreateResult(FeaturesResources.Semantic_search_query_terminated_with_exception);
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

            return CreateResult(errorMessage: null);

            ExecuteQueryResult CreateResult(string? errorMessage, params string[]? args)
                => new(errorMessage, args, executionTime);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
        finally
        {
            query.Dispose();
        }
    }

    private static void SetModuleCancellationToken(Assembly queryAssembly, CancellationToken cancellationToken)
    {
        var pidType = queryAssembly.GetType("<PrivateImplementationDetails>", throwOnError: true);
        Contract.ThrowIfNull(pidType);
        var moduleCancellationTokenField = pidType.GetField("ModuleCancellationToken", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        Contract.ThrowIfNull(moduleCancellationTokenField);
        moduleCancellationTokenField.SetValue(null, cancellationToken);
    }

    private static void SetToolImplementations(Assembly queryAssembly, ReferencingSyntaxFinder finder, SemanticModelGetter semanticModelGetter)
    {
        var toolsType = queryAssembly.GetType(SemanticSearchUtilities.ToolsTypeName, throwOnError: true);
        Contract.ThrowIfNull(toolsType);

        SetFieldValue(SemanticSearchUtilities.FindReferencingSyntaxNodesImplName, new Func<ISymbol, IEnumerable<SyntaxNode>>(finder.Find));
        SetFieldValue(SemanticSearchUtilities.GetSemanticModelImplName, new Func<SyntaxTree, Task<SemanticModel>>(semanticModelGetter.GetSemanticModelAsync));

        void SetFieldValue(string fieldName, object value)
        {
            var field = toolsType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Contract.ThrowIfNull(field);
            field.SetValue(null, value);
        }
    }

    private static bool TryGetQueryFunctions(
        Assembly queryAssembly,
        [NotNullWhen(true)] out QueryFunctions? functions,
        out QueryKind queryKind,
        [NotNullWhen(false)] out string? error,
        out string[]? errorMessageArgs)
    {
        functions = null;
        errorMessageArgs = null;
        queryKind = default;

        Type? program;
        try
        {
            program = queryAssembly.GetType(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, throwOnError: true);
        }
        catch (Exception e)
        {
            error = FeaturesResources.Unable_to_load_type_0_1;
            errorMessageArgs = [WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, e.Message];
            return false;
        }

        Contract.ThrowIfNull(program);

        try
        {
            if (TryGetFindFunction(out var findFunction, out error, out queryKind) &&
                TryGetUpdateFunction(LanguageNames.CSharp, out var updateFunctionCSharp, out error) &&
                TryGetUpdateFunction(LanguageNames.VisualBasic, out var updateFunctionVisualBasic, out error))
            {
                functions = new QueryFunctions(findFunction, updateFunctionCSharp, updateFunctionVisualBasic);
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }

        bool TryGetFindFunction([NotNullWhen(true)] out MethodInfo? function, [NotNullWhen(false)] out string? error, out QueryKind queryKind)
        {
            queryKind = default;

            if (!TryGetCandidate(
                SemanticSearchUtilities.FindMethodName,
                expectedParameters: null,
                overloadSelector: null,
                isOptional: false,
                out function,
                out var functionName,
                out error))
            {
                Contract.ThrowIfNull(error);
                return false;
            }

            Contract.ThrowIfNull(function);
            if (function.GetParameters() is not [var parameter])
            {
                error = string.Format(FeaturesResources.Top_level_function_0_must_have_a_single_parameter, functionName);
                return false;
            }

            if (!s_queryKindByParameterType.TryGetValue(parameter.ParameterType, out queryKind))
            {
                error = string.Format(
                    FeaturesResources.Parameter_type_0_is_not_among_supported_types_1,
                    parameter.ParameterType,
                    string.Join(", ", s_queryKindByParameterType.Keys.Select(t => $"'{t.Name}'")));

                return false;
            }

            if (function.ReturnType != typeof(IEnumerable<ISymbol>) &&
                function.ReturnType != typeof(IAsyncEnumerable<ISymbol>) &&
                function.ReturnType != typeof(IEnumerable<Location>) &&
                function.ReturnType != typeof(IAsyncEnumerable<Location>))
            {
                error = string.Format(
                    FeaturesResources.Return_type_0_of_function_1_is_not_among_supported_types_2,
                    function.ReturnType,
                    functionName,
                    "'IEnumerable<ISymbol>', 'IAsyncEnumerable<ISymbol>', 'IEnumerable<Location>', 'IAsyncEnumerable<Location>'");

                return false;
            }

            return true;
        }

        bool TryGetUpdateFunction(string language, out MethodInfo? function, [NotNullWhen(false)] out string? error)
        {
            var unitType = $"Microsoft.CodeAnalysis.{(language == LanguageNames.CSharp ? "CSharp" : "VisualBasic")}.Syntax.CompilationUnitSyntax";

            if (!TryGetCandidate(
                SemanticSearchUtilities.UpdateMethodName,
                expectedParameters: $"'{unitType}', 'IEnumerable<Location>'",
                overloadSelector: parameters => parameters is [var parameterSyntaxTree, var parameterLocations] &&
                  parameterSyntaxTree.ParameterType.IsAssignableTo(typeof(ICompilationUnitSyntax)) &&
                  parameterSyntaxTree.ParameterType.FullName == unitType &&
                  parameterLocations.ParameterType.IsAssignableTo(typeof(IEnumerable<TextSpan>)),
                isOptional: true,
                out function,
                out var functionName,
                out error))
            {
                return error == null;
            }

            if (function == null)
            {
                // Update function not defined.
                return true;
            }

            if (!(function.ReturnType.IsAssignableTo(typeof(ICompilationUnitSyntax)) &&
                  function.ReturnType.FullName == unitType))
            {
                error = string.Format(
                    FeaturesResources.Return_type_0_of_function_1_is_not_among_supported_types_2,
                    function.ReturnType,
                    functionName,
                    $"'{unitType}'");

                return false;
            }

            return true;
        }

        bool TryGetCandidate(
            string functionNamePrefix,
            string? expectedParameters,
            Predicate<ParameterInfo[]>? overloadSelector,
            bool isOptional,
            [NotNullWhen(true)] out MethodInfo? function,
            [NotNullWhen(true)] out string? functionName,
            out string? error)
        {
            Contract.ThrowIfFalse(overloadSelector is null == expectedParameters is null);

            function = null;
            functionName = null;

            var candidates = new TemporaryArray<(MethodInfo info, string name)>();
            var hasNonMatchingOverload = false;

            foreach (var candidate in program.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                var metadataPrefix = $"<{WellKnownMemberNames.TopLevelStatementsEntryPointMethodName}>g__{functionNamePrefix}";

                if (candidate.Name.StartsWith(metadataPrefix, StringComparison.Ordinal))
                {
                    var nameEnd = candidate.Name.IndexOf('|', startIndex: metadataPrefix.Length);
                    Contract.ThrowIfFalse(nameEnd > 0);

                    if (overloadSelector == null && nameEnd != metadataPrefix.Length)
                    {
                        continue;
                    }

                    if (overloadSelector != null && !overloadSelector(candidate.GetParameters()))
                    {
                        hasNonMatchingOverload = true;
                        continue;
                    }

                    candidates.Add((candidate, functionNamePrefix + candidate.Name[metadataPrefix.Length..nameEnd]));

                    if (overloadSelector == null)
                    {
                        // only one local function of the given exact name can be defined
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                error = isOptional
                    ? null
                    : hasNonMatchingOverload
                    ? string.Format(FeaturesResources.Top_level_function_0_must_have_1_parameters_of_types_2, functionNamePrefix, expectedParameters)
                    : string.Format(FeaturesResources.The_query_does_not_specify_0_top_level_function, functionNamePrefix);

                return false;
            }

            if (candidates.Count > 1)
            {
                error = string.Format(FeaturesResources.The_query_specifies_multiple_top_level_functions_with_prefix_1, functionNamePrefix);
                return false;
            }

            (function, functionName) = candidates[0];

            if (function.IsGenericMethod || !function.IsStatic)
            {
                error = string.Format(FeaturesResources.Method_0_must_be_static_and_non_generic, functionNamePrefix);
                return false;
            }

            error = null;
            return true;
        }
    }
}
#endif
