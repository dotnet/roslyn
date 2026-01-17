// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum SynthesizedReadOnlyListKind
    {
        /// <summary>
        /// The type is generated with a `T` field; used when collection expression has a single element.
        /// <code>
        /// sealed class &lt;&gt;z__ReadOnlySingleElementList&lt;T&gt; { private readonly T _item; }
        /// </code>
        /// </summary>
        SingleElement,
        /// <summary>
        /// The type is generated with an array field; used when collection expression has a known length.
        /// <code>
        /// sealed class &lt;&gt;z__ReadOnlyArray&lt;T&gt; { private readonly T[] _items; }
        /// </code>
        /// </summary>
        Array,
        /// <summary>
        /// The type is generated with a List&lt;T&gt; field; used when collection expression has an unknown length.
        /// <code>
        /// sealed class &lt;&gt;z__ReadOnlyList&lt;T&gt; { private readonly List&lt;T&gt; _items; }
        /// </code>
        /// </summary>
        List,
    }

    /// <summary>
    /// A synthesized type used for collection expressions where the target type
    /// is IEnumerable&lt;T&gt;, IReadOnlyCollection&lt;T&gt;, or IReadOnlyList&lt;T&gt;.
    /// If the collection expression has a known length, the type is generated with either
    /// an array field or a T field when the collection contains only one element: [e0];
    /// otherwise the type is generated with a List&lt;T&gt; field.
    /// <code>
    /// sealed class &lt;&gt;z__ReadOnlySingleElementList&lt;T&gt; { private readonly T _item; }
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
            SpecialType.System_Collections_Generic_ICollection_T,
            SpecialType.System_Collections_Generic_IList_T,
        };

        private static readonly SpecialType[] s_readOnlyInterfacesSpecialTypes = new[]
        {
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T,
            SpecialType.System_Collections_Generic_IReadOnlyList_T,
        };

        private static readonly WellKnownType[] s_requiredWellKnownTypes = new[]
        {
            WellKnownType.System_Collections_ICollection,
            WellKnownType.System_Collections_IList,
        };

        private static readonly SpecialMember[] s_requiredSpecialMembers = new[]
        {
            SpecialMember.System_Collections_IEnumerable__GetEnumerator,
            SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator,
            SpecialMember.System_Collections_Generic_ICollection_T__Count,
            SpecialMember.System_Collections_Generic_ICollection_T__IsReadOnly,
            SpecialMember.System_Collections_Generic_ICollection_T__Add,
            SpecialMember.System_Collections_Generic_ICollection_T__Clear,
            SpecialMember.System_Collections_Generic_ICollection_T__Contains,
            SpecialMember.System_Collections_Generic_ICollection_T__CopyTo,
            SpecialMember.System_Collections_Generic_ICollection_T__Remove,
            SpecialMember.System_Collections_Generic_IList_T__get_Item,
            SpecialMember.System_Collections_Generic_IList_T__IndexOf,
            SpecialMember.System_Collections_Generic_IList_T__Insert,
            SpecialMember.System_Collections_Generic_IList_T__RemoveAt,
        };

        private static readonly WellKnownMember[] s_requiredWellKnownMembers = new[]
        {
            WellKnownMember.System_Collections_ICollection__Count,
            WellKnownMember.System_Collections_ICollection__IsSynchronized,
            WellKnownMember.System_Collections_ICollection__SyncRoot,
            WellKnownMember.System_Collections_ICollection__CopyTo,
            WellKnownMember.System_Collections_IList__get_Item,
            WellKnownMember.System_Collections_IList__IsFixedSize,
            WellKnownMember.System_Collections_IList__IsReadOnly,
            WellKnownMember.System_Collections_IList__Add,
            WellKnownMember.System_Collections_IList__Clear,
            WellKnownMember.System_Collections_IList__Contains,
            WellKnownMember.System_Collections_IList__IndexOf,
            WellKnownMember.System_Collections_IList__Insert,
            WellKnownMember.System_Collections_IList__Remove,
            WellKnownMember.System_Collections_IList__RemoveAt,
            WellKnownMember.System_NotSupportedException__ctor,
        };

        private static readonly SpecialMember[] s_readOnlyInterfacesWellKnownMembers = new[]
        {
            SpecialMember.System_Collections_Generic_IReadOnlyCollection_T__Count,
            SpecialMember.System_Collections_Generic_IReadOnlyList_T__get_Item,
        };

        private static readonly WellKnownMember[] s_requiredWellKnownMembersUnknownLength = new[]
        {
            WellKnownMember.System_Collections_Generic_List_T__Count,
            WellKnownMember.System_Collections_Generic_List_T__Contains,
            WellKnownMember.System_Collections_Generic_List_T__CopyTo,
            WellKnownMember.System_Collections_Generic_List_T__get_Item,
            WellKnownMember.System_Collections_Generic_List_T__IndexOf,
        };

        internal static NamedTypeSymbol Create(SourceModuleSymbol containingModule, string name, SynthesizedReadOnlyListKind kind)
        {
            var compilation = containingModule.DeclaringCompilation;
            DiagnosticInfo? diagnosticInfo = null;

            var hasReadOnlyInterfaces =
                compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T) is not MissingMetadataTypeSymbol &&
                compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T) is not MissingMetadataTypeSymbol;

            foreach (var type in s_requiredSpecialTypes)
            {
                diagnosticInfo = compilation.GetSpecialType(type).GetUseSiteInfo().DiagnosticInfo;
                if (diagnosticInfo is { })
                {
                    break;
                }
            }

            if (hasReadOnlyInterfaces && diagnosticInfo is null)
            {
                foreach (var type in s_readOnlyInterfacesSpecialTypes)
                {
                    diagnosticInfo = compilation.GetSpecialType(type).GetUseSiteInfo().DiagnosticInfo;
                    if (diagnosticInfo is { })
                    {
                        break;
                    }
                }
            }

            if (diagnosticInfo is null)
            {
                foreach (var type in s_requiredWellKnownTypes)
                {
                    diagnosticInfo = compilation.GetWellKnownType(type).GetUseSiteInfo().DiagnosticInfo;
                    if (diagnosticInfo is { })
                    {
                        break;
                    }
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

            if (hasReadOnlyInterfaces && diagnosticInfo is null)
            {
                foreach (var member in s_readOnlyInterfacesWellKnownMembers)
                {
                    diagnosticInfo = getSpecialTypeMemberDiagnosticInfo(compilation, member);
                    if (diagnosticInfo is { })
                    {
                        break;
                    }
                }
            }

            if (kind == SynthesizedReadOnlyListKind.List)
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

            return new SynthesizedReadOnlyListTypeSymbol(containingModule, name, kind, hasReadOnlyInterfaces);

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
                return diagnosticInfo;
            }
        }

        private readonly ModuleSymbol _containingModule;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<Symbol> _members;
        private readonly FieldSymbol _field;
        private readonly NamedTypeSymbol? _enumeratorType;

        private bool IsSingleElement => _field.Type.IsTypeParameter();
        private bool IsArray => _field.Type.IsArray();

        private SynthesizedReadOnlyListTypeSymbol(SourceModuleSymbol containingModule, string name, SynthesizedReadOnlyListKind kind, bool hasReadOnlyInterfaces)
        {
            var compilation = containingModule.DeclaringCompilation;

            _containingModule = containingModule;
            Name = name;
            var typeParameter = new SynthesizedReadOnlyListTypeParameterSymbol(this);
            TypeParameters = ImmutableArray.Create<TypeParameterSymbol>(typeParameter);
            var typeArgs = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            TypeSymbol fieldType = kind switch
            {
                SynthesizedReadOnlyListKind.SingleElement => typeParameter,
                SynthesizedReadOnlyListKind.Array => compilation.CreateArrayTypeSymbol(elementType: typeParameter),
                SynthesizedReadOnlyListKind.List => compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(typeArgs),
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };

            _enumeratorType = kind == SynthesizedReadOnlyListKind.SingleElement ? new SynthesizedReadOnlyListEnumeratorTypeSymbol(this, typeParameter) : null;
            _field = new SynthesizedFieldSymbol(this, fieldType, kind == SynthesizedReadOnlyListKind.SingleElement ? "_item" : "_items", isReadOnly: true);

            var iEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            var iCollection = compilation.GetWellKnownType(WellKnownType.System_Collections_ICollection);
            var iList = compilation.GetWellKnownType(WellKnownType.System_Collections_IList);
            var iEnumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(typeArgs);
            var iReadOnlyCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T).Construct(typeArgs);
            var iReadOnlyListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T).Construct(typeArgs);
            var iCollectionT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).Construct(typeArgs);
            var iListT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).Construct(typeArgs);

            _interfaces = hasReadOnlyInterfaces
                ? ImmutableArray.Create(
                    iEnumerable,
                    iCollection,
                    iList,
                    iEnumerableT,
                    iReadOnlyCollectionT,
                    iReadOnlyListT,
                    iCollectionT,
                    iListT)
                : ImmutableArray.Create(
                    iEnumerable,
                    iCollection,
                    iList,
                    iEnumerableT,
                    iCollectionT,
                    iListT);

            var membersBuilder = ArrayBuilder<Symbol>.GetInstance();
            membersBuilder.Add(_field);
            membersBuilder.AddIfNotNull(_enumeratorType);
            membersBuilder.Add(
                new SynthesizedReadOnlyListConstructor(this, fieldType, kind == SynthesizedReadOnlyListKind.SingleElement ? "item" : "items"));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator),
                    generateGetEnumerator));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_ICollection__Count)!,
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_ICollection__IsSynchronized)!,
                    generateIsSynchronized));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_ICollection__SyncRoot)!,
                    generateSyncRoot));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_ICollection__CopyTo)!,
                    generateCopyTo));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)((MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__get_Item)!).AssociatedSymbol,
                    generateIndexer,
                    generateNotSupportedException));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__IsFixedSize)!,
                    generateIsFixedSize));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    (PropertySymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__IsReadOnly)!,
                    generateIsReadOnly));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__Add)!,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__Clear)!,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__Contains)!,
                    generateContains));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__IndexOf)!,
                    generateIndexOf));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__Insert)!,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__Remove)!,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_IList__RemoveAt)!,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator)!).AsMember(iEnumerableT),
                    generateGetEnumerator));
            if (hasReadOnlyInterfaces)
            {
                addProperty(membersBuilder,
                    new SynthesizedReadOnlyListProperty(
                        this,
                        ((PropertySymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IReadOnlyCollection_T__Count)!).AsMember(iReadOnlyCollectionT),
                        generateCount));
                addProperty(membersBuilder,
                    new SynthesizedReadOnlyListProperty(
                        this,
                        ((PropertySymbol)((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IReadOnlyList_T__get_Item)!).AssociatedSymbol).AsMember(iReadOnlyListT),
                        generateIndexer));
            }
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__Count)!).AsMember(iCollectionT),
                    generateCount));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__IsReadOnly)!).AsMember(iCollectionT),
                    generateIsReadOnly));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__Add)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__Clear)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__Contains)!).AsMember(iCollectionT),
                    generateContains));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__CopyTo)!).AsMember(iCollectionT),
                    generateCopyTo));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_ICollection_T__Remove)!).AsMember(iCollectionT),
                    generateNotSupportedException));
            addProperty(membersBuilder,
                new SynthesizedReadOnlyListProperty(
                    this,
                    ((PropertySymbol)((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IList_T__get_Item)!).AssociatedSymbol).AsMember(iListT),
                    generateIndexer,
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IList_T__IndexOf)!).AsMember(iListT),
                    generateIndexOf));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IList_T__Insert)!).AsMember(iListT),
                    generateNotSupportedException));
            membersBuilder.Add(
                new SynthesizedReadOnlyListMethod(
                    this,
                    ((MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IList_T__RemoveAt)!).AsMember(iListT),
                    generateNotSupportedException));
            _members = membersBuilder.ToImmutableAndFree();

            // IEnumerable.GetEnumerator(), IEnumerable<T>.GetEnumerator()
            static BoundStatement generateGetEnumerator(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                if (containingType.IsSingleElement)
                {
                    var enumeratorType = containingType._enumeratorType;
                    Debug.Assert(enumeratorType is not null);
                    // return new Enumerator(_item);
                    return f.Return(f.New(enumeratorType, fieldReference));
                }
                else
                {
                    // return _items.GetEnumerator();
                    NamedTypeSymbol interfaceType = interfaceMethod.ContainingType;
                    Debug.Assert(interfaceType.IsInterface);
                    Conversion c = f.ClassifyEmitConversion(fieldReference, interfaceType);
                    Debug.Assert(c.IsImplicit);
                    Debug.Assert(c.IsReference);

                    return f.Return(
                        f.Call(
                            f.Convert(
                                interfaceType,
                                fieldReference,
                                c),
                            interfaceMethod));
                }
            }

            // ICollection.Count, IReadOnlyCollection<T>.Count, ICollection<T>.Count
            static BoundStatement generateCount(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                if (containingType.IsSingleElement)
                {
                    // return 1;
                    return f.Return(
                        f.Literal(1));
                }

                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                if (containingType.IsArray)
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

            // ICollection.IsSynchronized
            static BoundStatement generateIsSynchronized(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                // return false;
                return f.Return(f.Literal(false));
            }

            // ICollection.SyncRoot
            static BoundStatement generateSyncRoot(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                // return (object)this;
                BoundThisReference thisRef = f.This();
                TypeSymbol returnType = interfaceMethod.ReturnType;
                Debug.Assert(returnType.IsObjectType());
                Conversion c = f.ClassifyEmitConversion(thisRef, returnType);
                Debug.Assert(c.IsImplicit);
                Debug.Assert(c.IsReference);

                return f.Return(
                    f.Convert(
                        returnType,
                        thisRef,
                        c));
            }

            // IList.IsFixedSize
            static BoundStatement generateIsFixedSize(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                // return true;
                return f.Return(f.Literal(true));
            }

            // IList.IsReadOnly, ICollection<T>.IsReadOnly
            static BoundStatement generateIsReadOnly(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                // return true;
                return f.Return(f.Literal(true));
            }

            // IList.Contains(object), ICollection<T>.Contains(T)
            static BoundStatement generateContains(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (containingType.IsSingleElement)
                {
                    // return EqualityComparer<T>.Default.Equals(_item, param0);
                    return f.Return(
                        makeEqualityComparerDefaultEquals(f, fieldReference, parameterReference));
                }
                else if (containingType.IsArray || !interfaceMethod.ContainingType.IsGenericType)
                {
                    // return ((ICollection<T>)_items).Contains(param0);
                    NamedTypeSymbol interfaceType = interfaceMethod.ContainingType;
                    Debug.Assert(interfaceType.IsInterface);
                    Conversion c = f.ClassifyEmitConversion(fieldReference, interfaceType);
                    Debug.Assert(c.IsImplicit);
                    Debug.Assert(c.IsReference);

                    return f.Return(
                        f.Call(
                            f.Convert(
                                interfaceType,
                                fieldReference,
                                c),
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

            // ICollection.CopyTo(Array, int), ICollection<T>.CopyTo(T[], int)
            static BoundStatement generateCopyTo(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference0 = f.Parameter(method.Parameters[0]);
                var parameterReference1 = f.Parameter(method.Parameters[1]);
                BoundStatement statement;
                if (containingType.IsSingleElement)
                {
                    if (!interfaceMethod.ContainingType.IsGenericType)
                    {
                        var arraySetValueMethod = (MethodSymbol)method.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Array__SetValue)!;

                        // param0.SetValue((object)_item, param1)
                        NamedTypeSymbol objectType = f.SpecialType(SpecialType.System_Object);
                        Conversion c = f.ClassifyEmitConversion(fieldReference, objectType);
                        Debug.Assert(c.IsImplicit);
                        Debug.Assert(c.IsBoxing);

                        statement = f.ExpressionStatement(
                            f.Call(parameterReference0, arraySetValueMethod,
                                f.Convert(objectType, fieldReference, c),
                                parameterReference1));
                    }
                    else
                    {
                        // param0[param1] = _item;
                        statement = f.Assignment(
                            f.ArrayAccess(
                                parameterReference0,
                                parameterReference1),
                            fieldReference);
                    }
                }
                else if (containingType.IsArray || !interfaceMethod.ContainingType.IsGenericType)
                {
                    // ((ICollection<T>)_items).CopyTo(param0, param1);
                    NamedTypeSymbol interfaceType = interfaceMethod.ContainingType;
                    Debug.Assert(interfaceType.IsInterface);
                    Conversion c = f.ClassifyEmitConversion(fieldReference, interfaceType);
                    Debug.Assert(c.IsImplicit);
                    Debug.Assert(c.IsReference);

                    statement = f.ExpressionStatement(
                        f.Call(
                            f.Convert(
                                interfaceType,
                                fieldReference,
                                c),
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

            // IList.this[int], IReadOnlyList<T>.this[int], IList<T>.this[int]
            static BoundStatement generateIndexer(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (containingType.IsSingleElement)
                {
                    // if (param0 != 0)
                    //      throw new IndexOutOfRangeException();
                    // return _item;
                    var constructor = (MethodSymbol)method.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_IndexOutOfRangeException__ctor)!;
                    return f.Block(
                        f.If(
                            f.IntNotEqual(parameterReference, f.Literal(0)),
                            f.Throw(f.New(constructor))),
                        f.Return(fieldReference));
                }
                else if (containingType.IsArray)
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

            // IList.IndexOf(object), IList<T>.IndexOf(T)
            static BoundStatement generateIndexOf(SyntheticBoundNodeFactory f, MethodSymbol method, MethodSymbol interfaceMethod)
            {
                var containingType = (SynthesizedReadOnlyListTypeSymbol)method.ContainingType;
                var field = containingType._field;
                var fieldReference = f.Field(f.This(), field);
                var parameterReference = f.Parameter(method.Parameters[0]);
                if (containingType.IsSingleElement)
                {
                    // return EqualityComparer<T>.Default.Equals(_item, param0) ? 0 : -1;
                    return f.Return(
                        f.Conditional(
                            makeEqualityComparerDefaultEquals(f, fieldReference, parameterReference),
                            f.Literal(0),
                            f.Literal(-1),
                            method.ReturnType));
                }
                else if (containingType.IsArray || !interfaceMethod.ContainingType.IsGenericType)
                {
                    // return ((IList<T>)_items).IndexOf(param0);
                    NamedTypeSymbol interfaceType = interfaceMethod.ContainingType;
                    Debug.Assert(interfaceType.IsInterface);
                    Conversion c = f.ClassifyEmitConversion(fieldReference, interfaceType);
                    Debug.Assert(c.IsImplicit);
                    Debug.Assert(c.IsReference);

                    return f.Return(
                        f.Call(
                            f.Convert(
                                interfaceType,
                                fieldReference,
                                c),
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

            static BoundCall makeEqualityComparerDefaultEquals(
                SyntheticBoundNodeFactory f, BoundFieldAccess fieldReference, BoundParameter parameterReference)
            {
                TypeSymbol fieldType = fieldReference.Type;
                Debug.Assert(fieldType.IsTypeParameter());
                Debug.Assert(parameterReference.Type.Equals(fieldType) ||
                             parameterReference.Type.IsObjectType());

                var equalityComparer_get_Default = f.WellKnownMethod(
                    WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
                var equalityComparer_Equals = f.WellKnownMethod(
                    WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals);
                var equalityComparerType = equalityComparer_Equals.ContainingType;
                var constructedEqualityComparer = equalityComparerType.Construct(fieldType);

                Conversion c = f.ClassifyEmitConversion(parameterReference, fieldType);
                Debug.Assert(c.IsUnboxing || c.IsIdentity);

                // If the parameter type is object:
                //
                //      EqualityComparer<T>.Default.Equals(_item, (T)param0)
                //
                // Otherwise:
                //
                //      EqualityComparer<T>.Default.Equals(_item, param0)
                //
                return f.Call(
                    f.StaticCall(
                        constructedEqualityComparer,
                        equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                    equalityComparer_Equals.AsMember(constructedEqualityComparer),
                    fieldReference,
                    f.Convert(fieldType, parameterReference, c));
            }
        }

        private Symbol GetFieldTypeMember(WellKnownMember member)
        {
            var symbol = DeclaringCompilation.GetWellKnownTypeMember(member);

            Debug.Assert(symbol is { });
            Debug.Assert(_field.Type.OriginalDefinition.Equals(symbol.ContainingType, TypeCompareKind.AllIgnoreOptions));

            return symbol.SymbolAsMember((NamedTypeSymbol)_field.Type);
        }

        public static bool CanCreateSingleElement(CSharpCompilation compilation)
        {
            // only checking for additional well-known types and members used in the single-element implementation
            return compilation.GetWellKnownType(WellKnownType.System_IndexOutOfRangeException) is not MissingMetadataTypeSymbol
                && compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_EqualityComparer_T) is not MissingMetadataTypeSymbol
                && compilation.GetWellKnownTypeMember(WellKnownMember.System_IndexOutOfRangeException__ctor) is not null
                && compilation.GetSpecialTypeMember(SpecialMember.System_Array__SetValue) is not null
                && compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default) is not null
                && compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals) is not null
                && compilation.GetSpecialType(SpecialType.System_IDisposable) is not MissingMetadataTypeSymbol
                && compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator) is not MissingMetadataTypeSymbol
                && compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T) is not MissingMetadataTypeSymbol
                && compilation.GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__Current) is not null
                && compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current) is not null
                && compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext) is not null
                && compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Reset) is not null
                && compilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose) is not null;
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

        internal override string? ExtensionGroupingName => null;

        internal override string? ExtensionMarkerName => null;

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

        internal override bool IsClosed => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal override bool HasCompilerLoweringPreserveAttribute => false;

        internal override bool IsInterpolatedStringHandlerType => false;

        internal sealed override ParameterSymbol? ExtensionParameter => null;

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

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray(static (m, name) => m.Name == name, name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
            => _enumeratorType is not null
                ? ImmutableArray.Create(_enumeratorType)
                : ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
            => GetTypeMembers(name).WhereAsArray(static (type, arity) => type.Arity == arity, arity);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
            => GetTypeMembers().WhereAsArray(static (type, name) => type.Name.AsSpan().SequenceEqual(name.Span), name);

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();

        internal override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => BaseTypeNoUseSiteDiagnostics;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => _interfaces;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _members.OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => _interfaces;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        internal override bool GetGuidString(out string? guidString)
        {
            guidString = null;
            return false;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            AddSynthesizedAttribute(ref attributes, DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

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

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => _interfaces;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
    }
}
