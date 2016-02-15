// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Manages anonymous types created on module level. All requests for anonymous type symbols 
    /// go via the instance of this class, the symbol will be either created or returned from cache.
    /// </summary>
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Cache of created anonymous type templates used as an implementation of anonymous 
        /// types in emit phase.
        /// </summary>
        private ConcurrentDictionary<string, AnonymousTypeTemplateSymbol> _lazyAnonymousTypeTemplates;

        /// <summary>
        /// Maps delegate signature shape (number of parameters and their ref-ness) to a synthesized generic delegate symbol.
        /// Unlike anonymous types synthesized delegates are not available through symbol APIs. They are only used in lowered bound trees.
        /// Currently used for dynamic call-site sites whose signature doesn't match any of the well-known Func or Action types.
        /// </summary>
        private ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> _lazySynthesizedDelegates;

        private struct SynthesizedDelegateKey : IEquatable<SynthesizedDelegateKey>
        {
            private readonly BitVector _byRefs;
            private readonly ushort _parameterCount;
            private readonly bool _returnsVoid;
            private readonly int _generation;

            public SynthesizedDelegateKey(int parameterCount, BitVector byRefs, bool returnsVoid, int generation)
            {
                _parameterCount = (ushort)parameterCount;
                _returnsVoid = returnsVoid;
                _generation = generation;
                _byRefs = byRefs;
            }

            public string MakeTypeName()
            {
                return GeneratedNames.MakeDynamicCallSiteDelegateName(_byRefs, _returnsVoid, _generation);
            }

            public override bool Equals(object obj)
            {
                return obj is SynthesizedDelegateKey && Equals((SynthesizedDelegateKey)obj);
            }

            public bool Equals(SynthesizedDelegateKey other)
            {
                return _parameterCount == other._parameterCount
                    && _returnsVoid == other._returnsVoid
                    && _generation == other._generation
                    && _byRefs.Equals(other._byRefs);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(
                    Hash.Combine((int)_parameterCount, _generation),
                    Hash.Combine(_returnsVoid.GetHashCode(), _byRefs.GetHashCode()));
            }
        }

        private struct SynthesizedDelegateValue
        {
            public readonly SynthesizedDelegateSymbol Delegate;

            // the manager that created this delegate:
            public readonly AnonymousTypeManager Manager;

            public SynthesizedDelegateValue(AnonymousTypeManager manager, SynthesizedDelegateSymbol @delegate)
            {
                Debug.Assert(manager != null && (object)@delegate != null);
                this.Manager = manager;
                this.Delegate = @delegate;
            }
        }

#if DEBUG
        /// <summary>
        /// Holds a collection of all the locations of anonymous types and delegates from source
        /// </summary>
        private readonly ConcurrentDictionary<Location, bool> _sourceLocationsSeen = new ConcurrentDictionary<Location, bool>();
