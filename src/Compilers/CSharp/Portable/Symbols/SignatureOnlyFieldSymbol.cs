// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A representation of a field symbol that is intended only to be used for comparison purposes
    /// (esp in MemberSignatureComparer).
    /// </summary>
    internal sealed class SignatureOnlyFieldSymbol : FieldSymbol
    {
        private readonly string _name;
        private readonly TypeSymbol _containingType;
        private readonly TypeWithAnnotations _type;

        public SignatureOnlyFieldSymbol(
            string name,
            TypeSymbol containingType,
            TypeWithAnnotations type)
        {
            _type = type;
            _containingType = containingType;
            _name = name;
        }

        public override string Name => _name;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound) => _type;

        public override Symbol ContainingSymbol => _containingType;

        #region Not used by MemberSignatureComparer
        public override bool IsReadOnly => throw ExceptionUtilities.Unreachable;

        public override bool IsStatic => throw ExceptionUtilities.Unreachable;

        internal override bool HasSpecialName => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable;

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable;

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw ExceptionUtilities.Unreachable;

        public override AssemblySymbol ContainingAssembly => throw ExceptionUtilities.Unreachable;

        internal override ModuleSymbol ContainingModule => throw ExceptionUtilities.Unreachable;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => throw ExceptionUtilities.Unreachable;

        public override Symbol AssociatedSymbol => throw ExceptionUtilities.Unreachable;

        public override bool IsVolatile => throw ExceptionUtilities.Unreachable;

        public override bool IsConst => throw ExceptionUtilities.Unreachable;

        internal override bool HasRuntimeSpecialName => throw ExceptionUtilities.Unreachable;

        internal override bool IsNotSerialized => throw ExceptionUtilities.Unreachable;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => throw ExceptionUtilities.Unreachable;

        internal override int? TypeLayoutOffset => throw ExceptionUtilities.Unreachable;

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes) => throw new System.NotImplementedException();

        #endregion Not used by MemberSignatureComparer
    }
}
