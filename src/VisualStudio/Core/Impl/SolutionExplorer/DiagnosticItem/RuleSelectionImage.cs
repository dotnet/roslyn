// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

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
