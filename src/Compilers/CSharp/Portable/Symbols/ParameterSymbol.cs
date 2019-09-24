// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a parameter of a method or indexer.
    /// </summary>
    internal abstract partial class ParameterSymbol : Symbol, IParameterSymbol
    {
        internal const string ValueParameterName = "value";

        internal ParameterSymbol()
        {
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual ParameterSymbol OriginalDefinition
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
        /// Gets the type of the parameter along with its annotations.
        /// </summary>
        public abstract TypeWithAnnotations TypeWithAnnotations { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        /// <summary>
        /// Determines if the parameter ref, out or neither.
        /// </summary>
        public abstract RefKind RefKind { get; }

        /// <summary>
        /// Returns true if the parameter is a discard parameter.
        /// </summary>
        public abstract bool IsDiscard { get; }

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// Describes how the parameter is marshalled when passed to native code.
        /// Null if no specific marshalling information is available for the parameter.
        /// </summary>
        /// <remarks>PE symbols don't provide this information and always return null.</remarks>
        internal abstract MarshalPseudoCustomAttributeData MarshallingInformation { get; }

        /// <summary>
        /// Returns the marshalling type of this parameter, or 0 if marshalling information isn't available.
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

        internal bool IsMarshalAsObject
        {
            get
            {
                switch (this.MarshallingType)
                {
                    case UnmanagedType.Interface:
                    case UnmanagedType.IUnknown:
                    case Cci.Constants.UnmanagedType_IDispatch:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the ordinal position of the parameter. The first parameter has ordinal zero.
        /// The "'this' parameter has ordinal -1.
        /// </summary>
        public abstract int Ordinal { get; }

        /// <summary>
        /// Returns true if the parameter was declared as a parameter array.
        /// Note: it is possible for any parameter to have the [ParamArray] attribute (for instance, in IL),
        ///     even if it is not the last parameter. So check for that.
        /// </summary>
        public abstract bool IsParams { get; }

        /// <summary>
        /// Returns true if the parameter is semantically optional.
        /// </summary>
        /// <remarks>
        /// True iff the parameter has a default argument syntax, 
        /// or the parameter is not a params-array and Optional metadata flag is set.
        /// </remarks>
        public bool IsOptional
        {
            get
            {
                // DEV10 COMPATIBILITY: Special handling for ParameterArray params
                //
                // Ideally we should not need the additional "isParams" check below
                // as a ParameterArray param cannot have a default value.
                // However, for certain cases of overriding this is not true.
                // See test "CodeGenTests.NoDefaultForParams_Dev10781558" for an example.
                // See Roslyn bug 10753 and Dev10 bug 781558 for details.
                //
                // To maintain compatibility with Dev10, we allow such code to compile but explicitly
                // classify a ParameterArray param as a required parameter.
                //
                // Also when we call f() where signature of f is void([Optional]params int[] args) 
                // an empty array is created and passed to f.
                //
                // We also do not consider ref/out parameters as optional, unless in COM interop scenarios 
                // and only for ref.
                RefKind refKind;
                return !IsParams && IsMetadataOptional &&
                       ((refKind = RefKind) == RefKind.None ||
                        (refKind == RefKind.In) ||
                        (refKind == RefKind.Ref && ContainingSymbol.ContainingType.IsComImport));
            }
        }

        /// <summary>
        /// True if Optional flag is set in metadata.
        /// </summary>
        internal abstract bool IsMetadataOptional { get; }

        /// <summary>
        /// True if In flag is set in metadata.
        /// </summary>
        internal abstract bool IsMetadataIn { get; }

        /// <summary>
        /// True if Out flag is set in metadata.
        /// </summary>
        internal abstract bool IsMetadataOut { get; }

        /// <summary>
        /// Returns true if the parameter explicitly specifies a default value to be passed
        /// when no value is provided as an argument to a call. 
        /// </summary>
        /// <remarks>
        /// True if the parameter has a default argument syntax, 
        /// or the parameter is from source and <see cref="DefaultParameterValueAttribute"/> is applied, 
        /// or the parameter is from metadata and HasDefault metadata flag is set. See
        /// <see cref="IsOptional"/> to determine if the parameter will be considered optional by
        /// overload resolution.
        /// 
        /// The default value can be obtained with <see cref="ExplicitDefaultValue"/> property.
        /// </remarks>
        public bool HasExplicitDefaultValue
        {
            get
            {
                // In the symbol model, only optional parameters have default values.
                // Internally, however, non-optional parameters may also have default
                // values (accessible via DefaultConstantValue).  For example, if the
                // DefaultParameterValue attribute is applied to a non-optional parameter
                // we still want to emit a default parameter value, even if it isn't
                // recognized by the language.
                // Special Case: params parameters are never optional, but can have
                // default values (e.g. if the params-ness is inherited from an
                // overridden method, but the current method declares the parameter
                // as optional).  In such cases, dev11 emits the default value.
                return IsOptional && ExplicitDefaultConstantValue != null;
            }
        }

        /// <summary>
        /// Returns the default value of the parameter. If <see cref="HasExplicitDefaultValue"/>
        /// returns false then DefaultValue throws an InvalidOperationException.
        /// </summary>
        /// <remarks>
        /// If the parameter type is a struct and the default value of the parameter
        /// is the default value of the struct type or of type parameter type which is 
        /// not known to be a referenced type, then this property will return null.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The parameter has no default value.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public object ExplicitDefaultValue
        {
            get
            {
                if (HasExplicitDefaultValue)
                {
                    return ExplicitDefaultConstantValue.Value;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Returns the default value constant of the parameter, 
        /// or null if the parameter doesn't have a default value or 
        /// the parameter type is a struct and the default value of the parameter
        /// is the default value of the struct type or of type parameter type which is 
        /// not known to be a referenced type.
        /// </summary>
        /// <remarks>
        /// This is used for emitting.  It does not reflect the language semantics
        /// (i.e. even non-optional parameters can have default values).
        /// </remarks>
        internal abstract ConstantValue ExplicitDefaultConstantValue { get; }

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Parameter;
            }
        }

        /// <summary>
        /// Implements visitor pattern. 
        /// </summary>
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameter(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitParameter(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitParameter(this);
        }

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared as requiring an override; i.e., declared with
        /// the "abstract" modifier. Also returns true on a type declared as "abstract", all
        /// interface types, and members of interface types.
        /// </summary>
        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member and was also
        /// sealed from further overriding; i.e., declared with the "sealed" modifier.  Also set for
        /// types that do not allow a derived class (declared with "sealed" or "static" or "struct"
        /// or "enum" or "delegate").
        /// </summary>
        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol is "virtual", has an implementation, and does not override a
        /// base class member; i.e., declared with the "virtual" modifier. Does not return true for
        /// members declared as abstract or override.
        /// </summary>
        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member; i.e., declared
        /// with the "override" modifier. Still returns true if member was declared to override
        /// something, but (erroneously) no member to override exists.
        /// </summary>
        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol is "static"; i.e., declared with the "static" modifier or
        /// implicitly static.
        /// </summary>
        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol has external implementation; i.e., declared with the 
        /// "extern" modifier. 
        /// </summary>
        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the parameter is the hidden 'this' parameter.
        /// </summary>
        public virtual bool IsThis
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal abstract bool IsIDispatchConstant { get; }

        internal abstract bool IsIUnknownConstant { get; }

        internal abstract bool IsCallerFilePath { get; }

        internal abstract bool IsCallerLineNumber { get; }

        internal abstract bool IsCallerMemberName { get; }

        internal abstract FlowAnalysisAnnotations FlowAnalysisAnnotations { get; }

        internal abstract ImmutableHashSet<string> NotNullIfParameterNotNull { get; }

        protected sealed override int HighestPriorityUseSiteError
        {
            get
            {
                return (int)ErrorCode.ERR_BogusType;
            }
        }

        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = null;
                DeriveUseSiteDiagnosticFromParameter(ref info, this);
                return (object)info != null && info.Code == (int)ErrorCode.ERR_BogusType;
            }
        }

        #region IParameterSymbol Members

        ITypeSymbol IParameterSymbol.Type
        {
            get { return this.Type; }
        }

        CodeAnalysis.NullableAnnotation IParameterSymbol.NullableAnnotation => TypeWithAnnotations.ToPublicAnnotation();

        ImmutableArray<CustomModifier> IParameterSymbol.CustomModifiers
        {
            get { return this.TypeWithAnnotations.CustomModifiers; }
        }

        ImmutableArray<CustomModifier> IParameterSymbol.RefCustomModifiers
        {
            get { return this.RefCustomModifiers; }
        }

        IParameterSymbol IParameterSymbol.OriginalDefinition
        {
            get { return this.OriginalDefinition; }
        }
        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitParameter(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitParameter(this);
        }

        #endregion
    }
}
