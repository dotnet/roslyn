﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class TypeConversions : ConversionsBase
    {
        public TypeConversions(AssemblySymbol corLibrary)
            : this(corLibrary, currentRecursionDepth: 0)
        {
        }

        private TypeConversions(AssemblySymbol corLibrary, int currentRecursionDepth)
            : base(corLibrary, currentRecursionDepth)
        {
        }

        protected override ConversionsBase CreateInstance(int currentRecursionDepth)
        {
            return new TypeConversions(this.corLibrary, currentRecursionDepth);
        }

        public override Conversion GetMethodGroupConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Conversions involving method groups require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        protected override Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Conversions involving interpolated strings require a Binder.
            throw ExceptionUtilities.Unreachable;
        }
    }
}
