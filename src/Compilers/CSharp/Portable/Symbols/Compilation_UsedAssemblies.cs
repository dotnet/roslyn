// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpCompilation
    {
        private ConcurrentSet<AssemblySymbol>? _lazyUsedAssemblyReferences;
        private bool _usedAssemblyReferencesFrozen;

        public override ImmutableArray<MetadataReference> GetUsedAssemblyReferences(CancellationToken cancellationToken = default)
        {
            ConcurrentSet<AssemblySymbol>? usedAssemblies = GetCompleteSetOfUsedAssemblies(cancellationToken);

            if (usedAssemblies is null)
            {
                return ImmutableArray<MetadataReference>.Empty;
            }

            var setOfReferences = new HashSet<MetadataReference>(ReferenceEqualityComparer.Instance);
            ImmutableDictionary<MetadataReference, ImmutableArray<MetadataReference>> mergedAssemblyReferencesMap = GetBoundReferenceManager().MergedAssemblyReferencesMap;

            foreach (var reference in References)
            {
                if (reference.Properties.Kind == MetadataImageKind.Assembly)
                {
                    Symbol? symbol = GetBoundReferenceManager().GetReferencedAssemblySymbol(reference);
                    if (symbol is object && usedAssemblies.Contains((AssemblySymbol)symbol) &&
                        setOfReferences.Add(reference) &&
                        mergedAssemblyReferencesMap.TryGetValue(reference, out ImmutableArray<MetadataReference> merged))
                    {
                        // Include all "merged" references as well because they might "define" used extern aliases.
                        setOfReferences.AddAll(merged);
                    }
                }
            }

            // Use stable ordering for the result, matching the order in References.
            var builder = ArrayBuilder<MetadataReference>.GetInstance(setOfReferences.Count);

            foreach (var reference in References)
            {
                if (setOfReferences.Contains(reference))
                {
                    builder.Add(reference);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private ConcurrentSet<AssemblySymbol>? GetCompleteSetOfUsedAssemblies(CancellationToken cancellationToken)
        {
            if (!_usedAssemblyReferencesFrozen && !Volatile.Read(ref _usedAssemblyReferencesFrozen))
            {
                var diagnostics = BindingDiagnosticBag.GetConcurrentInstance();
                RoslynDebug.Assert(diagnostics.DiagnosticBag is object);

                GetDiagnosticsWithoutSeverityFiltering(CompilationStage.Declare, includeEarlierStages: true, diagnostics, symbolFilter: null, cancellationToken);

                bool seenErrors = diagnostics.HasAnyErrors();
                if (!seenErrors)
                {
                    diagnostics.DiagnosticBag.Clear();
                    GetDiagnosticsForAllMethodBodies(diagnostics, doLowering: true, cancellationToken);
                    seenErrors = diagnostics.HasAnyErrors();

                    if (!seenErrors)
                    {
                        AddUsedAssemblies(diagnostics.DependenciesBag);
                    }
                }

                completeTheSetOfUsedAssemblies(seenErrors, cancellationToken);

                diagnostics.Free();
            }

            return _lazyUsedAssemblyReferences;

            void addUsedAssembly(AssemblySymbol dependency, ArrayBuilder<AssemblySymbol> stack)
            {
                if (AddUsedAssembly(dependency))
                {
                    stack.Push(dependency);
                }
            }

            void addReferencedAssemblies(AssemblySymbol assembly, bool includeMainModule, ArrayBuilder<AssemblySymbol> stack)
            {
                for (int i = (includeMainModule ? 0 : 1); i < assembly.Modules.Length; i++)
                {
                    foreach (var dependency in assembly.Modules[i].ReferencedAssemblySymbols)
                    {
                        addUsedAssembly(dependency, stack);
                    }
                }
            }

            void completeTheSetOfUsedAssemblies(bool seenErrors, CancellationToken cancellationToken)
            {
                if (_usedAssemblyReferencesFrozen || Volatile.Read(ref _usedAssemblyReferencesFrozen))
                {
                    return;
                }

                if (seenErrors)
                {
                    // Add all referenced assemblies
                    foreach (var assembly in SourceModule.ReferencedAssemblySymbols)
                    {
                        AddUsedAssembly(assembly);
                    }
                }
                else
                {
                    // Assume that all assemblies used by the added modules are also used
                    for (int i = 1; i < SourceAssembly.Modules.Length; i++)
                    {
                        foreach (var dependency in SourceAssembly.Modules[i].ReferencedAssemblySymbols)
                        {
                            AddUsedAssembly(dependency);
                        }
                    }

                    if (_usedAssemblyReferencesFrozen || Volatile.Read(ref _usedAssemblyReferencesFrozen))
                    {
                        return;
                    }

                    // Assume that all assemblies used by the used assemblies are also used
                    // This, for example, takes care of including facade assemblies that forward types around.
                    if (_lazyUsedAssemblyReferences is object)
                    {
                        lock (_lazyUsedAssemblyReferences)
                        {
                            if (_usedAssemblyReferencesFrozen || Volatile.Read(ref _usedAssemblyReferencesFrozen))
                            {
                                return;
                            }

                            var stack = ArrayBuilder<AssemblySymbol>.GetInstance(_lazyUsedAssemblyReferences.Count);
                            stack.AddRange(_lazyUsedAssemblyReferences);

                            while (stack.Count != 0)
                            {
                                AssemblySymbol current = stack.Pop();
                                ConcurrentSet<AssemblySymbol>? usedAssemblies;

                                switch (current)
                                {
                                    case SourceAssemblySymbol sourceAssembly:
                                        // The set of assemblies used by the referenced compilation feels like
                                        // a reasonable approximation to the set of assembly references that would
                                        // be emitted into the resulting binary for that compilation. An alternative
                                        // would be to attempt to emit and get the exact set of emitted references
                                        // in case of success. This might be too slow though.
                                        usedAssemblies = sourceAssembly.DeclaringCompilation.GetCompleteSetOfUsedAssemblies(cancellationToken);
                                        if (usedAssemblies is object)
                                        {
                                            foreach (AssemblySymbol dependency in usedAssemblies)
                                            {
                                                Debug.Assert(!dependency.IsLinked);
                                                addUsedAssembly(dependency, stack);
                                            }
                                        }
                                        break;

                                    case RetargetingAssemblySymbol retargetingAssembly:
                                        usedAssemblies = retargetingAssembly.UnderlyingAssembly.DeclaringCompilation.GetCompleteSetOfUsedAssemblies(cancellationToken);
                                        if (usedAssemblies is object)
                                        {
                                            foreach (AssemblySymbol underlyingDependency in retargetingAssembly.UnderlyingAssembly.SourceModule.ReferencedAssemblySymbols)
                                            {
                                                if (!underlyingDependency.IsLinked && usedAssemblies.Contains(underlyingDependency))
                                                {
                                                    AssemblySymbol dependency;

                                                    if (!((RetargetingModuleSymbol)retargetingAssembly.Modules[0]).RetargetingDefinitions(underlyingDependency, out dependency))
                                                    {
                                                        Debug.Assert(retargetingAssembly.Modules[0].ReferencedAssemblySymbols.Contains(underlyingDependency));
                                                        dependency = underlyingDependency;
                                                    }

                                                    addUsedAssembly(dependency, stack);
                                                }
                                            }
                                        }

                                        addReferencedAssemblies(retargetingAssembly, includeMainModule: false, stack);
                                        break;
                                    default:
                                        addReferencedAssemblies(current, includeMainModule: true, stack);
                                        break;
                                }
                            }

                            stack.Free();
                        }
                    }

                    if (SourceAssembly.CorLibrary is object)
                    {
                        // Add core library
                        AddUsedAssembly(SourceAssembly.CorLibrary);
                    }
                }

                _usedAssemblyReferencesFrozen = true;
            }
        }

        internal void AddUsedAssemblies(ICollection<AssemblySymbol>? assemblies)
        {
            if (!assemblies.IsNullOrEmpty())
            {
                foreach (var candidate in assemblies)
                {
                    AddUsedAssembly(candidate);
                }
            }
        }

        internal bool AddUsedAssembly(AssemblySymbol? assembly)
        {
            if (assembly is null || assembly == SourceAssembly || assembly.IsMissing)
            {
                return false;
            }

            if (_lazyUsedAssemblyReferences is null)
            {
                Interlocked.CompareExchange(ref _lazyUsedAssemblyReferences, new ConcurrentSet<AssemblySymbol>(), null);
            }

#if DEBUG
            bool wasFrozen = _usedAssemblyReferencesFrozen;
#endif
            bool added = _lazyUsedAssemblyReferences.Add(assembly);

#if DEBUG
            Debug.Assert(!added || !wasFrozen);
#endif
            return added;
        }

    }
}
