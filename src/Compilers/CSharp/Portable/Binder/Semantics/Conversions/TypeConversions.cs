// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class TypeConversions : ConversionsBase
    {
        public TypeConversions(AssemblySymbol corLibrary, bool includeNullability = false)
            : this(corLibrary, currentRecursionDepth: 0, includeNullability: includeNullability, otherNullabilityOpt: null)
        {
        }

        private TypeConversions(AssemblySymbol corLibrary, int currentRecursionDepth, bool includeNullability, TypeConversions otherNullabilityOpt)
            : base(corLibrary, currentRecursionDepth, includeNullability, otherNullabilityOpt)
        {
        }

        protected override ConversionsBase CreateInstance(int currentRecursionDepth)
        {
            return new TypeConversions(this.corLibrary, currentRecursionDepth, IncludeNullability, otherNullabilityOpt: null);
        }

        protected override ConversionsBase WithNullabilityCore(bool includeNullability)
        {
            Debug.Assert(IncludeNullability != includeNullability);
            return new TypeConversions(corLibrary, currentRecursionDepth, includeNullability, this);
        }

        public override Conversion GetMethodGroupConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Conversions involving method groups require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        public override Conversion GetStackAllocConversion(BoundStackAllocArrayCreation sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Conversions involving stackalloc expressions require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        protected override Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Conversions involving interpolated strings require a Binder.
            throw ExceptionUtilities.Unreachable;
        }
    }
}
