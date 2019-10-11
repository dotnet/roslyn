// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    public enum VariableDeclarationKind
    {
        /// <summary>
        /// A declaration.
        /// </summary>
        Default = 0,

        /// <summary>
        /// A using declaration.
        /// </summary>
        Using = 1,

        /// <summary>
        /// An asynchronous using declaration.
        /// </summary>
        AsynchronousUsing = 2
    }
}
