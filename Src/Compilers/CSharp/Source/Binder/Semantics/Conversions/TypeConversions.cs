// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
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
    }
}