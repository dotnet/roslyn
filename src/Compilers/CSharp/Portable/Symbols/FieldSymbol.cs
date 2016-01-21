// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field in a class, struct or enum
    /// </summary>
    internal abstract partial class FieldSymbol : Symbol, IFieldSymbol
    {
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        internal FieldSymbol()
        {
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual FieldSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        protected override sealed Symbol OriginalSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// Gets the type of this field.
        /// </summary>
        public TypeSymbolWithAnnotations Type
        {
            get
            {
                return GetFieldType(ConsList<FieldSymbol>.Empty);
            }
        }

        internal abstract TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        /// <summary>
        /// If this field serves as a backing variable for an automatically generated
        /// property or a field-like event, returns that 
        /// property/event. Otherwise returns null.
        /// Note, the set of possible associated symbols might be expanded in the future to 
        /// reflect changes in the languages.
        /// </summary>
        public abstract Symbol AssociatedSymbol { get; }

        /// <summary>
        /// Returns true if this field was declared as "readonly". 
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Returns true if this field was declared as "volatile". 
        /// </summary>
        public abstract bool IsVolatile { get; }

        /// <summary>
        /// Returns true if this field was declared as "fixed".
        /// Note that for a fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual bool IsFixed { get { return false; } }

        /// <summary>
        /// If IsFixed is true, the value between brackets in the fixed-size-buffer declaration.
        /// If IsFixed is false FixedSize is 0.
        /// Note that for fixed-a size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual int FixedSize { get { return 0; } }

        /// <summary>
        /// If this.IsFixed is true, returns the underlying implementation type for the
        /// fixed-size buffer when emitted.  Otherwise returns null.
        /// </summary>
        internal virtual NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            return null;
        }

        /// <summary>
        /// Returns true when field is a backing field for a captured frame pointer (typically "this").
        /// </summary>
        internal virtual bool IsCapturedFrame { get { return false; } }

        /// <summary>
        /// Returns true if this field was declared as "const" (i.e. is a constant declaration).
        /// Also returns true for an enum member.
        /// </summary>
        public abstract bool IsConst { get; }

        // Gets a value indicating whether this instance is metadata constant. A constant field is considered to be 
        // metadata constant unless they are of type decimal, because decimals are not regarded as constant by the CLR.
        public bool IsMetadataConstant
        {
            get { return this.IsConst && (this.Type.SpecialType != SpecialType.System_Decimal); }
        }

        /// <summary>
        /// Returns false if the field wasn't declared as "const", or constant value was omitted or erroneous.
        /// True otherwise.
        /// </summary>
        public virtual bool HasConstantValue
        {
            get
            {
                if (!IsConst)
                {
                    return false;
                }

                ConstantValue constantValue = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                return constantValue != null && !constantValue.IsBad; //can be null in error scenarios
            }
        }

        /// <summary>
        /// If IsConst returns true, then returns the constant value of the field or enum member. If IsConst returns
        /// false, then returns null.
        /// </summary>
        public virtual object ConstantValue
        {
            get
            {
                if (!IsConst)
                {
                    return null;
                }

                ConstantValue constantValue = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                return constantValue == null ? null : constantValue.Value; //can be null in error scenarios
            }
        }

        internal abstract ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes);

        internal override bool NullableOptOut
        {
            get
            {
                Debug.Assert(IsDefinition);

                var associatedSymbol = AssociatedSymbol;
                if ((object)associatedSymbol != null)
                {
                    switch (associatedSymbol.Kind)
                    {
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            return associatedSymbol.NullableOptOut;
                    }

                }

                return ContainingType?.NullableOptOut == true;
            }
        }

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Field;
            }
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitField(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitField(this);
        }

        /// <summary>
        /// Returns false because field can't be abstract.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be defined externally.
        /// </summary>
        public sealed override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be overridden.
        /// </summary>
        public sealed override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be sealed.
        /// </summary>
        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be virtual.
        /// </summary>
        public sealed override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// True if this symbol has a special name (metadata flag SpecialName is set).
        /// </summary>
        internal abstract bool HasSpecialName { get; }

        /// <summary>
        /// True if this symbol has a runtime-special name (metadata flag RuntimeSpecialName is set).
        /// </summary>
        internal abstract bool HasRuntimeSpecialName { get; }

        /// <summary>
        /// True if this field is not serialized (metadata flag NotSerialized is set).
        /// </summary>
        internal abstract bool IsNotSerialized { get; }

        /// <summary>
        /// True if this field has a pointer type.
        /// </summary>
        /// <remarks>
        /// By default we defer to this.Type.IsPointerType() 
        /// However in some cases this may cause circular dependency via binding a
        /// pointer that points to the type that contains the current field.
        /// Fortunately in those cases we do not need to force binding of the field's type 
        /// and can just check the declaration syntax if the field type is not yet known.
        /// </remarks>
        internal virtual bool HasPointerType
        {
            get
            {
                return this.Type.IsPointerType();
            }
        }

        /// <summary>
        /// Describes how the field is marshalled when passed to native code.
        /// Null if no specific marshalling information is available for the field.
        /// </summary>
        /// <remarks>PE symbols don't provide this information and always return null.</remarks>
        internal abstract MarshalPseudoCustomAttributeData MarshallingInformation { get; }

        /// <summary>
        /// Returns the marshalling type of this field, or 0 if marshalling information isn't available.
        /// </summary>
        /// <remarks>
        /// By default this information is extracted from <see cref="MarshallingInformation"/> if available. 
        /// Since the compiler does only need to know the marshalling type of symbols that aren't emitted 
        /// PE symbols just decode the type from metadata and don't provide full marshalling information.
        /// </remarks>
        internal virtual UnmanagedType MarshallingType
        {
            get
            {
                var info = MarshallingInformation;
                return info != null ? info.UnmanagedType : 0;
            }
        }

        /// <summary>
        /// Offset assigned to the field when the containing type is laid out by the VM.
        /// Null if unspecified.
        /// </summary>
        internal abstract int? TypeLayoutOffset { get; }

        internal FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return (newOwner == this.ContainingSymbol) ? this : new SubstitutedFieldSymbol(newOwner as SubstitutedNamedTypeSymbol, this);
        }

        #region Use-Site Diagnostics

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (this.IsDefinition)
            {
                return base.GetUseSiteDiagnostic();
            }

            return this.OriginalDefinition.GetUseSiteDiagnostic();
        }

        internal bool CalculateUseSiteDiagnostic(ref DiagnosticInfo result)
        {
            Debug.Assert(IsDefinition);

            // Check type, custom modifiers
            if (DeriveUseSiteDiagnosticFromType(ref result, this.Type))
            {
                return true;
            }

            // If the member is in an assembly with unified references, 
            // we check if its definition depends on a type from a unified reference.
            if (this.ContainingModule.HasUnifiedReferences)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                if (this.Type.GetUnificationUseSiteDiagnosticRecursive(ref result, this, ref unificationCheckedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return error code that has highest priority while calculating use site error for this symbol. 
        /// </summary>
        protected override int HighestPriorityUseSiteError
        {
            get
            {
                return (int)ErrorCode.ERR_BindToBogus;
            }
        }

        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteDiagnostic();
                return (object)info != null && info.Code == (int)ErrorCode.ERR_BindToBogus;
            }
        }

        #endregion

        #region IFieldSymbol Members

        ISymbol IFieldSymbol.AssociatedSymbol
        {
            get
            {
                return this.AssociatedSymbol;
            }
        }

        ITypeSymbol IFieldSymbol.Type
        {
            get
            {
                return this.Type.TypeSymbol;
            }
        }

        ImmutableArray<CustomModifier> IFieldSymbol.CustomModifiers
        {
            get { return this.Type.CustomModifiers; }
        }

        IFieldSymbol IFieldSymbol.OriginalDefinition
        {
            get { return this.OriginalDefinition; }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitField(this);
        }

        #endregion
    }
}
