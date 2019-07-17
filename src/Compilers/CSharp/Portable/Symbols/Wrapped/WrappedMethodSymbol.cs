// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal abstract class WrappedMethodSymbol : MethodSymbol
    {
        public WrappedMethodSymbol()
        {
        }

        public abstract MethodSymbol UnderlyingMethod
        {
            get;
        }

        public override bool IsVararg
        {
            get
            {
                return UnderlyingMethod.IsVararg;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return UnderlyingMethod.IsGenericMethod;
            }
        }

        public override int Arity
        {
            get
            {
                return UnderlyingMethod.Arity;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return UnderlyingMethod.RefKind;
            }
        }

        internal override int ParameterCount
        {
            get { return UnderlyingMethod.ParameterCount; }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return UnderlyingMethod.IsExtensionMethod;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return UnderlyingMethod.HidesBaseMethodsByName;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return UnderlyingMethod.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return UnderlyingMethod.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return UnderlyingMethod.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return UnderlyingMethod.IsStatic;
            }
        }

        public override bool RequiresInstanceReceiver
        {
            get
            {
                return UnderlyingMethod.RequiresInstanceReceiver;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return UnderlyingMethod.IsVirtual;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return UnderlyingMethod.IsAsync;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return UnderlyingMethod.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return UnderlyingMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return UnderlyingMethod.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return UnderlyingMethod.IsExtern;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return UnderlyingMethod.IsImplicitlyDeclared;
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return UnderlyingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return UnderlyingMethod.IsMetadataFinal;
            }
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return UnderlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return UnderlyingMethod.RequiresSecurityObject;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return UnderlyingMethod.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return UnderlyingMethod.ReturnValueMarshallingInformation;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return UnderlyingMethod.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return UnderlyingMethod.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return UnderlyingMethod.GetAppliedConditionalSymbols();
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return UnderlyingMethod.ObsoleteAttributeData;
            }
        }

        public override string Name
        {
            get
            {
                return UnderlyingMethod.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return UnderlyingMethod.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UnderlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return UnderlyingMethod.ImplementationAttributes;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return UnderlyingMethod.MethodKind;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return UnderlyingMethod.CallingConvention;
            }
        }

        internal override bool IsAccessCheckedOnOverride
        {
            get
            {
                return UnderlyingMethod.IsAccessCheckedOnOverride;
            }
        }

        internal override bool IsExternal
        {
            get
            {
                return UnderlyingMethod.IsExternal;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return UnderlyingMethod.HasRuntimeSpecialName;
            }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => UnderlyingMethod.ReturnTypeFlowAnalysisAnnotations;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => UnderlyingMethod.ReturnNotNullIfParameterNotNull;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => UnderlyingMethod.FlowAnalysisAnnotations;

        internal override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return UnderlyingMethod.ReturnValueIsMarshalledExplicitly;
            }
        }

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return UnderlyingMethod.ReturnValueMarshallingDescriptor;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return UnderlyingMethod.GenerateDebugInfo;
            }
        }

        internal override bool IsDeclaredReadOnly => UnderlyingMethod.IsDeclaredReadOnly;
    }
}
