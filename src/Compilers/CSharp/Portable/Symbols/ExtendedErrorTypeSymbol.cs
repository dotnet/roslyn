// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An error type, used to represent the type of a type binding
    /// operation when binding fails.
    /// </summary>
    internal sealed class ExtendedErrorTypeSymbol : ErrorTypeSymbol
    {
        private readonly string _name;
        private readonly int _arity;
        private readonly DiagnosticInfo? _errorInfo;
        private readonly NamespaceOrTypeSymbol? _containingSymbol;
        private readonly bool _unreported;
        public readonly bool VariableUsedBeforeDeclaration;
        private readonly ImmutableArray<Symbol> _candidateSymbols;  // Best guess at what user meant, but was wrong.
        private readonly LookupResultKind _resultKind; // why the guessSymbols were wrong.

        internal ExtendedErrorTypeSymbol(CSharpCompilation compilation, string name, int arity, DiagnosticInfo? errorInfo, bool unreported = false, bool variableUsedBeforeDeclaration = false)
            : this(compilation.Assembly.GlobalNamespace, name, arity, errorInfo, unreported, variableUsedBeforeDeclaration)
        {
        }

        internal ExtendedErrorTypeSymbol(NamespaceOrTypeSymbol? containingSymbol, string name, int arity, DiagnosticInfo? errorInfo, bool unreported = false, bool variableUsedBeforeDeclaration = false)
        {
            Debug.Assert(((object?)containingSymbol == null) ||
                (containingSymbol.Kind == SymbolKind.Namespace) ||
                (containingSymbol.Kind == SymbolKind.NamedType) ||
                (containingSymbol.Kind == SymbolKind.ErrorType));

            RoslynDebug.Assert(name != null);
            Debug.Assert(unreported == false || errorInfo != null);

            _name = name;
            _errorInfo = errorInfo;
            _containingSymbol = containingSymbol;
            _arity = arity;
            _unreported = unreported;
            this.VariableUsedBeforeDeclaration = variableUsedBeforeDeclaration;
            _resultKind = LookupResultKind.Empty;
        }

        private ExtendedErrorTypeSymbol(NamespaceOrTypeSymbol? containingSymbol, string name, int arity, DiagnosticInfo? errorInfo, bool unreported, bool variableUsedBeforeDeclaration, ImmutableArray<Symbol> candidateSymbols, LookupResultKind resultKind)
        {
            _name = name;
            _errorInfo = errorInfo;
            _containingSymbol = containingSymbol;
            _arity = arity;
            _unreported = unreported;
            this.VariableUsedBeforeDeclaration = variableUsedBeforeDeclaration;
            _candidateSymbols = candidateSymbols;
            _resultKind = resultKind;
        }

        internal ExtendedErrorTypeSymbol(NamespaceOrTypeSymbol guessSymbol, LookupResultKind resultKind, DiagnosticInfo errorInfo, bool unreported = false)
            : this(guessSymbol.ContainingNamespaceOrType(), guessSymbol, resultKind, errorInfo, unreported)
        {
        }

        internal ExtendedErrorTypeSymbol(NamespaceOrTypeSymbol? containingSymbol, Symbol guessSymbol, LookupResultKind resultKind, DiagnosticInfo errorInfo, bool unreported = false)
            : this(containingSymbol, ImmutableArray.Create<Symbol>(guessSymbol), resultKind, errorInfo, GetArity(guessSymbol), unreported)
        {
        }

        internal ExtendedErrorTypeSymbol(NamespaceOrTypeSymbol? containingSymbol, ImmutableArray<Symbol> candidateSymbols, LookupResultKind resultKind, DiagnosticInfo errorInfo, int arity, bool unreported = false)
            : this(containingSymbol, candidateSymbols[0].Name, arity, errorInfo, unreported)
        {
            _candidateSymbols = UnwrapErrorCandidates(candidateSymbols);
            _resultKind = resultKind;
            Debug.Assert(candidateSymbols.IsEmpty || resultKind != LookupResultKind.Viable, "Shouldn't use LookupResultKind.Viable with candidate symbols");
        }

        internal ExtendedErrorTypeSymbol AsUnreported()
        {
            return this.Unreported ? this :
                new ExtendedErrorTypeSymbol(_containingSymbol, _name, _arity, _errorInfo, true, VariableUsedBeforeDeclaration, _candidateSymbols, _resultKind);
        }

        private static ImmutableArray<Symbol> UnwrapErrorCandidates(ImmutableArray<Symbol> candidateSymbols)
        {
            var candidate = candidateSymbols.IsEmpty ? null : candidateSymbols[0] as ErrorTypeSymbol;
            return ((object?)candidate != null && !candidate.CandidateSymbols.IsEmpty) ? candidate.CandidateSymbols : candidateSymbols;
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override DiagnosticInfo? ErrorInfo
        {
            get
            {
                return _errorInfo;
            }
        }

        internal override LookupResultKind ResultKind
        {
            get
            {
                return _resultKind;
            }
        }

        public override ImmutableArray<Symbol> CandidateSymbols => _candidateSymbols.NullToEmpty();

        internal override bool Unreported
        {
            get { return _unreported; }
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (_unreported)
            {
                return new UseSiteInfo<AssemblySymbol>(this.ErrorInfo);
            }

            return default;
        }

        public override int Arity
        {
            get
            {
                return _arity;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return _arity > 0;
            }
        }

        internal override bool IsFileLocal => false;
        internal override FileIdentifier? AssociatedFileIdentifier => null;

        public override Symbol? ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override NamedTypeSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        // public override SymbolKind Kind { get { return SymbolKind.Error; } }
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        internal override NamedTypeSymbol? GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return null;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        /// <summary>
        /// If (we believe) we know which symbol the user intended, then we should retain that information
        /// in the corresponding error symbol - it can be useful for deciding how to handle the error.
        /// For example, we might want to know whether (we believe) the error type was supposed to be an
        /// interface, so that we can put it in a derived type's interface list, rather than in the base
        /// type slot.
        /// 
        /// Sometimes we will return the original definition of the intended symbol.  For example, if we see 
        /// <![CDATA[IGoo<int>]]> and we have an IGoo with a different arity or accessibility 
        /// (e.g. <![CDATA[IGoo<int>]]> was constructed from an error symbol based on <![CDATA[IGoo<T>]]>), 
        /// then we'll return <![CDATA[IGoo<T>]]>, rather than trying to construct a corresponding closed
        /// type (which may not be difficult/possible in the case of nested types or mismatched arities).
        /// 
        /// NOTE: Any non-null type symbol returned is guaranteed not to be an error type.
        /// </summary>
        /// <remarks>
        /// TypeSymbolExtensions.GetNonErrorGuess is a more discoverable version of this functionality.
        /// However, the real definition is in this class so that it can access the private field 
        /// nonErrorGuessType.
        /// </remarks>
        internal static TypeSymbol? ExtractNonErrorType(TypeSymbol? oldSymbol)
        {
            if ((object?)oldSymbol == null || oldSymbol.TypeKind != TypeKind.Error)
            {
                return oldSymbol;
            }

            // At this point, we know that oldSymbol is a non-null type symbol with kind error.
            // Hence, it is either an ErrorTypeSymbol or it has an ErrorTypeSymbol as its
            // original definition.  In the former case, it is its own original definition.
            // Thus, if there's a CSErrorTypeSymbol in there somewhere, it's returned by
            // OriginalDefinition.
            ExtendedErrorTypeSymbol? oldError = oldSymbol.OriginalDefinition as ExtendedErrorTypeSymbol;

            // If the original definition isn't a CSErrorTypeSymbol, then we don't know how to
            // pull out a non-error type.  If it is, then if there is a unambiguous type inside it,
            // use that.
            if ((object?)oldError != null && !oldError._candidateSymbols.IsDefault && oldError._candidateSymbols.Length == 1)
            {
                TypeSymbol? type = oldError._candidateSymbols[0] as TypeSymbol;
                if ((object?)type != null)
                    return type.GetNonErrorGuess();
            }

            return null;
        }

        // Get the type kind of a symbol, going to candidates if possible.
        internal static TypeKind ExtractNonErrorTypeKind(TypeSymbol oldSymbol)
        {
            if (oldSymbol.TypeKind != TypeKind.Error)
            {
                return oldSymbol.TypeKind;
            }

            // At this point, we know that oldSymbol is a non-null type symbol with kind error.
            // Hence, it is either an ErrorTypeSymbol or it has an ErrorTypeSymbol as its
            // original definition.  In the former case, it is its own original definition.
            // Thus, if there's a CSErrorTypeSymbol in there somewhere, it's returned by
            // OriginalDefinition.
            ExtendedErrorTypeSymbol? oldError = oldSymbol.OriginalDefinition as ExtendedErrorTypeSymbol;

            // If the original definition isn't a CSErrorTypeSymbol, then we don't know how to
            // pull out a non-error type.  If it is, then if there is a unambiguous type inside it,
            // use that.
            TypeKind commonTypeKind = TypeKind.Error;
            if ((object?)oldError != null && !oldError._candidateSymbols.IsDefault && oldError._candidateSymbols.Length > 0)
            {
                foreach (Symbol sym in oldError._candidateSymbols)
                {
                    TypeSymbol? type = sym as TypeSymbol;
                    if ((object?)type != null && type.TypeKind != TypeKind.Error)
                    {
                        if (commonTypeKind == TypeKind.Error)
                            commonTypeKind = type.TypeKind;
                        else if (commonTypeKind != type.TypeKind)
                            return TypeKind.Error;  // no common kind.
                    }
                }
            }

            return commonTypeKind;
        }

        internal override bool Equals(TypeSymbol? t2, TypeCompareKind comparison)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            var other = t2 as ExtendedErrorTypeSymbol;
            if ((object?)other == null || _unreported || other._unreported)
            {
                return false;
            }

            return
                ((object)this.ContainingType != null ? this.ContainingType.Equals(other.ContainingType, comparison) :
                 (object?)this.ContainingSymbol == null ? (object?)other.ContainingSymbol == null : this.ContainingSymbol.Equals(other.ContainingSymbol)) &&
                this.Name == other.Name && this.Arity == other.Arity;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Arity,
                        Hash.Combine((object?)this.ContainingSymbol != null ? this.ContainingSymbol.GetHashCode() : 0,
                                     this.Name != null ? this.Name.GetHashCode() : 0));
        }

        private static int GetArity(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    return ((NamedTypeSymbol)symbol).Arity;
                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).Arity;
                default:
                    return 0;
            }
        }
    }
}
