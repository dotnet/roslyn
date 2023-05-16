// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class Guids
    {
        public const string CSharpPackageIdString = "13c3bbb4-f18f-4111-9f54-a0fb010d9194";
        public const string CSharpProjectIdString = "fae04ec0-301f-11d3-bf4b-00c04f79efbc";
        public const string CSharpLanguageServiceIdString = "694dd9b6-b865-4c5b-ad85-86356e9c88dc";
        public const string CSharpEditorFactoryIdString = "a6c744a8-0e4a-4fc6-886a-064283054674";
        public const string CSharpCodePageEditorFactoryIdString = "08467b34-b90f-4d91-bdca-eb8c8cf3033a";
        public const string CSharpCommandSetIdString = "d91af2f7-61f6-4d90-be23-d057d2ea961b";
        public const string CSharpGroupIdString = "5d7e7f65-a63f-46ee-84f1-990b2cab23f9";
        public const string CSharpRefactorIconIdString = "b293db8b-3c72-4720-9966-2083af84dd82";
        public const string CSharpGenerateIconIdString = "ac9a0910-d9fd-4f2e-b9a1-acdc5d514437";
        public const string CSharpOrganizeIconIdString = "9420a4b2-b48b-449d-a4c0-335d6e864b82";
        public const string CSharpLibraryIdString = "58F1BAD0-2288-45b9-AC3A-D56398F7781D";
        public const string CSharpReplPackageIdString = "c5edd1ee-c43b-4360-9ce4-6b993ca12897";

        /// <summary>
        /// A <see cref="UIContext"/> that is set if there is a C# project in the <see cref="VisualStudioWorkspace"/>.
        /// </summary>
        public const string CSharpProjectExistsInWorkspaceUIContextString = "CA719A03-D55C-48F9-85DE-D934346E7F70";
        public static readonly Guid CSharpProjectExistsInWorkspaceUIContext = new(CSharpProjectExistsInWorkspaceUIContextString);

        public const string CSharpProjectRootIdString = "C7FEDB89-B36D-4a62-93F4-DC7A95999921";

        // from debugger\idl\makeapi\guid.c  
        public const string CSharpDebuggerLanguageIdString = "3f5162f8-07c6-11d3-9053-00c04fa302a1";

        public static readonly Guid CSharpPackageId = new(CSharpPackageIdString);
        public static readonly Guid CSharpProjectId = new(CSharpProjectIdString);
        public static readonly Guid CSharpLanguageServiceId = new(CSharpLanguageServiceIdString);
        public static readonly Guid CSharpEditorFactoryId = new(CSharpEditorFactoryIdString);
        public static readonly Guid CSharpCodePageEditorFactoryId = new(CSharpCodePageEditorFactoryIdString);
        public static readonly Guid CSharpCommandSetId = new(CSharpCommandSetIdString);     // guidCSharpCmdId
        public static readonly Guid CSharpGroupId = new(CSharpGroupIdString);               // guidCSharpGrpId
        public static readonly Guid CSharpRefactorIconId = new(CSharpRefactorIconIdString); // guidCSharpRefactorIcon
        public static readonly Guid CSharpGenerateIconId = new(CSharpGenerateIconIdString); // guidCSharpGenerateIcon
        public static readonly Guid CSharpOrganizeIconId = new(CSharpOrganizeIconIdString); // guidCSharpOrganizeIcon
        public static readonly Guid CSharpDebuggerLanguageId = new(CSharpDebuggerLanguageIdString);
        public static readonly Guid CSharpLibraryId = new(CSharpLibraryIdString);

        // option page guids from csharp\rad\pkg\guids.h
        public const string CSharpOptionPageAdvancedIdString = "8FD0B177-B244-4A97-8E37-6FB7B27DE3AF";
        public const string CSharpOptionPageNamingStyleIdString = "294FBC9C-EF70-4AA0-BD4F-EB0C6A5908D7";
        public const string CSharpOptionPageIntelliSenseIdString = "EDE66829-7A36-4c5d-8E20-9290195DCF80";
        public const string CSharpOptionPageCodeStyleIdString = "EAE577A7-ACB9-40F5-A7B1-D2878C3C7D6F";
        public const string CSharpOptionPageFormattingGeneralIdString = "DA0446DD-55BA-401F-A364-7D3238412AE4";
        public const string CSharpOptionPageFormattingIndentationIdString = "5E21D017-6D2A-4114-A1F1-C923F001CBBB";
        public const string CSharpOptionPageFormattingNewLinesIdString = "EADC6AD3-91D4-3CC8-BE96-3CDE7D3080F0";
        public const string CSharpOptionPageFormattingSpacingIdString = "234FB566-73DD-4612-8DE4-29031FF27052";
        public const string CSharpOptionPageFormattingWrappingIdString = "8E334D9C-B7DC-4CF3-B7B7-014B831FE76B";

        public const string VisualBasicPackageIdString = "574fc912-f74f-4b4e-92c3-f695c208a2bb";

        public const string VisualBasicReplPackageIdString = "F5C61C13-7037-4C50-98E6-ACC313359A34";

        public const string VbCompilerProjectIdString = "12C8A7D2-4681-11D2-B48A-0000F87572EB";

        public const string VisualBasicProjectIdString = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

        public const string VisualBasicCompilerServiceIdString = "019971d6-4685-11d2-b48a-0000f87572eb";
        public const string VisualBasicLanguageServiceIdString = "e34acdc0-baae-11d0-88bf-00a0c9110049";
        public const string VisualBasicEditorFactoryIdString = "2c015c70-c72c-11d0-88c3-00a0c9110049";
        public const string VisualBasicCodePageEditorFactoryIdString = "6c33e1aa-1401-4536-ab67-0e21e6e569da";
        public const string VisualBasicDebuggerLanguageIdString = "3a12d0b8-c26c-11d0-b442-00a0244a1dd2";
        public const string VisualBasicLibraryIdString = "414AC972-9829-4b6a-A8D7-A08152FEB8AA";
        public const string VisualBasicOptionPageCodeStyleIdString = "10C168E1-3470-448A-A1AC-73D6BC070750";

        /// <summary>
        /// A <see cref="UIContext"/> that is set if there is a Visual Basic project in the <see cref="VisualStudioWorkspace"/>.
        /// </summary>
        public const string VisualBasicProjectExistsInWorkspaceUIContextString = "EEC3DF0D-6D3F-4544-ABF9-8E26E6A90275";
        public static readonly Guid VisualBasicProjectExistsInWorkspaceUIContext = new(VisualBasicProjectExistsInWorkspaceUIContextString);

        public static readonly Guid VisualBasicPackageId = new(VisualBasicPackageIdString);
        public static readonly Guid VisualBasicCompilerServiceId = new(VisualBasicCompilerServiceIdString);
        public static readonly Guid VisualBasicLanguageServiceId = new(VisualBasicLanguageServiceIdString);
        public static readonly Guid VisualBasicEditorFactoryId = new(VisualBasicEditorFactoryIdString);
        public static readonly Guid VisualBasicCodePageEditorFactoryId = new(VisualBasicCodePageEditorFactoryIdString);
        public static readonly Guid VisualBasicLibraryId = new(VisualBasicLibraryIdString);

        public static readonly Guid VisualBasicProjectId = new(VisualBasicProjectIdString);

        // from debugger\idl\makeapi\guid.c  
        public static readonly Guid VisualBasicDebuggerLanguageId = new(VisualBasicDebuggerLanguageIdString);

        // option page guid from setupauthoring\vb\components\vblanguageservice.pkgdef
        public const string VisualBasicOptionPageVBSpecificIdString = "F1E1021E-A781-4862-9F4B-88746A288A67";
        public const string VisualBasicOptionPageNamingStyleIdString = "BCA454E0-95E4-4877-B4CB-B1D642B7BAFA";
        public const string VisualBasicOptionPageIntelliSenseIdString = "04460A3B-1B5F-4402-BC6D-89A4F6F0A8D7";

        public const string FSharpPackageIdString = "871D2A70-12A2-4e42-9440-425DD92A4116";

        public static readonly Guid FSharpPackageId = new(FSharpPackageIdString);

        // from vscommon\inc\textmgruuids.h
        public const string TextManagerPackageString = "F5E7E720-1401-11D1-883B-0000F87579D2";

        // Roslyn guids
        public const string RoslynPackageIdString = "6cf2e545-6109-4730-8883-cf43d7aec3e1";
        public const string RoslynCommandSetIdString = "9ed8fbd1-02d6-4223-a99c-a938f97e6dbe";
        public const string RoslynGroupIdString = "b61e1a20-8c13-49a9-a727-a0ec091647dd";

        public const string RoslynOptionPageFeatureManagerComponentsIdString = "6F738951-348C-4816-9BA4-F60D92D3E98E";
        public const string RoslynOptionPageFeatureManagerFeaturesIdString = "67989704-F8D7-454A-9053-8E1D3CFF679C";
        public const string RoslynOptionPagePerformanceFunctionIdIdString = "0C537218-3BDD-4CC8-AC4B-CEC152D4871A";
        public const string RoslynOptionPagePerformanceLoggersIdString = "236AC96F-A60D-4BD6-A480-D315151EDC2B";
        public const string RoslynOptionPageInternalDiagnosticsIdString = "48993C4C-C619-42AD-B1C8-79378AD8BEF2";
        public const string RoslynOptionPageInternalSolutionCrawlerIdString = "9702D3BD-F06C-4A6A-974B-7D0C2BC89A72";

        public static readonly Guid RoslynPackageId = new(RoslynPackageIdString);
        public static readonly Guid RoslynCommandSetId = new(RoslynCommandSetIdString);
        public static readonly Guid RoslynGroupId = new(RoslynGroupIdString);

        public const string ValueTrackingToolWindowIdString = "60a19d42-2dd7-43f3-be90-c7a9cb7d28f4";
        public static readonly Guid ValueTrackingToolWindowId = new(ValueTrackingToolWindowIdString);

        public const string StackTraceExplorerToolWindowIdString = "7FF2AB69-0A20-4BF5-BAEF-24D9EB6969E1";
        public static readonly Guid StackTraceExplorerToolWindowId = new(StackTraceExplorerToolWindowIdString);
        public const string StackTraceExplorerCommandIdString = "FB190424-4DFF-43DB-8CCA-E32D1CE8A5CA";
        public static readonly Guid StackTraceExplorerCommandId = new(StackTraceExplorerCommandIdString);

        public const string DocumentOutlineSearchCategoryIdString = "C80E47CF-B95A-46D4-8BE4-6ADA02888333";
        public static readonly Guid DocumentOutlineSearchCategoryId = new(DocumentOutlineSearchCategoryIdString);

        // TODO: Remove pending https://github.com/dotnet/roslyn/issues/8927 .
        // Interactive guids
        public const string InteractiveCommandSetIdString = "00B8868B-F9F5-4970-A048-410B05508506";
        public static readonly Guid InteractiveCommandSetId = new(InteractiveCommandSetIdString);

        /// <summary>
        /// The package GUID for GlobalHubClientPackage, which proffers ServiceHub brokered services in Visual Studio.
        /// </summary>
        public static readonly Guid GlobalHubClientPackageGuid = new("11AD60FC-6D87-4674-8F88-9ABE79176CBE");
    }
}
