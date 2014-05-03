// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying FieldSymbol, cannot be another RetargetingFieldSymbol.
        /// </summary>
        private readonly FieldSymbol underlyingField;

        private ImmutableArray<CustomModifier> lazyCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingFieldSymbol(RetargetingModuleSymbol retargetingModule, FieldSymbol underlyingField)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingField != null);
            Debug.Assert(!(underlyingField is RetargetingFieldSymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingField = underlyingField;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public FieldSymbol UnderlyingField
        {
            get
            {
                return this.underlyingField;
            }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingField.IsImplicitlyDeclared; }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingField.GetFieldType(fieldsBeingBound), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return this.RetargetingTranslator.RetargetModifiers(this.underlyingField.CustomModifiers, ref this.lazyCustomModifiers);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingField.ContainingSymbol);
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.underlyingField.DeclaredAccessibility;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingField.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(this.underlyingField.GetCustomAttributesToEmit(compilationState));
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return this.underlyingField.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return this.underlyingField.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return this.underlyingField.HasRuntimeSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return this.underlyingField.IsNotSerialized;
            }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return this.underlyingField.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingField.MarshallingInformation);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return this.underlyingField.MarshallingDescriptor;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return this.underlyingField.TypeLayoutOffset;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                var associated = this.underlyingField.AssociatedSymbol;
                return (object)associated == null ? null : this.RetargetingTranslator.Retarget(associated);
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return this.underlyingField.IsReadOnly;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                return this.underlyingField.IsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return this.underlyingField.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return underlyingField.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return this.underlyingField.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return this.underlyingField.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingField.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.underlyingField.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return this.underlyingField.IsStatic;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}