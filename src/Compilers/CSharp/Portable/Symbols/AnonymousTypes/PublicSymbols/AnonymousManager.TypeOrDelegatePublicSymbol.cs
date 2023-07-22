// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal abstract class AnonymousTypeOrDelegatePublicSymbol : NamedTypeSymbol
        {
            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            /// <summary> Anonymous type descriptor </summary>
            internal readonly AnonymousTypeDescriptor TypeDescriptor;

            internal AnonymousTypeOrDelegatePublicSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr)
            {
                typeDescr.AssertIsGood();

                this.Manager = manager;
                this.TypeDescriptor = typeDescr;
            }

            internal abstract NamedTypeSymbol MapToImplementationSymbol();

            internal abstract AnonymousTypeOrDelegatePublicSymbol SubstituteTypes(AbstractTypeMap typeMap);

            protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
                => throw ExceptionUtilities.Unreachable();

            internal sealed override IEnumerable<FieldSymbol> GetFieldsToEmit()
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal sealed override bool HasCodeAnalysisEmbeddedAttribute => false;

            internal sealed override bool IsInterpolatedStringHandlerType => false;

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
                get { return string.Empty; }
            }

            public sealed override string MetadataName
            {
                get { return string.Empty; }
            }

            internal sealed override bool MangleName
            {
                get { return false; }
            }

            internal sealed override bool IsFileLocal => false;
            internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

            public sealed override int Arity
            {
                get { return 0; }
            }

            public abstract override bool IsImplicitlyDeclared
            {
                get;
            }

            public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return ImmutableArray<TypeParameterSymbol>.Empty; }
            }

            internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get { return ImmutableArray<TypeWithAnnotations>.Empty; }
            }

            public sealed override bool IsAbstract
            {
                get { return false; }
            }

            public sealed override bool IsRefLikeType
            {
                get { return false; }
            }

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

            internal sealed override bool HasSpecialName
            {
                get { return false; }
            }

            internal override bool HasDeclaredRequiredMembers => false;

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

            internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
            {
                throw ExceptionUtilities.Unreachable();
            }

            internal abstract override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics { get; }

            public abstract override TypeKind TypeKind { get; }

            internal sealed override bool IsInterface
            {
                get { return false; }
            }

            public sealed override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray.Create<Location>(this.TypeDescriptor.Location); }
            }

            public abstract override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get;
            }

            public sealed override bool IsStatic
            {
                get { return false; }
            }

            public sealed override bool IsAnonymousType
            {
                get { return true; }
            }

            public sealed override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
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

            public sealed override bool AreLocalsZeroed
            {
                get { throw ExceptionUtilities.Unreachable(); }
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

            internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
            {
                return BaseTypeNoUseSiteDiagnostics;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

            internal sealed override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

            internal sealed override bool IsRecord => false;
            internal sealed override bool IsRecordStruct => false;

            internal abstract override bool Equals(TypeSymbol t2, TypeCompareKind comparison);

            public abstract override int GetHashCode();

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
        }
    }
}
