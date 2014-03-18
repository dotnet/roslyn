// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A representation of a property symbol that is intended only to be used for comparison purposes
    /// (esp in PropertySignatureComparer).
    /// </summary>
    internal sealed class SignatureOnlyPropertySymbol : PropertySymbol
    {
        private readonly string name;
        private readonly TypeSymbol containingType;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol type;
        private readonly ImmutableArray<CustomModifier> typeCustomModifiers;
        private readonly bool isStatic;
        private readonly ImmutableArray<PropertySymbol> explicitInterfaceImplementations;

        public SignatureOnlyPropertySymbol(
            string name,
            TypeSymbol containingType,
            ImmutableArray<ParameterSymbol> parameters,
            TypeSymbol type,
            ImmutableArray<CustomModifier> typeCustomModifiers,
            bool isStatic,
            ImmutableArray<PropertySymbol> explicitInterfaceImplementations)
        {
            this.type = type;
            this.typeCustomModifiers = typeCustomModifiers;
            this.isStatic = isStatic;
            this.parameters = parameters;
            this.explicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
            this.containingType = containingType;
            this.name = name;
        }

        public override TypeSymbol Type { get { return this.type; } }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers { get { return this.typeCustomModifiers; } }

        public override bool IsStatic { get { return this.isStatic; } }

        public override ImmutableArray<ParameterSymbol> Parameters { get { return parameters; } }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations { get { return explicitInterfaceImplementations; } }

        public override Symbol ContainingSymbol { get { return containingType; } }

        public override string Name { get { return name; } }

        #region Not used by PropertySignatureComparer

        internal override bool HasSpecialName { get { throw ExceptionUtilities.Unreachable; } }

        internal override Cci.CallingConvention CallingConvention { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<Location> Locations { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { throw ExceptionUtilities.Unreachable; } }

        public override Accessibility DeclaredAccessibility { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsVirtual { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsOverride { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsAbstract { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsSealed { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsExtern { get { throw ExceptionUtilities.Unreachable; } }

        internal override ObsoleteAttributeData ObsoleteAttributeData { get { throw ExceptionUtilities.Unreachable; } }

        public override AssemblySymbol ContainingAssembly { get { throw ExceptionUtilities.Unreachable; } }

        internal override ModuleSymbol ContainingModule { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool MustCallMethodsDirectly { get { throw ExceptionUtilities.Unreachable; } }

        public override MethodSymbol SetMethod { get { throw ExceptionUtilities.Unreachable; } }

        public override MethodSymbol GetMethod { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsIndexer { get { throw ExceptionUtilities.Unreachable; } }

        #endregion Not used by PropertySignatureComparer
    }
}
