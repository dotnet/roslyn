// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field that is based on another field.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedFieldSymbol : FieldSymbol
    {
        /// <summary>
        /// The underlying FieldSymbol.
        /// </summary>
        protected readonly FieldSymbol _underlyingField;

        public WrappedFieldSymbol(FieldSymbol underlyingField)
        {
            Debug.Assert((object)underlyingField != null);
            _underlyingField = underlyingField;
        }

        public FieldSymbol UnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingField.IsImplicitlyDeclared; }
        }

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return _underlyingField.FlowAnalysisAnnotations; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingField.DeclaredAccessibility;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingField.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingField.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingField.HasRuntimeSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return _underlyingField.IsNotSerialized;
            }
        }

        internal override bool HasPointerType => _underlyingField.HasPointerType;

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return _underlyingField.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return _underlyingField.MarshallingInformation;
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return _underlyingField.MarshallingDescriptor;
            }
        }

        public override bool IsFixedSizeBuffer
        {
            get
            {
                return _underlyingField.IsFixedSizeBuffer;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _underlyingField.TypeLayoutOffset;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _underlyingField.IsReadOnly;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                return _underlyingField.IsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return _underlyingField.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingField.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return _underlyingField.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return _underlyingField.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingField.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingField.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingField.IsStatic;
            }
        }

        internal sealed override bool IsRequired => _underlyingField.IsRequired;

        // If we need to un-seal this method, we should make it abstract.
        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
            => throw ExceptionUtilities.Unreachable();
    }
}
