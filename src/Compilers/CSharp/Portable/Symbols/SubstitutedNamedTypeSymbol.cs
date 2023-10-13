// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Either a SubstitutedNestedTypeSymbol or a ConstructedNamedTypeSymbol, which share in common that they
    /// have type parameters substituted.
    /// </summary>
    internal abstract class SubstitutedNamedTypeSymbol : WrappedNamedTypeSymbol
    {
        private static readonly Func<Symbol, NamedTypeSymbol, Symbol> s_symbolAsMemberFunc = SymbolExtensions.SymbolAsMember;

        private readonly bool _unbound;
        private readonly TypeMap _inputMap;

        // The container of a substituted named type symbol is typically a named type or a namespace.
        // However, in some error-recovery scenarios it might be some other container. For example,
        // consider "int Goo = 123; Goo<string> x = null;" What is the type of x? We construct an error
        // type symbol of arity one associated with local variable symbol Goo; when we construct
        // that error type symbol with <string>, the resulting substituted named type symbol has
        // the same containing symbol as the local: it is contained in the method.
        private readonly Symbol _newContainer;

        private TypeMap _lazyMap;
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;

        // computed on demand
        private int _hashCode;

        // lazily created, does not need to be unique
        private ConcurrentCache<string, ImmutableArray<Symbol>> _lazyMembersByNameCache;

        private ImmutableArray<Symbol> _lazyMembers;

        protected SubstitutedNamedTypeSymbol(Symbol newContainer, TypeMap map, NamedTypeSymbol originalDefinition, NamedTypeSymbol constructedFrom = null, bool unbound = false, TupleExtraData tupleData = null)
            : base(originalDefinition, tupleData)
        {
            Debug.Assert(originalDefinition.IsDefinition);
            Debug.Assert(!originalDefinition.IsErrorType());
            _newContainer = newContainer;
            _inputMap = map;
            _unbound = unbound;

            // if we're substituting to create a new unconstructed type as a member of a constructed type,
            // then we must alpha rename the type parameters.
            if ((object)constructedFrom != null)
            {
                Debug.Assert(ReferenceEquals(constructedFrom.ConstructedFrom, constructedFrom));
                _lazyTypeParameters = constructedFrom.TypeParameters;
                _lazyMap = map;
            }
        }

        public sealed override bool IsUnboundGenericType
        {
            get
            {
                return _unbound;
            }
        }

        private TypeMap Map
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyMap;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyTypeParameters;
            }
        }

        private void EnsureMapAndTypeParameters()
        {
            if (!_lazyTypeParameters.IsDefault)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;

            // We're creating a new unconstructed Method from another; alpha-rename type parameters.
            var newMap = _inputMap.WithAlphaRename(OriginalDefinition, this, out typeParameters);

            var prevMap = Interlocked.CompareExchange(ref _lazyMap, newMap, null);
            if (prevMap != null)
            {
                // There is a race with another thread who has already set the map
                // need to ensure that typeParameters, matches the map
                typeParameters = prevMap.SubstituteTypeParameters(OriginalDefinition.TypeParameters);
            }

            ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters, typeParameters, default(ImmutableArray<TypeParameterSymbol>));
            Debug.Assert(_lazyTypeParameters != null);
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _newContainer; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _newContainer as NamedTypeSymbol;
            }
        }

        public sealed override SymbolKind Kind
        {
            get { return OriginalDefinition.Kind; }
        }

        public sealed override NamedTypeSymbol OriginalDefinition
        {
            get { return _underlyingType; }
        }

        internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _unbound ? null : Map.SubstituteNamedType(OriginalDefinition.GetDeclaredBaseType(basesBeingResolved));
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.GetDeclaredInterfaces(basesBeingResolved));
        }

        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (_unbound)
                {
                    return null;
                }

                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    var baseType = Map.SubstituteNamedType(OriginalDefinition.BaseTypeNoUseSiteDiagnostics);
                    Interlocked.CompareExchange(ref _lazyBaseType, baseType, ErrorTypeSymbol.UnknownResultType);
                }

                Debug.Assert(!ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType));
                return _lazyBaseType;
            }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.InterfacesNoUseSiteDiagnostics(basesBeingResolved));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal abstract override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes);

        public sealed override IEnumerable<string> MemberNames
        {
            get
            {
                if (_unbound)
                {
                    return new List<string>(GetTypeMembersUnordered().Select(s => s.Name).Distinct());
                }

                if (IsTupleType)
                {
                    return GetMembers().Select(s => s.Name).Distinct();
                }

                return OriginalDefinition.MemberNames;
            }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return OriginalDefinition.GetAttributes();
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return OriginalDefinition.GetTypeMembersUnordered().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return OriginalDefinition.GetTypeMembers().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return OriginalDefinition.GetTypeMembers(name).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return OriginalDefinition.GetTypeMembers(name, arity).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        internal sealed override bool HasDeclaredRequiredMembers => !_unbound && OriginalDefinition.HasDeclaredRequiredMembers;

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            if (!_lazyMembers.IsDefault)
            {
                return _lazyMembers;
            }

            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (_unbound)
            {
                // Preserve order of members.
                foreach (var t in OriginalDefinition.GetMembers())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in OriginalDefinition.GetMembers())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            builder = AddOrWrapTupleMembersIfNecessary(builder);

            var result = builder.ToImmutableAndFree();
            ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, result);
            return _lazyMembers;
        }

        private ArrayBuilder<Symbol> AddOrWrapTupleMembersIfNecessary(ArrayBuilder<Symbol> builder)
        {
            if (IsTupleType)
            {
                var existingMembers = builder.ToImmutableAndFree();
                var replacedFields = new HashSet<Symbol>(ReferenceEqualityComparer.Instance);
                builder = MakeSynthesizedTupleMembers(existingMembers, replacedFields);
                foreach (var existingMember in existingMembers)
                {
                    // Note: fields for tuple elements have a tuple field symbol instead of a substituted field symbol
                    if (!replacedFields.Contains(existingMember))
                    {
                        builder.Add(existingMember);
                    }
                }
                Debug.Assert(builder is object);
            }

            return builder;
        }

        internal sealed override ImmutableArray<Symbol> GetMembersUnordered()
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (_unbound)
            {
                foreach (var t in OriginalDefinition.GetMembersUnordered())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in OriginalDefinition.GetMembersUnordered())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            builder = AddOrWrapTupleMembersIfNecessary(builder);

            return builder.ToImmutableAndFree();
        }

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            if (_unbound) return StaticCast<Symbol>.From(GetTypeMembers(name));

            ImmutableArray<Symbol> result;
            var cache = _lazyMembersByNameCache;
            if (cache != null && cache.TryGetValue(name, out result))
            {
                return result;
            }

            return GetMembersWorker(name);
        }

        private ImmutableArray<Symbol> GetMembersWorker(string name)
        {
            if (IsTupleType)
            {
                var result = GetMembers().WhereAsArray((m, name) => m.Name == name, name);
                cacheResult(result);
                return result;
            }

            var originalMembers = OriginalDefinition.GetMembers(name);
            if (originalMembers.IsDefaultOrEmpty)
            {
                return originalMembers;
            }

            var builder = ArrayBuilder<Symbol>.GetInstance(originalMembers.Length);
            foreach (var t in originalMembers)
            {
                builder.Add(t.SymbolAsMember(this));
            }

            var substitutedMembers = builder.ToImmutableAndFree();
            cacheResult(substitutedMembers);
            return substitutedMembers;

            void cacheResult(ImmutableArray<Symbol> result)
            {
                // cache of size 8 seems reasonable here.
                // considering that substituted methods have about 10 reference fields,
                // reusing just one may make the cache profitable.
                var cache = _lazyMembersByNameCache ??
                            (_lazyMembersByNameCache = new ConcurrentCache<string, ImmutableArray<Symbol>>(8));

                cache.TryAdd(name, result);
            }
        }

