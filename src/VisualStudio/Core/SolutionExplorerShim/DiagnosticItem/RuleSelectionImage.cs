// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    // Copied from src\edev\StaticAnalysis\StanPackage\Core\StanCoreImages.cs.
    internal enum RuleSelectionImage : int
    {
        NoImage = -1,
        Error = 0,
        Warning = 1,
        Info = 2,
        MultipleError = 3,
        MultipleWarning = 4,
        MultipleInfo = 5,
        MultipleMixed = 6,
        None = 7,
        MultipleNone = 8,
        Hidden = 9,
        MultipleHidden = 10,
    }
}
