﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field in a class, struct or enum
    /// </summary>
    internal abstract partial class FieldSymbol : Symbol, IFieldSymbolInternal
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

        protected sealed override Symbol OriginalSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// Gets the type of this field along with its annotations.
        /// </summary>
        public TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                return GetFieldType(ConsList<FieldSymbol>.Empty);
            }
        }

        public abstract FlowAnalysisAnnotations FlowAnalysisAnnotations { get; }

        /// <summary>
        /// Gets the type of this field.
        /// </summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        internal abstract TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

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
        /// Returns true if this symbol requires an instance reference as the implicit receiver. This is false if the symbol is static.
        /// </summary>
        public virtual bool RequiresInstanceReceiver => !IsStatic;

        /// <summary>
        /// Returns true if this field was declared as "fixed".
        /// Note that for a fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual bool IsFixedSizeBuffer { get { return false; } }

        /// <summary>
        /// If IsFixedSizeBuffer is true, the value between brackets in the fixed-size-buffer declaration.
        /// If IsFixedSizeBuffer is false FixedSize is 0.
        /// Note that for fixed-a size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual int FixedSize { get { return 0; } }

        /// <summary>
        /// If this.IsFixedSizeBuffer is true, returns the underlying implementation type for the
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
        /// By default we defer to this.Type.IsPointerOrFunctionPointer() 
        /// However in some cases this may cause circular dependency via binding a
        /// pointer that points to the type that contains the current field.
        /// Fortunately in those cases we do not need to force binding of the field's type 
        /// and can just check the declaration syntax if the field type is not yet known.
        /// </remarks>
        internal virtual bool HasPointerType
        {
            get
            {
                return this.Type.IsPointerOrFunctionPointer();
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

        internal virtual FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedFieldSymbol(newOwner as SubstitutedNamedTypeSymbol, this);
        }

        #region Use-Site Diagnostics

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (this.IsDefinition)
            {
                return new UseSiteInfo<AssemblySymbol>(PrimaryDependency);
            }

            return this.OriginalDefinition.GetUseSiteInfo();
        }

        internal bool CalculateUseSiteDiagnostic(ref UseSiteInfo<AssemblySymbol> result)
        {
            Debug.Assert(IsDefinition);

            // Check type, custom modifiers
            if (DeriveUseSiteInfoFromType(ref result, this.TypeWithAnnotations, AllowedRequiredModifierType.System_Runtime_CompilerServices_Volatile))
            {
                return true;
            }

            // If the member is in an assembly with unified references, 
            // we check if its definition depends on a type from a unified reference.
            if (this.ContainingModule.HasUnifiedReferences)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                DiagnosticInfo diagnosticInfo = result.DiagnosticInfo;
                if (this.TypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this, ref unificationCheckedTypes))
                {
                    result = result.AdjustDiagnosticInfo(diagnosticInfo);
                    return true;
                }

                result = result.AdjustDiagnosticInfo(diagnosticInfo);
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
                DiagnosticInfo info = GetUseSiteInfo().DiagnosticInfo;
                return (object)info != null && info.Code == (int)ErrorCode.ERR_BindToBogus;
            }
        }

        #endregion

        /// <summary>
        /// Returns True when field symbol is not mapped directly to a field in the underlying tuple struct.
        /// </summary>
        public virtual bool IsVirtualTupleField
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this is a field representing a Default element like Item1, Item2...
        /// </summary>
        public virtual bool IsDefaultTupleElement
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// If this is a field of a tuple type, return corresponding underlying field from the
        /// tuple underlying type. Otherwise, null. In case of a malformed underlying type
        /// the corresponding underlying field might be missing, return null in this case too.
        /// </summary>
        public virtual FieldSymbol TupleUnderlyingField
        {
            get
            {
                return ContainingType.IsTupleType ? this : null;
            }
        }

        /// <summary>
        /// If this field represents a tuple element, returns a corresponding default element field.
        /// Otherwise returns null.
        /// </summary>
        public virtual FieldSymbol CorrespondingTupleField
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if a given field is a tuple element
        /// </summary>
        internal bool IsTupleElement()
        {
            return this.CorrespondingTupleField is object;
        }

        /// <summary>
        /// If this is a field representing a tuple element,
        /// returns the index of the element (zero-based).
        /// Otherwise returns -1
        /// </summary>
        public virtual int TupleElementIndex
        {
            get
            {
                return -1;
            }
        }

        bool IFieldSymbolInternal.IsVolatile => this.IsVolatile;

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.FieldSymbol(this);
        }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (other is SubstitutedFieldSymbol sfs)
            {
                return sfs.Equals(this, compareKind);
            }

            return base.Equals(other, compareKind);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
