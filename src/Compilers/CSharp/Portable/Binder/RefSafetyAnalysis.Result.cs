// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class RefSafetyAnalysis
    {
        // PROTOTYPE: Reduce allocations, perhaps by using a struct.
        // PROTOTYPE: Move to separate file.
        private abstract class Result
        {
            internal static Result Create(SyntaxNode syntax, BoundExpression expression, bool checkingReceiver, bool isRef, uint escapeScope)
            {
                // PROTOTYPE: Assert !isRef. Otherwise, the caller should be using the Create() overload below and passing both refEscapeScope and valEscapeScope.
                return Create(syntax, expression, checkingReceiver, isRef, isRef ? escapeScope : UndefinedScope, isRef ? CallingMethodScope : escapeScope, hasErrors: false);
            }

            internal static Result Create(SyntaxNode syntax, BoundExpression expression, bool checkingReceiver, bool isRef, uint refEscapeScope, uint valEscapeScope, bool hasErrors)
            {
                // PROTOTYPE: If escapeScope == CallingMethod, do we really need a unique ExpressionResult instance
                // or could we return one of two "singletons" depending on resultKind?
                return new ExpressionResult(syntax, expression, checkingReceiver, isRef, isOriginallyRef: isRef, refEscapeScope, valEscapeScope, hasErrors);
            }

            internal abstract SyntaxNode Syntax { get; }
            internal abstract bool CheckingReceiver { get; }
            internal abstract bool IsRef { get; }
            internal abstract bool IsOriginallyRef { get; } // PROTOTYPE: Revisit this.
            internal abstract bool HasErrors { get; }
            internal abstract uint RefEscapeScope { get; }
            internal abstract uint ValEscapeScope { get; }
            internal abstract Result AsRefResult(bool isRef);
            internal abstract Result GetValResult();

            internal uint EscapeScope => IsRef ? RefEscapeScope : ValEscapeScope;

            internal Result WithParameter(SyntaxNode syntax, Symbol containingSymbol, ParameterSymbol? parameter, bool checkingReceiver)
            {
                return new ParameterResult(syntax, containingSymbol, parameter, this, checkingReceiver);
            }

            internal static Result Max(in Result a, in Result b)
            {
                Debug.Assert(a.IsRef == b.IsRef);
                // PROTOTYPE: Handle merging results if necessary.
                return a.EscapeScope < b.EscapeScope ? b : a;
            }

            // PROTOTYPE: Do we need these operators?
            public static bool operator >=(in Result a, Result b) { Debug.Assert(a.IsRef == b.IsRef); return a.EscapeScope >= b.EscapeScope; }
            public static bool operator <=(in Result a, Result b) { Debug.Assert(a.IsRef == b.IsRef); return a.EscapeScope <= b.EscapeScope; }
            public static bool operator >(in Result a, Result b) { Debug.Assert(a.IsRef == b.IsRef); return a.EscapeScope > b.EscapeScope; }
            public static bool operator <(in Result a, Result b) { Debug.Assert(a.IsRef == b.IsRef); return a.EscapeScope < b.EscapeScope; }

            // PROTOTYPE: Do we need these operators?
            public static bool operator >=(in Result a, uint b) => a.EscapeScope >= b;
            public static bool operator <=(in Result a, uint b) => a.EscapeScope <= b;
            public static bool operator >(in Result a, uint b) => a.EscapeScope > b;
            public static bool operator <(in Result a, uint b) => a.EscapeScope < b;

            // PROTOTYPE: Remove if not needed.
            public void Deconstruct(out uint refEscapeScope, out uint valEscapeScope)
            {
                refEscapeScope = RefEscapeScope;
                valEscapeScope = ValEscapeScope;
            }
        }

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private sealed class ExpressionResult : Result
        {
            public readonly BoundExpression Expression;

            internal ExpressionResult(SyntaxNode syntax, BoundExpression expression, bool checkingReceiver, bool isRef, bool isOriginallyRef, uint refEscapeScope, uint valEscapeScope, bool hasErrors)
            {
                Debug.Assert(valEscapeScope != UndefinedScope);
                Debug.Assert(!isRef || refEscapeScope != UndefinedScope);

                Syntax = syntax;
                RefEscapeScope = refEscapeScope;
                ValEscapeScope = valEscapeScope;
                Expression = expression;
                CheckingReceiver = checkingReceiver;
                IsRef = isRef;
                IsOriginallyRef = isOriginallyRef; ;
                HasErrors = hasErrors;
            }

            internal override SyntaxNode Syntax { get; }
            internal override bool CheckingReceiver { get; }
            internal override bool IsRef { get; }
            internal override bool IsOriginallyRef { get; }
            internal override bool HasErrors { get; }
            internal override uint RefEscapeScope { get; }
            internal override uint ValEscapeScope { get; }

            internal override Result AsRefResult(bool isRef)
            {
                if (IsRef == isRef)
                {
                    return this;
                }
                // PROTOTYPE: It seems incorrect (or at least confusing) to associate the isRef variant with the original expression.
                if (isRef)
                {
                    return new ExpressionResult(Syntax, Expression, CheckingReceiver, isRef, IsOriginallyRef, ValEscapeScope, CallingMethodScope, HasErrors);
                }
                return new ExpressionResult(Syntax, Expression, CheckingReceiver, isRef, IsOriginallyRef, UndefinedScope, RefEscapeScope, HasErrors);
            }

            internal override Result GetValResult()
            {
                if (!IsRef)
                {
                    return this;
                }
                return new ExpressionResult(Syntax, Expression, CheckingReceiver, isRef: false, isOriginallyRef: false, UndefinedScope, ValEscapeScope, HasErrors);
            }

            private string GetDebuggerDisplay()
            {
                return $"{Expression} ({RefEscapeScope}, {ValEscapeScope})";
            }
        }

        private sealed class ParameterResult : Result
        {
            public readonly Symbol ContainingSymbol;
            public readonly ParameterSymbol? Parameter;
            public readonly Result ArgumentResult;

            internal ParameterResult(SyntaxNode syntax, Symbol containingSymbol, ParameterSymbol? parameter, Result argumentResult, bool checkingReceiver)
            {
                Syntax = syntax;
                ContainingSymbol = containingSymbol;
                Parameter = parameter;
                ArgumentResult = argumentResult;
                CheckingReceiver = checkingReceiver;
            }

            internal override SyntaxNode Syntax { get; }
            internal override bool CheckingReceiver { get; }
            internal override bool IsRef => ArgumentResult.IsRef;
            internal override bool IsOriginallyRef => ArgumentResult.IsOriginallyRef;
            internal override bool HasErrors => false;
            internal override uint RefEscapeScope => ArgumentResult.RefEscapeScope;
            internal override uint ValEscapeScope => ArgumentResult.ValEscapeScope;

            internal override Result AsRefResult(bool isRef)
            {
                var argumentResult = ArgumentResult.AsRefResult(isRef);
                if ((object)argumentResult == ArgumentResult)
                {
                    return this;
                }
                return new ParameterResult(Syntax, ContainingSymbol, Parameter, argumentResult, CheckingReceiver);
            }

            internal override Result GetValResult()
            {
                var argumentResult = ArgumentResult.GetValResult();
                if ((object)argumentResult == ArgumentResult)
                {
                    return this;
                }
                return new ParameterResult(Syntax, ContainingSymbol, Parameter, argumentResult, CheckingReceiver);
            }
        }
    }
}
