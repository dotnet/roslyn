// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Determines whether the enter key is passed through to the editor after it has been used to commit a completion item.
    /// </summary>
    public enum EnterKeyRule
    {
        Default = 0,

        /// <summary>
        /// The enter key is never passed through to the editor after it has been used to commit the completion item.
        /// </summary>
        Never,

        /// <summary>
        /// The enter key is always passed through to the editor after it has been used to commit the completion item.
        /// </summary>
        Always,

        /// <summary>
        /// The enter is key only passed through to the editor if the completion item has been fully typed out.
        /// </summary>
        AfterFullyTypedWord
    }
}
