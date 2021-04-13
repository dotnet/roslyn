// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
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

        /// <remarks>
        /// The custom type info payload depends on the return type, which is not available when
        /// <see cref="CSharpLocalAndMethod"/> is created.
        /// </remarks>
        public override Guid GetCustomTypeInfo(out ReadOnlyCollection<byte>? payload)
        {
            payload = _method.GetCustomTypeInfoPayload();
            return (payload == null) ? default : CustomTypeInfo.PayloadTypeId;
        }
    }
}
