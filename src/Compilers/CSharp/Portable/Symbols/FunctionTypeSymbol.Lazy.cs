// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class FunctionTypeSymbol
    {
        /// <summary>
        /// A lazily calculated instance of <see cref="FunctionTypeSymbol"/> that represents
        /// the inferred signature of a lambda expression or method group.
        /// The actual signature is calculated on demand in <see cref="GetValue()"/>.
        /// </summary>
        internal sealed class Lazy
        {
            private readonly Binder _binder;
            private readonly Func<Binder, BoundExpression, NamedTypeSymbol?> _calculateDelegate;

            private FunctionTypeSymbol? _lazyFunctionType;
            private BoundExpression? _expression;

            internal static Lazy? CreateIfFeatureEnabled(SyntaxNode syntax, Binder binder, Func<Binder, BoundExpression, NamedTypeSymbol?> calculateDelegate)
            {
                return syntax.IsFeatureEnabled(MessageID.IDS_FeatureInferredDelegateType) ?
                    new Lazy(binder, calculateDelegate) :
                    null;
            }

            private Lazy(Binder binder, Func<Binder, BoundExpression, NamedTypeSymbol?> calculateDelegate)
            {
                _binder = binder;
                _calculateDelegate = calculateDelegate;
                _lazyFunctionType = FunctionTypeSymbol.Uninitialized;
            }

            internal void SetExpression(BoundExpression expression)
            {
                Debug.Assert((object?)_lazyFunctionType == FunctionTypeSymbol.Uninitialized);
                Debug.Assert(_expression is null);
                Debug.Assert(expression.Kind is BoundKind.MethodGroup or BoundKind.UnboundLambda);

                _expression = expression;
            }

            /// <summary>
            /// Returns the inferred signature as a <see cref="FunctionTypeSymbol"/>
            /// or null if the signature could not be inferred.
            /// </summary>
            internal FunctionTypeSymbol? GetValue()
            {
                Debug.Assert(_expression is { });

                if ((object?)_lazyFunctionType == FunctionTypeSymbol.Uninitialized)
                {
                    var delegateType = _calculateDelegate(_binder, _expression);
                    var functionType = delegateType is null ? null : new FunctionTypeSymbol(delegateType);
                    Interlocked.CompareExchange(ref _lazyFunctionType, functionType, FunctionTypeSymbol.Uninitialized);
                }

                return _lazyFunctionType;
            }
        }
    }
}
