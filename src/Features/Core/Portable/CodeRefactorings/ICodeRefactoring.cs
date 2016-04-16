// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// For testing purposes.
    /// </summary>
    internal interface ICodeRefactoring
    {
        IEnumerable<CodeAction> Actions { get; }
    }
}
