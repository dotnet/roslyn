// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Structure containing all semantic information about a for each statement.
    /// </summary>
    public struct ForEachStatementInfo : IEquatable<ForEachStatementInfo>
    {
        /// <summary>
        /// Whether this is an asynchronous foreach.
        /// </summary>
        public bool IsAsynchronous { get; }

        /// <summary>
        /// Gets the &quot;GetEnumerator&quot; method.
        /// </summary>
        public IMethodSymbol GetEnumeratorMethod { get; }

        /// <summary>
        /// Gets the &quot;MoveNext&quot; method (or &quot;MoveNextAsync&quot; in an asynchronous foreach).
        /// </summary>
        public IMethodSymbol MoveNextMethod { get; }

        /// <summary>
        /// Gets the &quot;Current&quot; property.
        /// </summary>
        public IPropertySymbol CurrentProperty { get; }

        /// <summary>
        /// Gets the &quot;Dispose&quot; method (or &quot;DisposeAsync&quot; in an asynchronous foreach).
        /// </summary>
        public IMethodSymbol DisposeMethod { get; }

        /// <summary>
        /// The intermediate type to which the output of the <see cref="CurrentProperty"/> is converted
        /// before being converted to the iteration variable type.
        /// </summary>
        /// <remarks>
        /// As you might hope, for an array, it is the element type of the array.
        /// </remarks>
        public ITypeSymbol ElementType { get; }

        /// <summary>
        /// The conversion from the <see cref="ElementType"/> to the iteration variable type.
        /// </summary>
        /// <remarks>
        /// May be user-defined.
        /// </remarks>
        public Conversion ElementConversion { get; }

        /// <summary>
        /// The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        /// </summary>
        public Conversion CurrentConversion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForEachStatementInfo" /> structure.
        /// </summary>
        internal ForEachStatementInfo(bool isAsync,
                                      IMethodSymbol getEnumeratorMethod,
                                      IMethodSymbol moveNextMethod,
                                      IPropertySymbol currentProperty,
                                      IMethodSymbol disposeMethod,
                                      ITypeSymbol elementType,
                                      Conversion elementConversion,
                                      Conversion currentConversion)
        {
            this.IsAsynchronous = isAsync;
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
            return this is { IsAsynchronous: other.IsAsynchronous, ElementConversion: other.ElementConversion, CurrentConversion: other.CurrentConversion } && object.Equals(this.GetEnumeratorMethod, other.GetEnumeratorMethod)
&& object.Equals(this.MoveNextMethod, other.MoveNextMethod)
&& object.Equals(this.CurrentProperty, other.CurrentProperty)
&& object.Equals(this.DisposeMethod, other.DisposeMethod)
&& object.Equals(this.ElementType, other.ElementType)
;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(IsAsynchronous,
                   Hash.Combine(GetEnumeratorMethod,
                   Hash.Combine(MoveNextMethod,
                   Hash.Combine(CurrentProperty,
                   Hash.Combine(DisposeMethod,
                   Hash.Combine(ElementType,
                   Hash.Combine(ElementConversion.GetHashCode(),
                                CurrentConversion.GetHashCode())))))));
        }
    }
}
