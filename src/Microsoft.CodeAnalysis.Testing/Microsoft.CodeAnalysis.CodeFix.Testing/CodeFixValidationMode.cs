// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
