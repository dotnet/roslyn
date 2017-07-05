// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    /// <summary>
    /// Contains navigate-to specific operations that depend on the version of the
    /// host they're running under.
    /// </summary>
    internal interface INavigateToHostVersionService
    {
        bool GetSearchCurrentDocument(INavigateToOptions options);
        INavigateToItemDisplayFactory CreateDisplayFactory();
    }
}
