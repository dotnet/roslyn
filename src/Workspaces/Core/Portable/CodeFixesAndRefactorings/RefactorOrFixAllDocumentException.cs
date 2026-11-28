// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal sealed class RefactorOrFixAllDocumentException(Document document, Exception? innerException)
    : Exception(
        string.Format(WorkspacesResources.Error_encountered_while_processing_0, document.FilePath ?? document.Name),
        innerException);