#nullable enable
        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            if (_unbound)
            {
                yield break;
            }

            foreach ((MethodSymbol body, MethodSymbol implemented) in OriginalDefinition.SynthesizedInterfaceMethodImpls())
            {
                var newBody = ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementation(body, this.TypeSubstitution);
                var newImplemented = ExplicitInterfaceHelpers.SubstituteExplicitInterfaceImplementation(implemented, this.TypeSubstitution);
                yield return (newBody, newImplemented);
            }
        }
#nullable disable

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return _unbound
                ? GetMembers()
                : OriginalDefinition.GetEarlyAttributeDecodingMembers().SelectAsArray(s_symbolAsMemberFunc, this);
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            if (_unbound) return GetMembers(name);

            var builder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var t in OriginalDefinition.GetEarlyAttributeDecodingMembers(name))
            {
                builder.Add(t.SymbolAsMember(this));
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return OriginalDefinition.EnumUnderlyingType;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = this.ComputeHashCode();
            }

            return _hashCode;
        }

        internal sealed override TypeMap TypeSubstitution
        {
            get { return this.Map; }
        }

        internal sealed override bool IsComImport
        {
            get { return OriginalDefinition.IsComImport; }
        }

        internal sealed override NamedTypeSymbol ComImportCoClass
        {
            get { return OriginalDefinition.ComImportCoClass; }
        }

#nullable enable
        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            return _underlyingType.HasCollectionBuilderAttribute(out builderType, out methodName);
        }
#nullable disable

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool IsFileLocal => _underlyingType.IsFileLocal;
        internal sealed override FileIdentifier AssociatedFileIdentifier => _underlyingType.AssociatedFileIdentifier;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal sealed override bool IsRecord => _underlyingType.IsRecord;
        internal sealed override bool IsRecordStruct => _underlyingType.IsRecordStruct;
        internal sealed override bool HasPossibleWellKnownCloneMethod() => _underlyingType.HasPossibleWellKnownCloneMethod();

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            return _underlyingType.HasInlineArrayAttribute(out length);
        }
    }
}
