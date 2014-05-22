// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Intended to be used to create ParameterSymbols for a SignatureOnlyMethodSymbol.
    /// </summary>
    internal sealed class SignatureOnlyParameterSymbol : ParameterSymbol
    {
        private readonly TypeSymbol type;
        private readonly ImmutableArray<CustomModifier> customModifiers;
        private readonly bool isParams;
        private readonly RefKind refKind;

        public SignatureOnlyParameterSymbol(
            TypeSymbol type,
            ImmutableArray<CustomModifier> customModifiers,
            bool isParams,
            RefKind refKind)
        {
            this.type = type;
            this.customModifiers = customModifiers;
            this.isParams = isParams;
            this.refKind = refKind;
        }

        public override TypeSymbol Type { get { return type; } }

        public override ImmutableArray<CustomModifier> CustomModifiers { get { return customModifiers; } }

        public override bool IsParams { get { return isParams; } }

        public override RefKind RefKind { get { return refKind; } }

        public override string Name { get { return ""; } }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        #region Not used by MethodSignatureComparer

        internal override bool IsMetadataIn { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsMetadataOut { get { throw ExceptionUtilities.Unreachable; } }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation { get { throw ExceptionUtilities.Unreachable; } }

        public override int Ordinal { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsMetadataOptional { get { throw ExceptionUtilities.Unreachable; } }

        internal override ConstantValue ExplicitDefaultConstantValue { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsIDispatchConstant { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsIUnknownConstant { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsCallerFilePath { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsCallerLineNumber { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool IsCallerMemberName { get { throw ExceptionUtilities.Unreachable; } }

        internal sealed override bool HasByRefBeforeCustomModifiers { get { throw ExceptionUtilities.Unreachable; } }

        public override Symbol ContainingSymbol { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<Location> Locations { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { throw ExceptionUtilities.Unreachable; } }

        public override AssemblySymbol ContainingAssembly { get { throw ExceptionUtilities.Unreachable; } }

        internal override ModuleSymbol ContainingModule { get { throw ExceptionUtilities.Unreachable; } }

        #endregion Not used by MethodSignatureComparer

    }
}