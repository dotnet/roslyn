// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// This interface only exists to maintain an overload of <see cref="DocumentProvider.TryGetDocumentForFile(IVisualStudioHostProject, string, CodeAnalysis.SourceCodeKind, System.Func{Text.ITextBuffer, bool}, System.Func{uint, System.Collections.Generic.IReadOnlyList{string}}, System.EventHandler, System.EventHandler{bool}, System.EventHandler{bool})"/>.
    /// </summary>
    [Obsolete("This overload is a compatibility shim for TypeScript; please do not use it.")]
    internal interface IVisualStudioHostProject
    {
        ProjectId Id { get; }
    }
}
