// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.CodeMapping;

internal sealed partial class CSharpMapCodeService
{
    /// <summary>
    /// The level of the scope where this node is located.
    /// NOTE: The scope does not represent where this node will be inserted.
    /// The scope represents the hierarchy this node has.
    /// For example, a Class Scope node means that it needs to be placed next to
    /// other classes or interfaces, and not inside them.
    /// NOTE: The order in which these scopes are setup on this enum matter.
    /// They should go from lower to higher in terms of what goes inside what.
    /// For example, Class is the highest scope here, because the class will usually contain methods, and methods cannot contain classes.
    /// Same with statements.
    /// </summary>
    internal enum Scope
    {
        /// <summary>
        /// Unknown scope.
        /// </summary>
        None,

        /// <summary>
        /// Statement represents all the nodes that are scoped statements such as
        /// if statements, while statements, class declaration statements.
        /// </summary>
        Statement,

        /// <summary>
        /// Method represents all elements that will be at level of method, with scope.
        /// Like method, and constructor.
        /// </summary>
        Method,

        /// <summary>
        /// Class represents all the nodes that can be set at the level of class, like interface,
        /// enum, class, etc.
        /// </summary>
        Class,
    }
}
