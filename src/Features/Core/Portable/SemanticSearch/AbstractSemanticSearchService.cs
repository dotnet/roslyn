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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

    private static readonly FindReferencesSearchOptions s_findReferencesSearchOptions = new()
    {
        DisplayAllDefinitions = true,
    };

    private const int StackDisplayDepthLimit = 32;

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

                await observer.OnCompilationFailureAsync(
                    emitResult.Diagnostics.SelectAsArray(
                        d => d.Severity == DiagnosticSeverity.Error,
                        d => new QueryCompilationError(d.Id, d.GetMessage(), (d.Location.SourceTree == queryTree) ? d.Location.SourceSpan : default)),
                    cancellationToken).ConfigureAwait(false);

                return CreateResult(FeaturesResources.Semantic_search_query_failed_to_compile);
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

                if (!TryGetFindMethod(queryAssembly, out var findMethod, out var errorMessage, out var errorMessageArgs))
                {
                    traceSource.TraceInformation($"Semantic search failed: {errorMessage}");
                    return CreateResult(errorMessage, errorMessageArgs);
                }

                var executionTimeStopWatch = new Stopwatch();

                foreach (var project in solution.Projects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        executionTimeStopWatch.Start();

                        try
                        {
                            var symbols = (IEnumerable<ISymbol?>?)findMethod.Invoke(null, [compilation]) ?? [];

                            foreach (var symbol in symbols)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (symbol != null)
                                {
                                    executionTimeStopWatch.Stop();

                                    try
                                    {
                                        var definitionItem = await symbol.ToClassifiedDefinitionItemAsync(
                                            classificationOptions, solution, s_findReferencesSearchOptions, isPrimary: true, includeHiddenLocations: false, cancellationToken).ConfigureAwait(false);

                                        await observer.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
                                    }
                                    catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                                    {
                                        // skip symbol
                                    }

                                    executionTimeStopWatch.Start();
                                }
                            }
                        }
                        finally
                        {
                            executionTimeStopWatch.Stop();
                            executionTime = executionTimeStopWatch.Elapsed;
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // exception from user code

                        if (e is TargetInvocationException { InnerException: { } innerException })
                        {
                            e = innerException;
                        }

                        var (projectName, projectFlavor) = project.State.NameAndFlavor;
                        projectName ??= project.Name;
                        var projectDisplay = string.IsNullOrEmpty(projectFlavor) ? projectName : $"{projectName} ({projectFlavor})";

                        FormatStackTrace(e, queryAssembly, out var position, out var stackTraceTaggedText);
                        var span = queryText.Lines.GetTextSpan(new LinePositionSpan(position, position));

                        var exceptionNameTaggedText = GetExceptionTypeTaggedText(e, compilation);

                        await observer.OnUserCodeExceptionAsync(new UserCodeExceptionInfo(projectDisplay, e.Message, exceptionNameTaggedText, stackTraceTaggedText, span), cancellationToken).ConfigureAwait(false);

                        traceSource.TraceInformation($"Semantic query execution failed due to user code exception: {e}");
                        return CreateResult(FeaturesResources.Semantic_search_query_terminated_with_exception);
                    }

                    // complete project progress item:
                    remainingProgressItemCount--;
                    await observer.ItemsCompletedAsync(1, cancellationToken).ConfigureAwait(false);
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
                => new(errorMessage, args, emitTime, executionTime);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static ImmutableArray<TaggedText> GetExceptionTypeTaggedText(Exception e, Compilation compilation)
        => e.GetType().FullName is { } exceptionTypeName
           ? compilation.GetTypeByMetadataName(exceptionTypeName) is { } exceptionTypeSymbol
                ? exceptionTypeSymbol.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat).ToTaggedText()
                : [new TaggedText(WellKnownTags.Class, exceptionTypeName)]
           : [new TaggedText(WellKnownTags.Class, nameof(Exception))];

    private static void FormatStackTrace(Exception e, Assembly queryAssembly, out LinePosition position, out ImmutableArray<TaggedText> formattedTrace)
    {
        position = default;

        try
        {
            var trace = new StackTrace(e, fNeedFileInfo: true);
            var frames = trace.GetFrames();
            var displayFrames = frames;
            var skippedFrameCount = 0;

            try
            {
                var hostAssembly = typeof(AbstractSemanticSearchService).Assembly;
                var displayFramesEnd = frames.Length;
                var foundPosition = false;
                for (var i = 0; i < frames.Length; i++)
                {
                    var frame = frames[i];

                    if (frame.GetMethod() is { } method)
                    {
                        var frameAssembly = method.DeclaringType?.Assembly;
                        if (frameAssembly == hostAssembly)
                        {
                            displayFramesEnd = i;
                            break;
                        }

                        if (!foundPosition &&
                            frameAssembly == queryAssembly &&
                            frame.GetFileName() is { } fileName &&
                            frame.GetFileLineNumber() is > 0 and var line &&
                            frame.GetFileColumnNumber() is > 0 and var column)
                        {
                            position = new LinePosition(line - 1, column - 1);
                            foundPosition = true;
                        }
                    }
                }

                // display last StackDisplayDepthLimit frames preceding the host frame:
                skippedFrameCount = Math.Max(0, displayFramesEnd - StackDisplayDepthLimit);
                displayFrames = frames[skippedFrameCount..displayFramesEnd];
            }
            catch
            {
                // nop
            }

            formattedTrace =
            [
                new TaggedText(tag: TextTags.Text, (skippedFrameCount > 0 ? "   ..." + Environment.NewLine : "") + GetStackTraceText(displayFrames))
            ];
        }
        catch
        {
            formattedTrace = [];
        }
    }

    private static string GetStackTraceText(IEnumerable<StackFrame> frames)
    {
#if NET8_0_OR_GREATER
        return new StackTrace(frames).ToString();
#else
        var builder = new StringBuilder();
        foreach (var frame in frames)
        {
            builder.Append(new StackTrace(frame).ToString());
        }

        return builder.ToString();
#endif
    }

    private static bool TryGetFindMethod(Assembly queryAssembly, [NotNullWhen(true)] out MethodInfo? method, out string? error, out string[]? errorMessageArgs)
    {
        // TODO: Use Compilation APIs to find the method

        method = null;
        error = null;
        errorMessageArgs = null;

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
                method = GetFindMethod(program, allowLocalFunction: true, ref error);
            }
            catch
            {
            }

            if (method != null)
            {
                return true;
            }
        }

        Type[] types;
        try
        {
            types = queryAssembly.GetTypes();
        }
        catch (TypeLoadException e)
        {
            error = FeaturesResources.Unable_to_load_type_0_1;
            errorMessageArgs = [e.TypeName, e.Message];
            method = null;
            return false;
        }

        foreach (var type in types)
        {
            method = GetFindMethod(type, allowLocalFunction: false, ref error);
            if (method != null)
            {
                return true;
            }
        }

        error ??= string.Format(FeaturesResources.The_query_does_not_specify_0_method_or_top_level_function, SemanticSearchUtilities.FindMethodName);
        return false;
    }

    private static MethodInfo? GetFindMethod(Type type, bool allowLocalFunction, ref string? error)
    {
        try
        {
            using var _ = ArrayBuilder<MethodInfo>.GetInstance(out var candidates);

            foreach (var candidate in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (candidate.Name == SemanticSearchUtilities.FindMethodName ||
                    allowLocalFunction && candidate.Name.StartsWith($"<{WellKnownMemberNames.TopLevelStatementsEntryPointMethodName}>g__{SemanticSearchUtilities.FindMethodName}|"))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates is [])
            {
                error = string.Format(FeaturesResources.The_query_does_not_specify_0_method_or_top_level_function, SemanticSearchUtilities.FindMethodName);
                return null;
            }

            candidates.RemoveAll(candidate => candidate.IsGenericMethod || !candidate.IsStatic);
            if (candidates is [])
            {
                error = string.Format(FeaturesResources.Method_0_must_be_static_and_non_generic, SemanticSearchUtilities.FindMethodName);
                return null;
            }

            candidates.RemoveAll(candidate => !(
                typeof(IEnumerable<ISymbol>).IsAssignableFrom(candidate.ReturnType) &&
                candidate.GetParameters() is [{ ParameterType: var paramType }] &&
                typeof(Compilation).IsAssignableFrom(paramType)));

            if (candidates is [])
            {
                error = string.Format(FeaturesResources.Method_0_must_have_a_single_parameter_of_type_1_and_return_2, SemanticSearchUtilities.FindMethodName, nameof(Compilation));
                return null;
            }

            Debug.Assert(candidates.Count == 1);
            return candidates[0];
        }
        catch (Exception e)
        {
            error = e.Message;
            return null;
        }
    }
}
#endif
