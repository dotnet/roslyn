// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
    ///                          why the result is non-viable.  A typical readon would be that it is the wrong kind of symbol.
    /// 
    /// Note that the class is poolable so its instances can be obtained from a pool vai GetInstance.
    /// Also it is a good idea to call Free on instances after they no longer needed.
    /// 
    /// The typical pattern is "caller allocates / caller frees" -
    ///    
    ///    var result = LookupResult.GetInstance();
    ///  
    ///    scope.Lookup(result, "foo");
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
        private LookupResultKind kind;

        // If there is more than one symbol, they are stored in this list.
        private readonly ArrayBuilder<Symbol> symbolList;

        // the error of the result, if it is NonViable or Inaccessible
        private DiagnosticInfo error;

        private readonly ObjectPool<LookupResult> pool;

        private LookupResult(ObjectPool<LookupResult> pool)
        {
            this.pool = pool;
            this.kind = LookupResultKind.Empty;
            this.symbolList = new ArrayBuilder<Symbol>();
            this.error = null;
        }

        internal bool IsClear
        {
            get
            {
                return kind == LookupResultKind.Empty && error == null && symbolList.Count == 0;
            }
        }

        internal void Clear()
        {
            this.kind = LookupResultKind.Empty;
            this.symbolList.Clear();
            this.error = null;
        }

        internal LookupResultKind Kind
        {
            get
            {
                return kind;
            }
        }

        /// <summary>
        /// Return the single symbol if there is exactly one, otherwise null.
        /// </summary>
        internal Symbol SingleSymbolOrDefault
        {
            get
            {
                return (this.symbolList.Count == 1) ? this.symbolList[0] : null;
            }
        }

        internal ArrayBuilder<Symbol> Symbols
        {
            get
            {
                return symbolList;
            }
        }

        internal DiagnosticInfo Error
        {
            get
            {
                return error;
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
                return Kind == LookupResultKind.Viable && symbolList.Count == 1;
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
            var diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadSKknown, unwrappedSymbol, unwrappedSymbol.GetKindText(), MessageID.IDS_SK_TYPE.Localize()) : null;
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
            this.kind = other.Kind;
            this.symbolList.Clear();
            this.symbolList.Add(other.Symbol);
            this.error = other.Error;
        }

        /// <summary>
        /// Set current result according to another.
        /// </summary>
        internal void SetFrom(LookupResult other)
        {
            this.kind = other.kind;
            this.symbolList.Clear();
            this.symbolList.AddRange(other.symbolList);
            this.error = other.error;
        }

        internal void SetFrom(DiagnosticInfo error)
        {
            this.Clear();
            this.error = error;
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
                this.symbolList.AddRange(other.symbolList);
            }
        }

        internal void MergeEqual(SingleLookupResult result)
        {
            if (Kind > result.Kind)
            {
                // existing result is better
            }
            else if (result.Kind > Kind)
            {
                this.SetFrom(result);
            }
            else if ((object)result.Symbol != null)
            {
                // Same goodness. Include all symbols
                this.symbolList.Add(result.Symbol);
            }
        }

        // global pool
        //TODO: consider if global pool is ok.
        private static readonly ObjectPool<LookupResult> PoolInstance = CreatePool();

        // if someone needs to create a pool
        internal static ObjectPool<LookupResult> CreatePool()
        {
            ObjectPool<LookupResult> pool = null;
            pool = new ObjectPool<LookupResult>(() => new LookupResult(pool), 128); // we rarely need more than 10
            return pool;
        }

        internal static LookupResult GetInstance()
        {
            var instance = PoolInstance.Allocate();
            Debug.Assert(instance.IsClear);
            return instance;
        }

        internal void Free()
        {
            this.Clear();
            if (pool != null)
            {
                pool.Free(this);
            }
        }
    }
}