// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// This annotation will be used by rename to mark all places where it needs to rename an identifier (token replacement) and where to 
    /// check if the semantics have been changes (conflict detection).
    /// </summary>
    /// <remarks>This annotation should be put on tokens only.</remarks>
    internal class RenameActionAnnotation(
        TextSpan originalSpan,
        bool isRenameLocation,
        string prefix,
        string suffix,
        bool isOriginalTextLocation,
        RenameDeclarationLocationReference[] renameDeclarationLocations,
        bool isNamespaceDeclarationReference,
        bool isInvocationExpression,
        bool isMemberGroupReference) : RenameAnnotation
    {
        /// <summary>
        /// The span this token occupied in the original syntax tree. Can be used to show e.g. conflicts in the UI.
        /// </summary>
        public readonly TextSpan OriginalSpan = originalSpan;

        /// <summary>
        /// A flag indicating whether this is a location that needs to be renamed or just tracked for conflicts.
        /// </summary>
        public readonly bool IsRenameLocation = isRenameLocation;

        /// <summary>
        /// A flag indicating whether the token at this location has the same ValueText then the original name 
        /// of the symbol that gets renamed.
        /// </summary>
        public readonly bool IsOriginalTextLocation = isOriginalTextLocation;

        /// <summary>
        /// When replacing the annotated token this string will be prepended to the token's value. This is used when renaming compiler 
        /// generated fields and methods backing properties (e.g. "get_X" or "_X" for property "X").
        /// </summary>
        public readonly string Prefix = prefix;

        /// <summary>
        /// When replacing the annotated token this string will be appended to the token's value. This is used when renaming compiler 
        /// generated types whose names are derived from user given names (e.g. "XEventHandler" for event "X").
        /// </summary>
        public readonly string Suffix = suffix;

        /// <summary>
        /// A single dimensional array of annotations to verify after rename.
        /// </summary>
        public readonly RenameDeclarationLocationReference[] RenameDeclarationLocationReferences = renameDeclarationLocations;

        /// <summary>
        /// States if this token is a Namespace Declaration Reference
        /// </summary>
        public readonly bool IsNamespaceDeclarationReference = isNamespaceDeclarationReference;

        /// <summary>
        /// States if this token is a member group reference, typically found in NameOf expressions
        /// </summary>
        public readonly bool IsMemberGroupReference = isMemberGroupReference;

        /// <summary>
        /// States if this token is annotated as a part of the Invocation Expression that needs to be checked for the Conflicts
        /// </summary>
        public readonly bool IsInvocationExpression = isInvocationExpression;
    }
}
