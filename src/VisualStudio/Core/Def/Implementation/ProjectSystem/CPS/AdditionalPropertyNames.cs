// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// A class that provides constants for common MSBuild property names.
    /// </summary>
    internal static class AdditionalPropertyNames
    {
        // All supported properties can be found in dotnet/project-system repo
        // https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Rules/LanguageService.xaml

        public const string RootNamespace = nameof(RootNamespace);
        public const string MaxSupportedLangVersion = nameof(MaxSupportedLangVersion);
        public const string RunAnalyzers = nameof(RunAnalyzers);
        public const string RunAnalyzersDuringLiveAnalysis = nameof(RunAnalyzersDuringLiveAnalysis);
    }
}
