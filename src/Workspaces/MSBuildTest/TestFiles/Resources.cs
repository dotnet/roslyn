// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.UnitTests.TestFiles
{
    public static class Resources
    {
        private static Stream GetResourceStream(string name)
        {
            var resourceName = $"Microsoft.CodeAnalysis.MSBuild.UnitTests.Resources.{name}";

            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resourceStream != null)
            {
                return resourceStream;
            }

            throw new InvalidOperationException($"Cannot find resource named: '{resourceName}'");
        }

        private static byte[] LoadBytes(string name)
        {
            using (var resourceStream = GetResourceStream(name))
            {
                var bytes = new byte[resourceStream.Length];
                resourceStream.Read(bytes, 0, (int)resourceStream.Length);
                return bytes;
            }
        }

        private static string LoadText(string name)
        {
            using (var streamReader = new StreamReader(GetResourceStream(name)))
            {
                return streamReader.ReadToEnd();
            }
        }

        private static readonly Func<string, byte[]> s_bytesLoader = LoadBytes;
        private static readonly Func<string, string> s_textLoader = LoadText;
        private static Dictionary<string, byte[]> s_bytesCache;
        private static Dictionary<string, string> s_textCache;

        private static TResult GetOrLoadValue<TResult>(string name, Func<string, TResult> loader, ref Dictionary<string, TResult> cache)
        {
            if (cache != null && cache.TryGetValue(name, out var result))
            {
                return result;
            }

            result = loader(name);

            cache ??= new Dictionary<string, TResult>();

            cache[name] = result;

            return result;
        }

        public static byte[] GetBytes(string name) => GetOrLoadValue(name, s_bytesLoader, ref s_bytesCache);
        public static string GetText(string name) => GetOrLoadValue(name, s_textLoader, ref s_textCache);

        public static string Directory_Build_props => GetText("Directory.Build.props");
        public static string Directory_Build_targets => GetText("Directory.Build.targets");
        public static byte[] Key_snk => GetBytes("key.snk");
        public static string NuGet_Config => GetText("NuGet.Config");

        public static class SolutionFilters
        {
            public static string Invalid => GetText("SolutionFilters.InvalidSolutionFilter.slnf");
            public static string CSharp => GetText("SolutionFilters.CSharpSolutionFilter.slnf");
        }

        public static class SolutionFiles
        {
            public static string AnalyzerReference => GetText("SolutionFiles.AnalyzerReference.sln");
            public static string CircularSolution => GetText("CircularProjectReferences.CircularSolution.sln");
            public static string CSharp => GetText("SolutionFiles.CSharp.sln");
            public static string CSharp_EmptyLines => GetText("SolutionFiles.CSharp_EmptyLines.sln");
            public static string CSharp_ProjectReference => GetText("SolutionFiles.CSharp_ProjectReference.sln");
            public static string CSharp_UnknownProjectExtension => GetText("SolutionFiles.CSharp_UnknownProjectExtension.sln");
            public static string CSharp_UnknownProjectTypeGuid => GetText("SolutionFiles.CSharp_UnknownProjectTypeGuid.sln");
            public static string CSharp_UnknownProjectTypeGuidAndUnknownExtension => GetText("SolutionFiles.CSharp_UnknownProjectTypeGuidAndUnknownExtension.sln");
            public static string DuplicatedGuids => GetText("SolutionFiles.DuplicatedGuids.sln");
            public static string DuplicatedGuidsBecomeSelfReferential => GetText("SolutionFiles.DuplicatedGuidsBecomeSelfReferential.sln");
            public static string DuplicatedGuidsBecomeCircularReferential => GetText("SolutionFiles.DuplicatedGuidsBecomeCircularReferential.sln");
            public static string EmptyLineBetweenProjectBlock => GetText("SolutionFiles.EmptyLineBetweenProjectBlock.sln");
            public static string Issue29122_Solution => GetText("Issue29122.TestVB2.sln");
            public static string Issue30174_Solution => GetText("Issue30174.Solution.sln");
            public static string InvalidProjectPath => GetText("SolutionFiles.InvalidProjectPath.sln");
            public static string MissingEndProject1 => GetText("SolutionFiles.MissingEndProject1.sln");
            public static string MissingEndProject2 => GetText("SolutionFiles.MissingEndProject2.sln");
            public static string MissingEndProject3 => GetText("SolutionFiles.MissingEndProject3.sln");
            public static string NetCoreMultiTFM_ProjectReferenceToFSharp = GetText("NetCoreMultiTFM_ProjectReferenceToFSharp.Solution.sln");
            public static string NonExistentProject => GetText("SolutionFiles.NonExistentProject.sln");
            public static string ProjectLoadErrorOnMissingDebugType => GetText("SolutionFiles.ProjectLoadErrorOnMissingDebugType.sln");
            public static string SolutionFolder => GetText("SolutionFiles.SolutionFolder.sln");
            public static string VisualBasic => GetText("SolutionFiles.VisualBasic.sln");
            public static string VB_and_CSharp => GetText("SolutionFiles.VB_and_CSharp.sln");
        }

        public static class ProjectFiles
        {
            public static class CSharp
            {
                public static string AnalyzerReference => GetText("ProjectFiles.CSharp.AnalyzerReference.csproj");
                public static string AllOptions => GetText("ProjectFiles.CSharp.AllOptions.csproj");
                public static string AssemblyNameIsPath => GetText("ProjectFiles.CSharp.AssemblyNameIsPath.csproj");
                public static string AssemblyNameIsPath2 => GetText("ProjectFiles.CSharp.AssemblyNameIsPath2.csproj");
                public static string BadHintPath => GetText("ProjectFiles.CSharp.BadHintPath.csproj");
                public static string BadLink => GetText("ProjectFiles.CSharp.BadLink.csproj");
                public static string BadElement => GetText("ProjectFiles.CSharp.BadElement.csproj");
                public static string BadTasks => GetText("ProjectFiles.CSharp.BadTasks.csproj");
                public static string CircularProjectReferences_CircularCSharpProject1 => GetText("CircularProjectReferences.CircularCSharpProject1.csproj");
                public static string CircularProjectReferences_CircularCSharpProject2 => GetText("CircularProjectReferences.CircularCSharpProject2.csproj");
                public static string CSharpProject => GetText("ProjectFiles.CSharp.CSharpProject.csproj");
                public static string AdditionalFile => GetText("ProjectFiles.CSharp.AdditionalFile.csproj");
                public static string DuplicateFile => GetText("ProjectFiles.CSharp.DuplicateFile.csproj");
                public static string DuplicateReferences => GetText("ProjectFiles.CSharp.DuplicateReferences.csproj");
                public static string DuplicatedGuidLibrary1 => GetText("ProjectFiles.CSharp.DuplicatedGuidLibrary1.csproj");
                public static string DuplicatedGuidLibrary2 => GetText("ProjectFiles.CSharp.DuplicatedGuidLibrary2.csproj");
                public static string DuplicatedGuidLibrary3 => GetText("ProjectFiles.CSharp.DuplicatedGuidLibrary3.csproj");
                public static string DuplicatedGuidLibrary4 => GetText("ProjectFiles.CSharp.DuplicatedGuidLibrary4.csproj");
                public static string DuplicatedGuidReferenceTest => GetText("ProjectFiles.CSharp.DuplicatedGuidReferenceTest.csproj");
                public static string DuplicatedGuidsBecomeSelfReferential => GetText("ProjectFiles.CSharp.DuplicatedGuidsBecomeSelfReferential.csproj");
                public static string DuplicatedGuidsBecomeCircularReferential => GetText("ProjectFiles.CSharp.DuplicatedGuidsBecomeCircularReferential.csproj");
                public static string Encoding => GetText("ProjectFiles.CSharp.Encoding.csproj");
                public static string ExternAlias => GetText("ProjectFiles.CSharp.ExternAlias.csproj");
                public static string ExternAlias2 => GetText("ProjectFiles.CSharp.ExternAlias2.csproj");
                public static string ForEmittedOutput => GetText("ProjectFiles.CSharp.ForEmittedOutput.csproj");
                public static string Issue30174_InspectedLibrary => GetText("Issue30174.InspectedLibrary.InspectedLibrary.csproj");
                public static string Issue30174_ReferencedLibrary => GetText("Issue30174.ReferencedLibrary.ReferencedLibrary.csproj");
                public static string MsbuildError => GetText("ProjectFiles.CSharp.MsbuildError.csproj");
                public static string MallformedAdditionalFilePath => GetText("ProjectFiles.CSharp.MallformedAdditionalFilePath.csproj");
                public static string NetCoreApp_Project => GetText("NetCoreApp.Project.csproj");
                public static string NetCoreAppAndLibrary_Project => GetText("NetCoreAppAndLibrary.Project.csproj");
                public static string NetCoreAppAndLibrary_Library => GetText("NetCoreAppAndLibrary.Library.csproj");
                public static string NetCoreAppAndTwoLibraries_Project => GetText("NetCoreAppAndTwoLibraries.Project.csproj");
                public static string NetCoreAppAndTwoLibraries_Library1 => GetText("NetCoreAppAndTwoLibraries.Library1.csproj");
                public static string NetCoreAppAndTwoLibraries_Library2 => GetText("NetCoreAppAndTwoLibraries.Library2.csproj");
                public static string NetCoreMultiTFM_Project => GetText("NetCoreMultiTFM.Project.csproj");
                public static string NetCoreMultiTFM_ExtensionWithConditionOnTFM_Project => GetText("NetCoreMultiTFM_ExtensionWithConditionOnTFM.Project.csproj");
                public static string NetCoreMultiTFM_ExtensionWithConditionOnTFM_ProjectTestProps => GetText("NetCoreMultiTFM_ExtensionWithConditionOnTFM.Project.csproj.test.props");
                public static string NetCoreMultiTFM_ProjectReference_Library => GetText("NetCoreMultiTFM_ProjectReference.Library.csproj");
                public static string NetCoreMultiTFM_ProjectReference_Project => GetText("NetCoreMultiTFM_ProjectReference.Project.csproj");
                public static string NetCoreMultiTFM_ProjectReferenceToFSharp_CSharpLib = GetText("NetCoreMultiTFM_ProjectReferenceToFSharp.csharplib.csharplib.csproj");
                public static string PortableProject => GetText("ProjectFiles.CSharp.PortableProject.csproj");
                public static string ProjectLoadErrorOnMissingDebugType => GetText("ProjectFiles.CSharp.ProjectLoadErrorOnMissingDebugType.csproj");
                public static string ProjectReference => GetText("ProjectFiles.CSharp.ProjectReference.csproj");
                public static string ReferencesPortableProject => GetText("ProjectFiles.CSharp.ReferencesPortableProject.csproj");
                public static string ShouldUnsetParentConfigurationAndPlatform => GetText("ProjectFiles.CSharp.ShouldUnsetParentConfigurationAndPlatform.csproj");
                public static string Wildcards => GetText("ProjectFiles.CSharp.Wildcards.csproj");
                public static string WithoutCSharpTargetsImported => GetText("ProjectFiles.CSharp.WithoutCSharpTargetsImported.csproj");
                public static string WithDiscoverEditorConfigFiles => GetText("ProjectFiles.CSharp.WithDiscoverEditorConfigFiles.csproj");
                public static string WithPrefer32Bit => GetText("ProjectFiles.CSharp.WithPrefer32Bit.csproj");
                public static string WithChecksumAlgorithm => GetText("ProjectFiles.CSharp.WithChecksumAlgorithm.csproj");
                public static string WithLink => GetText("ProjectFiles.CSharp.WithLink.csproj");
                public static string WithClassNotInProjectFolder => GetText("ProjectFiles.CSharp.WithClassNotInProjectFolder.csproj");
                public static string WithSystemNumerics => GetText("ProjectFiles.CSharp.WithSystemNumerics.csproj");
                public static string WithXaml => GetText("ProjectFiles.CSharp.WithXaml.csproj");
                public static string WithoutPrefer32Bit => GetText("ProjectFiles.CSharp.WithoutPrefer32Bit.csproj");
                public static string VBNetCoreAppWithGlobalImportAndLibrary_Library => GetText("VBNetCoreAppWithGlobalImportAndLibrary.Library.csproj");
            }

            public static class FSharp
            {
                public static string NetCoreMultiTFM_ProjectReferenceToFSharp_FSharpLib = GetText("NetCoreMultiTFM_ProjectReferenceToFSharp.fsharplib.fsharplib.fsproj");
            }

            public static class VisualBasic
            {
                public static string AnalyzerReference => GetText("ProjectFiles.VisualBasic.AnalyzerReference.vbproj");
                public static string Circular_Target => GetText("ProjectFiles.VisualBasic.Circular_Target.vbproj");
                public static string Circular_Top => GetText("ProjectFiles.VisualBasic.Circular_Top.vbproj");
                public static string Embed => GetText("ProjectFiles.VisualBasic.Embed.vbproj");
                public static string Issue29122_ClassLibrary1 => GetText("Issue29122.Proj1.ClassLibrary1.vbproj");
                public static string Issue29122_ClassLibrary2 => GetText("Issue29122.Proj2.ClassLibrary2.vbproj");
                public static string InvalidProjectReference => GetText("ProjectFiles.VisualBasic.InvalidProjectReference.vbproj");
                public static string NonExistentProjectReference => GetText("ProjectFiles.VisualBasic.NonExistentProjectReference.vbproj");
                public static string UnknownProjectExtension => GetText("ProjectFiles.VisualBasic.UnknownProjectExtension.vbproj");
                public static string VisualBasicProject => GetText("ProjectFiles.VisualBasic.VisualBasicProject.vbproj");
                public static string VisualBasicProject_3_5 => GetText("ProjectFiles.VisualBasic.VisualBasicProject_3_5.vbproj");
                public static string WithPrefer32Bit => GetText("ProjectFiles.VisualBasic.WithPrefer32Bit.vbproj");
                public static string WithChecksumAlgorithm => GetText("ProjectFiles.VisualBasic.WithChecksumAlgorithm.vbproj");
                public static string WithoutPrefer32Bit => GetText("ProjectFiles.VisualBasic.WithoutPrefer32Bit.vbproj");
                public static string WithoutVBTargetsImported => GetText("ProjectFiles.VisualBasic.WithoutVBTargetsImported.vbproj");
                public static string VBNetCoreAppWithGlobalImportAndLibrary_VBProject => GetText("VBNetCoreAppWithGlobalImportAndLibrary.VBProject.vbproj");
            }
        }

        public static class SourceFiles
        {
            public static class CSharp
            {
                public static string App => GetText("SourceFiles.CSharp.App.xaml.cs");
                public static string AssemblyInfo => GetText("SourceFiles.CSharp.AssemblyInfo.cs");
                public static string CSharpClass => GetText("SourceFiles.CSharp.CSharpClass.cs");
                public static string CSharpClass_WithConditionalAttributes => GetText("SourceFiles.CSharp.CSharpClass_WithConditionalAttributes.cs");
                public static string CSharpConsole => GetText("SourceFiles.CSharp.CSharpConsole.cs");
                public static string CSharpExternAlias => GetText("SourceFiles.CSharp.CSharpExternAlias.cs");
                public static string Issue30174_InspectedClass => GetText("Issue30174.InspectedLibrary.InspectedClass.cs");
                public static string Issue30174_SomeMetadataAttribute => GetText("Issue30174.ReferencedLibrary.SomeMetadataAttribute.cs");
                public static string NetCoreApp_Program => GetText("NetCoreApp.Program.cs");
                public static string NetCoreAppAndLibrary_Class1 => GetText("NetCoreAppAndLibrary.Class1.cs");
                public static string NetCoreAppAndLibrary_Program => GetText("NetCoreAppAndLibrary.Program.cs");
                public static string NetCoreAppAndTwoLibraries_Class1 => GetText("NetCoreAppAndTwoLibraries.Class1.cs");
                public static string NetCoreAppAndTwoLibraries_Class2 => GetText("NetCoreAppAndTwoLibraries.Class1.cs");
                public static string NetCoreAppAndTwoLibraries_Program => GetText("NetCoreAppAndTwoLibraries.Program.cs");
                public static string NetCoreMultiTFM_Program => GetText("NetCoreMultiTFM.Program.cs");
                public static string NetCoreMultiTFM_ProjectReference_Class1 => GetText("NetCoreMultiTFM_ProjectReference.Class1.cs");
                public static string NetCoreMultiTFM_ProjectReference_Program => GetText("NetCoreMultiTFM_ProjectReference.Program.cs");
                public static string NetCoreMultiTFM_ProjectReferenceToFSharp_CSharpLib_Class1 = GetText("NetCoreMultiTFM_ProjectReferenceToFSharp.csharplib.Class1.cs");
                public static string MainWindow => GetText("SourceFiles.CSharp.MainWindow.xaml.cs");
                public static string OtherStuff_Foo => GetText("SourceFiles.CSharp.OtherStuff_Foo.cs");
                public static string VBNetCoreAppWithGlobalImportAndLibrary_MyHelperClass => GetText("VBNetCoreAppWithGlobalImportAndLibrary.MyHelperClass.cs");
            }

            public static class FSharp
            {
                public static string NetCoreMultiTFM_ProjectReferenceToFSharp_FSharpLib_Library = GetText("NetCoreMultiTFM_ProjectReferenceToFSharp.fsharplib.Library.fs");
            }

            public static class Text
            {
                public static string ValidAdditionalFile => GetText("SourceFiles.Text.ValidAdditionalFile.txt");
            }

            public static class VisualBasic
            {
                public static string Application => GetText("SourceFiles.VisualBasic.Application.myapp");
                public static string Application_Designer => GetText("SourceFiles.VisualBasic.Application.Designer.vb");
                public static string AssemblyInfo => GetText("SourceFiles.VisualBasic.AssemblyInfo.vb");
                public static string Resources => GetText("SourceFiles.VisualBasic.Resources.resx_");
                public static string Resources_Designer => GetText("SourceFiles.VisualBasic.Resources.Designer.vb");
                public static string Settings => GetText("SourceFiles.VisualBasic.Settings.settings");
                public static string Settings_Designer => GetText("SourceFiles.VisualBasic.Settings.Designer.vb");
                public static string VisualBasicClass => GetText("SourceFiles.VisualBasic.VisualBasicClass.vb");
                public static string VisualBasicClass_WithConditionalAttributes => GetText("SourceFiles.VisualBasic.VisualBasicClass_WithConditionalAttributes.vb");
                public static string VBNetCoreAppWithGlobalImportAndLibrary_Program => GetText("VBNetCoreAppWithGlobalImportAndLibrary.Program.vb");
            }

            public static class Xaml
            {
                public static string App => GetText("SourceFiles.Xaml.App.xaml");
                public static string MainWindow => GetText("SourceFiles.Xaml.MainWindow.xaml");
            }
        }

        public static class Dlls
        {
            public static byte[] CSharpProject => GetBytes("Dlls.CSharpProject.dll");
            public static byte[] EmptyLibrary => GetBytes("Dlls.EmptyLibrary.dll");
        }
    }
}
