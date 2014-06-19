using System.Diagnostics;
using Roslyn.Compilers.Common;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Summarizes the semantic information about a syntax node. 
    /// </summary>
    internal class SemanticInfo : ISemanticInfo
    {
        /// <summary>
        /// The type of the expression represented by the syntax node. For expressions that do not
        /// have a type, null is returned. If the type could not be determined due to an error, than
        /// an object derived from ErrorTypeSymbol is returned.
        /// </summary>
        // should be best guess if there is one, or error type if none.
        internal TypeSymbol Type { get; private set; }

        /// <summary>
        /// The type of the expression after it has undergone an implicit conversion. If the type
        /// did not undergo an implicit conversion, returns the same as Type.
        /// </summary>
        internal TypeSymbol ConvertedType { get; private set; }

        /// <summary>
        /// If the expression underwent an implicit conversion, return information about that
        /// conversion. Otherwise, returns an identity conversion.
        /// </summary>
        internal Conversion ImplicitConversion { get; private set; }

        // The symbols resulting from binding, and what kind of binding problem might have resulted.
        private readonly ReadOnlyArray<Symbol> symbols;
        private readonly LookupResultKind resultKind;

        internal ReadOnlyArray<Symbol> AllSymbols
        {
            get
            {
                return symbols;
            }
        }

        internal LookupResultKind ResultKind
        {
            get
            {
                return resultKind;
            }
        }

        /// <summary>
        /// The symbol that was referred to by the syntax node, if any. Returns null if the given
        /// expression did not bind successfully to a single symbol. If null is returned, it may
        /// still be that case that we have one or more "best guesses" as to what symbol was
        /// intended. These best guesses are available via the CandidateSymbols property.
        /// </summary>
        internal Symbol Symbol
        {
            get
            {
                if (resultKind == LookupResultKind.Viable && symbols.Count > 0)
                {
                    Debug.Assert(symbols.Count == 1);
                    return symbols[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more
        /// symbols that may have been considered but discarded, this property returns those
        /// symbols. The reason that the symbols did not successfully resolve to a symbol are
        /// available in the CandidateReason property. For example, if the symbol was inaccessible,
        /// ambiguous, or used in the wrong context.
        /// </summary>
        internal ReadOnlyArray<Symbol> CandidateSymbols
        {
            get
            {
                if (resultKind != LookupResultKind.Viable && symbols.Count > 0)
                {
                    return symbols;
                }
                else
                {
                    return ReadOnlyArray<Symbol>.Empty;
                }
            }
        }

        ///<summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more
        /// symbols that may have been considered but discarded, this property describes why those
        /// symbol or symbols were not considered suitable.
        /// </summary>
        internal CandidateReason CandidateReason
        {
            get
            {
                return resultKind == LookupResultKind.Viable
                    ? CandidateReason.None
                    : resultKind.ToCandidateReason();
            }
        }

        /// <summary>
        /// When getting information for a symbol that resolves to a method group, from which a
        /// method is then chosen; the chosen method is present in Symbol; all methods in the
        /// group that was consulted are placed in this property.
        /// </summary>
        internal ReadOnlyArray<MethodSymbol> MethodGroup { get; private set; }

        private readonly ConstantValue constantValue;

        /// <summary>
        /// Returns true if the expression is a compile-time constant. The value of the constant can
        /// be obtained with the ConstantValue property.
        /// </summary>
        internal bool IsCompileTimeConstant
        {
            get
            {
                return constantValue != null && !constantValue.IsBad;
            }
        }

        /// <summary>
        /// If IsCompileTimeConstant returns true, then returns the constant value of the field or
        /// enum member. If IsCompileTimeConstant returns false, then returns null.
        /// </summary>
        internal object ConstantValue
        {
            get
            {
                //can be null in error scenarios
                return constantValue == null ? null : constantValue.Value;
            }
        }

        internal SemanticInfo(
            TypeSymbol type,
            Conversion conversion,
            TypeSymbol convertedType,
            ReadOnlyArray<Symbol> symbols,
            LookupResultKind resultKind,
            ReadOnlyArray<MethodSymbol> methodGroup,
            ConstantValue constantValue)
        {
            // When constructing the result for the Caas API, we expose the underlying symbols that
            // may have been hidden under error type, if the error type was immediate. We will
            // expose error types that were constructed, or type parameters of constructed types.
            this.Type = type.GetNonErrorGuess() ?? type;
            this.ConvertedType = convertedType.GetNonErrorGuess() ?? convertedType;
            this.ImplicitConversion = conversion;

            this.symbols = symbols;
            this.resultKind = resultKind;
            if (!symbols.Any())
            {
                this.resultKind = LookupResultKind.Empty;
            }

            this.MethodGroup = methodGroup;
            this.constantValue = constantValue;
        }

        /// <summary>
        /// A pre-created instance of SemanticInfo that has a "null" type, no symbols, no constant
        /// value, and no diagnostics.  
        /// </summary>
        public static readonly SemanticInfo None = new SemanticInfo(
            type: null,
            conversion: new Conversion(ConversionKind.Identity),
            convertedType: null,
            symbols: ReadOnlyArray<Symbol>.Empty,
            resultKind: LookupResultKind.Empty,
            methodGroup: ReadOnlyArray<MethodSymbol>.Empty,
            constantValue: null);
    }
}