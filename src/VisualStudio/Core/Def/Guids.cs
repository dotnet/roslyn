// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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

        public const string CSharpProjectRootIdString = "C7FEDB89-B36D-4a62-93F4-DC7A95999921";

        // from debugger\idl\makeapi\guid.c  
        public const string CSharpDebuggerLanguageIdString = "3f5162f8-07c6-11d3-9053-00c04fa302a1";

        public static readonly Guid CSharpPackageId = new Guid(CSharpPackageIdString);
        public static readonly Guid CSharpProjectId = new Guid(CSharpProjectIdString);
        public static readonly Guid CSharpLanguageServiceId = new Guid(CSharpLanguageServiceIdString);
        public static readonly Guid CSharpEditorFactoryId = new Guid(CSharpEditorFactoryIdString);
        public static readonly Guid CSharpCodePageEditorFactoryId = new Guid(CSharpCodePageEditorFactoryIdString);
        public static readonly Guid CSharpCommandSetId = new Guid(CSharpCommandSetIdString);     // guidCSharpCmdId
        public static readonly Guid CSharpGroupId = new Guid(CSharpGroupIdString);               // guidCSharpGrpId
        public static readonly Guid CSharpRefactorIconId = new Guid(CSharpRefactorIconIdString); // guidCSharpRefactorIcon
        public static readonly Guid CSharpGenerateIconId = new Guid(CSharpGenerateIconIdString); // guidCSharpGenerateIcon
        public static readonly Guid CSharpOrganizeIconId = new Guid(CSharpOrganizeIconIdString); // guidCSharpOrganizeIcon
        public static readonly Guid CSharpDebuggerLanguageId = new Guid(CSharpDebuggerLanguageIdString);
        public static readonly Guid CSharpLibraryId = new Guid(CSharpLibraryIdString);

        // option page guids from csharp\rad\pkg\guids.h
        public const string CSharpOptionPageAdvancedIdString = "8FD0B177-B244-4A97-8E37-6FB7B27DE3AF";
        public const string CSharpOptionPageIntelliSenseIdString = "EDE66829-7A36-4c5d-8E20-9290195DCF80";
        public const string CSharpOptionPageFormattingIdString = "3EB2CC0B-033E-4D75-B26A-B2362C25227E";
        public const string CSharpOptionPageFormattingIndentationIdString = "5E21D017-6D2A-4114-A1F1-C923F001CBBB";
        public const string CSharpOptionPageFormattingNewLinesIdString = "607D8062-68D1-41E4-9A35-B5E7F14D0481";
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

        public static readonly Guid VisualBasicPackageId = new Guid(VisualBasicPackageIdString);
        public static readonly Guid VisualBasicCompilerServiceId = new Guid(VisualBasicCompilerServiceIdString);
        public static readonly Guid VisualBasicLanguageServiceId = new Guid(VisualBasicLanguageServiceIdString);
        public static readonly Guid VisualBasicEditorFactoryId = new Guid(VisualBasicEditorFactoryIdString);
        public static readonly Guid VisualBasicCodePageEditorFactoryId = new Guid(VisualBasicCodePageEditorFactoryIdString);
        public static readonly Guid VisualBasicLibraryId = new Guid(VisualBasicLibraryIdString);

        public static readonly Guid VisualBasicProjectId = new Guid(VisualBasicProjectIdString);

        // from debugger\idl\makeapi\guid.c  
        public static readonly Guid VisualBasicDebuggerLanguageId = new Guid(VisualBasicDebuggerLanguageIdString);

        // option page guid from setupauthoring\vb\components\vblanguageservice.pkgdef
        public const string VisualBasicOptionPageVBSpecificIdString = "F1E1021E-A781-4862-9F4B-88746A288A67";

        // from vscommon\inc\textmgruuids.h
        public const string TextManagerPackageString = "F5E7E720-1401-11D1-883B-0000F87579D2";

        // Roslyn guids
        public const string RoslynPackageIdString = "6cf2e545-6109-4730-8883-cf43d7aec3e1";
        public const string RoslynCommandSetIdString = "9ed8fbd1-02d6-4223-a99c-a938f97e6dbe";
        public const string RoslynLibraryIdString = "82fab260-0c56-4f02-b186-508358588fee";
        public const string RoslynGroupIdString = "b61e1a20-8c13-49a9-a727-a0ec091647dd";

        public const string RoslynOptionPageFeatureManagerComponentsIdString = "6F738951-348C-4816-9BA4-F60D92D3E98E";
        public const string RoslynOptionPageFeatureManagerFeaturesIdString = "67989704-F8D7-454A-9053-8E1D3CFF679C";
        public const string RoslynOptionPagePerformanceFunctionIdIdString = "0C537218-3BDD-4CC8-AC4B-CEC152D4871A";
        public const string RoslynOptionPagePerformanceLoggersIdString = "236AC96F-A60D-4BD6-A480-D315151EDC2B";
        public const string RoslynOptionPageInternalDiagnosticsIdString = "48993C4C-C619-42AD-B1C8-79378AD8BEF2";
        public const string RoslynOptionPageInternalSolutionCrawlerIdString = "9702D3BD-F06C-4A6A-974B-7D0C2BC89A72";

        public static readonly Guid RoslynPackageId = new Guid(RoslynPackageIdString);
        public static readonly Guid RoslynCommandSetId = new Guid(RoslynCommandSetIdString);
        public static readonly Guid RoslynGroupId = new Guid(RoslynGroupIdString);
        public static readonly Guid RoslynLibraryId = new Guid(RoslynLibraryIdString);

        // TODO: Remove pending https://github.com/dotnet/roslyn/issues/8927 .
        // Interactive guids
        public const string InteractiveCommandSetIdString = "00B8868B-F9F5-4970-A048-410B05508506";
        public static readonly Guid InteractiveCommandSetId = new Guid(InteractiveCommandSetIdString);

        public static readonly string CSharpInteractiveCommandSetIdString = "1492DB0A-85A2-4E43-BF0D-CE55B89A8CC6";
        public static readonly Guid CSharpInteractiveCommandSetId = new Guid(CSharpInteractiveCommandSetIdString);

        public static readonly string VisualBasicInteractiveCommandSetIdString = "93DF185E-D75B-4FDB-9D47-E90F111971C5";
        public static readonly Guid VisualBasicInteractiveCommandSetId = new Guid(VisualBasicInteractiveCommandSetIdString);
    }
}
