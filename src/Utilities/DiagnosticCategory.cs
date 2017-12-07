// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities
{
    internal static class DiagnosticCategory
    {
        public static readonly string Design = AnalyzerUtilitiesResources.CategoryDesign;
        public static readonly string Globalization = AnalyzerUtilitiesResources.CategoryGlobalization;
        public static readonly string Interoperability = AnalyzerUtilitiesResources.CategoryInteroperability;
        public static readonly string Mobility = AnalyzerUtilitiesResources.CategoryMobility;
        public static readonly string Performance = AnalyzerUtilitiesResources.CategoryPerformance;
        public static readonly string Reliability = AnalyzerUtilitiesResources.CategoryReliability;
        public static readonly string Security = AnalyzerUtilitiesResources.CategorySecurity;
        public static readonly string Usage = AnalyzerUtilitiesResources.CategoryUsage;
        public static readonly string Naming = AnalyzerUtilitiesResources.CategoryNaming;
        public static readonly string Library = AnalyzerUtilitiesResources.CategoryLibrary;
        public static readonly string Documentation = AnalyzerUtilitiesResources.CategoryDocumentation;
        public static readonly string Maintainability = AnalyzerUtilitiesResources.CategoryMaintainability;

        public const string RoslyDiagnosticsDesign = nameof(RoslyDiagnosticsDesign);
        public const string RoslyDiagnosticsMaintainability = nameof(RoslyDiagnosticsMaintainability);
        public const string RoslyDiagnosticsPerformance = nameof(RoslyDiagnosticsPerformance);
        public const string RoslyDiagnosticsReliability = nameof(RoslyDiagnosticsReliability);
        public const string RoslyDiagnosticsUsage = nameof(RoslyDiagnosticsUsage);

        public const string MicrosoftCodeAnalysisCorrectness = nameof(MicrosoftCodeAnalysisCorrectness);
        public const string MicrosoftCodeAnalysisDesign = nameof(MicrosoftCodeAnalysisDesign);
        public const string MicrosoftCodeAnalysisDocumentation = nameof(MicrosoftCodeAnalysisDocumentation);
        public const string MicrosoftCodeAnalysisLocalization = nameof(MicrosoftCodeAnalysisLocalization);
        public const string MicrosoftCodeAnalysisPerformance = nameof(MicrosoftCodeAnalysisPerformance);
        public const string MicrosoftCodeAnalysisCompatibility = nameof(MicrosoftCodeAnalysisCompatibility);
    }
}