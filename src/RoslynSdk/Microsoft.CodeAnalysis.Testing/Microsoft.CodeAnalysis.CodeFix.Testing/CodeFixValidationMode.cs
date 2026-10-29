// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <inheritdoc cref="CodeActionValidationMode"/>
    [Obsolete("Use " + nameof(CodeActionValidationMode) + " instead.")]
    public enum CodeFixValidationMode
    {
        /// <inheritdoc cref="CodeActionValidationMode.None"/>
        None,

        /// <inheritdoc cref="CodeActionValidationMode.SemanticStructure"/>
        SemanticStructure,

        /// <inheritdoc cref="CodeActionValidationMode.Full"/>
        Full,
    }
}
