// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ExceptionLocalSymbol : PlaceholderLocalSymbol
    {
        internal ExceptionLocalSymbol(MethodSymbol method, string name, TypeSymbol type) :
            base(method, name, type)
        {
        }

        internal override bool IsWritable
        {
            get { return false; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax)
        {
            Debug.Assert(this.Name == this.Name.ToLowerInvariant());
            var method = container.GetOrAddSynthesizedMethod(
                this.Name,
                (c, n, s) =>
                {
                    var returnType = compilation.GetWellKnownType(WellKnownType.System_Exception);
                    return new PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        returnType,
                        m => ImmutableArray<ParameterSymbol>.Empty);
                });
            var call = BoundCall.Synthesized(syntax, receiverOpt: null, method: method);
            return ConvertToLocalType(compilation, call, this.Type);
        }
    }
}
