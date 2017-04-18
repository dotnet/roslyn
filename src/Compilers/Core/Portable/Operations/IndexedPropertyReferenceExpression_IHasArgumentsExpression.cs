// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal partial class IndexedPropertyReferenceExpression : IHasArgumentsExpression
    {
        /// <summary>
        /// Find the argument supplied for a given parameter of the target method.
        /// </summary>
        /// <param name="parameter">Parameter of the target method.</param>
        /// <returns>Argument corresponding to the parameter.</returns>
        public IArgument GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return this.ArgumentsInParameterOrder[parameter.Ordinal];
        }
    }

    internal partial class LazyIndexedPropertyReferenceExpression : IHasArgumentsExpression
    {
        /// <summary>
        /// Find the argument supplied for a given parameter of the target method.
        /// </summary>
        /// <param name="parameter">Parameter of the target method.</param>
        /// <returns>Argument corresponding to the parameter.</returns>
        public IArgument GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return this.ArgumentsInParameterOrder[parameter.Ordinal];
        }
    }
}
