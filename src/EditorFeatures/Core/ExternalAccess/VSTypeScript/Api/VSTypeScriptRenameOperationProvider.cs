// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptRenameOperationFactory
    {
        public static CodeActionOperation CreateRenameOperation(DocumentId documentId, int position)
            => new StartInlineRenameSessionOperation(documentId, position);
    }
}
