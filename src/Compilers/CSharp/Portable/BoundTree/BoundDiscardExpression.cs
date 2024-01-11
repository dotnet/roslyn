// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDiscardExpression
    {
        public BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type)
        {
            Debug.Assert(Type is null && type.HasType);
            Debug.Assert(this.IsInferred);
            return this.Update(type.NullableAnnotation, isInferred: true, type.Type);
        }

        public BoundDiscardExpression FailInference(Binder binder, BindingDiagnosticBag? diagnosticsOpt)
        {
            if (diagnosticsOpt?.DiagnosticBag != null)
            {
                Binder.Error(diagnosticsOpt, ErrorCode.ERR_DiscardTypeInferenceFailed, this.Syntax);
            }
            return this.Update(NullableAnnotation.Oblivious, this.IsInferred, binder.CreateErrorType("var"));
        }

        public override Symbol ExpressionSymbol
        {
            get
            {
                Debug.Assert(this.Type is { });
                return new DiscardSymbol(TypeWithAnnotations.Create(this.Type, this.TopLevelNullability.Annotation.ToInternalAnnotation()));
            }
        }
    }
}
