// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutDeconstructVarPendingInference
    {
        public BoundDeconstructValuePlaceholder? Placeholder;

        public BoundDeconstructValuePlaceholder SetInferredTypeWithAnnotations(TypeWithAnnotations type, bool success)
        {
            Debug.Assert(Placeholder is null);

            Placeholder = new BoundDeconstructValuePlaceholder(this.Syntax, variableSymbol: VariableSymbol, isDiscardExpression: IsDiscardExpression, type.Type, hasErrors: this.HasErrors || !success);
            return Placeholder;
        }

        public BoundDeconstructValuePlaceholder FailInference(Binder binder)
        {
            return SetInferredTypeWithAnnotations(TypeWithAnnotations.Create(binder.CreateErrorType()), success: false);
        }
    }
}
