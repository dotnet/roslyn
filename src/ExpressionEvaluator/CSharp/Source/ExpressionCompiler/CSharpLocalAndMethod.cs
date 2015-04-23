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

        public CSharpLocalAndMethod(string localName, MethodSymbol method, DkmClrCompilationResultFlags flags)
            : base(localName, method.Name, flags)
        {
            Debug.Assert(method is EEMethodSymbol); // Expected but not required.
            _method = method;
        }

        public override CustomTypeInfo GetCustomTypeInfo() =>
            new CustomTypeInfo(DynamicFlagsCustomTypeInfo.PayloadTypeId, _method.GetCustomTypeInfoPayload());
    }
}
