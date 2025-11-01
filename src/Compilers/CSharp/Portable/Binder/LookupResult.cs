// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// represents one-to-one symbol -> SingleLookupResult filter.
    /// </summary>
    internal delegate SingleLookupResult LookupFilter(Symbol sym);

    /// <summary>
    /// A LookupResult summarizes the result of a name lookup within a scope It also allows
    /// combining name lookups from different scopes in an easy way.
    /// 
    /// A LookupResult can be ONE OF:
    ///    empty - nothing found.
    ///    a viable result - this kind of result prevents lookup into further scopes of lower priority.
    ///                      Viable results should be without error; ambiguity is handled in the caller.
    ///                      (Note that handling multiple "viable" results is not the same as in the VB compiler)
    ///    a non-accessible result - this kind of result means that search continues into further scopes of lower priority for
    ///                      a viable result. An error is attached with the inaccessibility errors. Non-accessible results take priority over
    ///                      non-viable results.
    ///    a non-viable result - a result that means that the search continues into further scopes of lower priority for
    ///                          a viable or non-accessible result. An error is attached with the error that indicates
    ///                          why the result is non-viable.  A typical reason would be that it is the wrong kind of symbol.
    /// 
    /// Note that the class is poolable so its instances can be obtained from a pool via GetInstance.
    /// Also it is a good idea to call Free on instances after they no longer needed.
    /// 
    /// The typical pattern is "caller allocates / caller frees" -
    ///    
    ///    var result = LookupResult.GetInstance();
    ///  
    ///    scope.Lookup(result, "goo");
    ///    ... use result ...
    ///         
    ///    result.Clear();
    ///    anotherScope.Lookup(result, "moo");
    ///    ... use result ...
    /// 
    ///    result.Free();   //result and its content is invalid after this
    ///    
    /// 
    /// 
    /// </summary>
    /// <remarks>
    /// Currently LookupResult is intended only for name lookup, not for overload resolution. It is
    /// not clear if overload resolution will work with the structure as is, require enhancements,
    /// or be best served by an alternate mechanism.
    /// 
    /// We might want to extend this to a more general priority scheme.
    /// 
    /// </remarks>
    internal sealed class LookupResult
    {
        // the kind of result.
        private LookupResultKind _kind;

        // If there is more than one symbol, they are stored in this list.
        private readonly ArrayBuilder<Symbol> _symbolList;

        // the error of the result, if it is NonViable or Inaccessible
        private DiagnosticInfo _error;

        private readonly ObjectPool<LookupResult> _pool;

        private LookupResult(ObjectPool<LookupResult> pool)
        {
            _pool = pool;
            _kind = LookupResultKind.Empty;
            _symbolList = new ArrayBuilder<Symbol>();
            _error = null;
        }

        internal bool IsClear
        {
            get
            {
                return _kind == LookupResultKind.Empty && _error == null && _symbolList.Count == 0;
            }
        }

        internal void Clear()
        {
            _kind = LookupResultKind.Empty;
            _symbolList.Clear();
            _error = null;
        }

        internal LookupResultKind Kind
        {
            get
            {
                return _kind;
            }
        }

        /// <summary>
        /// Return the single symbol if there is exactly one, otherwise null.
        /// </summary>
        internal Symbol SingleSymbolOrDefault
        {
            get
            {
                return (_symbolList.Count == 1) ? _symbolList[0] : null;
            }
        }

        internal ArrayBuilder<Symbol> Symbols
        {
            get
            {
                return _symbolList;
            }
        }

        internal DiagnosticInfo Error
        {
            get
            {
                return _error;
            }
        }

        /// <summary>
        /// Is the result viable with one or more symbols?
        /// </summary>
        internal bool IsMultiViable
        {
            get
            {
                return Kind == LookupResultKind.Viable;
            }
        }

        /// <summary>
        /// NOTE: Even there is a single viable symbol, it may be an error type symbol.
        /// </summary>
        internal bool IsSingleViable
        {
            get
            {
                return Kind == LookupResultKind.Viable && _symbolList.Count == 1;
            }
        }

        internal static SingleLookupResult Good(Symbol symbol)
        {
            return new SingleLookupResult(LookupResultKind.Viable, symbol, null);
        }

        internal static SingleLookupResult WrongArity(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.WrongArity, symbol, error);
        }

        internal static SingleLookupResult Empty()
        {
            return new SingleLookupResult(LookupResultKind.Empty, null, null);
        }

        internal static SingleLookupResult NotReferencable(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.NotReferencable, symbol, error);
        }

        internal static SingleLookupResult StaticInstanceMismatch(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.StaticInstanceMismatch, symbol, error);
        }

        internal static SingleLookupResult Inaccessible(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.Inaccessible, symbol, error);
        }

        internal static SingleLookupResult NotInvocable(Symbol unwrappedSymbol, Symbol symbol, bool diagnose)
        {
            var diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_NonInvocableMemberCalled, unwrappedSymbol) : null;
            return new SingleLookupResult(LookupResultKind.NotInvocable, symbol, diagInfo);
        }

        internal static SingleLookupResult NotLabel(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.NotLabel, symbol, error);
        }

        internal static SingleLookupResult NotTypeOrNamespace(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.NotATypeOrNamespace, symbol, error);
        }

        internal static SingleLookupResult NotTypeOrNamespace(Symbol unwrappedSymbol, Symbol symbol, bool diagnose)
        {
            // TODO: determine correct diagnosis 
            var diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadSKknown, unwrappedSymbol.Name, unwrappedSymbol.GetKindText(), MessageID.IDS_SK_TYPE.Localize()) : null;
            return new SingleLookupResult(LookupResultKind.NotATypeOrNamespace, symbol, diagInfo);
        }

        internal static SingleLookupResult NotAnAttributeType(Symbol symbol, DiagnosticInfo error)
        {
            return new SingleLookupResult(LookupResultKind.NotAnAttributeType, symbol, error);
        }

        /// <summary>
        /// Set current result according to another.
        /// </summary>
        internal void SetFrom(SingleLookupResult other)
        {
            _kind = other.Kind;
            _symbolList.Clear();
            _symbolList.Add(other.Symbol);
            _error = other.Error;
        }

        /// <summary>
        /// Set current result according to another.
        /// </summary>
        internal void SetFrom(LookupResult other)
        {
            _kind = other._kind;
            _symbolList.Clear();
            _symbolList.AddRange(other._symbolList);
            _error = other._error;
        }

        internal void SetFrom(DiagnosticInfo error)
        {
            this.Clear();
            _error = error;
        }

        // Merge another result with this one, with the current result being prioritized
        // over the other if they are of equal "goodness". Mutates the current result.
        internal void MergePrioritized(LookupResult other)
        {
            if (other.Kind > Kind)
            {
                SetFrom(other);
            }
        }

        /// <summary>
        /// Merge another result with this one, with the symbols combined if both
        /// this and other are viable. Otherwise the highest priority result wins (this if equal 
        /// priority and non-viable.)
        /// </summary>
        internal void MergeEqual(LookupResult other)
        {
            if (Kind > other.Kind)
            {
                return;
            }
            else if (other.Kind > Kind)
            {
                this.SetFrom(other);
            }
            else if (Kind != LookupResultKind.Viable)
            {
                // this makes the operator not symmetrical, but so far we do not care.
                // it is really a matter of which error gets reported.
                return;
            }
            else
            {
                // Merging two viable results together. We will always end up with at least two symbols.
                _symbolList.AddRange(other._symbolList);
            }
        }

        internal void MergeEqual(SingleLookupResult result)
        {
            if (Kind > result.Kind)
            {
                // Existing result is strictly better.  Ignore what is incoming.  
                return;
            }

            if (result.Kind > Kind)
            {
                // Incoming result is better.  Let it win completely over anything we've built up so far.
                this.SetFrom(result);
                return;
            }

            if (Kind == LookupResultKind.WrongArity && result.Kind == LookupResultKind.WrongArity)
            {
                if (isNonGenericVersusGeneric(result.Symbol, this.SingleSymbolOrDefault))
                {
                    // Current result is generic, and incoming is not.  We just want stick with what we currently have
                    // as the better symbol to be referring to when generics are provided, but arity is wrong.
                    return;
                }

                if (isNonGenericVersusGeneric(this.SingleSymbolOrDefault, result.Symbol))
                {
                    // Current result is non generic, but incoming is generic.  It's strictly the better symbol to be
                    // referring to when generics are provided, but arity is wrong.
                    this.SetFrom(result);
                    return;
                }

                // Neither is preferred, fall through and include all symbols.
            }

            // Same goodness. Include all symbols
            _symbolList.AddIfNotNull(result.Symbol);

            static bool isNonGenericVersusGeneric(Symbol firstSymbol, Symbol secondSymbol)
                => firstSymbol.GetArity() == 0 && secondSymbol.GetArity() > 0;
        }

        // global pool
        //TODO: consider if global pool is ok.
        private static readonly ObjectPool<LookupResult> s_poolInstance = CreatePool();

        // if someone needs to create a pool
        internal static ObjectPool<LookupResult> CreatePool()
        {
            ObjectPool<LookupResult> pool = null;
            pool = new ObjectPool<LookupResult>(() => new LookupResult(pool), 128); // we rarely need more than 10
            return pool;
        }

        internal static LookupResult GetInstance()
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance.IsClear);
            return instance;
        }

        internal void Free()
        {
            this.Clear();
            if (_pool != null)
            {
                _pool.Free(this);
            }
        }
    }
}
