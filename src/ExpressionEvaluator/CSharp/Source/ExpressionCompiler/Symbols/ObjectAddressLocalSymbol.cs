// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ObjectAddressLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly ulong _address;

        internal ObjectAddressLocalSymbol(MethodSymbol method, string name, TypeSymbol type) :
            base(method, name, name, type)
        {
            Debug.Assert(type.SpecialType == SpecialType.System_Object);
            if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var valueText = name.Substring(2);
                if (!ulong.TryParse(valueText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _address))
                {
                    // Invalid value should have been caught by Lexer.
                    throw ExceptionUtilities.UnexpectedValue(valueText);
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(name);
            }
        }

        internal override bool IsWritableVariable
        {
            // Return true?
            get { return false; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var method = GetIntrinsicMethod(compilation, ExpressionCompilerConstants.GetObjectAtAddressMethodName);
            var argument = new BoundLiteral(
                syntax,
                Microsoft.CodeAnalysis.ConstantValue.Create(_address),
                method.Parameters[0].Type);
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                method: method,
                arguments: ImmutableArray.Create<BoundExpression>(argument));
            Debug.Assert(TypeSymbol.Equals(call.Type, this.Type, TypeCompareKind.ConsiderEverything2));
            return call;
        }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            return other is ObjectAddressLocalSymbol otherLocal && ContainingSymbol == otherLocal.ContainingSymbol && Name == otherLocal.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
