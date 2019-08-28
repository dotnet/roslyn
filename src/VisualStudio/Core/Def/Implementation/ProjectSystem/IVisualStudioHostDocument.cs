// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal interface IVisualStudioHostDocument
    {
        /// <summary>
        /// The workspace document Id for this document.
        /// </summary>
        DocumentId Id { get; }
    }
}
