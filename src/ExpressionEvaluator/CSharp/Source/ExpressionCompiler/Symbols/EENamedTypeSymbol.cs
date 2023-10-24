// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EENamedTypeSymbol : NamedTypeSymbol
    {
        internal readonly NamedTypeSymbol SubstitutedSourceType;
        internal readonly ImmutableArray<TypeParameterSymbol> SourceTypeParameters;

        private readonly NamespaceSymbol _container;
        private readonly NamedTypeSymbol _baseType;
        private readonly string _name;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<MethodSymbol> _methods;

        internal EENamedTypeSymbol(
            NamespaceSymbol container,
            NamedTypeSymbol baseType,
            CSharpSyntaxNode syntax,
            MethodSymbol currentFrame,
            string typeName,
            string methodName,
            CompilationContext context,
            GenerateMethodBody generateMethodBody) :
            this(container, baseType, currentFrame, typeName, (m, t) => ImmutableArray.Create<MethodSymbol>(context.CreateMethod(t, methodName, syntax, generateMethodBody)))
        {
        }

        internal EENamedTypeSymbol(
            NamespaceSymbol container,
            NamedTypeSymbol baseType,
            MethodSymbol currentFrame,
            string typeName,
            Func<MethodSymbol, EENamedTypeSymbol, ImmutableArray<MethodSymbol>> getMethods,
            ImmutableArray<TypeParameterSymbol> sourceTypeParameters,
            Func<NamedTypeSymbol, EENamedTypeSymbol, ImmutableArray<TypeParameterSymbol>> getTypeParameters)
        {
            _container = container;
            _baseType = baseType;
            _name = typeName;
            this.SourceTypeParameters = sourceTypeParameters;
            _typeParameters = getTypeParameters(currentFrame.ContainingType, this);
            VerifyTypeParameters(this, _typeParameters);
            _methods = getMethods(currentFrame, this);
        }

        internal EENamedTypeSymbol(
            NamespaceSymbol container,
            NamedTypeSymbol baseType,
            MethodSymbol currentFrame,
            string typeName,
            Func<MethodSymbol, EENamedTypeSymbol, ImmutableArray<MethodSymbol>> getMethods)
        {
            _container = container;
            _baseType = baseType;
            _name = typeName;

            // What we want is to map all original type parameters to the corresponding new type parameters
            // (since the old ones have the wrong owners).  Unfortunately, we have a circular dependency:
            //   1) Each new type parameter requires the entire map in order to be able to construct its constraint list.
            //   2) The map cannot be constructed until all new type parameters exist.
            // Our solution is to pass each new type parameter a lazy reference to the type map.  We then 
            // initialize the map as soon as the new type parameters are available - and before they are 
            // handed out - so that there is never a period where they can require the type map and find
            // it uninitialized.

            var sourceType = currentFrame.ContainingType;
            this.SourceTypeParameters = sourceType.GetAllTypeParameters();

            TypeMap typeMap = null;
            var getTypeMap = new Func<TypeMap>(() => typeMap);
            _typeParameters = this.SourceTypeParameters.SelectAsArray(
                (tp, i, arg) => (TypeParameterSymbol)new EETypeParameterSymbol(this, tp, i, getTypeMap),
                (object)null);

            typeMap = new TypeMap(this.SourceTypeParameters, _typeParameters);

            VerifyTypeParameters(this, _typeParameters);

            this.SubstitutedSourceType = typeMap.SubstituteNamedType(sourceType);
            TypeParameterChecker.Check(this.SubstitutedSourceType, _typeParameters);

            _methods = getMethods(currentFrame, this);
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
            => throw ExceptionUtilities.Unreachable();

        internal ImmutableArray<MethodSymbol> Methods
        {
            get { return _methods; }
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            return SpecializedCollections.EmptyEnumerable<FieldSymbol>();
        }

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            return _methods;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override int Arity
        {
            get { return _typeParameters.Length; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        public override bool MightContainExtensionMethods
        {
            get { return false; }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override string Name
        {
            get { return _name; }
        }

        // No additional name mangling since CompileExpression
        // is providing an explicit type name.
        internal override bool MangleName
        {
            get { return false; }
        }

        internal override bool IsFileLocal => false;
        internal override FileIdentifier AssociatedFileIdentifier => null;

        public override IEnumerable<string> MemberNames
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool HasDeclaredRequiredMembers => false;

        public override ImmutableArray<Symbol> GetMembers()
        {
            return _methods.Cast<MethodSymbol, Symbol>();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            // Should not be requesting generated members
            // by name other than constructors.
            Debug.Assert((name == WellKnownMemberNames.InstanceConstructorName) || (name == WellKnownMemberNames.StaticConstructorName));
            return this.GetMembers().WhereAsArray((m, name) => m.Name == name, name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _baseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override bool IsComImport
        {
            get { return false; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        public override bool IsSerializable
        {
            get { return false; }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { return true; }
        }

        internal override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        internal override System.Runtime.InteropServices.CharSet MarshallingCharSet
        {
            get { return System.Runtime.InteropServices.CharSet.Ansi; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return _baseType; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get { return null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override bool IsAbstract
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

        public override bool IsSealed
        {
            get { return true; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal override bool IsInterface
        {
            get { return false; }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal override bool IsRecord => false;
        internal override bool IsRecordStruct => false;
        internal override bool HasPossibleWellKnownCloneMethod() => false;
        internal override bool IsInterpolatedStringHandlerType => false;

        [Conditional("DEBUG")]
        internal static void VerifyTypeParameters(Symbol container, ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            for (int i = 0; i < typeParameters.Length; i++)
            {
                var typeParameter = typeParameters[i];
                Debug.Assert((object)typeParameter.ContainingSymbol == (object)container);
                Debug.Assert(typeParameter.Ordinal == i);
            }
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

#nullable enable
        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }
#nullable disable
    }
}
