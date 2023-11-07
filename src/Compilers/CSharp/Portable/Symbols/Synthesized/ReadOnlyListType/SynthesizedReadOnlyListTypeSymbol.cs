// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized type used for collection expressions where the target type
    /// is IEnumerable&lt;T&gt;, IReadOnlyCollection&lt;T&gt;, or IReadOnlyList&lt;T&gt;.
    /// If the collection expression has a known length, the type is generated with
    /// an array field; otherwise the type is generated with a List&lt;T&gt; field.
    /// <code>
    /// sealed class &lt;&gt;z__ReadOnlyArray&lt;T&gt; { private readonly T[] _items; }
    /// sealed class &lt;&gt;z__ReadOnlyList&lt;T&gt; { private readonly List&lt;T&gt; _items; }
    /// </code>
    /// </summary>
    internal sealed class SynthesizedReadOnlyListTypeSymbol : NamedTypeSymbol
    {
        private static readonly SpecialType[] s_requiredSpecialTypes = new[]
        {
            SpecialType.System_Collections_IEnumerable,
            SpecialType.System_Collections_Generic_IEnumerable_T,
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T,
            SpecialType.System_Collections_Generic_IReadOnlyList_T,
            SpecialType.System_Collections_Generic_ICollection_T,
            SpecialType.System_Collections_Generic_IList_T,
        };

        private static readonly WellKnownType[] s_requiredWellKnownTypes = new[]
        {
            WellKnownType.System_Collections_Generic_List_T,
        };

        private static readonly SpecialMember[] s_requiredSpecialMembers = new[]
        {
            SpecialMember.System_Collections_IEnumerable__GetEnumerator,
            SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator,
        };

        private static readonly WellKnownMember[] s_requiredWellKnownMembers = new[]
        {
            WellKnownMember.System_Collections_Generic_IReadOnlyCollection_T__Count,
            WellKnownMember.System_Collections_Generic_IReadOnlyList_T__get_Item,
            WellKnownMember.System_Collections_Generic_ICollection_T__Count,
            WellKnownMember.System_Collections_Generic_ICollection_T__IsReadOnly,
            WellKnownMember.System_Collections_Generic_ICollection_T__Add,
            WellKnownMember.System_Collections_Generic_ICollection_T__Clear,
            WellKnownMember.System_Collections_Generic_ICollection_T__Contains,
            WellKnownMember.System_Collections_Generic_ICollection_T__CopyTo,
            WellKnownMember.System_Collections_Generic_ICollection_T__Remove,
            WellKnownMember.System_Collections_Generic_IList_T__get_Item,
            WellKnownMember.System_Collections_Generic_IList_T__IndexOf,
            WellKnownMember.System_Collections_Generic_IList_T__Insert,
            WellKnownMember.System_Collections_Generic_IList_T__RemoveAt,
            WellKnownMember.System_NotSupportedException__ctor,
        };

        private static readonly WellKnownMember[] s_requiredWellKnownMembersUnknownLength = new[]
        {
            WellKnownMember.System_Collections_Generic_List_T__Count,
            WellKnownMember.System_Collections_Generic_List_T__Contains,
            WellKnownMember.System_Collections_Generic_List_T__CopyTo,
            WellKnownMember.System_Collections_Generic_List_T__get_Item,
            WellKnownMember.System_Collections_Generic_List_T__IndexOf,
        };

        internal static NamedTypeSymbol Create(SourceModuleSymbol containingModule, string name, bool hasKnownLength)
        {
            var compilation = containingModule.DeclaringCompilation;
            DiagnosticInfo? diagnosticInfo = null;

            foreach (var type in s_requiredSpecialTypes)
            {
                diagnosticInfo = compilation.GetSpecialType(type).GetUseSiteInfo().DiagnosticInfo;
                if (diagnosticInfo is { })
                {
                    break;
                }
            }

            if (diagnosticInfo is null)
            {
                foreach (var member in s_requiredSpecialMembers)
                {
                    diagnosticInfo = getSpecialTypeMemberDiagnosticInfo(compilation, member);
                    if (diagnosticInfo is { })
                    {
                        break;
                    }
                }
            }

            if (diagnosticInfo is null)
            {
                foreach (var member in s_requiredWellKnownMembers)
                {
                    diagnosticInfo = getWellKnownTypeMemberDiagnosticInfo(compilation, member);
                    if (diagnosticInfo is { })
                    {
                        break;
                    }
                }
            }

            if (!hasKnownLength)
            {
                if (diagnosticInfo is null)
                {
                    diagnosticInfo = compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).GetUseSiteInfo().DiagnosticInfo;
                }

                if (diagnosticInfo is null)
                {
                    foreach (var member in s_requiredWellKnownMembersUnknownLength)
                    {
                        diagnosticInfo = getWellKnownTypeMemberDiagnosticInfo(compilation, member);
                        if (diagnosticInfo is { })
                        {
                            break;
                        }
                    }
                }
            }

            if (diagnosticInfo is { })
            {
                return new ExtendedErrorTypeSymbol(compilation, name, arity: 1, diagnosticInfo, unreported: true);
            }

            return new SynthesizedReadOnlyListTypeSymbol(containingModule, name, hasKnownLength);

            static DiagnosticInfo? getSpecialTypeMemberDiagnosticInfo(CSharpCompilation compilation, SpecialMember member)
            {
                var symbol = compilation.GetSpecialTypeMember(member);
                if (symbol is { })
                {
                    return null;
                }
                var memberDescriptor = SpecialMembers.GetDescriptor(member);
                return new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name);
            }

            static DiagnosticInfo? getWellKnownTypeMemberDiagnosticInfo(CSharpCompilation compilation, WellKnownMember member)
            {
                var symbol = Binder.GetWellKnownTypeMember(compilation, member, out var useSiteInfo);
                if (symbol is { })
                {
                    return null;
                }
                var diagnosticInfo = useSiteInfo.DiagnosticInfo;
                Debug.Assert(diagnosticInfo is { });
                return diagnosticInfo; ;
            }
        }

        private readonly ModuleSymbol _containingModule;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<Symbol> _members;
        private readonly FieldSymbol _field;

        private SynthesizedReadOnlyListTypeSymbol(SourceModuleSymbol containingModule, string name, bool hasKnownLength)
        {
            var compilation = containingModule.DeclaringCompilation;

            _containingModule = containingModule;
            Name = name;
            var typeParameter = new SynthesizedReadOnlyListTypeParameterSymbol(this);
            TypeParameters = ImmutableArray.Create<TypeParameterSymbol>(typeParameter);
            var typeArgs = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            TypeSymbol fieldType = hasKnownLength ?
                compilation.CreateArrayTypeSymbol(elementType: typeParameter) :
                compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(typeArgs);
            _field = new SynthesizedFieldSymbol(this, fieldType, "_items", isReadOnly: true);

            var iEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            var iEnumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(typeArgs);
            var iReadOnlyCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T).Construct(typeArgs);
            var iReadOnlyListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T).Construct(typeArgs);
            var iCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs);
            var iListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs);

            _interfaces = ImmutableArray.Create(
                iEnumerable,
                iEnumerableT,
                iReadOnlyCollectionT,
                iReadOnlyListT,
                iCollectionT,
                iListT);

            var membersBuilder = ArrayBuilder<Symbol>.GetInstance();
            membersBuilder.Add(
                _field);
            membersBuilder.Add(
                new SynthesizedReadOnlyListConstructor(this, fieldType));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator),
                    generateGetEnumerator));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator)!).AsMember(iEnumerableT),
                    generateGetEnumeratorT));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IReadOnlyCollection_T__Count)!).AsMember(iReadOnlyCollectionT),
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IReadOnlyList_T__get_Item)!).AssociatedSymbol).AsMember(iReadOnlyListT),
                    generateIndexer));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Count)!).AsMember(iCollectionT),
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__IsReadOnly)!).AsMember(iCollectionT),
                    generateIsReadOnly));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Add)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Clear)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Contains)!).AsMember(iCollectionT),
                    generateContains));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__CopyTo)!).AsMember(iCollectionT),
                    generateCopyTo));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_ICollection_T__Remove)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__get_Item)!).AssociatedSymbol).AsMember(iListT),
                    generateIndexer,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__IndexOf)!).AsMember(iListT),
                    generateIndexOf));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__Insert)!).AsMember(iListT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IList_T__RemoveAt)!).AsMember(iListT),
                    generateNotSupportedException));
            _members = membersBuilder.ToImmutableAndFree();

            // IEnumerable.GetEnumerator()
            static BoundStatement generateGetEnumerator(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                // return _items.GetEnumerator();
                return f.Return(
                    f.Call(
                        f.Convert(
                            interfaceMethod.ContainingType,
                            fieldReference),
                        interfaceMethod));
            }

            // IEnumerable<T>.GetEnumerator()
            static BoundStatement generateGetEnumeratorT(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                // return _items.GetEnumerator();
                return f.Return(
                    f.Call(
                        f.Convert(
                            interfaceMethod.ContainingType,
                            fieldReference),
                        interfaceMethod));
            }

            // IReadOnlyCollection<T>.Count, ICollection<T>.Count
            static BoundStatement generateCount(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                if (field.Type.IsArray())
                {
                    // return _items.Length;
                    return f.Return(
                        f.ArrayLength(fieldReference));
                }
                else
                {
                    // return _items.Count;
                    var listMember = (PropertySymbol)containingType.GetFieldTypeMember(WellKnownMember.System_Collections_Generic_List_T__Count);
                    return f.Return(
                        f.Property(fieldReference, listMember));
                }
            }

            // ICollection<T>.IsReadOnly
            static BoundStatement generateIsReadOnly(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                // return true;
                return f.Return(f.Literal(true));
            }

            // ICollection<T>.Contains(T)
            static BoundStatement generateContains(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (field.Type.IsArray())
                {
                    // return ((ICollection<T>)_items).Contains(param0);
                    return f.Return(
                        f.Call(
                            f.Convert(
                                interfaceMethod.ContainingType,
                                fieldReference),
                            interfaceMethod,
                            parameterReference));
                }
                else
                {
                    // return _items.Contains(param0);
                    var listMember = (MethodSymbol)containingType.GetFieldTypeMember(WellKnownMember.System_Collections_Generic_List_T__Contains);
                    return f.Return(
                        f.Call(
                            fieldReference,
                            listMember,
                            parameterReference));
                }
            }

            // ICollection<T>.CopyTo(T[], int)
            static BoundStatement generateCopyTo(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference0 = f.Parameter(method.Parameters[0]);
                var parameterReference1 = f.Parameter(method.Parameters[1]);
                BoundStatement statement;
                if (field.Type.IsArray())
                {
                    // ((ICollection<T>)_items).CopyTo(param0, param1);
                    statement = f.ExpressionStatement(
                        f.Call(
                            f.Convert(
                                interfaceMethod.ContainingType,
                                fieldReference),
                            interfaceMethod,
                            parameterReference0,
                            parameterReference1));
                }
                else
                {
                    // _items.CopyTo(param0, param1);
                    var listMember = (MethodSymbol)containingType.GetFieldTypeMember(WellKnownMember.System_Collections_Generic_List_T__CopyTo);
                    statement = f.ExpressionStatement(
                        f.Call(
                            fieldReference,
                            listMember,
                            parameterReference0,
                            parameterReference1));
                }
                return f.Block(statement, f.Return());
            }

            // IReadOnlyList<T>.this[int], IList<T>.this[int]
            static BoundStatement generateIndexer(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (field.Type.IsArray())
                {
                    // return _items[param0];
                    return f.Return(
                        f.ArrayAccess(fieldReference, parameterReference));
                }
                else
                {
                    // return _items[param0];
                    var listMember = (PropertySymbol)((MethodSymbol)containingType.GetFieldTypeMember(WellKnownMember.System_Collections_Generic_List_T__get_Item)).AssociatedSymbol;
                    return f.Return(
                        f.Indexer(fieldReference, listMember, parameterReference));
                }
            }

            // IList<T>.IndexOf(T)
            static BoundStatement generateIndexOf(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (field.Type.IsArray())
                {
                    // return ((IList<T>)_items).IndexOf(param0);
                    return f.Return(
                        f.Call(
                            f.Convert(
                                interfaceMethod.ContainingType,
                                fieldReference),
                            interfaceMethod,
                            parameterReference));
                }
                else
                {
                    // return _items.IndexOf(param0);
                    var listMember = (MethodSymbol)containingType.GetFieldTypeMember(WellKnownMember.System_Collections_Generic_List_T__IndexOf);
                    return f.Return(
                        f.Call(
                            fieldReference,
                            listMember,
                            parameterReference));
                }
            }

            static BoundStatement generateNotSupportedException(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var constructor = (MethodSymbol)method.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_NotSupportedException__ctor)!;
                // throw new System.NotSupportedException();
                return f.Throw(f.New(constructor));
            }

            static void addProperty(ArrayBuilder<Symbol> builder, PropertySymbol property)
            {
                builder.Add(property);
                builder.AddIfNotNull(property.GetMethod);
                builder.AddIfNotNull(property.SetMethod);
            }
        }

        private Symbol GetFieldTypeMember(WellKnownMember member)
        {
            var symbol = DeclaringCompilation.GetWellKnownTypeMember(member);

            Debug.Assert(symbol is { });
            Debug.Assert(_field.Type.OriginalDefinition.Equals(symbol.ContainingType, TypeCompareKind.AllIgnoreOptions));

            return symbol.SymbolAsMember((NamedTypeSymbol)_field.Type);
        }

        public override int Arity => 1;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name { get; }

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => true;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => false;

        public override Symbol? ContainingSymbol => _containingModule.GlobalNamespace;

        internal override ModuleSymbol ContainingModule => _containingModule;

        public override AssemblySymbol ContainingAssembly => _containingModule.ContainingAssembly;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => GetTypeParametersAsTypeArguments();

        internal override bool IsFileLocal => false;

        internal override FileIdentifier? AssociatedFileIdentifier => null;

        internal override bool MangleName => true;

        internal override bool HasDeclaredRequiredMembers => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal override bool IsInterpolatedStringHandlerType => false;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        internal override TypeLayout Layout => default;

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => _members;

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray(m => m.Name == name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();

        internal override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => BaseTypeNoUseSiteDiagnostics;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _members.OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => _interfaces;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        internal override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => _interfaces;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
    }
}
