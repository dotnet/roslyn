// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
     /// Used so we can mock out our solution load complete behavior in unit tests.
     /// </summary>
    internal interface IPackageSearchSolutionLoadCompleteService
    {
        bool SolutionLoadComplete { get; }
    }
}
