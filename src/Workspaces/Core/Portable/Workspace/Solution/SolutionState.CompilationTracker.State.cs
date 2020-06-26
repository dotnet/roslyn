// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private partial class CompilationTracker
        {
            /// <summary>
            /// The base type of all <see cref="CompilationTracker"/> states. The state of a <see cref="CompilationTracker" />
            /// starts at <see cref="Empty"/>, and then will progress through the other states until it finally reaches
            /// <see cref="FinalState" />.
            /// </summary>
            private class State
            {
                /// <summary>
                /// The base <see cref="State"/> that starts with everything empty.
                /// </summary>
                public static readonly State Empty = new State(
                    compilation: null, declarationOnlyCompilation: null,
                    generatorDriver: new TrackedGeneratorDriver(null));

                /// <summary>
                /// A strong reference to the declaration-only compilation. This compilation isn't used to produce symbols,
                /// nor does it have any references. It just holds the declaration table alive.
                /// </summary>
                public Compilation? DeclarationOnlyCompilation { get; }

                /// <summary>
                /// The best compilation that is available that source generators have not ran on. May be an in-progress, full declaration,
                /// a final compilation, or <see langword="null"/>.
                /// </summary>
                public ValueSource<Optional<Compilation>>? Compilation { get; }

                public TrackedGeneratorDriver GeneratorDriver { get; }

                /// <summary>
                /// Weak table of the assembly, module and dynamic symbols that this compilation tracker has created.
                /// This can be used to determine which project an assembly symbol came from after the fact.  This is
                /// needed as the compilation an assembly came from can be GC'ed and further requests to get that
                /// compilation (or any of it's assemblies) may produce new assembly symbols.
                /// </summary>
                /// <remarks>
                /// Ideally this would just be <c>ConditionalWeakSet&lt;ISymbol&gt;</c>.  Effectively we just want to
                /// hold onto the symbols as long as someone else is keeping them alive.  And we don't actually need
                /// them to map to anything.  We just use their existence to know if our project was the project it came
                /// from.  However, ConditionalWeakTable is the best tool we have, so we simulate a set by just using a
                /// table and mapping the keys to the <see langword="null"/> value.
                /// </remarks>
                public readonly ConditionalWeakTable<ISymbol, object?>? UnrootedSymbolSet;

                /// <summary>
                /// Specifies whether <see cref="FinalCompilation"/> and all compilations it depends on contain full information or not. This can return
                /// <see langword="null"/> if the state isn't at the point where it would know, and it's necessary to transition to <see cref="FinalState"/> to figure that out.
                /// </summary>
                public virtual bool? HasSuccessfullyLoaded => null;

                /// <summary>
                /// The final compilation if available, otherwise <see langword="null"/>.
                /// </summary>
                public virtual ValueSource<Optional<Compilation>>? FinalCompilation => null;

                protected State(
                    ValueSource<Optional<Compilation>>? compilation,
                    Compilation? declarationOnlyCompilation,
                    TrackedGeneratorDriver generatorDriver)
                {
                    // Declaration-only compilations should never have any references
                    Contract.ThrowIfTrue(declarationOnlyCompilation != null && declarationOnlyCompilation.ExternalReferences.Any());

                    Compilation = GetWrappedCompilation(compilation);
                    DeclarationOnlyCompilation = declarationOnlyCompilation;
                    GeneratorDriver = generatorDriver;
                    if (Compilation != null)
                        UnrootedSymbolSet = new ConditionalWeakTable<ISymbol, object?>();
                }

                protected ValueSource<Optional<Compilation>>? GetWrappedCompilation(
                    ValueSource<Optional<Compilation>>? compilation)
                {
                    if (compilation == null)
                        return null;

                    return new ComputeUnrootedSymbolsValueSource(this, compilation);
                }

                private class ComputeUnrootedSymbolsValueSource : ValueSource<Optional<Compilation>>
                {
                    private readonly State _state;
                    private readonly ValueSource<Optional<Compilation>> _compilation;

                    // Zero means we haven't computed the values.  Non zero means we have.  Multiple threads can compute
                    // the values safely.  They'll just stomp over each other with the same results.
                    private bool _computedUnrootedSymbols;

                    public ComputeUnrootedSymbolsValueSource(State state, ValueSource<Optional<Compilation>> compilation)
                    {
                        _state = state;
                        _compilation = compilation;
                    }

                    public override Optional<Compilation> GetValue(CancellationToken cancellationToken = default)
                    {
                        var result = _compilation.GetValue(cancellationToken);
                        ComputeUnrootedSymbols(result);
                        return result;
                    }

                    public override async Task<Optional<Compilation>> GetValueAsync(CancellationToken cancellationToken = default)
                    {
                        var result = await _compilation.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        ComputeUnrootedSymbols(result);
                        return result;
                    }

                    public override bool TryGetValue([MaybeNullWhen(false)] out Optional<Compilation> value)
                    {
                        if (!_compilation.TryGetValue(out var temp))
                        {
                            value = default;
                            return false;
                        }

                        ComputeUnrootedSymbols(temp);
                        value = temp;
                        return true;
                    }

                    private void ComputeUnrootedSymbols(Optional<Compilation> compilationOpt)
                    {
                        if (!compilationOpt.HasValue)
                            return;

                        Contract.ThrowIfNull(_state.UnrootedSymbolSet);
                        var compilation = compilationOpt.Value;
                        if (Volatile.Read(ref _computedUnrootedSymbols) == false)
                        {
                            AddUnrootedSymbols(compilation, _state.UnrootedSymbolSet);
                            Volatile.Write(ref _computedUnrootedSymbols, true);
                        }
                    }
                }

                public static State Create(
                    Compilation compilation,
                    TrackedGeneratorDriver generatorDriver,
                    ImmutableArray<ValueTuple<ProjectState, CompilationAndGeneratorDriverTranslationAction>> intermediateProjects)
                {
                    Contract.ThrowIfNull(compilation);
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);

                    // If we don't have any intermediate projects to process, just initialize our
                    // DeclarationState now.
                    return intermediateProjects.Length == 0
                        ? new FullDeclarationState(compilation, generatorDriver)
                        : (State)new InProgressState(compilation, generatorDriver, intermediateProjects);
                }

                public static ValueSource<Optional<Compilation>> CreateValueSource(
                    Compilation compilation,
                    SolutionServices services)
                {
                    return services.SupportsCachingRecoverableObjects
                        ? new WeakValueSource<Compilation>(compilation)
                        : (ValueSource<Optional<Compilation>>)new ConstantValueSource<Optional<Compilation>>(compilation);
                }

                public static void AddUnrootedSymbols(Compilation compilation, ConditionalWeakTable<ISymbol, object?> unrootedSymbols)
                {
                    var compAssembly = compilation.Assembly;
                    unrootedSymbols.Add(compAssembly, null);

                    // The dynamic type is also unrooted (i.e. doesn't point back at the compilation or source
                    // assembly).  So we have to keep track of it so we can get back from it to a project in case the 
                    // underlying compilation is GC'ed.
                    if (compilation.Language == LanguageNames.CSharp)
                        unrootedSymbols.Add(compilation.DynamicType, null);

                    foreach (var reference in compilation.References)
                    {
                        var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                        if (symbol == null)
                            continue;

                        unrootedSymbols.Add(symbol, null);
                    }
                }
            }

            /// <summary>
            /// A state where we are holding onto a previously built compilation, and have a known set of transformations
            /// that could get us to a more final state.
            /// </summary>
            private sealed class InProgressState : State
            {
                public ImmutableArray<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> IntermediateProjects { get; }

                public InProgressState(
                    Compilation inProgressCompilation,
                    TrackedGeneratorDriver inProgressGeneratorDriver,
                    ImmutableArray<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> intermediateProjects)
                    : base(compilation: new ConstantValueSource<Optional<Compilation>>(inProgressCompilation),
                           declarationOnlyCompilation: null,
                           generatorDriver: inProgressGeneratorDriver)
                {
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);
                    Contract.ThrowIfFalse(intermediateProjects.Length > 0);

                    this.IntermediateProjects = intermediateProjects;
                }
            }

            /// <summary>
            /// Declaration-only state that has no associated references or symbols. just declaration table only.
            /// </summary>
            private sealed class LightDeclarationState : State
            {
                public LightDeclarationState(Compilation declarationOnlyCompilation)
                    : base(compilation: null,
                           declarationOnlyCompilation: declarationOnlyCompilation,
                           generatorDriver: new TrackedGeneratorDriver(null))
                {
                }
            }

            /// <summary>
            /// A built compilation for the tracker that contains the fully built DeclarationTable,
            /// but may not have references initialized
            /// </summary>
            private sealed class FullDeclarationState : State
            {
                public FullDeclarationState(Compilation declarationCompilation, TrackedGeneratorDriver generatorDriver)
                    : base(new WeakValueSource<Compilation>(declarationCompilation),
                           declarationCompilation.Clone().RemoveAllReferences(),
                           generatorDriver)
                {
                }
            }

            /// <summary>
            /// The final state a compilation tracker reaches. The <see cref="State.DeclarationOnlyCompilation"/> is available,
            /// as well as the real <see cref="State.FinalCompilation"/>.
            /// </summary>
            private sealed class FinalState : State
            {
                public override bool? HasSuccessfullyLoaded { get; }

                /// <summary>
                /// The final compilation, with all references and source generators run. This is distinct from
                /// <see cref="Compilation"/>, which in the <see cref="FinalState"/> case will be the compilation
                /// before any source generators were ran. This ensures that a later invocation of the source generators
                /// consumes <see cref="Compilation"/> which will avoid generators being ran a second time on a compilation that
                /// already contains the output of other generators. If source generators are not active, this is equal to <see cref="Compilation"/>.
                /// </summary>
                public override ValueSource<Optional<Compilation>>? FinalCompilation { get; }

                public FinalState(
                    ValueSource<Optional<Compilation>> finalCompilationSource,
                    ValueSource<Optional<Compilation>> compilationWithoutGeneratedFilesSource,
                    Compilation compilationWithoutGeneratedFiles,
                    TrackedGeneratorDriver generatorDriver,
                    bool hasSuccessfullyLoaded)
                    : base(compilationWithoutGeneratedFilesSource,
                           compilationWithoutGeneratedFiles.Clone().RemoveAllReferences(),
                           generatorDriver)
                {
                    HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                    FinalCompilation = GetWrappedCompilation(finalCompilationSource);

#if DEBUG

                    if (generatorDriver.GeneratorDriver == null)
                    {
                        // In this case, the finalCompilationSource and compilationWithoutGeneratedFilesSource should point to the
                        // same Compilation, which should be compilationWithoutGeneratedFiles itself
                        Debug.Assert(finalCompilationSource.TryGetValue(out var finalCompilation));
                        Debug.Assert(object.ReferenceEquals(finalCompilation.Value, compilationWithoutGeneratedFiles));
                    }

#endif
                }
            }
        }
    }
}
