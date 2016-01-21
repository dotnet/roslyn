// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a field in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another FieldSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingFieldSymbol : FieldSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying FieldSymbol, cannot be another RetargetingFieldSymbol.
        /// </summary>
        private readonly FieldSymbol _underlyingField;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingFieldSymbol(RetargetingModuleSymbol retargetingModule, FieldSymbol underlyingField)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingField != null);
            Debug.Assert(!(underlyingField is RetargetingFieldSymbol));

            _retargetingModule = retargetingModule;
            _underlyingField = underlyingField;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public FieldSymbol UnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingField.IsImplicitlyDeclared; }
        }

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.RetargetingTranslator.Retarget(_underlyingField.GetFieldType(fieldsBeingBound), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingField.ContainingSymbol);
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingField.DeclaredAccessibility;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingField.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingField.GetCustomAttributesToEmit(compilationState));
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
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
                return this.RetargetingTranslator.Retarget(_underlyingField.MarshallingInformation);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return _underlyingField.MarshallingDescriptor;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _underlyingField.TypeLayoutOffset;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                var associated = _underlyingField.AssociatedSymbol;
                return (object)associated == null ? null : this.RetargetingTranslator.Retarget(associated);
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

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool NullableOptOut
        {
            get
            {
                return _underlyingField.NullableOptOut;
            }
        }
    }
}
