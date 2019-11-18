// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpCompilation
    {
        private ConcurrentSet<AssemblySymbol> _lazyUsedAssemblyReferences;
        private bool _usedAssemblyReferencesFrozen;

        internal override ImmutableArray<MetadataReference> GetUsedAssemblyReferences(CancellationToken cancellationToken = default)
        {
            ConcurrentSet<AssemblySymbol> usedAssemblies = GetCompleteSetOfUsedAssemblies(cancellationToken);

            if (usedAssemblies is null)
            {
                return ImmutableArray<MetadataReference>.Empty;
            }

            var builder = ArrayBuilder<MetadataReference>.GetInstance(usedAssemblies.Count);

            foreach (var reference in References)
            {
                if (reference.Properties.Kind == MetadataImageKind.Assembly &&
                    usedAssemblies.Contains((AssemblySymbol)GetAssemblyOrModuleSymbol(reference)))
                {
                    builder.Add(reference);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private ConcurrentSet<AssemblySymbol> GetCompleteSetOfUsedAssemblies(CancellationToken cancellationToken)
        {
            if (!_usedAssemblyReferencesFrozen && !Volatile.Read(ref _usedAssemblyReferencesFrozen))
            {
                // PROTOTYPE(UsedAssemblyReferences): Try to optimize scenarios when GetDiagnostics was called before
                //                                    and we either already encountered errors, or have done all the work 
                //                                    to record usage.
                var diagnostics = DiagnosticBag.GetInstance();
                GetDiagnostics(CompilationStage.Compile, includeEarlierStages: true, diagnostics, cancellationToken);

                completeTheSetOfUsedAssemblies(diagnostics, cancellationToken);

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

            void completeTheSetOfUsedAssemblies(DiagnosticBag diagnostics, CancellationToken cancellationToken)
            {
                if (_usedAssemblyReferencesFrozen || Volatile.Read(ref _usedAssemblyReferencesFrozen))
                {
                    return;
                }

                if (diagnostics.HasAnyErrors())
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

                    // Assume that all assemblies used by the used assemblies are also used
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
                                ConcurrentSet<AssemblySymbol> usedAssemblies;

                                switch (current)
                                {
                                    case SourceAssemblySymbol sourceAssembly:
                                        // PROTOTYPE(UsedAssemblyReferences): The set of assemblies used by the referenced compilation feels like
                                        //                                    a reasonable approximation to the set of assembly references that would
                                        //                                    be emitted into the resulting binary for that compilation. An alternative
                                        //                                    would be to attempt to emit and get the exact set of emitted references
                                        //                                    in case of success. This might be too slow though.
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

        internal bool AddUsedAssembly(AssemblySymbol assembly)
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
            bool result = _lazyUsedAssemblyReferences.Add(assembly);

#if DEBUG
            Debug.Assert(!result || !wasFrozen);
#endif
            return result;
        }

        internal void AddAssembliesUsedByTypeReference(TypeSymbol typeOpt)
        {
            while (true)
            {
                switch (typeOpt)
                {
                    case null:
                    case TypeParameterSymbol _:
                    case DynamicTypeSymbol _:
                        return;
                    case PointerTypeSymbol pointer:
                        typeOpt = pointer.PointedAtTypeWithAnnotations.DefaultType;
                        break;
                    case ArrayTypeSymbol array:
                        typeOpt = array.ElementTypeWithAnnotations.DefaultType;
                        break;
                    case NamedTypeSymbol named:
                        named = named.TupleUnderlyingTypeOrSelf();
                        AddUsedAssembly(named.ContainingAssembly);
                        do
                        {
                            foreach (var typeArgument in named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                            {
                                AddAssembliesUsedByTypeReference(typeArgument.DefaultType);
                            }

                            named = named.ContainingType;
                        }
                        while (named is object);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeOpt.TypeKind);
                }
            }
        }

        internal void AddAssembliesUsedByNamespaceReference(NamespaceSymbol ns)
        {
            // Treat all assemblies contributing to this namespace symbol as used
            if (ns.Extent.Kind == NamespaceKind.Compilation)
            {
                foreach (var constituent in ns.ConstituentNamespaces)
                {
                    AddAssembliesUsedByNamespaceReference(constituent);
                }
            }
            else
            {
                AddUsedAssembly(ns.ContainingAssembly);
            }
        }
    }
}
