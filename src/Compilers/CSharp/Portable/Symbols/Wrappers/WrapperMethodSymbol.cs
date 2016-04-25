// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a method that is based on another method.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrapperMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// The underlying MethodSymbol.
        /// </summary>
        protected readonly MethodSymbol _underlyingMethod;

        public WrapperMethodSymbol(MethodSymbol underlyingMethod)
        {
            Debug.Assert((object)underlyingMethod != null);
            _underlyingMethod = underlyingMethod;
        }

        public MethodSymbol UnderlyingMethod
        {
            get
            {
                return _underlyingMethod;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return _underlyingMethod.IsVararg;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return _underlyingMethod.IsGenericMethod;
            }
        }

        public override int Arity
        {
            get
            {
                return _underlyingMethod.Arity;
            }
        }

        public override abstract ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get;
        }

        public override abstract ImmutableArray<TypeSymbol> TypeArguments
        {
            get;
        }

        public override abstract bool ReturnsVoid
        {
            get;
        }

        internal override RefKind RefKind
        {
            get
            {
                return _underlyingMethod.RefKind;
            }
        }

        public override abstract TypeSymbol ReturnType
        {
            get;
        }

        public override abstract ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get;
        }

        internal override int ParameterCount
        {
            get { return _underlyingMethod.ParameterCount; }
        }

        public override abstract ImmutableArray<ParameterSymbol> Parameters
        {
            get;
        }

        public override abstract Symbol AssociatedSymbol
        {
            get;
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return _underlyingMethod.IsExtensionMethod;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return _underlyingMethod.HidesBaseMethodsByName;
            }
        }

        public override abstract Symbol ContainingSymbol
        {
            get;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingMethod.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingMethod.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingMethod.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingMethod.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _underlyingMethod.IsVirtual;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return _underlyingMethod.IsAsync;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return _underlyingMethod.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingMethod.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return _underlyingMethod.IsExtern;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _underlyingMethod.IsImplicitlyDeclared;
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return _underlyingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return _underlyingMethod.IsMetadataFinal;
            }
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return _underlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return _underlyingMethod.RequiresSecurityObject;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return _underlyingMethod.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return _underlyingMethod.ReturnValueMarshallingInformation;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return _underlyingMethod.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _underlyingMethod.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _underlyingMethod.GetAppliedConditionalSymbols();
        }

        public override abstract ImmutableArray<CSharpAttributeData> GetAttributes();

        // Get return type attributes
        public override abstract ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes();

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingMethod.ObsoleteAttributeData;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingMethod.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingMethod.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return _underlyingMethod.ImplementationAttributes;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return _underlyingMethod.MethodKind;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _underlyingMethod.CallingConvention;
            }
        }

        internal override abstract bool IsExplicitInterfaceImplementation
        {
            get;
        }

        public override abstract ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get;
        }

        internal override bool IsAccessCheckedOnOverride
        {
            get
            {
                return _underlyingMethod.IsAccessCheckedOnOverride;
            }
        }

        internal override bool IsExternal
        {
            get
            {
                return _underlyingMethod.IsExternal;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingMethod.HasRuntimeSpecialName;
            }
        }

        internal override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return _underlyingMethod.ReturnValueIsMarshalledExplicitly;
            }
        }

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return _underlyingMethod.ReturnValueMarshallingDescriptor;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return _underlyingMethod.GenerateDebugInfo;
            }
        }

        internal override abstract int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree);
    }
}
