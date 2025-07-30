// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ExtensionGroupingInfo
    {
        /// <summary>
        /// Extension block symbols declared in a class are grouped by their corresponding grouping type metadata name (top level key),
        /// then grouped by their corresponding extension marker type metadata name (the secondary key used by MultiDictionary).
        /// <see cref="SourceNamedTypeSymbol"/>s are the extension blocks.
        /// </summary>
        private readonly Dictionary<string, MultiDictionary<string, SourceNamedTypeSymbol>> _groupingMap;
        private ImmutableArray<ExtensionGroupingType> _lazyGroupingTypes;

        public ExtensionGroupingInfo(SourceMemberContainerTypeSymbol container)
        {
            var groupingMap = new Dictionary<string, MultiDictionary<string, SourceNamedTypeSymbol>>(EqualityComparer<string>.Default);

            foreach (var type in container.GetTypeMembers(""))
            {
                if (!type.IsExtension)
                {
                    continue;
                }

                var sourceNamedType = (SourceNamedTypeSymbol)type;
                var groupingMetadataName = sourceNamedType.ExtensionGroupingName;

                MultiDictionary<string, SourceNamedTypeSymbol>? markerMap;

                if (!groupingMap.TryGetValue(groupingMetadataName, out markerMap))
                {
                    markerMap = new MultiDictionary<string, SourceNamedTypeSymbol>(EqualityComparer<string>.Default, ReferenceEqualityComparer.Instance);
                    groupingMap.Add(groupingMetadataName, markerMap);
                }

                markerMap.Add(sourceNamedType.ExtensionMarkerName, sourceNamedType);
            }

            _groupingMap = groupingMap;
        }

        public ImmutableArray<Cci.INestedTypeDefinition> GetGroupingTypes()
        {
            if (_lazyGroupingTypes.IsDefault)
            {
                var builder = ArrayBuilder<ExtensionGroupingType>.GetInstance(_groupingMap.Count);

                foreach (KeyValuePair<string, MultiDictionary<string, SourceNamedTypeSymbol>> pair in _groupingMap)
                {
                    builder.Add(new ExtensionGroupingType(pair.Key, pair.Value));
                }

                builder.Sort();

                ImmutableInterlocked.InterlockedInitialize(ref _lazyGroupingTypes, builder.ToImmutableAndFree());
            }

            return ImmutableArray<Cci.INestedTypeDefinition>.CastUp(_lazyGroupingTypes);
        }

        public Cci.ITypeDefinition GetCorrespondingMarkerType(SynthesizedExtensionMarker markerMethod)
        {
            return GetCorrespondingMarkerType((SourceNamedTypeSymbol)markerMethod.ContainingType);
        }

        private ExtensionMarkerType GetCorrespondingMarkerType(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);
            GetGroupingTypes();

            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : Optimize lookup with side dictionaries?
            var groupingName = extension.ExtensionGroupingName;
            var markerName = extension.ExtensionMarkerName;

            foreach (var groupingType in _lazyGroupingTypes)
            {
                if (groupingType.Name != groupingName)
                {
                    continue;
                }

                foreach (var markerType in groupingType.ExtensionMarkerTypes)
                {
                    if (markerType.Name == markerName)
                    {
                        return markerType;
                    }
                }

                break;
            }

            throw ExceptionUtilities.Unreachable();
        }

        public Cci.TypeMemberVisibility GetCorrespondingMarkerMethodVisibility(SynthesizedExtensionMarker marker)
        {
            Debug.Assert(Cci.TypeMemberVisibility.Public > Cci.TypeMemberVisibility.Assembly);
            Debug.Assert(Cci.TypeMemberVisibility.Assembly > Cci.TypeMemberVisibility.Private);

            var result = Cci.TypeMemberVisibility.Private;

            foreach (var extension in GetCorrespondingMarkerType((SourceNamedTypeSymbol)marker.ContainingType).UnderlyingExtensions)
            {
                foreach (var symbol in extension.GetMembers())
                {
                    var memberVisibility = symbol.MetadataVisibility;
                    Debug.Assert(memberVisibility is Cci.TypeMemberVisibility.Public or Cci.TypeMemberVisibility.Assembly or Cci.TypeMemberVisibility.Private);

                    if (memberVisibility == Cci.TypeMemberVisibility.Public)
                    {
                        return TypeMemberVisibility.Public;
                    }

                    if (result < memberVisibility)
                    {
                        // If we have a more visible member, use its visibility.
                        result = memberVisibility;
                    }
                }
            }

            // PROTOTYPE: Is there a real need to cache this result for reuse?
            return result;
        }

        public Cci.ITypeDefinition GetCorrespondingGroupingType(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);
            GetGroupingTypes();

            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : Optimize lookup with a side dictionary?
            var groupingName = extension.ExtensionGroupingName;

            foreach (var groupingType in _lazyGroupingTypes)
            {
                if (groupingType.Name == groupingName)
                {
                    return groupingType;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        // Given an extension block, returns all the extensions that are grouped together with it
        internal ImmutableArray<SourceNamedTypeSymbol> GetMergedExtensions(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);
            return GetCorrespondingMarkerType(extension).UnderlyingExtensions;
        }

        // Returns all the extension blocks but grouped/merged by equivalency (ie. same marker name)
        internal IEnumerable<ImmutableArray<SourceNamedTypeSymbol>> EnumerateMergedExtensionBlocks()
        {
            GetGroupingTypes();
            foreach (var groupingType in _lazyGroupingTypes)
            {
                foreach (var markerType in groupingType.ExtensionMarkerTypes)
                {
                    yield return markerType.UnderlyingExtensions;
                }
            }
        }

        private abstract class ExtensionGroupingOrMarkerType : Cci.INestedTypeDefinition
        {
            ushort ITypeDefinition.Alignment => 0;

            IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters => GenericParameters;

            protected abstract IEnumerable<IGenericTypeParameter> GenericParameters { get; }

            ushort ITypeDefinition.GenericParameterCount => GenericParameterCount;

            ushort INamedTypeReference.GenericParameterCount => GenericParameterCount;

            protected abstract ushort GenericParameterCount { get; }

            bool ITypeDefinition.HasDeclarativeSecurity => false;

            bool ITypeDefinition.IsAbstract => IsAbstract;

            protected abstract bool IsAbstract { get; }

            bool ITypeDefinition.IsBeforeFieldInit => false;

            bool ITypeDefinition.IsComObject => false;

            bool ITypeDefinition.IsGeneric => GenericParameterCount != 0;

            bool ITypeDefinition.IsInterface => false;

            bool ITypeDefinition.IsDelegate => false;

            bool ITypeDefinition.IsRuntimeSpecial => false;

            bool ITypeDefinition.IsSerializable => false;

            bool ITypeDefinition.IsSpecialName => true;

            bool ITypeDefinition.IsWindowsRuntimeImport => false;

            bool ITypeDefinition.IsSealed => IsSealed;

            protected abstract bool IsSealed { get; }

            LayoutKind ITypeDefinition.Layout => LayoutKind.Auto;

            IEnumerable<SecurityAttribute> ITypeDefinition.SecurityAttributes => SpecializedCollections.EmptyEnumerable<SecurityAttribute>();

            uint ITypeDefinition.SizeOf => 0;

            CharSet ITypeDefinition.StringFormat => CharSet.Ansi;

            ITypeDefinition ITypeDefinitionMember.ContainingTypeDefinition => ContainingTypeDefinition;

            protected abstract ITypeDefinition ContainingTypeDefinition { get; }

            TypeMemberVisibility ITypeDefinitionMember.Visibility => TypeMemberVisibility.Public;

            bool IDefinition.IsEncDeleted => false;

            bool INamedTypeReference.MangleName => false;

            string? INamedTypeReference.AssociatedFileIdentifier => null;

            bool ITypeReference.IsEnum => false;

            bool ITypeReference.IsValueType => false;

            Cci.PrimitiveTypeCode ITypeReference.TypeCode => Cci.PrimitiveTypeCode.NotPrimitive;

            TypeDefinitionHandle ITypeReference.TypeDef => default;

            IGenericMethodParameterReference? ITypeReference.AsGenericMethodParameterReference => null;

            IGenericTypeInstanceReference? ITypeReference.AsGenericTypeInstanceReference => null;

            IGenericTypeParameterReference? ITypeReference.AsGenericTypeParameterReference => null;

            INamespaceTypeReference? ITypeReference.AsNamespaceTypeReference => null;

            INestedTypeReference? ITypeReference.AsNestedTypeReference => this;

            ISpecializedNestedTypeReference? ITypeReference.AsSpecializedNestedTypeReference => null;

            string? INamedEntity.Name => Name;

            public abstract string Name { get; }

            IDefinition? IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            INamespaceTypeDefinition? ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
            {
                return null;
            }

            INestedTypeDefinition? ITypeReference.AsNestedTypeDefinition(EmitContext context)
            {
                return this;
            }

            bool Cci.INestedTypeReference.InheritsEnclosingTypeTypeParameters => false;

            ITypeDefinition? ITypeReference.AsTypeDefinition(EmitContext context)
            {
                return this;
            }

            void IReference.Dispatch(MetadataVisitor visitor)
            {
                visitor.Visit((INamedTypeDefinition)this);
            }

            IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context)
            {
                return GetAttributes(context);
            }

            protected abstract IEnumerable<ICustomAttribute> GetAttributes(EmitContext context);

            ITypeReference? ITypeDefinition.GetBaseClass(EmitContext context)
            {
                return ObjectType;
            }

            protected abstract ITypeReference? ObjectType { get; }

            ITypeReference ITypeMemberReference.GetContainingType(EmitContext context)
            {
                return ContainingTypeDefinition;
            }

            IEnumerable<IEventDefinition> ITypeDefinition.GetEvents(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<IEventDefinition>();
            }

            IEnumerable<Cci.MethodImplementation> ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<Cci.MethodImplementation>();
            }

            IEnumerable<IFieldDefinition> ITypeDefinition.GetFields(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<IFieldDefinition>();
            }

            ISymbolInternal? IReference.GetInternalSymbol()
            {
                return null;
            }

            IEnumerable<IMethodDefinition> ITypeDefinition.GetMethods(EmitContext context)
            {
                return GetMethods(context);
            }

            protected abstract IEnumerable<IMethodDefinition> GetMethods(EmitContext context);

            IEnumerable<INestedTypeDefinition> ITypeDefinition.GetNestedTypes(EmitContext context)
            {
                return NestedTypes;
            }

            protected abstract IEnumerable<INestedTypeDefinition> NestedTypes { get; }

            IEnumerable<IPropertyDefinition> ITypeDefinition.GetProperties(EmitContext context)
            {
                return GetProperties(context);
            }

            protected abstract IEnumerable<IPropertyDefinition> GetProperties(EmitContext context);

            ITypeDefinition? ITypeReference.GetResolvedType(EmitContext context)
            {
                return this;
            }

            IEnumerable<TypeReferenceWithAttributes> ITypeDefinition.Interfaces(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<TypeReferenceWithAttributes>();
            }

            public sealed override bool Equals(object? obj)
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw ExceptionUtilities.Unreachable();
            }

            public sealed override int GetHashCode()
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw ExceptionUtilities.Unreachable();
            }
        }

        private sealed class ExtensionGroupingType : ExtensionGroupingOrMarkerType, IComparable<ExtensionGroupingType>
        {
            private readonly string _name;
            public readonly ImmutableArray<ExtensionMarkerType> ExtensionMarkerTypes;
            private readonly ImmutableArray<ExtensionGroupingTypeTypeParameter> _typeParameters;

            public ExtensionGroupingType(string name, MultiDictionary<string, SourceNamedTypeSymbol> extensionMarkerTypes)
            {
                _name = name;

                var builder = ArrayBuilder<ExtensionMarkerType>.GetInstance(extensionMarkerTypes.Count);

                foreach (var pair in extensionMarkerTypes)
                {
                    builder.Add(new ExtensionMarkerType(this, name: pair.Key, extensions: pair.Value));
                }

                builder.Sort();
                ExtensionMarkerTypes = builder.ToImmutableAndFree();

                _typeParameters = ExtensionMarkerTypes[0].UnderlyingExtensions[0].Arity != 0 ?
                    ((INestedTypeDefinition)ExtensionMarkerTypes[0].UnderlyingExtensions[0].GetCciAdapter()).GenericParameters.SelectAsArray(static (p, @this) => new ExtensionGroupingTypeTypeParameter(@this, p), this) :
                    [];
            }

            int IComparable<ExtensionGroupingType>.CompareTo(ExtensionGroupingType? other)
            {
                Debug.Assert(other is { });
                return ExtensionMarkerTypes[0].CompareTo(other.ExtensionMarkerTypes[0]);
            }

            protected override IEnumerable<IGenericTypeParameter> GenericParameters => _typeParameters;

            protected override ushort GenericParameterCount => (ushort)ExtensionMarkerTypes[0].UnderlyingExtensions[0].Arity;

            protected override bool IsAbstract => false;

            protected override bool IsSealed => true;

            protected override ITypeDefinition ContainingTypeDefinition
            {
                get
                {
                    Debug.Assert(ExtensionMarkerTypes[0].UnderlyingExtensions[0].ContainingType is not null);
                    return ExtensionMarkerTypes[0].UnderlyingExtensions[0].ContainingType!.GetCciAdapter();
                }
            }

            public override string Name => _name;

            protected override ITypeReference? ObjectType => ExtensionMarkerTypes[0].UnderlyingExtensions[0].ContainingAssembly.GetSpecialType(SpecialType.System_Object).GetCciAdapter();

            protected override IEnumerable<IMethodDefinition> GetMethods(EmitContext context)
            {
                foreach (var marker in ExtensionMarkerTypes)
                {
                    foreach (var type in marker.UnderlyingExtensions)
                    {
                        foreach (var method in type.GetMethodsToEmit())
                        {
                            Debug.Assert((object)method != null);

                            if (method.GetCciAdapter().ShouldInclude(context))
                            {
                                yield return method.GetCciAdapter();
                            }
                        }
                    }
                }
            }

            protected override IEnumerable<INestedTypeDefinition> NestedTypes => ExtensionMarkerTypes;

            protected override IEnumerable<IPropertyDefinition> GetProperties(EmitContext context)
            {
                foreach (var marker in ExtensionMarkerTypes)
                {
                    foreach (var type in marker.UnderlyingExtensions)
                    {
                        foreach (PropertySymbol property in type.GetPropertiesToEmit())
                        {
                            Debug.Assert((object)property != null);
                            IPropertyDefinition definition = property.GetCciAdapter();
                            // If any accessor should be included, then the property should be included too
                            if (definition.ShouldInclude(context) || !definition.GetAccessors(context).IsEmpty())
                            {
                                yield return definition;
                            }
                        }
                    }
                }
            }

            protected override IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
            {
                SynthesizedAttributeData? extensionAttribute = ExtensionMarkerTypes[0].UnderlyingExtensions[0].DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);

                if (extensionAttribute is { })
                {
                    yield return extensionAttribute;
                }
            }
        }

        private sealed class ExtensionGroupingTypeTypeParameter : InheritedTypeParameter
        {
            internal ExtensionGroupingTypeTypeParameter(ExtensionGroupingType inheritingType, IGenericTypeParameter parentParameter) :
                base(parentParameter.Index, inheritingType, parentParameter)
            {
            }

            public override string? Name => "$T" + Index;

            public override IEnumerable<TypeReferenceWithAttributes> GetConstraints(EmitContext context)
            {
                // Drop all attributes from constraints, they are about C# specific semantics.
                foreach (var constraint in base.GetConstraints(context))
                {
                    yield return new TypeReferenceWithAttributes(constraint.TypeRef);
                }
            }

            public override IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
            {
                // Preserve only the synthesized IsUnmanagedAttribute.
                if (MustBeValueType)
                {
                    // PROTOTYPE: Make sure we have coverage for the WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor case
                    var unmanagedCtor = ((PEModuleBuilder)context.Module).TryGetSynthesizedIsUnmanagedAttribute()?.Constructors[0] ??
                        ((ExtensionGroupingType)DefiningType).ExtensionMarkerTypes[0].UnderlyingExtensions[0].DeclaringCompilation.
                            GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor);

                    if (unmanagedCtor is { })
                    {
                        foreach (var attribute in base.GetAttributes(context))
                        {
                            if (attribute is SynthesizedAttributeData synthesized &&
                                synthesized.AttributeConstructor == unmanagedCtor)
                            {
                                return [synthesized];
                            }
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
            }
        }

        private sealed class ExtensionMarkerType : ExtensionGroupingOrMarkerType, IComparable<ExtensionMarkerType>
        {
            public readonly ExtensionGroupingType GroupingType;
            private readonly string _name;
            public readonly ImmutableArray<SourceNamedTypeSymbol> UnderlyingExtensions;
            private readonly ImmutableArray<InheritedTypeParameter> _typeParameters;

            public ExtensionMarkerType(ExtensionGroupingType groupingType, string name, MultiDictionary<string, SourceNamedTypeSymbol>.ValueSet extensions)
            {
                GroupingType = groupingType;
                _name = name;

                var builder = ArrayBuilder<SourceNamedTypeSymbol>.GetInstance(extensions.Count);
                builder.AddRange(extensions);
                builder.Sort(LexicalOrderSymbolComparer.Instance);
                UnderlyingExtensions = builder.ToImmutableAndFree();

                _typeParameters = UnderlyingExtensions[0].Arity != 0 ?
                    ((INestedTypeDefinition)UnderlyingExtensions[0].GetCciAdapter()).GenericParameters.SelectAsArray(static (p, @this) => new InheritedTypeParameter(p.Index, @this, p), this) :
                    [];
            }

            public int CompareTo(ExtensionMarkerType? other)
            {
                Debug.Assert(other is { });
                return LexicalOrderSymbolComparer.Instance.Compare(UnderlyingExtensions[0], other.UnderlyingExtensions[0]);
            }

            protected override IEnumerable<IGenericTypeParameter> GenericParameters => _typeParameters;

            protected override ushort GenericParameterCount => (ushort)UnderlyingExtensions[0].Arity;

            protected override bool IsAbstract => true;

            protected override bool IsSealed => true;

            protected override ITypeDefinition ContainingTypeDefinition => GroupingType;

            public override string Name => _name;

            protected override ITypeReference? ObjectType => UnderlyingExtensions[0].ContainingAssembly.GetSpecialType(SpecialType.System_Object).GetCciAdapter();

            protected override IEnumerable<IMethodDefinition> GetMethods(EmitContext context)
            {
                var marker = UnderlyingExtensions[0].TryGetOrCreateExtensionMarker();

                if (marker is { })
                {
                    yield return marker.GetCciAdapter();
                }
            }

            protected override IEnumerable<INestedTypeDefinition> NestedTypes => SpecializedCollections.EmptyEnumerable<INestedTypeDefinition>();

            protected override IEnumerable<IPropertyDefinition> GetProperties(EmitContext context) => SpecializedCollections.EmptyEnumerable<IPropertyDefinition>();

            protected override IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
            }
        }
    }
}
