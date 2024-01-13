// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the nullability of values that can be assigned
    /// to an expression used as an lvalue.
    /// </summary>
    // Review docs: https://github.com/dotnet/roslyn/issues/35046
    public enum NullableAnnotation : byte
    {
        /// <summary>
        /// The expression has not been analyzed, or the syntax is
        /// not an expression (such as a statement).
        /// <para>There are a few different reasons the expression could have not been analyzed:
        /// </para>
        /// <list type="number">
        /// <item><description>The symbol producing the expression comes from a method that has not been annotated, such as invoking a C# 7.3 or earlier method, or a method in this compilation that is in a disabled context.</description></item>
        /// <item><description>Nullable is completely disabled in this compilation.</description></item>
        /// </list>
        /// </summary>
        None = 0,
        /// <summary>
        /// The expression is not annotated (does not have a ?).
        /// </summary>
        NotAnnotated,
        /// <summary>
        /// The expression is annotated (does have a ?).
        /// </summary>
        Annotated,
    }
}
