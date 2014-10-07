using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal sealed partial class RenameLocationSet
    {
        /// <summary>
        /// A class that aggregates the results of an asynchronous find references. The aggregation
        /// is done on a per-document level, so it can provide a Task{RenameLocationSet} for a
        /// single document or for the entire solution.
        /// </summary>
        internal sealed class AsynchronousSetSource
        {
            private ConstituentAsynchronousSetSource baseSymbolResult;
            private ConstituentAsynchronousSetSource overloadedSymbolsResult;
            
            private readonly Solution solution;
            private readonly ISymbol symbol;
            private readonly CancellationToken cancellationToken;

            /// <summary>
            /// This gate guards the non-thread-safe fields in this class, namely the below
            /// here.
            /// </summary>
            private readonly object gate = new object();
            private int totalCandidates = 0;
            private int countReferenceCompleted = 0;
            private Dictionary<DocumentId, TaskCompletionSource<RenameLocationSet>> perDocumentTaskCompletionSources;
            private TaskCompletionSource<RenameLocationSet> allLocationsCompletionSource;
            private bool searchComplete;
            private bool notStarted = true;

            private bool RenameInComments { get; set; }
            private bool RenameInStrings { get; set; }
            private ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> perDocumentLocationsInComments;
            private ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> perDocumentLocationsInStrings;

            private AsynchronousSetSource(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            {
                this.symbol = symbol;
                this.solution = solution;
                this.cancellationToken = cancellationToken;

                this.allLocationsCompletionSource = new TaskCompletionSource<RenameLocationSet>();
                this.perDocumentTaskCompletionSources = new Dictionary<DocumentId, TaskCompletionSource<RenameLocationSet>>();
            }

            private void ResetTaskCompletionSources()
            {
                this.allLocationsCompletionSource = new TaskCompletionSource<RenameLocationSet>();
                var keys = this.perDocumentTaskCompletionSources.Keys.ToArray();
                foreach (var key in keys)
                {
                    this.perDocumentTaskCompletionSources[key] = new TaskCompletionSource<RenameLocationSet>();
                }
            }

            internal void SetBaseSymbolResult(ConstituentAsynchronousSetSource baseSymbolResult)
            {
                this.baseSymbolResult = baseSymbolResult;
                StartConstituentOrUpdateContainer(this.baseSymbolResult);
            }

            internal void SetOverloadSymbolResult(ConstituentAsynchronousSetSource overloadedSymbolsResult)
            {
                this.overloadedSymbolsResult = overloadedSymbolsResult;
                StartConstituentOrUpdateContainer(this.overloadedSymbolsResult);
            }

            private ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> GetOrComputeRenameLocationsInComments()
            {
                Contract.ThrowIfFalse(this.RenameInComments);

                if (perDocumentLocationsInComments == null)
                {
                    var locationsInComments = ComputeRenameLocationsInStringsOrCommentsAsync(symbol, solution, renameInStrings: false, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken);
                    Interlocked.CompareExchange(ref this.perDocumentLocationsInComments, locationsInComments, null);
                }

                return this.perDocumentLocationsInComments;
            }

            private ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> GetOrComputeRenameLocationsInStrings()
            {
                Contract.ThrowIfFalse(this.RenameInStrings);

                if (perDocumentLocationsInStrings == null)
                {
                    var locationsInStrings = ComputeRenameLocationsInStringsOrCommentsAsync(symbol, solution, renameInStrings: true, cancellationToken: cancellationToken)
                        .WaitAndGetResult(cancellationToken);
                    Interlocked.CompareExchange(ref this.perDocumentLocationsInStrings, locationsInStrings, null);
                }

                return this.perDocumentLocationsInStrings;
            }

            private static async Task<ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>>> ComputeRenameLocationsInStringsOrCommentsAsync(
                ISymbol symbol,
                Solution solution,
                bool renameInStrings,
                CancellationToken cancellationToken)
            {
                var locationsInStrings = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    symbol,
                    solution,
                    renameInStrings: renameInStrings,
                    renameInComments: !renameInStrings,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var perDocumentLocations = new ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>>();
                AddRenameLocations(perDocumentLocations, solution, locationsInStrings);
                return perDocumentLocations;
            }

            /// <summary>
            /// *IMPORTANT*
            /// This method is important and it must be called after all the constituents are set.
            /// Without setting the notStarted flag, this object will never be marked complete. 
            /// </summary>
            internal void TrySetCompletion()
            {
                notStarted = false;
                CheckForCompletionAndTrySetValue();
            }

            /// <summary>
            /// This method is called when a ConstituentAsynchronousSetSource is added to the container i.e. AsynchronousSetSource
            /// It helps to either the start the constituent or update its previously calculated result to the containter (and also update the container is required)
            /// </summary>
            /// <param name="constitutent"></param>
            private void StartConstituentOrUpdateContainer(ConstituentAsynchronousSetSource constitutent)
            {
                if (constitutent.Container == this)
                {
                    if (constitutent.HasNotStarted)
                    {
                        constitutent.StartCalculating();
                    }
                    else
                    {
                        constitutent.UpdateContainerWithCurrentResult();
                        constitutent.UpdateContainterWithTotalCandidates();
                    }
                }
                else
                {
                    // Update the constituent with the new Container
                    constitutent.UpdateContainerAndResult(this);
                }
            }

            /// <summary>
            /// Check to see if all the candidates have reached FAR completion and sets results accordingly
            /// </summary>
            private void CheckForCompletionAndTrySetValue()
            {
                if (notStarted)
                {
                    return;
                }

                if (countReferenceCompleted < totalCandidates)
                {
                    return;
                }

                if (baseSymbolResult != null && baseSymbolResult.HasNotStarted)
                {
                    return;
                }

                if (overloadedSymbolsResult != null && overloadedSymbolsResult.HasNotStarted)
                {
                    return;
                }

                lock (gate)
                {
                    // Accumulate all the result together
                    var allLocations = new HashSet<RenameLocation>();
                    var allImplicitLocations = new HashSet<ReferenceLocation>();
                    var symbolProcessedForDefinition = new ConcurrentSet<ISymbol>();

                    CheckForNullAndUpdateFromConstituent(baseSymbolResult, allLocations, allImplicitLocations, symbolProcessedForDefinition);
                    CheckForNullAndUpdateFromConstituent(overloadedSymbolsResult, allLocations, allImplicitLocations, symbolProcessedForDefinition);
                    
                    if (this.RenameInStrings)
                    {
                        this.GetOrComputeRenameLocationsInStrings().Values.Do(perDocLocations => allLocations.AddRange(perDocLocations));
                    }

                    if (this.RenameInComments)
                    {
                        this.GetOrComputeRenameLocationsInComments().Values.Do(perDocLocations => allLocations.AddRange(perDocLocations));
                    }

                    allLocationsCompletionSource.TrySetResult(new RenameLocationSet(allLocations, symbol, solution, symbolProcessedForDefinition, allImplicitLocations));

                    // We also have make sure we complete any remaining documents that we have been
                    // asked about. It's possible we may have been asked for the locations in a
                    // document the FAR engine knew could never contain a location.
                    foreach (var documentId in perDocumentTaskCompletionSources.Keys)
                    {
                        CompleteSourceForDocument(documentId);
                    }

                    searchComplete = true;
                }
            }

            private void CheckForNullAndUpdateFromConstituent(
                ConstituentAsynchronousSetSource constituent,
                HashSet<RenameLocation> allLocations,
                HashSet<ReferenceLocation> allImplicitLocations,
                ConcurrentSet<ISymbol> symbolProcessedForDefinition)
            {
                if (constituent != null)
                {
                    foreach (var documentLocations in constituent.PerDocumentLocations.Values)
                    {
                        allLocations.AddAll(documentLocations);
                    }

                    foreach (var documentLocations in constituent.PerDocumentImplicitLocations.Values)
                    {
                        allImplicitLocations.AddAll(documentLocations);
                    }

                    foreach (var defSymbol in constituent.SymbolsProcessedForDefinitions)
                    {
                        symbolProcessedForDefinition.Add(defSymbol);
                    }
                }
            }

            /// <summary>
            /// Returns the overloaded symbol for a given symbol
            /// </summary>
            /// <param name="symbol"></param>
            /// <returns></returns>
            internal static IEnumerable<ISymbol> GetOverloadedSymbols(ISymbol symbol)
            {
                if (symbol is IMethodSymbol)
                {
                    var containingType = symbol.ContainingType;
                    if (containingType.Kind == CommonSymbolKind.NamedType)
                    {
                        var members = containingType.GetMembers();

                        foreach (var member in members)
                        {
                            if (member.MetadataName == symbol.MetadataName && member is IMethodSymbol && !member.Equals(symbol))
                            {
                                yield return member;
                            }
                        }
                    }
                }
            }

            internal void UpdateCountReferenceCompleted(int count)
            {
                lock (gate)
                {
                    this.countReferenceCompleted += count;
                }
            }

            internal void UpdateTotalCandidates(int count)
            {
                lock (gate)
                {
                    this.totalCandidates += count;
                }
            }

            /// <summary>
            /// Helps in creating a new Container i.e. (AsynchronousSetSource) from the previously existing (if any)container 
            /// </summary>
            /// <param name="previousResult"></param>
            /// <param name="optionSet"></param>
            /// <param name="symbol"></param>
            /// <param name="solution"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            internal static AsynchronousSetSource GetAsynchronousSetSource(AsynchronousSetSource previousResult, OptionSet optionSet, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            {
                var isRenamingOverloads = optionSet.GetOption(RenameOptions.RenameOverloads);
                var isRenamingInStrings = optionSet.GetOption(RenameOptions.RenameInStrings);
                var isRenamingInComments = optionSet.GetOption(RenameOptions.RenameInComments);

                // At this point, the object is created but nothing is running in it
                AsynchronousSetSource newResult = new AsynchronousSetSource(symbol, solution, cancellationToken);
                ConstituentAsynchronousSetSource baseSymbolResult;

                // Check to see if the previous result could be reused
                if (previousResult == null)
                {
                    baseSymbolResult = new ConstituentAsynchronousSetSource(newResult, RenameEntityKind.BaseSymbol, symbol, solution, cancellationToken);
                }
                else if ((isRenamingOverloads && previousResult.overloadedSymbolsResult != null) ||
                   (!isRenamingOverloads && previousResult.overloadedSymbolsResult == null))
                {
                    if (isRenamingInComments != previousResult.RenameInComments ||
                        isRenamingInStrings != previousResult.RenameInStrings)
                    {
                        previousResult.ResetTaskCompletionSources();
                        previousResult.RenameInComments = isRenamingInComments;
                        previousResult.RenameInStrings = isRenamingInStrings;
                        previousResult.TrySetCompletion();
                    }

                    return previousResult;
                }
                else
                {
                    // Reuse the baseSymbolResult from the previousResult
                    baseSymbolResult = previousResult.baseSymbolResult;
                }

                newResult.SetBaseSymbolResult(baseSymbolResult);

                if (isRenamingOverloads)
                {
                    var overloadedSymbolResult = new ConstituentAsynchronousSetSource(newResult, RenameEntityKind.OverloadedSymbols, symbol, solution, cancellationToken);
                    newResult.SetOverloadSymbolResult(overloadedSymbolResult);
                }

                newResult.RenameInComments = isRenamingInComments;
                newResult.RenameInStrings = isRenamingInStrings;
                
                newResult.TrySetCompletion();
                return newResult;
            }

            internal bool SearchComplete
            {
                get { return this.searchComplete; }
            }
            
            public Task<RenameLocationSet> GetLocationsForSingleDocumentAsync(Document document)
            {
                lock (gate)
                {
                    TaskCompletionSource<RenameLocationSet> completionSource;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!perDocumentTaskCompletionSources.TryGetValue(document.Id, out completionSource))
                    {
                        completionSource = new TaskCompletionSource<RenameLocationSet>();
                        perDocumentTaskCompletionSources.Add(document.Id, completionSource);

                        // If cannot be sure until the very end of the rename the list of document that can contain the locations
                        if (searchComplete)
                        {
                            CompleteSourceForDocument(document.Id);
                        }
                    }

                    return completionSource.Task;
                }
            }

            public Task<RenameLocationSet> GetAllLocationsAsync()
            {
                return allLocationsCompletionSource.Task;
            }

            internal void OnCompletedTask(int count)
            {
                // From some one constituent completed
                UpdateCountReferenceCompleted(count);

                CheckForCompletionAndTrySetValue();
            }

            internal void OnFindInDocumentCompletedTask(Document document, int updatingCount)
            {
            }

            private static void AddRenameLocations(ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> perDocumentLocations, Solution solution, IEnumerable<RenameLocation> renameLocations)
            {
                foreach (var location in renameLocations)
                {
                    var documentForLocation = solution.GetDocument(location.Location.SourceTree);
                    AddLocation(perDocumentLocations, documentForLocation, location);
                }
            }

            private static void AddLocation(ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> perDocumentLocations, Document document, RenameLocation location)
            {
                perDocumentLocations.GetOrAdd(document.Id, _ => new ConcurrentSet<RenameLocation>()).Add(location);
            }

            /// <summary>
            /// Completes a per-document-locations task for the given document. This method is
            /// idempotent.
            /// </summary>
            private void CompleteSourceForDocument(DocumentId documentId)
            {
                lock (gate)
                {
                    // Complete the source for the given document. TaskContinuationSource's
                    // TrySetResult method will only allow the result to be set once, so any
                    // repeated sets are guaranteed to leave the task untouched.
                    var renameLocation = new HashSet<RenameLocation>();
                    ConcurrentSet<RenameLocation> renameLocations = null;

                    // Check with the Base Symbol Result
                    if (baseSymbolResult != null && baseSymbolResult.PerDocumentLocations.TryGetValue(documentId, out renameLocations))
                    {
                        renameLocation.AddRange(renameLocations.ToSet());
                    }

                    // Check with the overloaded Symbols Result
                    if (overloadedSymbolsResult != null && overloadedSymbolsResult.PerDocumentLocations.TryGetValue(documentId, out renameLocations))
                    {
                        renameLocation.AddRange(renameLocations.ToSet());
                    }

                    if (this.RenameInStrings && this.GetOrComputeRenameLocationsInStrings().TryGetValue(documentId, out renameLocations))
                    {
                        renameLocation.AddRange(renameLocations.ToSet());
                    }

                    if (this.RenameInComments && this.GetOrComputeRenameLocationsInComments().TryGetValue(documentId, out renameLocations))
                    {
                        renameLocation.AddRange(renameLocations.ToSet());
                    }

                    perDocumentTaskCompletionSources[documentId].TrySetResult(new RenameLocationSet(renameLocation, symbol, solution, SpecializedCollections.EmptyEnumerable<ISymbol>(), SpecializedCollections.EmptyEnumerable<ReferenceLocation>()));
                }
            }

            private void OnCancellationTokenCancelled()
            {
                // Make sure we cancel off all tasks we've started
                lock (gate)
                {
                    foreach (var completionSource in perDocumentTaskCompletionSources.Values)
                    {
                        completionSource.TrySetCanceled();
                    }

                    allLocationsCompletionSource.TrySetCanceled();
                }
            }

            /// <summary>
            /// This class is responsible for running the FAR tasks obtaining the results and propagating the results to
            /// the container<see cref="AsynchronousSetSource"/>
            /// </summary>
            internal class ConstituentAsynchronousSetSource : IFindReferencesProgress
            {
                private readonly RenameEntityKind renameEntityKind;

                private readonly Solution solution;
                private readonly ISymbol symbol;
                private readonly CancellationToken cancellationToken;
                
                /// <summary>
                /// This gate is used specifically for guarding the updation of the non-thread-safe pendingTasksForDefinitionAndReference
                /// </summary>
                private readonly object taskUpdateGate = new object();
                private List<Task> pendingTasksForDefinitionAndReference;

                private readonly ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> perDocumentLocations;
                private readonly ConcurrentDictionary<DocumentId, ConcurrentSet<ReferenceLocation>> perDocumentImplicitLocations;
                private readonly ConcurrentSet<ISymbol> symbolsProcessedForDefinitions;

                private bool notStarted = true;
                private int totalCandidates = 0;

                /// <summary>
                /// This gate guards the non-thread-safe fields in this class, namely the fields after it here.
                /// </summary>
                private readonly object gate = new object();

                /// <summary>
                /// The container is reported of the result updation and subsequently the container produces the 
                /// result for the rename engine
                /// </summary>
                private AsynchronousSetSource container;
                private int countReferenceCompleted = 0;
                private bool searchComplete;

                public ConstituentAsynchronousSetSource(AsynchronousSetSource container, RenameEntityKind renameEntityKind, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
                {
                    Debug.Assert(container != null, "The Container AsynchronousSetSource can never be null");
                    this.symbol = symbol;
                    this.solution = solution;
                    this.cancellationToken = cancellationToken;
                    this.container = container;
                    this.renameEntityKind = renameEntityKind;

                    this.perDocumentLocations = new ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>>();
                    this.perDocumentImplicitLocations = new ConcurrentDictionary<DocumentId, ConcurrentSet<ReferenceLocation>>();
                    this.symbolsProcessedForDefinitions = new ConcurrentSet<ISymbol>();

                    this.pendingTasksForDefinitionAndReference = new List<Task>();
                }

                internal void UpdateContainerWithCurrentResult()
                {
                    if (this.searchComplete)
                    {
                        this.container.UpdateCountReferenceCompleted(this.totalCandidates);
                    }
                }

                internal void UpdateContainterWithTotalCandidates()
                {
                    this.container.UpdateTotalCandidates(this.totalCandidates);
                }

                internal void UpdateContainerAndResult(AsynchronousSetSource container)
                {
                    lock (gate)
                    {
                        // TODO : Dispose things in the old container if any;
                        this.container = container;
                    }

                    UpdateContainerWithCurrentResult();
                    UpdateContainterWithTotalCandidates();
                    if (notStarted)
                    {
                        StartCalculating();
                    }
                }

                internal void StartCalculating()
                {
                    Debug.Assert(notStarted, "You cannot trigger the calculation more than once");
                    var registration = cancellationToken.Register(OnCancellationTokenCancelled);

                    lock (gate)
                    {
                        Task<IEnumerable<ReferencedSymbol>> taskForFAR = null;

                        if (this.renameEntityKind == RenameEntityKind.OverloadedSymbols)
                        {
                            // Make sure that the result has a container to report the result
                            Debug.Assert(container != null);
                            if (GetOverloadedSymbols(symbol).Any())
                            {
                                var overloadedSymbols = GetOverloadedSymbols(symbol);
                                totalCandidates = overloadedSymbols.Count();
                                this.container.UpdateTotalCandidates(totalCandidates);
                                foreach (var overload in overloadedSymbols)
                                {
                                    taskForFAR = SymbolFinder.FindReferencesAsync(overload, solution, progress: this, documents: null, cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                // Since there is no symbol to search for we mark the search as complete
                                this.searchComplete = true;
                            }
                        }
                        else
                        {
                            totalCandidates = 1;
                            this.container.UpdateTotalCandidates(totalCandidates);
                            taskForFAR = SymbolFinder.FindReferencesAsync(this.symbol, solution, progress: this, documents: null, cancellationToken: cancellationToken);
                        }

                        if (taskForFAR != null)
                        {
                            taskForFAR.ContinueWith(t => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        }

                        notStarted = false;
                    }
                }

                internal bool HasNotStarted
                {
                    get { return this.notStarted; }
                }

                internal AsynchronousSetSource Container
                {
                    get { return this.container; }
                }

                internal ConcurrentDictionary<DocumentId, ConcurrentSet<RenameLocation>> PerDocumentLocations
                {
                    get { return this.perDocumentLocations; }
                }

                internal ConcurrentDictionary<DocumentId, ConcurrentSet<ReferenceLocation>> PerDocumentImplicitLocations
                {
                    get { return this.perDocumentImplicitLocations; }
                }

                internal ConcurrentSet<ISymbol> SymbolsProcessedForDefinitions
                {
                    get { return this.symbolsProcessedForDefinitions; }
                }

                private void AddImplicitLocation(ReferenceLocation location)
                {
                    perDocumentImplicitLocations.GetOrAdd(location.Document.Id, _ => new ConcurrentSet<ReferenceLocation>()).Add(location);
                }

                private void OnCancellationTokenCancelled()
                {
                    this.container.OnCancellationTokenCancelled();
                }

                #region IFindReferencesProgress Implementation

                void IFindReferencesProgress.ReportProgress(int current, int maximum)
                {
                }

                void IFindReferencesProgress.OnStarted()
                {
                }

                void IFindReferencesProgress.OnCompleted()
                {
                    // FAR for one Symbol is done so increment it
                    lock (gate)
                    {
                        countReferenceCompleted++;
                    }

                    if (countReferenceCompleted < totalCandidates)
                    {
                        return;
                    }

                    lock (gate)
                    {
                        searchComplete = true;

                        // We wait for all the Renamable Definition and Reference Task to complete before marking this search progress complete.
                        var taskToTrackCompletionOfLocationAddition = Task.WhenAll(this.pendingTasksForDefinitionAndReference);
                        taskToTrackCompletionOfLocationAddition.SafeContinueWith(
                            t => this.container.OnCompletedTask(countReferenceCompleted),
                            this.cancellationToken,
                            TaskContinuationOptions.OnlyOnRanToCompletion,
                            TaskScheduler.Default);
                    }
                }

                void IFindReferencesProgress.OnFindInDocumentStarted(Document document)
                {
                }

                void IFindReferencesProgress.OnFindInDocumentCompleted(Document document)
                {
                }

                void IFindReferencesProgress.OnDefinitionFound(ISymbol symbol)
                {
                    if (symbolsProcessedForDefinitions.Add(symbol))
                    {
                        lock (taskUpdateGate)
                        {
                            this.pendingTasksForDefinitionAndReference.Add(Task.Run(() => OnDefinitionFoundWorkerAsync(symbol), this.cancellationToken));
                        }
                    }
                }

                private void OnDefinitionFoundWorkerAsync(ISymbol symbol)
                {
                    var locations = ReferenceProcessing.GetRenamableDefinitionLocationsAsync(symbol, this.symbol, this.solution, this.cancellationToken).WaitAndGetResult(this.cancellationToken);
                    AddRenameLocations(this.perDocumentLocations, this.solution, locations);
                }

                void IFindReferencesProgress.OnReferenceFound(ISymbol symbol, ReferenceLocation location)
                {
                    lock (taskUpdateGate)
                    {
                        this.pendingTasksForDefinitionAndReference.Add(Task.Run(() => OnReferenceFoundWorkerAsync(symbol, location), this.cancellationToken));
                    }
                }

                private void OnReferenceFoundWorkerAsync(ISymbol symbol, ReferenceLocation location)
                {
                    var locations = ReferenceProcessing.GetRenamableReferenceLocationsAsync(symbol, this.symbol, location, this.solution, this.cancellationToken).WaitAndGetResult(this.cancellationToken);
                    foreach (var renamableLocation in locations)
                    {
                        AddLocation(this.perDocumentLocations, location.Document, renamableLocation);
                    }

                    if (location.IsImplicit)
                    { 
                        AddImplicitLocation(location);
                    }
                }

                #endregion
            }
        }
    }
}
