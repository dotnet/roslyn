﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
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
        /// Currently used for dynamic call-sites and inferred delegate types whose signature doesn't match any of the well-known Func or Action types.
        /// </summary>
        private ConcurrentDictionary<SynthesizedDelegateKey, AnonymousDelegateTemplateSymbol> _lazyAnonymousDelegates;

        private readonly struct SynthesizedDelegateKey : IEquatable<SynthesizedDelegateKey>
        {
            internal readonly string Name;
            internal readonly int ParameterCount;
            internal readonly AnonymousTypeDescriptor TypeDescriptor;

            public SynthesizedDelegateKey(int parameterCount, RefKindVector byRefs, bool returnsVoid, int generation)
            {
                Name = GeneratedNames.MakeSynthesizedDelegateName(byRefs, returnsVoid, generation);
                ParameterCount = parameterCount;
                TypeDescriptor = default;
            }

            public SynthesizedDelegateKey(AnonymousTypeDescriptor typeDescr)
            {
                Name = null;
                ParameterCount = -1;
                TypeDescriptor = typeDescr;
            }

            public override bool Equals(object obj)
            {
                return obj is SynthesizedDelegateKey && Equals((SynthesizedDelegateKey)obj);
            }

            public bool Equals(SynthesizedDelegateKey other)
            {
                if (!string.Equals(Name, other.Name))
                {
                    return false;
                }
                if (Name is null)
                {
                    return TypeDescriptor.Equals(other.TypeDescriptor);
                }
                return ParameterCount == other.ParameterCount;
            }

            public override int GetHashCode()
            {
                if (Name is null)
                {
                    return TypeDescriptor.GetHashCode();
                }
                return Hash.Combine((int)ParameterCount, Name.GetHashCode());
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

        private ConcurrentDictionary<SynthesizedDelegateKey, AnonymousDelegateTemplateSymbol> AnonymousDelegates
        {
            get
            {
                if (_lazyAnonymousDelegates == null)
                {
                    CSharpCompilation previousSubmission = this.Compilation.PreviousSubmission;

                    // TODO (tomat): avoid recursion
                    var previousCache = (previousSubmission == null) ? null : previousSubmission.AnonymousTypeManager._lazyAnonymousDelegates;

                    Interlocked.CompareExchange(ref _lazyAnonymousDelegates,
                                                previousCache == null
                                                    ? new ConcurrentDictionary<SynthesizedDelegateKey, AnonymousDelegateTemplateSymbol>()
                                                    : new ConcurrentDictionary<SynthesizedDelegateKey, AnonymousDelegateTemplateSymbol>(previousCache),
                                                null);
                }

                return _lazyAnonymousDelegates;
            }
        }

#nullable enable
        internal AnonymousDelegateTemplateSymbol SynthesizeDelegate(int parameterCount, RefKindVector refKinds, bool returnsVoid, int generation)
        {
            // parameterCount doesn't include return type
            Debug.Assert(refKinds.IsNull || parameterCount == refKinds.Capacity - (returnsVoid ? 0 : 1));

            var key = new SynthesizedDelegateKey(parameterCount, refKinds, returnsVoid, generation);

            AnonymousDelegateTemplateSymbol? synthesizedDelegate;
            if (this.AnonymousDelegates.TryGetValue(key, out synthesizedDelegate))
            {
                return synthesizedDelegate;
            }

            // NOTE: the newly created template may be thrown away if another thread wins
            synthesizedDelegate = new AnonymousDelegateTemplateSymbol(
                this,
                key.Name,
                this.System_Object,
                Compilation.GetSpecialType(SpecialType.System_IntPtr),
                returnsVoid ? Compilation.GetSpecialType(SpecialType.System_Void) : null,
                parameterCount,
                refKinds);
            return this.AnonymousDelegates.GetOrAdd(key, synthesizedDelegate);
        }

        private NamedTypeSymbol ConstructAnonymousDelegateImplementationSymbol(AnonymousDelegatePublicSymbol anonymous, int generation)
        {
            var typeDescr = anonymous.TypeDescriptor;
            Debug.Assert(typeDescr.Location.IsInSource); // AnonymousDelegateTemplateSymbol requires a location in source for ordering.

            // If all parameter types and return type are valid type arguments, construct
            // the delegate type from a generic template. Otherwise, use a non-generic template.
            if (allValidTypeArguments(typeDescr))
            {
                var fields = typeDescr.Fields;
                Debug.Assert(fields.All(f => f.Scope == DeclarationScope.Unscoped));

                bool returnsVoid = fields.Last().Type.IsVoidType();
                int nTypeArguments = fields.Length - (returnsVoid ? 1 : 0);
                var refKinds = default(RefKindVector);
                if (fields.Any(f => f.RefKind != RefKind.None))
                {
                    refKinds = RefKindVector.Create(nTypeArguments);
                    for (int i = 0; i < nTypeArguments; i++)
                    {
                        refKinds[i] = fields[i].RefKind;
                    }
                }

                var typeArgumentsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(nTypeArguments);
                for (int i = 0; i < nTypeArguments; i++)
                {
                    typeArgumentsBuilder.Add(fields[i].TypeWithAnnotations);
                }

                var typeArguments = typeArgumentsBuilder.ToImmutableAndFree();
                var template = SynthesizeDelegate(parameterCount: fields.Length - 1, refKinds, returnsVoid, generation);

                Debug.Assert(typeArguments.Length == template.TypeParameters.Length);
                return typeArguments.Length == 0 ?
                    template :
                    template.Construct(typeArguments);
            }
            else
            {
                var typeParameters = GetReferencedTypeParameters(typeDescr);
                var key = getTemplateKey(typeDescr, typeParameters);

                // Get anonymous delegate template
                AnonymousDelegateTemplateSymbol? template;
                if (!this.AnonymousDelegates.TryGetValue(key, out template))
                {
                    template = this.AnonymousDelegates.GetOrAdd(key, new AnonymousDelegateTemplateSymbol(this, typeDescr, typeParameters));
                }

                // Adjust template location if the template is owned by this manager
                if (ReferenceEquals(template.Manager, this))
                {
                    template.AdjustLocation(typeDescr.Location);
                }

                Debug.Assert(typeParameters.Length == template.TypeParameters.Length);
                return typeParameters.Length == 0 ?
                    template :
                    template.Construct(typeParameters);
            }

            static bool allValidTypeArguments(AnonymousTypeDescriptor typeDescr)
            {
                var fields = typeDescr.Fields;
                int n = fields.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    if (!isValidTypeArgument(fields[i]))
                    {
                        return false;
                    }
                }
                var returnParameter = fields[n - 1];
                return returnParameter.Type.IsVoidType() || isValidTypeArgument(returnParameter);
            }

            static bool isValidTypeArgument(AnonymousTypeField field)
            {
                return field.Scope == DeclarationScope.Unscoped &&
                    field.Type is { } type &&
                    !type.IsPointerOrFunctionPointer() &&
                    !type.IsRestrictedType();
            }

            static SynthesizedDelegateKey getTemplateKey(AnonymousTypeDescriptor typeDescr, ImmutableArray<TypeParameterSymbol> typeParameters)
            {
                if (typeParameters.Length > 0)
                {
                    var typeMap = new TypeMap(typeParameters, IndexedTypeParameterSymbol.Take(typeParameters.Length), allowAlpha: true);
                    typeDescr = typeDescr.SubstituteTypes(typeMap, out bool changed);
                    Debug.Assert(changed);
                }
                return new SynthesizedDelegateKey(typeDescr);
            }
        }

        private static ImmutableArray<TypeParameterSymbol> GetReferencedTypeParameters(AnonymousTypeDescriptor typeDescr)
        {
            var referenced = PooledHashSet<TypeParameterSymbol>.GetInstance();
            foreach (var field in typeDescr.Fields)
            {
                field.TypeWithAnnotations.VisitType(
                    type: null,
                    typeWithAnnotationsPredicate: null,
                    typePredicate: static (type, referenced, _) =>
                    {
                        if (type is TypeParameterSymbol typeParameter)
                        {
                            referenced.Add(typeParameter);
                        }
                        return false;
                    },
                    arg: referenced,
                    visitCustomModifiers: true);
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;
            if (referenced.Count == 0)
            {
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                var builder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
                builder.AddRange(referenced);
                builder.Sort((x, y) => compareTypeParameters(x, y));
                typeParameters = builder.ToImmutableAndFree();
            }
            referenced.Free();
            return typeParameters;

            static int compareTypeParameters(TypeParameterSymbol x, TypeParameterSymbol y)
            {
                var xOwner = x.ContainingSymbol;
                var yOwner = y.ContainingSymbol;
                if (xOwner.Equals(yOwner))
                {
                    return x.Ordinal - y.Ordinal;
                }
                else if (isContainedIn(xOwner, yOwner))
                {
                    return 1;
                }
                else
                {
                    Debug.Assert(isContainedIn(yOwner, xOwner));
                    return -1;
                }
            }

            static bool isContainedIn(Symbol symbol, Symbol container)
            {
                var other = symbol.ContainingSymbol;
                while (other is { })
                {
                    if (other.Equals(container))
                    {
                        return true;
                    }
                    other = other.ContainingSymbol;
                }
                return false;
            }
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
            AnonymousTypeTemplateSymbol? template;
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
            var typeArguments = typeDescr.Fields.SelectAsArray(f => f.Type);
            return template.Construct(typeArguments);
        }
#nullable disable

        private AnonymousTypeTemplateSymbol CreatePlaceholderTemplate(Microsoft.CodeAnalysis.Emit.AnonymousTypeKey key)
        {
            var fields = key.Fields.SelectAsArray(f => new AnonymousTypeField(f.Name, Location.None, typeWithAnnotations: default, refKind: RefKind.None, DeclarationScope.Unscoped));
            var typeDescr = new AnonymousTypeDescriptor(fields, Location.None);
            return new AnonymousTypeTemplateSymbol(this, typeDescr);
        }

        private AnonymousDelegateTemplateSymbol CreatePlaceholderSynthesizedDelegateValue(string name, RefKindVector refKinds, bool returnsVoid, int parameterCount)
        {
            return new AnonymousDelegateTemplateSymbol(
                this,
               MetadataHelpers.InferTypeArityAndUnmangleMetadataName(name, out _),
               this.System_Object,
               Compilation.GetSpecialType(SpecialType.System_IntPtr),
               returnsVoid ? Compilation.GetSpecialType(SpecialType.System_Void) : null,
               parameterCount,
               refKinds);
        }

        /// <summary>
        /// Resets numbering in anonymous type names and compiles the
        /// anonymous type methods. Also seals the collection of templates.
        /// </summary>
        public void AssignTemplatesNamesAndCompile(MethodCompiler compiler, PEModuleBuilder moduleBeingBuilt, BindingDiagnosticBag diagnostics)
        {
            // Ensure all previous anonymous type templates are included so the
            // types are available for subsequent edit and continue generations.
            foreach (var key in moduleBeingBuilt.GetPreviousAnonymousTypes())
            {
                Debug.Assert(!key.IsDelegate);
                var templateKey = AnonymousTypeDescriptor.ComputeKey(key.Fields, f => f.Name);
                this.AnonymousTypeTemplates.GetOrAdd(templateKey, k => this.CreatePlaceholderTemplate(key));
            }

            // Get all anonymous types owned by this manager
            var anonymousTypes = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            var anonymousDelegatesWithFixedTypes = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            GetCreatedAnonymousTypeTemplates(anonymousTypes);
            GetCreatedAnonymousDelegatesWithFixedTypes(anonymousDelegatesWithFixedTypes);

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

                int submissionSlotIndex = this.Compilation.GetSubmissionSlotIndex();

                int typeIndex = moduleBeingBuilt.GetNextAnonymousTypeIndex();
                foreach (var template in anonymousTypes)
                {
                    string name;
                    int index;
                    if (!moduleBeingBuilt.TryGetAnonymousTypeName(template, out name, out index))
                    {
                        index = typeIndex++;
                        name = GeneratedNames.MakeAnonymousTypeOrDelegateTemplateName(index, submissionSlotIndex, moduleId, isDelegate: false);
                    }
                    // normally it should only happen once, but in case there is a race
                    // NameAndIndex.set has an assert which guarantees that the
                    // template name provided is the same as the one already assigned
                    template.NameAndIndex = new NameAndIndex(name, index);
                }

                int delegateIndex = 0;
                foreach (var template in anonymousDelegatesWithFixedTypes)
                {
                    int index = delegateIndex++;
                    string name = GeneratedNames.MakeAnonymousTypeOrDelegateTemplateName(index, submissionSlotIndex, moduleId, isDelegate: true);
                    template.NameAndIndex = new NameAndIndex(name, index);
                }

                this.SealTemplates();
            }

            if (anonymousTypes.Count > 0 && !ReportMissingOrErroneousSymbols(diagnostics))
            {
                // Process all the templates
                foreach (var template in anonymousTypes)
                {
                    foreach (var method in template.SpecialMembers)
                    {
                        moduleBeingBuilt.AddSynthesizedDefinition(template, method.GetCciAdapter());
                    }
                    compiler.Visit(template, null);
                }
            }

            anonymousTypes.Free();

            // Ensure all previous synthesized delegates are included so the
            // types are available for subsequent edit and continue generations.
            foreach (var key in moduleBeingBuilt.GetPreviousAnonymousDelegates())
            {
                if (GeneratedNames.TryParseSynthesizedDelegateName(key.Name, out var refKinds, out var returnsVoid, out var generation, out var parameterCount))
                {
                    var delegateKey = new SynthesizedDelegateKey(parameterCount, refKinds, returnsVoid, generation);
                    this.AnonymousDelegates.GetOrAdd(delegateKey, (k, args) => CreatePlaceholderSynthesizedDelegateValue(key.Name, args.refKinds, args.returnsVoid, args.parameterCount), (refKinds, returnsVoid, parameterCount));
                }
            }

            var anonymousDelegates = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            GetCreatedAnonymousDelegates(anonymousDelegates);
            if (anonymousDelegatesWithFixedTypes.Count > 0 || anonymousDelegates.Count > 0)
            {
                ReportMissingOrErroneousSymbolsForDelegates(diagnostics);
                foreach (var anonymousDelegate in anonymousDelegatesWithFixedTypes)
                {
                    compiler.Visit(anonymousDelegate, null);
                }
                foreach (var anonymousDelegate in anonymousDelegates)
                {
                    compiler.Visit(anonymousDelegate, null);
                }
            }
            anonymousDelegates.Free();

            anonymousDelegatesWithFixedTypes.Free();
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
                // Sort types and delegates using smallest location
                builder.Sort(new AnonymousTypeOrDelegateComparer(this.Compilation));
            }
        }

        private void GetCreatedAnonymousDelegatesWithFixedTypes(ArrayBuilder<AnonymousDelegateTemplateSymbol> builder)
        {
            Debug.Assert(!builder.Any());
            var anonymousDelegates = _lazyAnonymousDelegates;
            if (anonymousDelegates != null)
            {
                foreach (var template in anonymousDelegates.Values)
                {
                    if (ReferenceEquals(template.Manager, this) && template.HasFixedTypes)
                    {
                        builder.Add(template);
                    }
                }
                // Sort types and delegates using smallest location
                builder.Sort(new AnonymousTypeOrDelegateComparer(this.Compilation));
            }
        }

        /// <summary>
        /// The set of synthesized delegates created by
        /// this AnonymousTypeManager.
        /// </summary>
        private void GetCreatedAnonymousDelegates(ArrayBuilder<AnonymousDelegateTemplateSymbol> builder)
        {
            Debug.Assert(!builder.Any());
            var delegates = _lazyAnonymousDelegates;
            if (delegates != null)
            {
                foreach (var template in delegates.Values)
                {
                    if (ReferenceEquals(template.Manager, this) && !template.HasFixedTypes)
                    {
                        builder.Add(template);
                    }
                }
                builder.Sort(SynthesizedDelegateSymbolComparer.Instance);
            }
        }

        private class SynthesizedDelegateSymbolComparer : IComparer<AnonymousDelegateTemplateSymbol>
        {
            public static readonly SynthesizedDelegateSymbolComparer Instance = new SynthesizedDelegateSymbolComparer();

            public int Compare(AnonymousDelegateTemplateSymbol x, AnonymousDelegateTemplateSymbol y)
            {
                return x.MetadataName.CompareTo(y.MetadataName);
            }
        }

        internal IReadOnlyDictionary<CodeAnalysis.Emit.SynthesizedDelegateKey, CodeAnalysis.Emit.SynthesizedDelegateValue> GetAnonymousDelegates()
        {
            var result = new Dictionary<CodeAnalysis.Emit.SynthesizedDelegateKey, CodeAnalysis.Emit.SynthesizedDelegateValue>();
            var anonymousDelegates = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            GetCreatedAnonymousDelegates(anonymousDelegates);
            foreach (var delegateSymbol in anonymousDelegates)
            {
                var key = new CodeAnalysis.Emit.SynthesizedDelegateKey(delegateSymbol.MetadataName);
                var value = new CodeAnalysis.Emit.SynthesizedDelegateValue(delegateSymbol.GetCciAdapter());
                result.Add(key, value);
            }
            anonymousDelegates.Free();
            return result;
        }

        internal IReadOnlyDictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue> GetAnonymousTypeMap()
        {
            var result = new Dictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue>();
            var templates = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            // Get anonymous types.
            GetCreatedAnonymousTypeTemplates(templates);
            foreach (AnonymousTypeTemplateSymbol template in templates)
            {
                var nameAndIndex = template.NameAndIndex;
                var key = template.GetAnonymousTypeKey();
                var value = new Microsoft.CodeAnalysis.Emit.AnonymousTypeValue(nameAndIndex.Name, nameAndIndex.Index, template.GetCciAdapter());
                result.Add(key, value);
            }
            templates.Free();
            return result;
        }

        internal IReadOnlyDictionary<string, AnonymousTypeValue> GetAnonymousDelegatesWithFixedTypes()
        {
            var result = new Dictionary<string, AnonymousTypeValue>();
            var templates = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            // Get anonymous delegates with fixed types (distinct from
            // anonymous delegates from GetAnonymousDelegates() above).
            GetCreatedAnonymousDelegatesWithFixedTypes(templates);
            foreach (var template in templates)
            {
                var nameAndIndex = template.NameAndIndex;
                var name = nameAndIndex.Name;
                var value = new AnonymousTypeValue(name, nameAndIndex.Index, template.GetCciAdapter());
                result.Add(name, value);
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

            var anonymousDelegatesWithFixedTypes = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            GetCreatedAnonymousDelegatesWithFixedTypes(anonymousDelegatesWithFixedTypes);
            builder.AddRange(anonymousDelegatesWithFixedTypes);
            anonymousDelegatesWithFixedTypes.Free();

            var anonymousDelegates = ArrayBuilder<AnonymousDelegateTemplateSymbol>.GetInstance();
            GetCreatedAnonymousDelegates(anonymousDelegates);
            builder.AddRange(anonymousDelegates);
            anonymousDelegates.Free();

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

            var anonymous = (AnonymousTypeOrDelegatePublicSymbol)type;
            return anonymous.MapToImplementationSymbol();
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
        /// Comparator being used for stable ordering in anonymous type or delegate indices.
        /// </summary>
        private sealed class AnonymousTypeOrDelegateComparer : IComparer<AnonymousTypeOrDelegateTemplateSymbol>
        {
            private readonly CSharpCompilation _compilation;

            public AnonymousTypeOrDelegateComparer(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            public int Compare(AnonymousTypeOrDelegateTemplateSymbol x, AnonymousTypeOrDelegateTemplateSymbol y)
            {
                if ((object)x == (object)y)
                {
                    return 0;
                }

                // We compare two anonymous type templates by comparing their locations and descriptor keys

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
