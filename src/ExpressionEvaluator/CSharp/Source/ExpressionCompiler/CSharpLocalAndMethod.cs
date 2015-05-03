// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CSharpLocalAndMethod : LocalAndMethod
    {
        private readonly MethodSymbol _method;

        public CSharpLocalAndMethod(string name, string displayName, MethodSymbol method, DkmClrCompilationResultFlags flags)
            : base(name, displayName, method.Name, flags)
        {
            Debug.Assert(method is EEMethodSymbol); // Expected but not required.
            _method = method;
        }

        public CSharpLocalAndMethod(LocalSymbol local, MethodSymbol method, DkmClrCompilationResultFlags flags)
            // Note: The native EE doesn't do this, but if we don't escape keyword identifiers,
            // the ResultProvider needs to be able to disambiguate cases like "this" and "@this",
            // which it can't do correctly without semantic information.
            : this(
                  SyntaxHelpers.EscapeKeywordIdentifiers(local.Name),
                  (local as PlaceholderLocalSymbol)?.DisplayName ?? SyntaxHelpers.EscapeKeywordIdentifiers(local.Name), 
                  method, 
                  flags)
        {
        }

        public override CustomTypeInfo GetCustomTypeInfo() =>
            new CustomTypeInfo(DynamicFlagsCustomTypeInfo.PayloadTypeId, _method.GetCustomTypeInfoPayload());
    }
}
