// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class QualifyMemberAccessCodeAction : CodeAction.DocumentChangeAction
    {
        public QualifyMemberAccessCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id)
            : base(title, createChangedDocument, id)
        {
        }
    }
}
