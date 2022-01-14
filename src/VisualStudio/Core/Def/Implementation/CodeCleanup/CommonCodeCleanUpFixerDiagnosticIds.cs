// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    internal static class CommonCodeCleanUpFixerDiagnosticIds
    {
        [Export]
        [FixId(IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_this_or_Me_qualification))]
        public static readonly FixIdDefinition? AddQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_this_or_Me_qualification))]
        public static readonly FixIdDefinition? RemoveQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddBracesDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Add_accessibility_modifiers))]
        public static readonly FixIdDefinition? AddAccessibilityModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Order_modifiers))]
        public static readonly FixIdDefinition? OrderModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Name(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(AnalyzersResources), nameof(AnalyzersResources.Make_field_readonly))]
        public static readonly FixIdDefinition? MakeFieldReadonlyDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Order(After = IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Remove_unnecessary_casts))]
        public static readonly FixIdDefinition? RemoveUnnecessaryCastDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition? UseObjectInitializerDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition? UseCollectionInitializerDiagnosticId;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [Name(AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Format_document))]
        public static readonly FixIdDefinition? FormatDocument;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [Name(AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [Order(After = AbstractCodeCleanUpFixer.FormatDocumentFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Remove_unnecessary_usings))]
        public static readonly FixIdDefinition? RemoveUnusedImports;

        [Export]
        [FixId(AbstractCodeCleanUpFixer.SortImportsFixId)]
        [Name(AbstractCodeCleanUpFixer.SortImportsFixId)]
        [Order(After = AbstractCodeCleanUpFixer.RemoveUnusedImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(ServicesVSResources), nameof(ServicesVSResources.Sort_usings))]
        public static readonly FixIdDefinition? SortImports;

        [Export]
        [FixId(IDEDiagnosticIds.FileHeaderMismatch)]
        [Name(IDEDiagnosticIds.FileHeaderMismatch)]
        [Order(After = AbstractCodeCleanUpFixer.SortImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.Apply_file_header_preferences))]
        public static readonly FixIdDefinition? FileHeaderMismatch;
    }
}
