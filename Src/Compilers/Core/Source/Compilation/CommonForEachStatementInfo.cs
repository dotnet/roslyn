using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Structure containing all semantic information about a for each statement.
    /// </summary>
    public struct CommonForEachStatementInfo
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
        public readonly CommonConversion ElementConversion;

        /// <summary>
        /// The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        /// </summary>
        public readonly CommonConversion CurrentConversion;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonForEachStatementInfo" /> structure.
        /// </summary>
        internal CommonForEachStatementInfo(IMethodSymbol getEnumeratorMethod,
                                            IMethodSymbol moveNextMethod,
                                            IPropertySymbol currentProperty,
                                            IMethodSymbol disposeMethod,
                                            ITypeSymbol elementType,
                                            CommonConversion elementConversion,
                                            CommonConversion currentConversion)
            : this()
        {
            this.GetEnumeratorMethod = getEnumeratorMethod;
            this.MoveNextMethod = moveNextMethod;
            this.CurrentProperty = currentProperty;
            this.DisposeMethod = disposeMethod;
            this.ElementType = elementType;
            this.ElementConversion = elementConversion;
            this.CurrentConversion = currentConversion;
        }
    }
}