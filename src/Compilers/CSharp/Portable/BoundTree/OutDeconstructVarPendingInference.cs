// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutDeconstructVarPendingInference
    {
        public BoundDeconstructValuePlaceholder Placeholder;

        public BoundDeconstructValuePlaceholder SetInferredType(TypeSymbol type, bool success)
        {
            Debug.Assert((object)Placeholder == null);

            Placeholder = new BoundDeconstructValuePlaceholder(this.Syntax, type, hasErrors: this.HasErrors || !success);
            return Placeholder;
        }

        public BoundDeconstructValuePlaceholder FailInference(Binder binder)
        {
            return SetInferredType(binder.CreateErrorType(), success: false);
        }
    }
}