#endif

        [Conditional("DEBUG")]
        private void CheckSourceLocationSeen(AnonymousTypePublicSymbol anonymous)
        {
#if DEBUG
            Location location = anonymous.Locations[0];
            if (location.IsInSource)
            {
                if (this.AreTemplatesSealed)
                {
                    Debug.Assert(_sourceLocationsSeen.ContainsKey(location));
                }
                else
                {
                    _sourceLocationsSeen.TryAdd(location, true);
                }
            }
#endif
        }

        private ConcurrentDictionary<string, AnonymousTypeTemplateSymbol> AnonymousTypeTemplates
        {
            get
            {
                // Lazily create a template types cache
                if (_lazyAnonymousTypeTemplates == null)
                {
                    CSharpCompilation previousSubmission = this.Compilation.PreviousSubmission;

                    // TODO (tomat): avoid recursion
                    var previousCache = (previousSubmission == null) ? null : previousSubmission.AnonymousTypeManager.AnonymousTypeTemplates;

                    Interlocked.CompareExchange(ref _lazyAnonymousTypeTemplates,
                                                previousCache == null
                                                    ? new ConcurrentDictionary<string, AnonymousTypeTemplateSymbol>()
                                                    : new ConcurrentDictionary<string, AnonymousTypeTemplateSymbol>(previousCache),
                                                null);
                }

                return _lazyAnonymousTypeTemplates;
            }
        }

        private ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> SynthesizedDelegates
        {
            get
            {
                if (_lazySynthesizedDelegates == null)
                {
                    CSharpCompilation previousSubmission = this.Compilation.PreviousSubmission;

                    // TODO (tomat): avoid recursion
                    var previousCache = (previousSubmission == null) ? null : previousSubmission.AnonymousTypeManager._lazySynthesizedDelegates;

                    Interlocked.CompareExchange(ref _lazySynthesizedDelegates,
                                                previousCache == null
                                                    ? new ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>()
                                                    : new ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>(previousCache),
                                                null);
                }

                return _lazySynthesizedDelegates;
            }
        }

        internal SynthesizedDelegateSymbol SynthesizeDelegate(int parameterCount, BitVector byRefParameters, bool returnsVoid, int generation)
        {
            // parameterCount doesn't include return type
            Debug.Assert(byRefParameters.IsNull || parameterCount == byRefParameters.Capacity);

            var key = new SynthesizedDelegateKey(parameterCount, byRefParameters, returnsVoid, generation);

            SynthesizedDelegateValue result;
            if (this.SynthesizedDelegates.TryGetValue(key, out result))
            {
                return result.Delegate;
            }

            // NOTE: the newly created template may be thrown away if another thread wins
            return this.SynthesizedDelegates.GetOrAdd(key,
                new SynthesizedDelegateValue(
                    this,
                    new SynthesizedDelegateSymbol(
                        this.Compilation.Assembly.GlobalNamespace,
                        key.MakeTypeName(),
                        this.System_Object,
                        Compilation.GetSpecialType(SpecialType.System_IntPtr),
                        returnsVoid ? Compilation.GetSpecialType(SpecialType.System_Void) : null,
                        parameterCount,
                        byRefParameters))).Delegate;
        }

        /// <summary>
        /// Given anonymous type provided constructs an implementation type symbol to be used in emit phase; 
        /// if the anonymous type has at least one field the implementation type symbol will be created based on 
        /// a generic type template generated for each 'unique' anonymous type structure, otherwise the template
        /// type will be non-generic.
        /// </summary>
        private NamedTypeSymbol ConstructAnonymousTypeImplementationSymbol(AnonymousTypePublicSymbol anonymous)
        {
            Debug.Assert(ReferenceEquals(this, anonymous.Manager));

            CheckSourceLocationSeen(anonymous);

            AnonymousTypeDescriptor typeDescr = anonymous.TypeDescriptor;
            typeDescr.AssertIsGood();

            // Get anonymous type template
            AnonymousTypeTemplateSymbol template;
            if (!this.AnonymousTypeTemplates.TryGetValue(typeDescr.Key, out template))
            {
                // NOTE: the newly created template may be thrown away if another thread wins
                template = this.AnonymousTypeTemplates.GetOrAdd(typeDescr.Key, new AnonymousTypeTemplateSymbol(this, typeDescr));
            }

            // Adjust template location if the template is owned by this manager
            if (ReferenceEquals(template.Manager, this))
            {
                template.AdjustLocation(typeDescr.Location);
            }

            // In case template is not generic, just return it
            if (template.Arity == 0)
            {
                return template;
            }

            // otherwise construct type using the field types
            var typeArguments = typeDescr.Fields.SelectAsArray(f => f.Type.TypeSymbol);
            return template.Construct(typeArguments);
        }

        private AnonymousTypeTemplateSymbol CreatePlaceholderTemplate(Microsoft.CodeAnalysis.Emit.AnonymousTypeKey key)
        {
            var fields = key.Fields.SelectAsArray(f => new AnonymousTypeField(f.Name, Location.None, (TypeSymbolWithAnnotations)null));
            var typeDescr = new AnonymousTypeDescriptor(fields, Location.None);
            return new AnonymousTypeTemplateSymbol(this, typeDescr);
        }

        /// <summary>
        /// Resets numbering in anonymous type names and compiles the
        /// anonymous type methods. Also seals the collection of templates.
        /// </summary>
        public void AssignTemplatesNamesAndCompile(MethodCompiler compiler, PEModuleBuilder moduleBeingBuilt, DiagnosticBag diagnostics)
        {
            // Ensure all previous anonymous type templates are included so the
            // types are available for subsequent edit and continue generations.
            foreach (var key in moduleBeingBuilt.GetPreviousAnonymousTypes())
            {
                var templateKey = AnonymousTypeDescriptor.ComputeKey(key.Fields, f => f.Name);
                this.AnonymousTypeTemplates.GetOrAdd(templateKey, k => this.CreatePlaceholderTemplate(key));
            }

            // Get all anonymous types owned by this manager
            var builder = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            GetCreatedAnonymousTypeTemplates(builder);

            // If the collection is not sealed yet we should assign 
            // new indexes to the created anonymous type templates
            if (!this.AreTemplatesSealed)
            {
                // If we are emitting .NET module, include module's name into type's name to ensure
                // uniqueness across added modules.
                string moduleId;

                if (moduleBeingBuilt.OutputKind == OutputKind.NetModule)
                {
                    moduleId = moduleBeingBuilt.Name;

                    string extension = OutputKind.NetModule.GetDefaultExtension();

                    if (moduleId.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        moduleId = moduleId.Substring(0, moduleId.Length - extension.Length);
                    }

                    moduleId = MetadataHelpers.MangleForTypeNameIfNeeded(moduleId);
                }
                else
                {
                    moduleId = string.Empty;
                }

                int nextIndex = moduleBeingBuilt.GetNextAnonymousTypeIndex();
                foreach (var template in builder)
                {
                    string name;
                    int index;
                    if (!moduleBeingBuilt.TryGetAnonymousTypeName(template, out name, out index))
                    {
                        index = nextIndex++;
                        name = GeneratedNames.MakeAnonymousTypeTemplateName(index, this.Compilation.GetSubmissionSlotIndex(), moduleId);
                    }
                    // normally it should only happen once, but in case there is a race
                    // NameAndIndex.set has an assert which guarantees that the
                    // template name provided is the same as the one already assigned
                    template.NameAndIndex = new NameAndIndex(name, index);
                }

                this.SealTemplates();
            }

            if (builder.Count > 0 && !ReportMissingOrErroneousSymbols(diagnostics))
            {
                // Process all the templates
                foreach (var template in builder)
                {
                    foreach (var method in template.SpecialMembers)
                    {
                        moduleBeingBuilt.AddSynthesizedDefinition(template, method);
                    }

                    compiler.Visit(template, null);
                }
            }

            builder.Free();

            var synthesizedDelegates = ArrayBuilder<SynthesizedDelegateSymbol>.GetInstance();
            GetCreatedSynthesizedDelegates(synthesizedDelegates);
            foreach (var synthesizedDelegate in synthesizedDelegates)
            {
                compiler.Visit(synthesizedDelegate, null);
            }
            synthesizedDelegates.Free();
        }

        /// <summary>
        /// The set of anonymous type templates created by
        /// this AnonymousTypeManager, in fixed order.
        /// </summary>
        private void GetCreatedAnonymousTypeTemplates(ArrayBuilder<AnonymousTypeTemplateSymbol> builder)
        {
            Debug.Assert(!builder.Any());
            var anonymousTypes = _lazyAnonymousTypeTemplates;
            if (anonymousTypes != null)
            {
                foreach (var template in anonymousTypes.Values)
                {
                    if (ReferenceEquals(template.Manager, this))
                    {
                        builder.Add(template);
                    }
                }
                // Sort type templates using smallest location
                builder.Sort(new AnonymousTypeComparer(this.Compilation));
            }
        }

        /// <summary>
        /// The set of synthesized delegates created by
        /// this AnonymousTypeManager.
        /// </summary>
        private void GetCreatedSynthesizedDelegates(ArrayBuilder<SynthesizedDelegateSymbol> builder)
        {
            Debug.Assert(!builder.Any());
            var delegates = _lazySynthesizedDelegates;
            if (delegates != null)
            {
                foreach (var template in delegates.Values)
                {
                    if (ReferenceEquals(template.Manager, this))
                    {
                        builder.Add(template.Delegate);
                    }
                }
                builder.Sort(SynthesizedDelegateSymbolComparer.Instance);
            }
        }

        private class SynthesizedDelegateSymbolComparer : IComparer<SynthesizedDelegateSymbol>
        {
            public static readonly SynthesizedDelegateSymbolComparer Instance = new SynthesizedDelegateSymbolComparer();

            public int Compare(SynthesizedDelegateSymbol x, SynthesizedDelegateSymbol y)
            {
                return x.MetadataName.CompareTo(y.MetadataName);
            }
        }

        internal static Microsoft.CodeAnalysis.Emit.AnonymousTypeKey GetAnonymousTypeKey(NamedTypeSymbol type)
        {
            return ((AnonymousTypeTemplateSymbol)type).GetAnonymousTypeKey();
        }

        internal IReadOnlyDictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue> GetAnonymousTypeMap()
        {
            var result = new Dictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue>();
            var templates = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            // Get anonymous types but not synthesized delegates. (Delegate types are
            // not reused across generations since reuse would add complexity (such
            // as parsing delegate type names from metadata) without a clear benefit.)
            GetCreatedAnonymousTypeTemplates(templates);
            foreach (var template in templates)
            {
                var nameAndIndex = template.NameAndIndex;
                var key = template.GetAnonymousTypeKey();
                var value = new Microsoft.CodeAnalysis.Emit.AnonymousTypeValue(nameAndIndex.Name, nameAndIndex.Index, template);
                result.Add(key, value);
            }
            templates.Free();
            return result;
        }

        /// <summary>
        /// Returns all templates owned by this type manager
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> GetAllCreatedTemplates()
        {
            // NOTE: templates may not be sealed in case metadata is being emitted without IL

            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            var anonymousTypes = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            GetCreatedAnonymousTypeTemplates(anonymousTypes);
            builder.AddRange(anonymousTypes);
            anonymousTypes.Free();

            var synthesizedDelegates = ArrayBuilder<SynthesizedDelegateSymbol>.GetInstance();
            GetCreatedSynthesizedDelegates(synthesizedDelegates);
            builder.AddRange(synthesizedDelegates);
            synthesizedDelegates.Free();

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns true if the named type is an implementation template for an anonymous type
        /// </summary>
        internal static bool IsAnonymousTypeTemplate(NamedTypeSymbol type)
        {
            return type is AnonymousTypeTemplateSymbol;
        }

        /// <summary>
        /// Retrieves methods of anonymous type template which are not placed to symbol table.
        /// In current implementation those are overridden 'ToString', 'Equals' and 'GetHashCode'
        /// </summary>
        internal static ImmutableArray<MethodSymbol> GetAnonymousTypeHiddenMethods(NamedTypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return ((AnonymousTypeTemplateSymbol)type).SpecialMembers;
        }

        /// <summary>
        /// Translates anonymous type public symbol into an implementation type symbol to be used in emit.
        /// </summary>
        internal static NamedTypeSymbol TranslateAnonymousTypeSymbol(NamedTypeSymbol type)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsAnonymousType);

            var anonymous = (AnonymousTypePublicSymbol)type;
            return anonymous.Manager.ConstructAnonymousTypeImplementationSymbol(anonymous);
        }

        /// <summary>
        /// Translates anonymous type method symbol into an implementation method symbol to be used in emit.
        /// </summary>
        internal static MethodSymbol TranslateAnonymousTypeMethodSymbol(MethodSymbol method)
        {
            Debug.Assert((object)method != null);
            NamedTypeSymbol translatedType = TranslateAnonymousTypeSymbol(method.ContainingType);
            // find a method in anonymous type template by name
            foreach (var member in ((NamedTypeSymbol)translatedType.OriginalDefinition).GetMembers(method.Name))
            {
                if (member.Kind == SymbolKind.Method)
                {
                    // found a method definition, get a constructed method
                    return ((MethodSymbol)member).AsMember(translatedType);
                }
            }
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary> 
        /// Comparator being used for stable ordering in anonymous type indices.
        /// </summary>
        private sealed class AnonymousTypeComparer : IComparer<AnonymousTypeTemplateSymbol>
        {
            private readonly CSharpCompilation _compilation;

            public AnonymousTypeComparer(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            public int Compare(AnonymousTypeTemplateSymbol x, AnonymousTypeTemplateSymbol y)
            {
                if ((object)x == (object)y)
                {
                    return 0;
                }

                // We compare two anonymous type templated by comparing their locations and descriptor keys

                // NOTE: If anonymous type got to this phase it must have the location set
                int result = this.CompareLocations(x.SmallestLocation, y.SmallestLocation);

                if (result == 0)
                {
                    // It is still possible for two templates to have the same smallest location 
                    // in case they are implicitly created and use the same syntax for location
                    result = string.CompareOrdinal(x.TypeDescriptorKey, y.TypeDescriptorKey);
                }

                return result;
            }

            private int CompareLocations(Location x, Location y)
            {
                if (x == y)
                {
                    return 0;
                }
                else if (x == Location.None)
                {
                    return -1;
                }
                else if (y == Location.None)
                {
                    return 1;
                }
                else
                {
                    return _compilation.CompareSourceLocations(x, y);
                }
            }
        }
    }
}
