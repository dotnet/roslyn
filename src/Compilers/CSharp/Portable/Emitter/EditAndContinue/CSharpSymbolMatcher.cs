// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Emit.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class CSharpSymbolMatcher : SymbolMatcher
    {
        private readonly Visitor _visitor;

        public CSharpSymbolMatcher(
            SourceAssemblySymbol sourceAssembly,
            SourceAssemblySymbol otherAssembly,
            SynthesizedTypeMaps synthesizedTypes,
            IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? otherSynthesizedMembers,
            IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? otherDeletedMembers)
        {
            _visitor = new Visitor(sourceAssembly, otherAssembly, synthesizedTypes, otherSynthesizedMembers, otherDeletedMembers, new DeepTranslator(otherAssembly.GetSpecialType(SpecialType.System_Object)));
        }

        public CSharpSymbolMatcher(
            SynthesizedTypeMaps synthesizedTypes,
            SourceAssemblySymbol sourceAssembly,
            PEAssemblySymbol otherAssembly)
        {
            _visitor = new Visitor(
                sourceAssembly,
                otherAssembly,
                synthesizedTypes,
                otherSynthesizedMembers: null,
                deepTranslator: null,
                otherDeletedMembers: null);
        }

        public override Cci.IDefinition? MapDefinition(Cci.IDefinition definition)
        {
            if (definition.GetInternalSymbol() is Symbol symbol)
            {
                return (Cci.IDefinition?)_visitor.Visit(symbol)?.GetCciAdapter();
            }

            // For simplicity, PID helpers and no-PIA embedded definitions are not reused across generations, so we don't map them here.
            // Instead, new ones are regenerated as needed.
            Debug.Assert(definition is PrivateImplementationDetails or Cci.IEmbeddedDefinition);

            return null;
        }

        public override Cci.INamespace? MapNamespace(Cci.INamespace @namespace)
        {
            if (@namespace.GetInternalSymbol() is NamespaceSymbol symbol)
            {
                return (Cci.INamespace?)_visitor.Visit(symbol)?.GetCciAdapter();
            }

            return null;
        }

        public override Cci.ITypeReference? MapReference(Cci.ITypeReference reference)
        {
            if (reference.GetInternalSymbol() is Symbol symbol)
            {
                return (Cci.ITypeReference?)_visitor.Visit(symbol)?.GetCciAdapter();
            }

            return null;
        }

        internal bool TryGetAnonymousTypeValue(AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol template, out AnonymousTypeValue typeValue)
            => _visitor.TryGetAnonymousTypeValue(template, out typeValue);

        private sealed class Visitor : CSharpSymbolVisitor<Symbol?>
        {
            private readonly SynthesizedTypeMaps _synthesizedTypes;
            private readonly SourceAssemblySymbol _sourceAssembly;

            // metadata or source assembly:
            private readonly AssemblySymbol _otherAssembly;

            /// <summary>
            /// Members that are not listed directly on their containing type or namespace symbol as they were synthesized in a lowering phase,
            /// after the symbol has been created.
            /// </summary>
            private readonly IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? _otherSynthesizedMembers;

            private readonly IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? _otherDeletedMembers;

            private readonly SymbolComparer _comparer;
            private readonly ConcurrentDictionary<Symbol, Symbol?> _matches = new(ReferenceEqualityComparer.Instance);

            /// <summary>
            /// A cache of members per type, populated when the first member for a given
            /// type is needed. Within each type, members are indexed by name. The reason
            /// for caching, and indexing by name, is to avoid searching sequentially
            /// through all members of a given kind each time a member is matched.
            /// </summary>
            private readonly ConcurrentDictionary<ISymbolInternal, IReadOnlyDictionary<string, ImmutableArray<ISymbolInternal>>> _otherMembers = new(ReferenceEqualityComparer.Instance);

            public Visitor(
                SourceAssemblySymbol sourceAssembly,
                AssemblySymbol otherAssembly,
                SynthesizedTypeMaps synthesizedTypes,
                IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? otherSynthesizedMembers,
                IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? otherDeletedMembers,
                DeepTranslator? deepTranslator)
            {
                _synthesizedTypes = synthesizedTypes;
                _sourceAssembly = sourceAssembly;
                _otherAssembly = otherAssembly;
                _otherSynthesizedMembers = otherSynthesizedMembers;
                _otherDeletedMembers = otherDeletedMembers;
                _comparer = new SymbolComparer(this, deepTranslator);
            }

            public override Symbol DefaultVisit(Symbol symbol)
            {
                // Symbol should have been handled elsewhere.
                throw ExceptionUtilities.Unreachable();
            }

            public override Symbol? Visit(Symbol symbol)
            {
                Debug.Assert((object)symbol.ContainingAssembly != (object)_otherAssembly);

                // Add an entry for the match, even if there is no match, to avoid
                // matching the same symbol unsuccessfully multiple times.
                return _matches.GetOrAdd(symbol, base.Visit);
            }

            public override Symbol? VisitArrayType(ArrayTypeSymbol symbol)
            {
                var otherElementType = (TypeSymbol?)Visit(symbol.ElementType);
                if (otherElementType is null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }

                var otherModifiers = VisitCustomModifiers(symbol.ElementTypeWithAnnotations.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(_otherAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(otherElementType, otherModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(_otherAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(otherElementType, otherModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
            }

            public override Symbol? VisitEvent(EventSymbol symbol)
                => VisitNamedTypeMember(symbol, AreEventsEqual);

            public override Symbol? VisitField(FieldSymbol symbol)
                => VisitNamedTypeMember(symbol, AreFieldsEqual);

            public override Symbol? VisitMethod(MethodSymbol symbol)
            {
                // Not expecting constructed method.
                Debug.Assert(symbol.IsDefinition);
                return VisitNamedTypeMember(symbol, AreMethodsEqual);
            }

            public override Symbol? VisitModule(ModuleSymbol module)
            {
                var otherAssembly = (AssemblySymbol?)Visit(module.ContainingAssembly);
                if (otherAssembly is null)
                {
                    return null;
                }

                // manifest module:
                if (module.Ordinal == 0)
                {
                    return otherAssembly.Modules[0];
                }

                // match non-manifest module by name:
                for (int i = 1; i < otherAssembly.Modules.Length; i++)
                {
                    var otherModule = otherAssembly.Modules[i];

                    // use case sensitive comparison -- modules whose names differ in casing are considered distinct:
                    if (StringComparer.Ordinal.Equals(otherModule.Name, module.Name))
                    {
                        return otherModule;
                    }
                }

                return null;
            }

            public override Symbol? VisitAssembly(AssemblySymbol assembly)
            {
                if (assembly.IsLinked)
                {
                    return assembly;
                }

                // When we map synthesized symbols from previous generations to the latest compilation
                // we might encounter a symbol that is defined in arbitrary preceding generation,
                // not just the immediately preceding generation. If the source assembly uses time-based
                // versioning assemblies of preceding generations might differ in their version number.
                if (IdentityEqualIgnoringVersionWildcard(assembly, _sourceAssembly))
                {
                    return _otherAssembly;
                }

                // find a referenced assembly with the same source identity (modulo assembly version patterns):
                foreach (var otherReferencedAssembly in _otherAssembly.Modules[0].ReferencedAssemblySymbols)
                {
                    if (IdentityEqualIgnoringVersionWildcard(assembly, otherReferencedAssembly))
                    {
                        return otherReferencedAssembly;
                    }
                }

                return null;
            }

            private static bool IdentityEqualIgnoringVersionWildcard(AssemblySymbol left, AssemblySymbol right)
            {
                var leftIdentity = left.Identity;
                var rightIdentity = right.Identity;

                return AssemblyIdentityComparer.SimpleNameComparer.Equals(leftIdentity.Name, rightIdentity.Name) &&
                       (left.AssemblyVersionPattern ?? leftIdentity.Version).Equals(right.AssemblyVersionPattern ?? rightIdentity.Version) &&
                       AssemblyIdentity.EqualIgnoringNameAndVersion(leftIdentity, rightIdentity);
            }

            public override Symbol? VisitNamespace(NamespaceSymbol @namespace)
            {
                var otherContainer = Visit(@namespace.ContainingSymbol);

                // Containing namespace will be missing from other assembly
                // if its was added in the (newer) source assembly.
                if (otherContainer is null)
                {
                    return null;
                }

                switch (otherContainer.Kind)
                {
                    case SymbolKind.NetModule:
                        Debug.Assert(@namespace.IsGlobalNamespace);
                        return ((ModuleSymbol)otherContainer).GlobalNamespace;

                    case SymbolKind.Namespace:
                        return FindMatchingMember(otherContainer, @namespace, AreNamespacesEqual);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol)
            {
                return _otherAssembly.GetSpecialType(SpecialType.System_Object);
            }

            public override Symbol? VisitNamedType(NamedTypeSymbol sourceType)
            {
                var originalDef = sourceType.OriginalDefinition;
                if ((object)originalDef != (object)sourceType)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    var typeArguments = sourceType.GetAllTypeArguments(ref discardedUseSiteInfo);

                    var otherDef = (NamedTypeSymbol?)Visit(originalDef);
                    if (otherDef is null)
                    {
                        return null;
                    }

                    var otherTypeParameters = otherDef.GetAllTypeParameters();
                    bool translationFailed = false;

                    var otherTypeArguments = typeArguments.SelectAsArray((t, v) =>
                    {
                        var newType = (TypeSymbol?)v.Visit(t.Type);

                        if (newType is null)
                        {
                            // For a newly added type, there is no match in the previous generation, so it could be null.
                            translationFailed = true;
                            newType = t.Type;
                        }

                        return t.WithTypeAndModifiers(newType, v.VisitCustomModifiers(t.CustomModifiers));
                    }, this);

                    if (translationFailed)
                    {
                        // For a newly added type, there is no match in the previous generation, so it could be null.
                        return null;
                    }

                    // TODO: LambdaFrame has alpha renamed type parameters, should we rather fix that?
                    var typeMap = new TypeMap(otherTypeParameters, otherTypeArguments, allowAlpha: true);
                    return typeMap.SubstituteNamedType(otherDef);
                }

                Debug.Assert(sourceType.IsDefinition);

                var otherContainer = this.Visit(sourceType.ContainingSymbol);
                // Containing type will be missing from other assembly
                // if the type was added in the (newer) source assembly.
                if (otherContainer is null)
                {
                    return null;
                }

                switch (otherContainer.Kind)
                {
                    case SymbolKind.Namespace:
                        if (sourceType is AnonymousTypeManager.AnonymousTypeTemplateSymbol typeTemplate)
                        {
                            Debug.Assert((object)otherContainer == (object)_otherAssembly.GlobalNamespace);
                            TryGetAnonymousTypeValue(typeTemplate, out var value);
                            return (NamedTypeSymbol?)value.Type?.GetInternalSymbol();
                        }
                        else if (sourceType is AnonymousTypeManager.AnonymousDelegateTemplateSymbol delegateTemplate)
                        {
                            Debug.Assert((object)otherContainer == (object)_otherAssembly.GlobalNamespace);
                            if (delegateTemplate.HasIndexedName)
                            {
                                TryGetAnonymousTypeValue(delegateTemplate, out var value);
                                return (NamedTypeSymbol?)value.Type?.GetInternalSymbol();
                            }
                            else
                            {
                                TryGetAnonymousDelegateValue(delegateTemplate, out var value);
                                return (NamedTypeSymbol?)value.Delegate?.GetInternalSymbol();
                            }
                        }

                        if (sourceType.IsAnonymousType)
                        {
                            return Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(sourceType));
                        }

                        return FindMatchingMember(otherContainer, sourceType, AreNamedTypesEqual);

                    case SymbolKind.NamedType:
                        return FindMatchingMember(otherContainer, sourceType, AreNamedTypesEqual);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitParameter(ParameterSymbol parameter)
            {
                // Should never reach here. Should be matched as a result of matching the container.
                throw ExceptionUtilities.Unreachable();
            }

            public override Symbol? VisitPointerType(PointerTypeSymbol symbol)
            {
                var otherPointedAtType = (TypeSymbol?)Visit(symbol.PointedAtType);
                if (otherPointedAtType is null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }

                var otherModifiers = VisitCustomModifiers(symbol.PointedAtTypeWithAnnotations.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtTypeWithAnnotations.WithTypeAndModifiers(otherPointedAtType, otherModifiers));
            }

            public override Symbol? VisitFunctionPointerType(FunctionPointerTypeSymbol symbol)
            {
                var sig = symbol.Signature;

                var otherReturnType = (TypeSymbol?)Visit(sig.ReturnType);
                if (otherReturnType is null)
                {
                    return null;
                }

                var otherRefCustomModifiers = VisitCustomModifiers(sig.RefCustomModifiers);
                var otherReturnTypeWithAnnotations = sig.ReturnTypeWithAnnotations.WithTypeAndModifiers(otherReturnType, VisitCustomModifiers(sig.ReturnTypeWithAnnotations.CustomModifiers));

                var otherParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
                ImmutableArray<ImmutableArray<CustomModifier>> otherParamRefCustomModifiers = default;

                if (sig.ParameterCount > 0)
                {
                    var otherParamsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(sig.ParameterCount);
                    var otherParamRefCustomModifiersBuilder = ArrayBuilder<ImmutableArray<CustomModifier>>.GetInstance(sig.ParameterCount);

                    foreach (var param in sig.Parameters)
                    {
                        var otherType = (TypeSymbol?)Visit(param.Type);
                        if (otherType is null)
                        {
                            otherParamsBuilder.Free();
                            otherParamRefCustomModifiersBuilder.Free();
                            return null;
                        }

                        otherParamRefCustomModifiersBuilder.Add(VisitCustomModifiers(param.RefCustomModifiers));
                        otherParamsBuilder.Add(param.TypeWithAnnotations.WithTypeAndModifiers(otherType, VisitCustomModifiers(param.TypeWithAnnotations.CustomModifiers)));
                    }

                    otherParameterTypes = otherParamsBuilder.ToImmutableAndFree();
                    otherParamRefCustomModifiers = otherParamRefCustomModifiersBuilder.ToImmutableAndFree();
                }

                return symbol.SubstituteTypeSymbol(otherReturnTypeWithAnnotations, otherParameterTypes, otherRefCustomModifiers, otherParamRefCustomModifiers);
            }

            public override Symbol? VisitProperty(PropertySymbol symbol)
                => VisitNamedTypeMember(symbol, ArePropertiesEqual);

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol)
            {
                if (symbol is IndexedTypeParameterSymbol indexed)
                {
                    return indexed;
                }

                var otherContainer = Visit(symbol.ContainingSymbol);
                RoslynDebug.AssertNotNull(otherContainer);

                var otherTypeParameters = otherContainer.Kind switch
                {
                    SymbolKind.NamedType or SymbolKind.ErrorType => ((NamedTypeSymbol)otherContainer).TypeParameters,
                    SymbolKind.Method => ((MethodSymbol)otherContainer).TypeParameters,
                    _ => throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind),
                };

                return otherTypeParameters[symbol.Ordinal];
            }

            private ImmutableArray<CustomModifier> VisitCustomModifiers(ImmutableArray<CustomModifier> modifiers)
            {
                return modifiers.SelectAsArray(VisitCustomModifier);
            }

            private CustomModifier VisitCustomModifier(CustomModifier modifier)
            {
                var type = (NamedTypeSymbol?)Visit(((CSharpCustomModifier)modifier).ModifierSymbol);
                RoslynDebug.AssertNotNull(type);

                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(type) :
                    CSharpCustomModifier.CreateRequired(type);
            }

            internal bool TryGetAnonymousDelegateValue(AnonymousTypeManager.AnonymousDelegateTemplateSymbol delegateSymbol, out SynthesizedDelegateValue otherDelegateSymbol)
            {
                Debug.Assert((object)delegateSymbol.ContainingSymbol == (object)_sourceAssembly.GlobalNamespace);

                var key = new SynthesizedDelegateKey(delegateSymbol.MetadataName);
                return _synthesizedTypes.AnonymousDelegates.TryGetValue(key, out otherDelegateSymbol);
            }

            internal bool TryGetAnonymousTypeValue(AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol template, out AnonymousTypeValue otherType)
            {
                Debug.Assert((object)template.ContainingSymbol == (object)_sourceAssembly.GlobalNamespace);

                if (template is AnonymousTypeManager.AnonymousTypeTemplateSymbol typeTemplate)
                {
                    return _synthesizedTypes.AnonymousTypes.TryGetValue(typeTemplate.GetAnonymousTypeKey(), out otherType);
                }

                var delegateTemplate = (AnonymousTypeManager.AnonymousDelegateTemplateSymbol)template;
                Debug.Assert(delegateTemplate.DelegateInvokeMethod != null);

                var key = new AnonymousDelegateWithIndexedNamePartialKey(delegateTemplate.Arity, delegateTemplate.DelegateInvokeMethod.ParameterCount);
                if (_synthesizedTypes.AnonymousDelegatesWithIndexedNames.TryGetValue(key, out var otherTypeCandidates))
                {
                    // The key is partial (not unique). Find a matching Invoke method signature.

                    foreach (var otherTypeCandidate in otherTypeCandidates)
                    {
                        var otherDelegateType = (NamedTypeSymbol?)otherTypeCandidate.Type.GetInternalSymbol();
                        Debug.Assert(otherDelegateType is not null);

                        if (isCorrespondingAnonymousDelegate(delegateTemplate, otherDelegateType))
                        {
                            otherType = otherTypeCandidate;
                            return true;
                        }
                    }
                }

                otherType = default;
                return false;

                bool isCorrespondingAnonymousDelegate(NamedTypeSymbol type, NamedTypeSymbol otherType)
                {
                    Debug.Assert(type.Arity == otherType.Arity);

                    type = SubstituteTypeParameters(type);
                    otherType = SubstituteTypeParameters(otherType);

                    return type.DelegateInvokeMethod is { } invokeMethod &&
                        otherType.DelegateInvokeMethod is { } otherInvokeMethod &&
                        invokeMethod.Parameters.SequenceEqual(otherInvokeMethod.Parameters,
                            (x, y) => isCorrespondingType(x.TypeWithAnnotations, y.TypeWithAnnotations) &&
                                x.ExplicitDefaultConstantValue == y.ExplicitDefaultConstantValue &&
                                x.IsParamsArray == y.IsParamsArray &&
                                x.IsParamsCollection == y.IsParamsCollection) &&
                        isCorrespondingType(invokeMethod.ReturnTypeWithAnnotations, otherInvokeMethod.ReturnTypeWithAnnotations);
                }

                bool isCorrespondingType(TypeWithAnnotations type, TypeWithAnnotations expectedType)
                {
                    var otherType = type.WithTypeAndModifiers((TypeSymbol?)this.Visit(type.Type), this.VisitCustomModifiers(type.CustomModifiers));
                    return otherType.Equals(expectedType, TypeCompareKind.CLRSignatureCompareOptions);
                }
            }

            private Symbol? VisitNamedTypeMember<T>(T member, Func<T, T, bool> predicate)
                where T : Symbol
            {
                var otherType = (NamedTypeSymbol?)Visit(member.ContainingType);

                // Containing type may be null for synthesized
                // types such as iterators.
                if (otherType is null)
                {
                    return null;
                }

                return FindMatchingMember(otherType, member, predicate);
            }

            private T? FindMatchingMember<T>(ISymbolInternal otherTypeOrNamespace, T sourceMember, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(sourceMember.MetadataName));

                var otherMembersByName = _otherMembers.GetOrAdd(otherTypeOrNamespace, GetAllEmittedMembers);
                if (otherMembersByName.TryGetValue(sourceMember.MetadataName, out var otherMembers))
                {
                    foreach (var otherMember in otherMembers)
                    {
                        if (otherMember is T other && predicate(sourceMember, other))
                        {
                            return other;
                        }
                    }
                }

                return null;
            }

            private bool AreArrayTypesEqual(ArrayTypeSymbol type, ArrayTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.ElementTypeWithAnnotations.CustomModifiers.IsEmpty);
                Debug.Assert(other.ElementTypeWithAnnotations.CustomModifiers.IsEmpty);

                return type.HasSameShapeAs(other) &&
                    AreTypesEqual(type.ElementType, other.ElementType);
            }

            private bool AreEventsEqual(EventSymbol @event, EventSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(@event.Name, other.Name));

                // Events can't be overloaded on type.
                // ECMA: Within the rows owned by a given row in the TypeDef table, there shall be no duplicates based upon Name [ERROR]
                return true;
            }

            private bool AreFieldsEqual(FieldSymbol field, FieldSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(field.Name, other.Name));
                return _comparer.Equals(field.Type, other.Type);
            }

            private bool AreMethodsEqual(MethodSymbol method, MethodSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(method.Name, other.Name));

                Debug.Assert(method.IsDefinition);
                Debug.Assert(other.IsDefinition);

                method = SubstituteTypeParameters(method);
                other = SubstituteTypeParameters(other);

                return _comparer.Equals(method.ReturnType, other.ReturnType) &&
                    method.RefKind.Equals(other.RefKind) &&
                    method.Parameters.SequenceEqual(other.Parameters, AreParametersEqual) &&
                    method.TypeParameters.SequenceEqual(other.TypeParameters, AreTypesEqual);
            }

            private static MethodSymbol SubstituteTypeParameters(MethodSymbol method)
            {
                Debug.Assert(method.IsDefinition);

                var typeParameters = method.TypeParameters;
                int n = typeParameters.Length;
                if (n == 0)
                {
                    return method;
                }

                return method.Construct(IndexedTypeParameterSymbol.Take(n));
            }

            private bool AreNamedTypesEqual(NamedTypeSymbol type, NamedTypeSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(type.MetadataName, other.MetadataName));

                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.All(t => t.CustomModifiers.IsEmpty));
                Debug.Assert(other.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.All(t => t.CustomModifiers.IsEmpty));

                return type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, AreTypesEqual);
            }

            private static NamedTypeSymbol SubstituteTypeParameters(NamedTypeSymbol type)
            {
                Debug.Assert(type.IsDefinition);

                var typeParameters = type.TypeParameters;
                int n = typeParameters.Length;
                if (n == 0)
                {
                    return type;
                }

                return type.Construct(IndexedTypeParameterSymbol.Take(n));
            }

            private bool AreNamespacesEqual(NamespaceSymbol @namespace, NamespaceSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(@namespace.MetadataName, other.MetadataName));
                return true;
            }

            private bool AreParametersEqual(ParameterSymbol parameter, ParameterSymbol other)
            {
                Debug.Assert(parameter.Ordinal == other.Ordinal);

                // allow a different ref-kind as long as the runtime type is the same:
                return parameter.RefKind is RefKind.None == other.RefKind is RefKind.None &&
                    _comparer.Equals(parameter.Type, other.Type);
            }

            private bool ArePointerTypesEqual(PointerTypeSymbol type, PointerTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.PointedAtTypeWithAnnotations.CustomModifiers.IsEmpty);
                Debug.Assert(other.PointedAtTypeWithAnnotations.CustomModifiers.IsEmpty);

                return AreTypesEqual(type.PointedAtType, other.PointedAtType);
            }

            private bool AreFunctionPointerTypesEqual(FunctionPointerTypeSymbol type, FunctionPointerTypeSymbol other)
            {
                var sig = type.Signature;
                var otherSig = other.Signature;

                ValidateFunctionPointerParamOrReturn(sig.ReturnTypeWithAnnotations, sig.RefKind, sig.RefCustomModifiers, allowOut: false);
                ValidateFunctionPointerParamOrReturn(otherSig.ReturnTypeWithAnnotations, otherSig.RefKind, otherSig.RefCustomModifiers, allowOut: false);
                if (sig.RefKind != otherSig.RefKind || !AreTypesEqual(sig.ReturnTypeWithAnnotations, otherSig.ReturnTypeWithAnnotations))
                {
                    return false;
                }

                return sig.Parameters.SequenceEqual(otherSig.Parameters, AreFunctionPointerParametersEqual);
            }

            private bool AreFunctionPointerParametersEqual(ParameterSymbol param, ParameterSymbol otherParam)
            {
                ValidateFunctionPointerParamOrReturn(param.TypeWithAnnotations, param.RefKind, param.RefCustomModifiers, allowOut: true);
                ValidateFunctionPointerParamOrReturn(otherParam.TypeWithAnnotations, otherParam.RefKind, otherParam.RefCustomModifiers, allowOut: true);

                return param.RefKind == otherParam.RefKind && AreTypesEqual(param.TypeWithAnnotations, otherParam.TypeWithAnnotations);
            }

            [Conditional("DEBUG")]
            private static void ValidateFunctionPointerParamOrReturn(TypeWithAnnotations type, RefKind refKind, ImmutableArray<CustomModifier> refCustomModifiers, bool allowOut)
            {
                Debug.Assert(type.CustomModifiers.IsEmpty);
                Debug.Assert(verifyRefModifiers(refCustomModifiers, refKind, allowOut));

                static bool verifyRefModifiers(ImmutableArray<CustomModifier> modifiers, RefKind refKind, bool allowOut)
                {
                    Debug.Assert(RefKind.RefReadOnly == RefKind.In);
                    switch (refKind)
                    {
                        case RefKind.RefReadOnly:
                        case RefKind.Out when allowOut:
                            return modifiers.Length == 1;
                        default:
                            return modifiers.IsEmpty;
                    }
                }
            }

            private bool ArePropertiesEqual(PropertySymbol property, PropertySymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(property.MetadataName, other.MetadataName));

                // Properties may be overloaded on their signature.
                // ECMA: Within the rows owned by a given row in the TypeDef table, there shall be no duplicates based upon Name+Type [ERROR]
                return _comparer.Equals(property.Type, other.Type) &&
                    property.RefKind.Equals(other.RefKind) &&
                    property.Parameters.SequenceEqual(other.Parameters, AreParametersEqual);
            }

            private static bool AreTypeParametersEqual(TypeParameterSymbol type, TypeParameterSymbol other)
            {
                Debug.Assert(type.Ordinal == other.Ordinal);
                Debug.Assert(StringOrdinalComparer.Equals(type.Name, other.Name));
                // Comparing constraints is unnecessary: two methods cannot differ by
                // constraints alone and changing the signature of a method is a rude
                // edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint == other.HasConstructorConstraint);
                Debug.Assert(type.HasValueTypeConstraint == other.HasValueTypeConstraint);
                Debug.Assert(type.HasUnmanagedTypeConstraint == other.HasUnmanagedTypeConstraint);
                Debug.Assert(type.HasReferenceTypeConstraint == other.HasReferenceTypeConstraint);
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length == other.ConstraintTypesNoUseSiteDiagnostics.Length);
                return true;
            }

            private bool AreTypesEqual(TypeWithAnnotations type, TypeWithAnnotations other)
            {
                Debug.Assert(type.CustomModifiers.IsDefaultOrEmpty);
                Debug.Assert(other.CustomModifiers.IsDefaultOrEmpty);
                return AreTypesEqual(type.Type, other.Type);
            }

            private bool AreTypesEqual(TypeSymbol type, TypeSymbol other)
            {
                if (type.Kind != other.Kind)
                {
                    return false;
                }

                switch (type.Kind)
                {
                    case SymbolKind.ArrayType:
                        return AreArrayTypesEqual((ArrayTypeSymbol)type, (ArrayTypeSymbol)other);

                    case SymbolKind.PointerType:
                        return ArePointerTypesEqual((PointerTypeSymbol)type, (PointerTypeSymbol)other);

                    case SymbolKind.FunctionPointerType:
                        return AreFunctionPointerTypesEqual((FunctionPointerTypeSymbol)type, (FunctionPointerTypeSymbol)other);

                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        return AreNamedTypesEqual((NamedTypeSymbol)type, (NamedTypeSymbol)other);

                    case SymbolKind.TypeParameter:
                        return AreTypeParametersEqual((TypeParameterSymbol)type, (TypeParameterSymbol)other);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.Kind);
                }
            }

            private IReadOnlyDictionary<string, ImmutableArray<ISymbolInternal>> GetAllEmittedMembers(ISymbolInternal symbol)
            {
                var members = ArrayBuilder<ISymbolInternal>.GetInstance();

                if (symbol.Kind == SymbolKind.NamedType)
                {
                    var type = (NamedTypeSymbol)symbol;
                    members.AddRange(type.GetEventsToEmit());
                    members.AddRange(type.GetFieldsToEmit());
                    members.AddRange(type.GetMethodsToEmit());
                    members.AddRange(type.GetTypeMembers());
                    members.AddRange(type.GetPropertiesToEmit());
                }
                else
                {
                    members.AddRange(((NamespaceSymbol)symbol).GetMembers());
                }

                if (_otherSynthesizedMembers != null && _otherSynthesizedMembers.TryGetValue(symbol, out var synthesizedMembers))
                {
                    members.AddRange(synthesizedMembers);
                }

                if (_otherDeletedMembers?.TryGetValue(symbol, out var deletedMembers) == true)
                {
                    members.AddRange(deletedMembers);
                }

                var result = members.ToDictionary(s => s.MetadataName, StringOrdinalComparer.Instance);
                members.Free();
                return result;
            }

            private sealed class SymbolComparer
            {
                private readonly Visitor _matcher;
                private readonly DeepTranslator? _deepTranslator;

                public SymbolComparer(Visitor matcher, DeepTranslator? deepTranslator)
                {
                    Debug.Assert(matcher != null);
                    _matcher = matcher;
                    _deepTranslator = deepTranslator;
                }

                public bool Equals(TypeSymbol source, TypeSymbol other)
                {
                    if (ReferenceEquals(source, other))
                    {
                        return true;
                    }

                    var visitedSource = (TypeSymbol?)_matcher.Visit(source);
                    var visitedOther = (_deepTranslator != null) ? (TypeSymbol)_deepTranslator.Visit(other) : other;

                    return visitedSource?.Equals(visitedOther, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == true;
                }
            }
        }

        internal sealed class DeepTranslator : CSharpSymbolVisitor<Symbol>
        {
            private readonly ConcurrentDictionary<Symbol, Symbol> _matches;
            private readonly NamedTypeSymbol _systemObject;

            public DeepTranslator(NamedTypeSymbol systemObject)
            {
                _matches = new ConcurrentDictionary<Symbol, Symbol>(ReferenceEqualityComparer.Instance);
                _systemObject = systemObject;
            }

            public override Symbol DefaultVisit(Symbol symbol)
            {
                // Symbol should have been handled elsewhere.
                throw ExceptionUtilities.Unreachable();
            }

            public override Symbol Visit(Symbol symbol)
            {
                return _matches.GetOrAdd(symbol, base.Visit(symbol));
            }

            public override Symbol VisitArrayType(ArrayTypeSymbol symbol)
            {
                var translatedElementType = (TypeSymbol)this.Visit(symbol.ElementType);
                var translatedModifiers = VisitCustomModifiers(symbol.ElementTypeWithAnnotations.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(translatedElementType, translatedModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(translatedElementType, translatedModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol)
            {
                return _systemObject;
            }

            public override Symbol VisitNamedType(NamedTypeSymbol type)
            {
                var originalDef = type.OriginalDefinition;
                if ((object)originalDef != type)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    var translatedTypeArguments = type.GetAllTypeArguments(ref discardedUseSiteInfo).SelectAsArray((t, v) => t.WithTypeAndModifiers((TypeSymbol)v.Visit(t.Type),
                                                                                                                                                  v.VisitCustomModifiers(t.CustomModifiers)),
                                                                                                                 this);

                    var translatedOriginalDef = (NamedTypeSymbol)this.Visit(originalDef);
                    var typeMap = new TypeMap(translatedOriginalDef.GetAllTypeParameters(), translatedTypeArguments, allowAlpha: true);
                    return typeMap.SubstituteNamedType(translatedOriginalDef);
                }

                Debug.Assert(type.IsDefinition);

                if (type.IsAnonymousType)
                {
                    return this.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type));
                }

                return type;
            }

            public override Symbol VisitPointerType(PointerTypeSymbol symbol)
            {
                var translatedPointedAtType = (TypeSymbol)this.Visit(symbol.PointedAtType);
                var translatedModifiers = VisitCustomModifiers(symbol.PointedAtTypeWithAnnotations.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtTypeWithAnnotations.WithTypeAndModifiers(translatedPointedAtType, translatedModifiers));
            }

            public override Symbol VisitFunctionPointerType(FunctionPointerTypeSymbol symbol)
            {
                var sig = symbol.Signature;
                var translatedReturnType = (TypeSymbol)Visit(sig.ReturnType);
                var translatedReturnTypeWithAnnotations = sig.ReturnTypeWithAnnotations.WithTypeAndModifiers(translatedReturnType, VisitCustomModifiers(sig.ReturnTypeWithAnnotations.CustomModifiers));
                var translatedRefCustomModifiers = VisitCustomModifiers(sig.RefCustomModifiers);

                var translatedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
                ImmutableArray<ImmutableArray<CustomModifier>> translatedParamRefCustomModifiers = default;

                if (sig.ParameterCount > 0)
                {
                    var translatedParamsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(sig.ParameterCount);
                    var translatedParamRefCustomModifiersBuilder = ArrayBuilder<ImmutableArray<CustomModifier>>.GetInstance(sig.ParameterCount);

                    foreach (var param in sig.Parameters)
                    {
                        var translatedParamType = (TypeSymbol)Visit(param.Type);
                        translatedParamsBuilder.Add(param.TypeWithAnnotations.WithTypeAndModifiers(translatedParamType, VisitCustomModifiers(param.TypeWithAnnotations.CustomModifiers)));
                        translatedParamRefCustomModifiersBuilder.Add(VisitCustomModifiers(param.RefCustomModifiers));
                    }

                    translatedParameterTypes = translatedParamsBuilder.ToImmutableAndFree();
                    translatedParamRefCustomModifiers = translatedParamRefCustomModifiersBuilder.ToImmutableAndFree();
                }

                return symbol.SubstituteTypeSymbol(translatedReturnTypeWithAnnotations, translatedParameterTypes, translatedRefCustomModifiers, translatedParamRefCustomModifiers);
            }

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol)
            {
                return symbol;
            }

            private ImmutableArray<CustomModifier> VisitCustomModifiers(ImmutableArray<CustomModifier> modifiers)
            {
                return modifiers.SelectAsArray(VisitCustomModifier);
            }

            private CustomModifier VisitCustomModifier(CustomModifier modifier)
            {
                var translatedType = (NamedTypeSymbol)this.Visit(((CSharpCustomModifier)modifier).ModifierSymbol);
                Debug.Assert((object)translatedType != null);
                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(translatedType) :
                    CSharpCustomModifier.CreateRequired(translatedType);
            }
        }
    }
}
