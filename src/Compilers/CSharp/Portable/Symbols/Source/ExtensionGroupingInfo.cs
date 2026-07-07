// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly ImmutableArray<ExtensionGroupingType> _groupingTypes;

        public ExtensionGroupingInfo(SourceMemberContainerTypeSymbol container)
        {
            // Extension block symbols declared in a class are grouped by their corresponding grouping type metadata name (top level key),
            // then grouped by their corresponding extension marker type metadata name (the secondary key used by MultiDictionary).
            // SourceNamedTypeSymbols are the extension blocks.
            var groupingMap = new Dictionary<string, MultiDictionary<string, SourceNamedTypeSymbol>>(EqualityComparer<string>.Default);

            foreach (var type in container.GetTypeMembers(""))
            {
                if (!type.IsExtension)
                {
                    continue;
                }

                var sourceNamedType = (SourceNamedTypeSymbol)type;
                Debug.Assert(sourceNamedType.ExtensionGroupingName is not null);
                var groupingMetadataName = sourceNamedType.ExtensionGroupingName;

                MultiDictionary<string, SourceNamedTypeSymbol>? markerMap;

                if (!groupingMap.TryGetValue(groupingMetadataName, out markerMap))
                {
                    markerMap = new MultiDictionary<string, SourceNamedTypeSymbol>(EqualityComparer<string>.Default, ReferenceEqualityComparer.Instance);
                    groupingMap.Add(groupingMetadataName, markerMap);
                }

                Debug.Assert(sourceNamedType.ExtensionMarkerName is not null);
                markerMap.Add(sourceNamedType.ExtensionMarkerName, sourceNamedType);
            }

            var builder = ArrayBuilder<ExtensionGroupingType>.GetInstance(groupingMap.Count);

            foreach (KeyValuePair<string, MultiDictionary<string, SourceNamedTypeSymbol>> pair in groupingMap)
            {
                builder.Add(new ExtensionGroupingType(pair.Key, pair.Value));
            }

            builder.Sort();

            _groupingTypes = builder.ToImmutableAndFree();
            AssertInvariants(container);
        }

        [Conditional("DEBUG")]
        private void AssertInvariants(SourceMemberContainerTypeSymbol container)
        {
            ImmutableArray<NamedTypeSymbol> typeMembers = container.GetTypeMembers("");

            for (int i = 0; i < typeMembers.Length; i++)
            {
                var type1 = (SourceNamedTypeSymbol)typeMembers[i];
                if (!type1.IsExtension)
                {
                    continue;
                }

                for (int j = i + 1; j < typeMembers.Length; j++)
                {
                    var type2 = (SourceNamedTypeSymbol)typeMembers[j];
                    if (!type2.IsExtension)
                    {
                        continue;
                    }

                    bool groupingNamesMatch = type1.ComputeExtensionGroupingRawName() == type2.ComputeExtensionGroupingRawName();
                    Debug.Assert(groupingNamesMatch || !HaveSameILSignature(type1, type2),
                            "If the IL-level comparer considers two extensions equal, then they must have the same grouping name.");

                    bool markerNamesMatch = type1.ComputeExtensionMarkerRawName() == type2.ComputeExtensionMarkerRawName();
                    Debug.Assert(markerNamesMatch || !HaveSameCSharpSignature(type1, type2),
                            "If the C#-level comparer considers two extensions equal, then they must have the same marker name.");

                    Debug.Assert(groupingNamesMatch || !markerNamesMatch, "If the marker names are equal, then the grouping names must also be equal.");
                }
            }
        }

        public ImmutableArray<Cci.INestedTypeDefinition> GetGroupingTypes()
        {
            return ImmutableArray<Cci.INestedTypeDefinition>.CastUp(_groupingTypes);
        }

        public Cci.ITypeDefinition GetCorrespondingMarkerType(SynthesizedExtensionMarker markerMethod)
        {
            return GetCorrespondingMarkerType((SourceNamedTypeSymbol)markerMethod.ContainingType);
        }

        private ExtensionMarkerType GetCorrespondingMarkerType(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);

            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : Optimize lookup with side dictionaries?
            var groupingName = extension.ExtensionGroupingName;
            var markerName = extension.ExtensionMarkerName;

            foreach (var groupingType in _groupingTypes)
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

            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : optimization, Is there a real need to cache this result for reuse?
            return result;
        }

        public Cci.ITypeDefinition GetCorrespondingGroupingType(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);

            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : Optimize lookup with a side dictionary?
            var groupingName = extension.ExtensionGroupingName;

            foreach (var groupingType in _groupingTypes)
            {
                if (groupingType.Name == groupingName)
                {
                    return groupingType;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// Given an extension block, returns all the extensions that are grouped together with it
        /// </summary>
        internal ImmutableArray<SourceNamedTypeSymbol> GetMergedExtensions(SourceNamedTypeSymbol extension)
        {
            Debug.Assert(extension.IsExtension);
            return GetCorrespondingMarkerType(extension).UnderlyingExtensions;
        }

        /// <summary>
        /// Returns all the extension blocks but grouped/merged by equivalency (ie. same marker name)
        /// </summary>
        internal IEnumerable<ImmutableArray<SourceNamedTypeSymbol>> EnumerateMergedExtensionBlocks()
        {
            foreach (var groupingType in _groupingTypes)
            {
                foreach (var markerType in groupingType.ExtensionMarkerTypes)
                {
                    yield return markerType.UnderlyingExtensions;
                }
            }
        }

        internal static bool HaveSameILSignature(SourceNamedTypeSymbol extension1, SourceNamedTypeSymbol extension2)
        {
            Debug.Assert(extension1.IsExtension);
            Debug.Assert(extension2.IsExtension);

            if (extension1.Arity != extension2.Arity)
            {
                return false;
            }

            TypeMap? typeMap1 = MemberSignatureComparer.GetTypeMap(extension1);
            TypeMap? typeMap2 = MemberSignatureComparer.GetTypeMap(extension2);
            if (extension1.Arity > 0
                && !MemberSignatureComparer.HaveSameConstraints(extension1.TypeParameters, typeMap1, extension2.TypeParameters, typeMap2, TypeCompareKind.CLRSignatureCompareOptions))
            {
                return false;
            }

            ParameterSymbol? parameter1 = extension1.ExtensionParameter;
            ParameterSymbol? parameter2 = extension2.ExtensionParameter;
            if (parameter1 is null || parameter2 is null)
            {
                return parameter1 is null && parameter2 is null;
            }

            if (!MemberSignatureComparer.HaveSameParameterType(parameter1, typeMap1, parameter2, typeMap2,
                refKindCompareMode: MemberSignatureComparer.RefKindCompareMode.IgnoreRefKind,
                considerDefaultValues: false, TypeCompareKind.CLRSignatureCompareOptions))
            {
                return false;
            }

            return true;
        }

        internal static bool HaveSameCSharpSignature(SourceNamedTypeSymbol extension1, SourceNamedTypeSymbol extension2)
        {
            Debug.Assert(extension1.IsExtension);
            Debug.Assert(extension2.IsExtension);

            int arity1 = extension1.Arity;
            if (arity1 != extension2.Arity)
            {
                return false;
            }

            TypeMap? typeMap1 = MemberSignatureComparer.GetTypeMap(extension1);
            TypeMap? typeMap2 = MemberSignatureComparer.GetTypeMap(extension2);
            if (arity1 > 0)
            {
                ImmutableArray<TypeParameterSymbol> typeParams1 = extension1.TypeParameters;
                ImmutableArray<TypeParameterSymbol> typeParams2 = extension2.TypeParameters;

                if (!typeParams1.SequenceEqual(typeParams2, (p1, p2) => p1.Name == p2.Name))
                {
                    return false;
                }

                if (!typeParams1.SequenceEqual(typeParams2, (p1, p2) => hasSameAttributes(p1.GetAttributes(), p2.GetAttributes())))
                {
                    return false;
                }

                for (int i = 0; i < arity1; i++)
                {
                    if (!haveSameConstraints(typeParams1[i], typeMap1, typeParams2[i], typeMap2))
                    {
                        return false;
                    }
                }
            }

            ParameterSymbol? parameter1 = extension1.ExtensionParameter;
            ParameterSymbol? parameter2 = extension2.ExtensionParameter;
            if (parameter1 is null)
            {
                return parameter2 is null;
            }
            else if (parameter2 is null)
            {
                return parameter1 is null;
            }

            if (parameter1.DeclaredScope != parameter2.DeclaredScope)
            {
                return false;
            }

            if (parameter1.Name != parameter2.Name)
            {
                return false;
            }

            if (!MemberSignatureComparer.HaveSameParameterType(parameter1, typeMap1, parameter2, typeMap2,
                refKindCompareMode: MemberSignatureComparer.RefKindCompareMode.ConsiderDifferences,
                considerDefaultValues: false, TypeCompareKind.ConsiderEverything))
            {
                return false;
            }

            if (!hasSameAttributes(parameter1.GetAttributes(), parameter2.GetAttributes()))
            {
                return false;
            }

            return true;

            static bool hasSameAttributes(ImmutableArray<CSharpAttributeData> attributes1, ImmutableArray<CSharpAttributeData> attributes2)
            {
                if (attributes1.IsEmpty && attributes2.IsEmpty)
                {
                    return true;
                }

                // Tracked by https://github.com/dotnet/roslyn/issues/78827 : optimization, consider using a pool
                var comparer = CommonAttributeDataComparer.InstanceIgnoringNamedArgumentOrder;
                var counts = new Dictionary<CSharpAttributeData, int>(comparer);

                foreach (var attribute in attributes1)
                {
                    if (attribute.IsConditionallyOmitted)
                    {
                        continue;
                    }

                    counts[attribute] = counts.TryGetValue(attribute, out var foundCount) ? foundCount + 1 : 1;
                }

                foreach (var attribute in attributes2)
                {
                    if (attribute.IsConditionallyOmitted)
                    {
                        continue;
                    }

                    if (!counts.TryGetValue(attribute, out var foundCount) || foundCount == 0)
                    {
                        return false;
                    }

                    counts[attribute] = foundCount - 1;
                }

                return counts.Values.All(c => c == 0);
            }

            static bool haveSameConstraints(TypeParameterSymbol typeParameter1, TypeMap? typeMap1, TypeParameterSymbol typeParameter2, TypeMap? typeMap2)
            {
                if ((typeParameter1.HasConstructorConstraint != typeParameter2.HasConstructorConstraint) ||
                    (typeParameter1.HasReferenceTypeConstraint != typeParameter2.HasReferenceTypeConstraint) ||
                    (typeParameter1.HasValueTypeConstraint != typeParameter2.HasValueTypeConstraint) ||
                    (typeParameter1.AllowsRefLikeType != typeParameter2.AllowsRefLikeType) ||
                    (typeParameter1.HasUnmanagedTypeConstraint != typeParameter2.HasUnmanagedTypeConstraint) ||
                    (typeParameter1.Variance != typeParameter2.Variance) ||
                    (typeParameter1.HasNotNullConstraint != typeParameter2.HasNotNullConstraint))
                {
                    return false;
                }

                return haveSameTypeConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2);
            }

            static bool haveSameTypeConstraints(TypeParameterSymbol typeParameter1, TypeMap? typeMap1, TypeParameterSymbol typeParameter2, TypeMap? typeMap2)
            {
                // Since the purpose is to ensure that we can safely round-trip metadata
                // and since top-level nullability is encoded per type constraint
                // we need to check nullability (including top-level nullability) per type constraint.

                ImmutableArray<TypeWithAnnotations> constraintTypes1 = typeParameter1.ConstraintTypesNoUseSiteDiagnostics;
                ImmutableArray<TypeWithAnnotations> constraintTypes2 = typeParameter2.ConstraintTypesNoUseSiteDiagnostics;

                if (constraintTypes1.IsEmpty && constraintTypes2.IsEmpty)
                {
                    return true;
                }

                var comparer = TypeWithAnnotations.EqualsComparer.ConsiderEverythingComparer;
                var substitutedTypes1 = new HashSet<TypeWithAnnotations>(comparer);
                var substitutedTypes2 = new HashSet<TypeWithAnnotations>(comparer);

                substituteConstraintTypes(constraintTypes1, typeMap1, substitutedTypes1);
                substituteConstraintTypes(constraintTypes2, typeMap2, substitutedTypes2);

                return areConstraintTypesSubset(substitutedTypes1, substitutedTypes2, typeParameter2) &&
                    areConstraintTypesSubset(substitutedTypes2, substitutedTypes1, typeParameter1);
            }

            static bool areConstraintTypesSubset(HashSet<TypeWithAnnotations> constraintTypes1, HashSet<TypeWithAnnotations> constraintTypes2, TypeParameterSymbol typeParameter2)
            {
                foreach (TypeWithAnnotations constraintType in constraintTypes1)
                {
                    if (!constraintTypes2.Contains(constraintType))
                    {
                        return false;
                    }
                }

                return true;
            }

            static void substituteConstraintTypes(ImmutableArray<TypeWithAnnotations> types, TypeMap? typeMap, HashSet<TypeWithAnnotations> result)
            {
                foreach (TypeWithAnnotations type in types)
                {
                    result.Add(MemberSignatureComparer.SubstituteType(typeMap, type));
                }
            }
        }

        /// <summary>
        /// Reports diagnostic when:
        /// two extension blocks grouped into a single grouping type have different IL-level signatures, or
        /// two extension blocks grouped into a single marker type have different C#-level signatures.
        /// </summary>
        internal void CheckSignatureCollisions(BindingDiagnosticBag diagnostics)
        {
            PooledHashSet<SourceNamedTypeSymbol>? alreadyReportedExtensions = null;

            foreach (ExtensionGroupingType groupingType in _groupingTypes)
            {
                checkCollisions(enumerateExtensionsInGrouping(groupingType), HaveSameILSignature, ref alreadyReportedExtensions, diagnostics);
            }

            foreach (ImmutableArray<SourceNamedTypeSymbol> mergedBlocks in EnumerateMergedExtensionBlocks())
            {
                checkCollisions(mergedBlocks, HaveSameCSharpSignature, ref alreadyReportedExtensions, diagnostics);
            }

            alreadyReportedExtensions?.Free();
            return;

            static IEnumerable<SourceNamedTypeSymbol> enumerateExtensionsInGrouping(ExtensionGroupingType groupingType)
            {
                foreach (var marker in groupingType.ExtensionMarkerTypes)
                {
                    foreach (var extension in marker.UnderlyingExtensions)
                    {
                        yield return extension;
                    }
                }
            }

            static void checkCollisions(IEnumerable<SourceNamedTypeSymbol> extensions, Func<SourceNamedTypeSymbol, SourceNamedTypeSymbol, bool> compare,
                ref PooledHashSet<SourceNamedTypeSymbol>? alreadyReportedExtensions, BindingDiagnosticBag diagnostics)
            {
                SourceNamedTypeSymbol? first = null;

                foreach (SourceNamedTypeSymbol extension in extensions)
                {
                    Debug.Assert(extension.IsExtension);
                    Debug.Assert(extension.IsDefinition);

                    if (first is null)
                    {
                        first = extension;
                        continue;
                    }

                    if (!compare(first, extension))
                    {
                        alreadyReportedExtensions ??= PooledHashSet<SourceNamedTypeSymbol>.GetInstance();
                        if (alreadyReportedExtensions.Add(extension))
                        {
                            diagnostics.Add(ErrorCode.ERR_ExtensionBlockCollision, extension.GetFirstLocation());
                        }
                    }
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

            bool INamedTypeReference.MangleName => MangleName;

            protected abstract bool MangleName { get; }

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
            private ImmutableArray<ExtensionGroupingTypeTypeParameter> _lazyTypeParameters;

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
            }

            int IComparable<ExtensionGroupingType>.CompareTo(ExtensionGroupingType? other)
            {
                Debug.Assert(other is { });
                return ExtensionMarkerTypes[0].CompareTo(other.ExtensionMarkerTypes[0]);
            }

            protected override IEnumerable<IGenericTypeParameter> GenericParameters
            {
                get
                {
                    if (_lazyTypeParameters.IsDefault)
                    {
                        var typeParameters = ExtensionMarkerTypes[0].UnderlyingExtensions[0].Arity != 0 ?
                            ((INestedTypeDefinition)ExtensionMarkerTypes[0].UnderlyingExtensions[0].GetCciAdapter()).GenericParameters.SelectAsArray(static (p, @this) => new ExtensionGroupingTypeTypeParameter(@this, p), this) :
                            [];
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters, typeParameters);
                    }

                    return _lazyTypeParameters;
                }
            }

            protected override ushort GenericParameterCount => (ushort)ExtensionMarkerTypes[0].UnderlyingExtensions[0].Arity;

            protected override bool IsAbstract => false;

            protected override bool IsSealed => true;

            protected override ITypeDefinition ContainingTypeDefinition
            {
                get
                {
                    NamedTypeSymbol? containingType = ExtensionMarkerTypes[0].UnderlyingExtensions[0].ContainingType;
                    Debug.Assert(containingType is not null);
                    return containingType.GetCciAdapter();
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

            protected override bool MangleName => GenericParameterCount != 0;

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

                if (synthesizeDefaultMemberAttributeIfNeeded(ExtensionMarkerTypes) is { } defaultMemberAttribute)
                {
                    yield return defaultMemberAttribute;
                }

                static SynthesizedAttributeData? synthesizeDefaultMemberAttributeIfNeeded(ImmutableArray<ExtensionMarkerType> extensionMarkerTypes)
                {
                    PropertySymbol? firstIndexer = tryGetFirstIndexer(extensionMarkerTypes);

                    if (firstIndexer is not null)
                    {
                        var compilation = firstIndexer.DeclaringCompilation;
                        var defaultMemberNameConstant = new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, firstIndexer.MetadataName);
                        return compilation.TrySynthesizeAttribute(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor, [defaultMemberNameConstant]);
                    }

                    return null;
                }

                static PropertySymbol? tryGetFirstIndexer(ImmutableArray<ExtensionMarkerType> extensionMarkerTypes)
                {
                    foreach (var markerType in extensionMarkerTypes)
                    {
                        foreach (var extension in markerType.UnderlyingExtensions)
                        {
                            foreach (var member in extension.GetMembers())
                            {
                                if (member is PropertySymbol { IsIndexer: true } property)
                                {
                                    return property;
                                }
                            }
                        }
                    }

                    return null;
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
            private ImmutableArray<InheritedTypeParameter> _lazyTypeParameters;

            public ExtensionMarkerType(ExtensionGroupingType groupingType, string name, MultiDictionary<string, SourceNamedTypeSymbol>.ValueSet extensions)
            {
                GroupingType = groupingType;
                _name = name;

                var builder = ArrayBuilder<SourceNamedTypeSymbol>.GetInstance(extensions.Count);
                builder.AddRange(extensions);
                builder.Sort(LexicalOrderSymbolComparer.Instance);
                UnderlyingExtensions = builder.ToImmutableAndFree();
            }

            public int CompareTo(ExtensionMarkerType? other)
            {
                Debug.Assert(other is { });
                return LexicalOrderSymbolComparer.Instance.Compare(UnderlyingExtensions[0], other.UnderlyingExtensions[0]);
            }

            protected override IEnumerable<IGenericTypeParameter> GenericParameters
            {
                get
                {
                    if (_lazyTypeParameters.IsDefault)
                    {
                        var typeParameters = UnderlyingExtensions[0].Arity != 0 ?
                            ((INestedTypeDefinition)UnderlyingExtensions[0].GetCciAdapter()).GenericParameters.SelectAsArray(static (p, @this) => new InheritedTypeParameter(p.Index, @this, p), this) :
                            [];
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters, typeParameters);
                    }

                    return _lazyTypeParameters;
                }
            }

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

            protected override bool MangleName => false;

            protected override IEnumerable<IPropertyDefinition> GetProperties(EmitContext context) => SpecializedCollections.EmptyEnumerable<IPropertyDefinition>();

            protected override IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
            }
        }
    }
}
