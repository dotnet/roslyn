﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a parameter that is based on another parameter.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedParameterSymbol : ParameterSymbol
    {
        protected readonly ParameterSymbol _underlyingParameter;

        protected WrappedParameterSymbol(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this._underlyingParameter = underlyingParameter;
        }

        public ParameterSymbol UnderlyingParameter
        {
            get
            {
                return _underlyingParameter;
            }
        }

        #region Forwarded

        public override TypeSymbolWithAnnotations Type
        {
            get { return _underlyingParameter.Type; }
        }

        public sealed override RefKind RefKind
        {
            get { return _underlyingParameter.RefKind; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return _underlyingParameter.IsMetadataIn; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return _underlyingParameter.IsMetadataOut; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return _underlyingParameter.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return _underlyingParameter.DeclaringSyntaxReferences; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingParameter.GetAttributes();
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            _underlyingParameter.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }

        internal sealed override ConstantValue ExplicitDefaultConstantValue
        {
            get { return _underlyingParameter.ExplicitDefaultConstantValue; }
        }

        public override int Ordinal
        {
            get { return _underlyingParameter.Ordinal; }
        }

        public override bool IsParams
        {
            get { return _underlyingParameter.IsParams; }
        }

        internal override bool IsMetadataOptional
        {
            get { return _underlyingParameter.IsMetadataOptional; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingParameter.IsImplicitlyDeclared; }
        }

        public sealed override string Name
        {
            get { return _underlyingParameter.Name; }
        }

        public sealed override string MetadataName
        {
            get { return _underlyingParameter.MetadataName; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _underlyingParameter.RefCustomModifiers; }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return _underlyingParameter.MarshallingInformation; }
        }

        internal override UnmanagedType MarshallingType
        {
            get { return _underlyingParameter.MarshallingType; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return _underlyingParameter.IsIDispatchConstant; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return _underlyingParameter.IsIUnknownConstant; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return _underlyingParameter.IsCallerLineNumber; }
        }

        internal override bool IsCallerFilePath
        {
            get { return _underlyingParameter.IsCallerFilePath; }
        }

        internal override bool IsCallerMemberName
        {
            get { return _underlyingParameter.IsCallerMemberName; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            // https://github.com/dotnet/roslyn/issues/30073: Consider moving to leaf types
            get { return _underlyingParameter.FlowAnalysisAnnotations; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            return _underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        #endregion
    }
}
