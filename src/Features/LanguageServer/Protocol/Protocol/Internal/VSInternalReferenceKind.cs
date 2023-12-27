// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Enum which represents the various reference kinds.
    /// </summary>
    internal enum VSInternalReferenceKind
    {
        /// <summary>
        /// Reference in inactive code block.
        /// </summary>
        Inactive,

        /// <summary>
        /// Reference in comment.
        /// </summary>
        Comment,

        /// <summary>
        /// Reference in a string.
        /// </summary>
        String,

        /// <summary>
        /// Read operation on the reference.
        /// </summary>
        Read,

        /// <summary>
        /// Write operation on the reference.
        /// </summary>
        Write,

        /// <summary>
        /// Reference.
        /// </summary>
        Reference,

        /// <summary>
        /// Name.
        /// </summary>
        Name,

        /// <summary>
        /// Qualified.
        /// </summary>
        Qualified,

        /// <summary>
        /// Type Argument.
        /// </summary>
        TypeArgument,

        /// <summary>
        /// Type Constraint.
        /// </summary>
        TypeConstraint,

        /// <summary>
        /// Base Type.
        /// </summary>
        BaseType,

        /// <summary>
        /// Construct.
        /// </summary>
        Constructor,

        /// <summary>
        /// Destructor.
        /// </summary>
        Destructor,

        /// <summary>
        /// Import.
        /// </summary>
        Import,

        /// <summary>
        /// Declaration.
        /// </summary>
        Declaration,

        /// <summary>
        /// Address of.
        /// </summary>
        AddressOf,

        /// <summary>
        /// Not a reference.
        /// </summary>
        NotReference,

        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown,
    }
}
