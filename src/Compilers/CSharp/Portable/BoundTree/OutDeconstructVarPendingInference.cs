﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutDeconstructVarPendingInference
    {
        public BoundDeconstructValuePlaceholder Placeholder;

        public BoundDeconstructValuePlaceholder SetInferredType(TypeSymbolWithAnnotations type, Binder binder, bool success)
        {
            Debug.Assert(Placeholder is null);

            // The val escape scope for this placeholder won't be used, so defaulting to narrowest scope
            Placeholder = new BoundDeconstructValuePlaceholder(this.Syntax, binder.LocalScopeDepth, type.TypeSymbol, hasErrors: this.HasErrors || !success);
            return Placeholder;
        }

        public BoundDeconstructValuePlaceholder FailInference(Binder binder)
        {
            return SetInferredType(TypeSymbolWithAnnotations.Create(binder.CreateErrorType()), binder, success: false);
        }

        protected override BoundExpression ShallowClone()
            => throw ExceptionUtilities.Unreachable;
    }
}
