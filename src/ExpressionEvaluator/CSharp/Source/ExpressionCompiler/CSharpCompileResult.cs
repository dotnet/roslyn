// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            ReadOnlyCollection<string>? formatSpecifiers)
            : base(assembly, method.ContainingType.MetadataName, method.MetadataName, formatSpecifiers)
        {
            Debug.Assert(method is EEMethodSymbol); // Expected but not required.
            _method = method;
        }

        public override Guid GetCustomTypeInfo(out ReadOnlyCollection<byte>? payload)
        {
            payload = _method.GetCustomTypeInfoPayload();
            return (payload == null) ? default : CustomTypeInfo.PayloadTypeId;
        }
    }
}
