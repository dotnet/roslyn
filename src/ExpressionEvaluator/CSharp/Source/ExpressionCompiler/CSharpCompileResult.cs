// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CSharpCompileResult : CompileResult
    {
        private readonly MethodSymbol _method;

        internal CSharpCompileResult(
            byte[] assembly,
            MethodSymbol method,
            ReadOnlyCollection<string> formatSpecifiers)
            : base(assembly, method.ContainingType.MetadataName, method.MetadataName, formatSpecifiers)
        {
            Debug.Assert(method is EEMethodSymbol); // Expected but not required.
            _method = method;
        }

        public override CustomTypeInfo GetCustomTypeInfo() =>
            new CustomTypeInfo(DynamicFlagsCustomTypeInfo.PayloadTypeId, _method.GetCustomTypeInfoPayload());
    }
}
