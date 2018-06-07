// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class SimplifyTypeNameCodeAction : CodeAction.DocumentChangeAction
    {
        public SimplifyTypeNameCodeAction(
            string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
            : base(title, createChangedDocument, equivalenceKey)
        {
        }
    }
}
