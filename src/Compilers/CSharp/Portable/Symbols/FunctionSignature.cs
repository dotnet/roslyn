// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An inferred signature for a lambda expression of method group.
    /// The actual signature is calculated on demand in <see cref="GetSignatureAsTypeSymbol"/>,
    /// so a non-null <see cref="FunctionSignature"/> does not necessarily mean a signature could be inferred.
    /// </summary>
    internal sealed class FunctionSignature
    {
        private readonly AssemblySymbol _assembly;
        private readonly Binder? _binder;

        private FunctionTypeSymbol? _lazyFunctionType;
        private BoundExpression? _expression;
        private Func<Binder, BoundExpression, NamedTypeSymbol?>? _calculateDelegate;

        internal FunctionSignature(Binder binder)
        {
            _assembly = binder.Compilation.Assembly;
            _binder = binder;
            _lazyFunctionType = FunctionTypeSymbol.Uninitialized;
        }

        internal void SetCallback(BoundExpression expression, Func<Binder, BoundExpression, NamedTypeSymbol?> calculateDelegate)
        {
            Debug.Assert(_calculateDelegate is null);
            Debug.Assert((object?)_lazyFunctionType == FunctionTypeSymbol.Uninitialized);

            _expression = expression;
            _calculateDelegate = calculateDelegate;
        }

        /// <summary>
        /// Returns the inferred signature or null if the signature could not be inferred.
        /// </summary>
        internal FunctionTypeSymbol? GetSignatureAsTypeSymbol()
        {
            if ((object?)_lazyFunctionType == FunctionTypeSymbol.Uninitialized)
            {
                var delegateType = _calculateDelegate!(_binder!, _expression!);
                var functionType = delegateType is null ? null : new FunctionTypeSymbol(_assembly, delegateType);
                Interlocked.CompareExchange(ref _lazyFunctionType, functionType, FunctionTypeSymbol.Uninitialized);
            }
            return _lazyFunctionType;
        }
    }
}
