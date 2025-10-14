// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal sealed class NameAndIndex
        {
            public NameAndIndex(string name, int index)
            {
                this.Name = name;
                this.Index = index;
            }

            public readonly string Name;
            public readonly int Index;
        }

        internal abstract class AnonymousTypeOrDelegateTemplateSymbol : NamedTypeSymbol
        {
            /// <summary> Name to be used as metadata name during emit </summary>
            private NameAndIndex? _nameAndIndex;

            /// <summary> Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT </summary>
            private Location _smallestLocation;

            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            internal AnonymousTypeOrDelegateTemplateSymbol(AnonymousTypeManager manager, Location location)
            {
                this.Manager = manager;
                _smallestLocation = location;

                // Will be set when the type's metadata is ready to be emitted, 
                // <anonymous-type>.Name will throw exception if requested
                // before that moment.
                _nameAndIndex = null;
            }

            internal abstract string TypeDescriptorKey { get; }

            protected sealed override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
                => throw ExceptionUtilities.Unreachable();

            /// <summary>
            /// Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT;
            /// 
            /// NOTE: if this property is queried, smallest location must not be null.
            /// </summary>
            internal Location SmallestLocation
            {
                get
                {
                    Debug.Assert(_smallestLocation != null);
                    return _smallestLocation;
                }
            }

            internal NameAndIndex? NameAndIndex
            {
                get
                {
                    return _nameAndIndex;
                }
                set
                {
                    Debug.Assert(value != null);
                    var oldValue = Interlocked.CompareExchange(ref _nameAndIndex, value, null);
                    Debug.Assert(oldValue == null ||
                        ((oldValue.Name == value.Name) && (oldValue.Index == value.Index)));
                }
            }

            /// <summary>
            /// In emit phase every time a created anonymous type is referenced we try to store the lowest 
            /// location of the template. It will be used for ordering templates and assigning emitted type names.
            /// </summary>
            internal void AdjustLocation(Location location)
            {
                Debug.Assert(location.IsInSource);

                while (true)
                {
                    // Loop until we managed to set location OR we detected that we don't need to set it 
                    // in case 'location' in type descriptor is bigger that the one in smallestLocation

                    Location currentSmallestLocation = _smallestLocation;
                    if (currentSmallestLocation != null && this.Manager.Compilation.CompareSourceLocations(currentSmallestLocation, location) < 0)
                    {
                        // The template's smallest location do not need to be changed
                        return;
                    }

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _smallestLocation, location, currentSmallestLocation), currentSmallestLocation))
                    {
                        // Changed successfully, proceed to updating the fields
                        return;
                    }
                }
            }

            internal override bool GetGuidString(out string? guidString)
            {
                guidString = null;
                return false;
            }

            internal sealed override bool HasCodeAnalysisEmbeddedAttribute => false;

            internal sealed override bool HasCompilerLoweringPreserveAttribute => false;

            internal sealed override bool IsInterpolatedStringHandlerType => false;

            internal sealed override ParameterSymbol? ExtensionParameter => null;

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
            {
                return this.GetMembersUnordered();
            }

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
            {
                return this.GetMembers(name);
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return this.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            public sealed override string Name
            {
                get { return _nameAndIndex!.Name; }
            }

            internal sealed override bool HasSpecialName
            {
                get { return false; }
            }

            public sealed override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public sealed override bool IsAbstract
            {
                get { return false; }
            }

            public sealed override bool IsRefLikeType
            {
                get { return false; }
            }

            internal sealed override string? ExtensionGroupingName => null;

            internal sealed override string? ExtensionMarkerName => null;

            public sealed override bool IsReadOnly
            {
                get { return false; }
            }

            public sealed override bool IsSealed
            {
                get { return true; }
            }

            public sealed override bool MightContainExtensionMethods
            {
                get { return false; }
            }

            public sealed override bool AreLocalsZeroed
            {
                get { return ContainingModule.AreLocalsZeroed; }
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Internal; }
            }

            internal sealed override bool IsInterface
            {
                get { return false; }
            }

            public sealed override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get { return ImmutableArray<SyntaxReference>.Empty; }
            }

            public sealed override bool IsStatic
            {
                get { return false; }
            }

            public sealed override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
            }

            internal abstract override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics { get; }

            internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
            {
                return this.Manager.System_Object;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override bool MangleName
            {
                get { return this.Arity > 0; }
            }

            internal sealed override bool IsFileLocal => false;
            internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

            internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get { return GetTypeParametersAsTypeArguments(); }
            }

            public sealed override int Arity
            {
                get { return TypeParameters.Length; }
            }

            internal sealed override bool ShouldAddWinRTMembers
            {
                get { return false; }
            }

            internal sealed override bool IsWindowsRuntimeImport
            {
                get { return false; }
            }

            internal sealed override bool IsComImport
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
            {
                get { return null; }
            }

            internal sealed override TypeLayout Layout
            {
                get { return default(TypeLayout); }
            }

            internal sealed override CharSet MarshallingCharSet
            {
                get { return DefaultMarshallingCharSet; }
            }

            public sealed override bool IsSerializable
            {
                get { return false; }
            }

            internal sealed override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal sealed override AttributeUsageInfo GetAttributeUsageInfo()
            {
                return AttributeUsageInfo.Null;
            }

            internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

            internal sealed override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

            internal sealed override bool IsRecord => false;

            internal sealed override bool IsRecordStruct => false;

            internal sealed override bool HasPossibleWellKnownCloneMethod() => false;

            internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
            {
                return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
            }

            internal sealed override bool HasInlineArrayAttribute(out int length)
            {
                length = 0;
                return false;
            }

            internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
            {
                builderType = null;
                methodName = null;
                return false;
            }

            internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
            {
                builderArgument = null;
                return false;
            }
        }
    }
}
