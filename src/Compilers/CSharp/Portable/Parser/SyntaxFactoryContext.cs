// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Because syntax nodes need to be constructed with context information - to allow us to 
    /// determine whether or not they can be reused during incremental parsing - the syntax
    /// factory needs a view of some internal parser state.
    /// </summary>
    /// <remarks>
    /// Read-only outside SyntaxParser (not enforced for perf reasons).
    /// Reference type so that the factory stays up-to-date.
    /// </remarks>
    internal class SyntaxFactoryContext
    {
        /// <summary>
        /// If a method goes from async to non-async, or vice versa, then every occurrence of "await"
        /// within the method (but not within a lambda) needs to be reinterpreted, to determine whether
        /// it is a keyword or an identifier.
        /// </summary>
        internal bool IsInAsync;

        internal int QueryDepth;

        /// <summary>
        /// If the end of a query expression statement is commented out, then the following statement may
        /// appear to be part of the query.  When this occurs, identifiers within the following statement
        /// may need to be reinterpreted as query keywords.
        /// </summary>
        internal bool IsInQuery
        {
            get { return QueryDepth > 0; }
        }

        internal bool IsInReplace;
    }
}
