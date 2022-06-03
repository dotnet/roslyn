// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        public override Conversion GetMethodGroupDelegateConversion(BoundMethodGroup source, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Conversions involving method groups require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        public override Conversion GetMethodGroupFunctionPointerConversion(BoundMethodGroup source, FunctionPointerTypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Conversions involving method groups require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        public override Conversion GetStackAllocConversion(BoundStackAllocArrayCreation sourceExpression, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Conversions involving stackalloc expressions require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        protected override Conversion GetInterpolatedStringConversion(BoundExpression source, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Conversions involving interpolated strings require a Binder.
            throw ExceptionUtilities.Unreachable;
        }

        protected override CSharpCompilation Compilation => null;
    }
}
