// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a source parameter cloned from another <see cref="SourceParameterSymbol"/>, when they must share attribute data and default constant value.
    /// For example, parameters on a property symbol are cloned to generate parameters on accessors.
    /// Similarly parameters on delegate invoke method are cloned to delegate begin/end invoke methods.
    /// </summary>
    internal sealed class SourceClonedParameterSymbol : SourceParameterSymbolBase
    {
        // if true suppresses params-array and default value:
        private readonly bool suppressOptional;

        private readonly SourceParameterSymbol originalParam;

        internal SourceClonedParameterSymbol(SourceParameterSymbol originalParam, Symbol newOwner, int newOrdinal, bool suppressOptional)
            : base(newOwner, newOrdinal)
        {
            Debug.Assert((object)originalParam != null);

            this.suppressOptional = suppressOptional;
            this.originalParam = originalParam;
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                // Since you can't get from the syntax node that represents the orginal parameter 
                // back to this symbol we decided not to return the original syntax node here.
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool IsParams
        {
            get { return !suppressOptional && originalParam.IsParams; }
        }

        internal override bool IsMetadataOptional
        {
            get 
            {
                // pseudo-custom attributes are not suppressed:
                return suppressOptional ? originalParam.HasOptionalAttribute : originalParam.IsMetadataOptional; 
            }
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get 
            {
                // pseudo-custom attributes are not suppressed:
                return suppressOptional ? originalParam.DefaultValueFromAttributes : originalParam.ExplicitDefaultConstantValue;
            }
        }

        internal override ConstantValue DefaultValueFromAttributes
        {
            get { return originalParam.DefaultValueFromAttributes; }
        }

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, bool hasByRefBeforeCustomModifiers, bool newIsParams)
        {
            return new SourceClonedParameterSymbol(
                originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, hasByRefBeforeCustomModifiers, newIsParams),
                this.ContainingSymbol,
                this.Ordinal,
                this.suppressOptional);
        }

        #region Forwarded

        public override TypeSymbol Type
        {
            get { return originalParam.Type; }
        }

        public override RefKind RefKind
        {
            get { return originalParam.RefKind; }
        }

        internal override bool IsMetadataIn
        {
            get { return originalParam.IsMetadataIn; }
        }

        internal override bool IsMetadataOut
        {
            get { return originalParam.IsMetadataOut; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return originalParam.Locations; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return originalParam.GetAttributes();
        }

        public sealed override string Name
        {
            get { return originalParam.Name; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return originalParam.CustomModifiers; }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return originalParam.MarshallingInformation; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return originalParam.IsIDispatchConstant; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return originalParam.IsIUnknownConstant; }
        }

        internal override bool IsCallerFilePath
        {
            get { return originalParam.IsCallerFilePath; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return originalParam.IsCallerLineNumber; }
        }

        internal override bool IsCallerMemberName
        {
            get { return originalParam.IsCallerMemberName; }
        }

        #endregion
    }
}