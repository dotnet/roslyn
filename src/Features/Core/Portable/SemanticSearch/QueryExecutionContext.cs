// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET6_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class QueryExecutionContext(
    Solution solution,
    SourceText queryText,
    QueryFunctions functions,
    ISemanticSearchResultsObserver resultsObserver,
    OptionsProvider<ClassificationOptions> classificationOptions,
    TraceSource traceSource)
{
    private static readonly FindReferencesSearchOptions s_findReferencesSearchOptions = new()
    {
        DisplayAllDefinitions = true,
    };

    private const int StackDisplayDepthLimit = 32;

    private long _executionTime;
    private int _processedProjectCount;
    public bool TerminatedWithException { get; private set; }

    public long ExecutionTime => _executionTime;
    public int ProcessedProjectCount => _processedProjectCount;

    private readonly ConcurrentDictionary<SyntaxTree, ArrayBuilder<TextSpan>>? _updateInputs
        = functions.UpdateCSharp != null || functions.UpdateVisualBasic != null ? new() : null;

    public async Task InvokeAsync(Solution solution, QueryKind queryKind, CancellationToken cancellationToken)
    {
        // Invoke query on projects and types in parallel and on members serially.
        // Cancel execution if the query throws an exception.

        using var symbolEnumerationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Find
            await Parallel.ForEachAsync(solution.Projects, symbolEnumerationCancellationSource.Token, async (project, cancellationToken) =>
            {
                try
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    if (compilation == null)
                    {
                        return;
                    }

                    // only search source symbols:
                    var rootNamespace = compilation.Assembly.GlobalNamespace;

                    switch (queryKind)
                    {
                        case QueryKind.Compilation:
                            await InvokeFindAsync(project, compilation, entity: compilation, symbolEnumerationCancellationSource: symbolEnumerationCancellationSource, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;

                        case QueryKind.Namespace:
                            await Parallel.ForEachAsync(rootNamespace.GetAllNamespaces(cancellationToken), cancellationToken, async (namespaceSymbol, cancellationToken) =>
                            {
                                await InvokeFindAsync(project, compilation, entity: namespaceSymbol, symbolEnumerationCancellationSource: symbolEnumerationCancellationSource, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }).ConfigureAwait(false);
                            break;

                        case QueryKind.NamedType:
                        case QueryKind.Field:
                        case QueryKind.Method:
                        case QueryKind.Property:
                        case QueryKind.Event:

                            var kind = GetSymbolKind(queryKind);

                            await Parallel.ForEachAsync(rootNamespace.GetAllTypes(cancellationToken), async (type, cancellationToken) =>
                            {
                                if (kind == SymbolKind.NamedType)
                                {
                                    await InvokeFindAsync(project, compilation, entity: type, symbolEnumerationCancellationSource: symbolEnumerationCancellationSource, cancellationToken: cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    foreach (var member in type.GetMembers())
                                    {
                                        if (member.Kind == kind)
                                        {
                                            await InvokeFindAsync(project, compilation, entity: member, symbolEnumerationCancellationSource: symbolEnumerationCancellationSource, cancellationToken: cancellationToken).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }).ConfigureAwait(false);
                            break;
                    }
                }
                finally
                {
                    // complete project progress item:
                    Interlocked.Increment(ref _processedProjectCount);
                    await resultsObserver.ItemsCompletedAsync(1, cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            // Update
            if (_updateInputs == null)
            {
                return;
            }

            Contract.ThrowIfNull(_updateInputs);

            await Parallel.ForEachAsync(_updateInputs, symbolEnumerationCancellationSource.Token, async (treeAndSpans, cancellationToken) =>
            {
                var (syntaxTree, spans) = treeAndSpans;
                try
                {
                    await InvokeUpdateAsync(syntaxTree, spans, symbolEnumerationCancellationSource, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    spans.Free();

                    // TODO: report progress
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (symbolEnumerationCancellationSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // enumeration terminated due to exception in user code
        }
    }

    private async ValueTask InvokeFindAsync(Project project, Compilation compilation, object entity, CancellationTokenSource symbolEnumerationCancellationSource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executionTime = TimeSpan.Zero;

        try
        {
            var executionStart = Stopwatch.GetTimestamp();

            try
            {
                var results = functions.Find.Invoke(null, [entity]) switch
                {
                    IAsyncEnumerable<ISymbol> asyncEnumerableSymbols => SelectAsync<ISymbol, (ISymbol?, Location?)>(asyncEnumerableSymbols, static symbol => (symbol, null), cancellationToken),
                    IEnumerable<ISymbol> enumerableSymbols => enumerableSymbols.Select<ISymbol, (ISymbol?, Location?)>(static symbol => (symbol, null)).AsAsyncEnumerable(),
                    IAsyncEnumerable<Location> asyncEnumerableLocations => SelectAsync<Location, (ISymbol?, Location?)>(asyncEnumerableLocations, static location => (null, location), cancellationToken),
                    IEnumerable<Location> enumerableLocations => enumerableLocations.Select<Location, (ISymbol?, Location?)>(static location => (null, location)).AsAsyncEnumerable(),
                    null => Array.Empty<(ISymbol?, Location?)>().AsAsyncEnumerable(),

                    // we shouldn't have compiled the query:
                    var other => throw ExceptionUtilities.UnexpectedValue(other)
                };

                await foreach (var (symbol, location) in results.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (symbol == null && location == null)
                    {
                        continue;
                    }

                    executionTime += Stopwatch.GetElapsedTime(executionStart);

                    AddUpdateInput(symbol, location);

                    try
                    {
                        var definitionItem = await CreateDefinitionItemAsync(project, symbol, location, cancellationToken).ConfigureAwait(false);

                        if (definitionItem == null)
                        {
                            continue;
                        }

                        await resultsObserver.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                    {
                        // skip symbol
                    }

                    executionStart = Stopwatch.GetTimestamp();
                }
            }
            finally
            {
                executionTime += Stopwatch.GetElapsedTime(executionStart);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // exception from user code
            TerminatedWithException = true;

            if (e is TargetInvocationException { InnerException: { } innerException })
            {
                e = innerException;
            }

            var (projectName, projectFlavor) = project.State.NameAndFlavor;
            projectName ??= project.Name;
            var projectDisplay = string.IsNullOrEmpty(projectFlavor) ? projectName : $"{projectName} ({projectFlavor})";

            Contract.ThrowIfNull(functions.Find.DeclaringType);
            FormatStackTrace(e, functions.Find.DeclaringType.Assembly, out var position, out var stackTraceTaggedText);
            var span = queryText.Lines.GetTextSpan(new LinePositionSpan(position, position));

            var exceptionNameTaggedText = GetExceptionTypeTaggedText(e, compilation);

            await resultsObserver.OnUserCodeExceptionAsync(new UserCodeExceptionInfo(projectDisplay, e.Message, exceptionNameTaggedText, stackTraceTaggedText, span), cancellationToken).ConfigureAwait(false);

            traceSource.TraceInformation($"Semantic query execution failed due to user code exception: {e}");

            symbolEnumerationCancellationSource.Cancel();
        }

        Interlocked.Add(ref _executionTime, executionTime.Ticks);
    }

    private void AddUpdateInput(ISymbol? symbol, Location? location)
    {
        if (_updateInputs == null)
        {
            return;
        }

        if (location != null)
        {
            AddLocation(location);
        }

        if (symbol != null)
        {
            foreach (var symbolLocation in symbol.Locations)
            {
                AddLocation(symbolLocation);
            }
        }

        void AddLocation(Location location)
        {
            if (location is not { SourceTree: { } tree })
            {
                // skip non-source locations
                return;
            }

            if (!solution.GetDocument(tree).CanApplyChange())
            {
                // skip immutable documents
                return;
            }

            var spans = _updateInputs.GetOrAdd(tree, static _ => ArrayBuilder<TextSpan>.GetInstance());
            lock (spans)
            {
                spans.Add(location.SourceSpan);
            }
        }
    }

    private async ValueTask InvokeUpdateAsync(SyntaxTree oldSyntaxTree, IEnumerable<Span> spans, CancellationTokenSource symbolEnumerationCancellationSource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executionTime = TimeSpan.Zero;

        try
        {
            var executionStart = Stopwatch.GetTimestamp();

            try
            {
                var updateFunction = oldSyntaxTree.Options.Language == LanguageNames.CSharp ? functions.UpdateCSharp : functions.UpdateVisualBasic;
                if (updateFunction == null)
                {
                    return;
                }

                var oldRoot = await oldSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = (SyntaxNode?)updateFunction.Invoke(null, [oldRoot, spans]);

                var oldDocument = solution.GetRequiredDocument(oldSyntaxTree);

                // null means delete the document:
                var changes = newRoot != null ? oldSyntaxTree.WithRootAndOptions(newRoot, oldSyntaxTree.Options).GetChanges(oldSyntaxTree) : [];

                await resultsObserver.OnDocumentUpdatedAsync(oldDocument.Id, changes.ToImmutableArrayOrEmpty(), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                executionTime += Stopwatch.GetElapsedTime(executionStart);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // TODO:
            // exception from user code
            //TerminatedWithException = true;

            //if (e is TargetInvocationException { InnerException: { } innerException })
            //{
            //    e = innerException;
            //}

            //var (projectName, projectFlavor) = project.State.NameAndFlavor;
            //projectName ??= project.Name;
            //var projectDisplay = string.IsNullOrEmpty(projectFlavor) ? projectName : $"{projectName} ({projectFlavor})";

            //Contract.ThrowIfNull(findMethod.DeclaringType);
            //FormatStackTrace(e, findMethod.DeclaringType.Assembly, out var position, out var stackTraceTaggedText);
            //var span = queryText.Lines.GetTextSpan(new LinePositionSpan(position, position));

            //var exceptionNameTaggedText = GetExceptionTypeTaggedText(e, compilation);

            //await resultsObserver.OnUserCodeExceptionAsync(new UserCodeExceptionInfo(projectDisplay, e.Message, exceptionNameTaggedText, stackTraceTaggedText, span), cancellationToken).ConfigureAwait(false);

            //traceSource.TraceInformation($"Semantic query execution failed due to user code exception: {e}");

            //symbolEnumerationCancellationSource.Cancel();
        }

        Interlocked.Add(ref _executionTime, executionTime.Ticks);
    }

    private static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(IAsyncEnumerable<TSource> sequence, Func<TSource, TResult> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in sequence.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return selector(item);
        }
    }

    private async Task<DefinitionItem?> CreateDefinitionItemAsync(Project project, ISymbol? symbol, Location? location, CancellationToken cancellationToken)
    {
        if (symbol != null)
        {
            return await symbol.ToClassifiedDefinitionItemAsync(
                classificationOptions, project.Solution, s_findReferencesSearchOptions, isPrimary: true, includeHiddenLocations: false, cancellationToken).ConfigureAwait(false);
        }

        Contract.ThrowIfNull(location);

        if (location.Kind == LocationKind.None)
        {
            return null;
        }

        if (location.MetadataModule is { } module)
        {
            var metadataLocation = DefinitionItemFactory.GetMetadataLocation(module.ContainingAssembly, project.Solution, out var originatingProjectId);

            return DefinitionItem.Create(
                tags: [],
                displayParts: [],
                sourceSpans: [],
                classifiedSpans: [],
                metadataLocations: [metadataLocation],
                properties: ImmutableDictionary<string, string>.Empty.WithMetadataSymbolProperties(module.ContainingAssembly, originatingProjectId));
        }

        if (project.Solution.GetDocument(location.SourceTree) is { } document)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var displaySpan = Clip(location.SourceSpan, maxLength: 100);
            var displayText = new TaggedText(TextTags.Text, text.ToString(displaySpan));

            return DefinitionItem.Create(
                tags: [],
                displayParts: displaySpan.Length == location.SourceSpan.Length ? [displayText] : [displayText, new TaggedText(TextTags.Punctuation, "…")],
                sourceSpans: [new DocumentSpan(document, location.SourceSpan)],
                classifiedSpans: [],
                metadataLocations: []);
        }

        return DefinitionItem.CreateNonNavigableItem(
            tags: [],
            displayParts: [new TaggedText(TextTags.Text, location.ToString())]);
    }

    private static TextSpan Clip(TextSpan span, int maxLength)
        => new(span.Start, Math.Min(span.Length, maxLength));

    private static SymbolKind GetSymbolKind(QueryKind targetEntity)
        => targetEntity switch
        {
            QueryKind.Field => SymbolKind.Field,
            QueryKind.Method => SymbolKind.Method,
            QueryKind.Property => SymbolKind.Property,
            QueryKind.Event => SymbolKind.Event,
            QueryKind.NamedType => SymbolKind.NamedType,
            QueryKind.Namespace => SymbolKind.Namespace,
            _ => default
        };

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
}
#endif
