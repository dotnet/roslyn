// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Structure containing all semantic information about a for each statement.
    /// </summary>
    public struct ForEachStatementInfo : IEquatable<ForEachStatementInfo>
    {
        /// <summary>
        /// Gets the &quot;GetEnumerator&quot; method.
        /// </summary>
        public readonly IMethodSymbol GetEnumeratorMethod;

        /// <summary>
        /// Gets the &quot;MoveNext&quot; method.
        /// </summary>
        public readonly IMethodSymbol MoveNextMethod;

        /// <summary>
        /// Gets the &quot;Current&quot; property.
        /// </summary>
        public readonly IPropertySymbol CurrentProperty;

        /// <summary>
        /// Gets the &quot;Dispose&quot; method.
        /// </summary>
        public readonly IMethodSymbol DisposeMethod;

        /// <summary>
        /// The intermediate type to which the output of the <see cref="CurrentProperty"/> is converted
        /// before being converted to the iteration variable type.
        /// </summary>
        /// <remarks>
        /// As you might hope, for an array, it is the element type of the array.
        /// </remarks>
        public readonly ITypeSymbol ElementType;

        /// <summary>
        /// The conversion from the <see cref="ElementType"/> to the iteration variable type.
        /// </summary>
        /// <remarks>
        /// May be user-defined.
        /// </remarks>
        public readonly Conversion ElementConversion;

        /// <summary>
        /// The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        /// </summary>
        public readonly Conversion CurrentConversion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForEachStatementInfo" /> structure.
        /// </summary>
        internal ForEachStatementInfo(IMethodSymbol getEnumeratorMethod,
                                      IMethodSymbol moveNextMethod,
                                      IPropertySymbol currentProperty,
                                      IMethodSymbol disposeMethod,
                                      ITypeSymbol elementType,
                                      Conversion elementConversion,
                                      Conversion currentConversion)
        {
            this.GetEnumeratorMethod = getEnumeratorMethod;
            this.MoveNextMethod = moveNextMethod;
            this.CurrentProperty = currentProperty;
            this.DisposeMethod = disposeMethod;
            this.ElementType = elementType;
            this.ElementConversion = elementConversion;
            this.CurrentConversion = currentConversion;
        }

        public override bool Equals(object obj)
        {
            return obj is ForEachStatementInfo && Equals((ForEachStatementInfo)obj);
        }

        public bool Equals(ForEachStatementInfo other)
        {
            return object.Equals(this.GetEnumeratorMethod, other.GetEnumeratorMethod)
                && object.Equals(this.MoveNextMethod, other.MoveNextMethod)
                && object.Equals(this.CurrentProperty, other.CurrentProperty)
                && object.Equals(this.DisposeMethod, other.DisposeMethod)
                && object.Equals(this.ElementType, other.ElementType)
                && this.ElementConversion == other.ElementConversion
                && this.CurrentConversion == other.CurrentConversion;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetEnumeratorMethod,
                   Hash.Combine(MoveNextMethod,
                   Hash.Combine(CurrentProperty,
                   Hash.Combine(DisposeMethod,
                   Hash.Combine(ElementType,
                   Hash.Combine(ElementConversion.GetHashCode(),
                                CurrentConversion.GetHashCode()))))));
        }
    }
}