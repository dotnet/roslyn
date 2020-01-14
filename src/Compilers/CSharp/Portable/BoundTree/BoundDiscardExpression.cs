// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDiscardExpression
    {
        public BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type)
        {
            Debug.Assert((object)Type == null && type.HasType);
            return this.Update(type.Type);
        }

        public BoundDiscardExpression FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            if (diagnosticsOpt != null)
            {
                Binder.Error(diagnosticsOpt, ErrorCode.ERR_DiscardTypeInferenceFailed, this.Syntax);
            }
            return this.Update(binder.CreateErrorType("var"));
        }

        public override Symbol ExpressionSymbol
        {
            get
            {
                Debug.Assert((object)this.Type != null);
                return new DiscardSymbol(TypeWithAnnotations.Create(this.Type, this.TopLevelNullability.Annotation.ToInternalAnnotation()));
            }
        }
    }
}
