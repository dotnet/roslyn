// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.Editor.UnitTests.SolutionParsing
{
    public class SolutionParsingTests
    {
#pragma warning disable 414
        private static string s_visualStudio2010 = @"# Visual Studio 2010";
        private static string s_visualStudio2012 = @"# Visual Studio 2012";
#pragma warning restore 414

#if MSBUILD12
        [Fact]
        public void ParseEmptyFile()
        {
            var emptySolution = string.Empty;

            Assert.Throws<Exception>(() =>
            {
                var file = Microsoft.CodeAnalysis.MSBuild.SolutionFile.Parse(new StringReader(emptySolution));
            });
        }

        [Fact]
        public void ParseEmptySolution()
        {
            var emptySolution = @"
Microsoft Visual Studio Solution File, Format Version 11.00
" + s_visualStudio2010 + @"
Global
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

            var file = Parse(new StringReader(emptySolution));

            Assert.Empty(file.ProjectBlocks);
            var solutionProperties = file.GlobalSectionBlocks.Single();

            Assert.Equal("GlobalSection", solutionProperties.Type);
            Assert.Equal("SolutionProperties", solutionProperties.ParenthesizedName);

            var singleKeyValuePair = solutionProperties.KeyValuePairs.Single();

            Assert.Equal("HideSolutionNode", singleKeyValuePair.Key);
            Assert.Equal("FALSE", singleKeyValuePair.Value);

            // Verify that we round trip properly
            Assert.Equal(file.GetText(), emptySolution);
        }

        [Fact]
        public void ParseVisualBasicConsoleApplicationSolution()
        {
            var vbConsoleApplicationSolution = @"
Microsoft Visual Studio Solution File, Format Version 11.00
" + s_visualStudio2010 + @"
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""ConsoleApplication1"", ""ConsoleApplication1\ConsoleApplication1.vbproj"", ""{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x86 = Debug|x86
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.ActiveCfg = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.Build.0 = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.ActiveCfg = Release|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.Build.0 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

            var file = Parse(new StringReader(vbConsoleApplicationSolution));

            var projectBlock = file.ProjectBlocks.Single();
            Assert.Equal(Guid.Parse("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"), projectBlock.ProjectTypeGuid);
            Assert.Equal("ConsoleApplication1", projectBlock.ProjectName);
            Assert.Equal(@"ConsoleApplication1\ConsoleApplication1.vbproj", projectBlock.ProjectPath);
            Assert.Equal(Guid.Parse("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}"), projectBlock.ProjectGuid);
            Assert.Empty(projectBlock.ProjectSections);

            var solutionConfigurationPlatforms = file.GlobalSectionBlocks
                                                              .Single(b => b.ParenthesizedName == "SolutionConfigurationPlatforms");

            Assert.Contains(new KeyValuePair<string, string>("Debug|x86", "Debug|x86"), solutionConfigurationPlatforms.KeyValuePairs);
            Assert.Contains(new KeyValuePair<string, string>("Release|x86", "Release|x86"), solutionConfigurationPlatforms.KeyValuePairs);

            var projectConfigurationPlatforms = file.GlobalSectionBlocks
                                                             .Single(b => b.ParenthesizedName == "ProjectConfigurationPlatforms");

            Assert.Contains(new KeyValuePair<string, string>("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.ActiveCfg", "Debug|x86"), projectConfigurationPlatforms.KeyValuePairs);
            Assert.Contains(new KeyValuePair<string, string>("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.Build.0", "Debug|x86"), projectConfigurationPlatforms.KeyValuePairs);
            Assert.Contains(new KeyValuePair<string, string>("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.ActiveCfg", "Release|x86"), projectConfigurationPlatforms.KeyValuePairs);
            Assert.Contains(new KeyValuePair<string, string>("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.Build.0", "Release|x86"), projectConfigurationPlatforms.KeyValuePairs);

            // Verify that we round trip properly
            Assert.Equal(file.GetText(), vbConsoleApplicationSolution);
        }

        [Fact]
        [WorkItem(547294, "DevDiv")]
        public void ParseSolutionWithMissingWhiteSpaces()
        {
            var vbConsoleApplicationSolution = @"
Microsoft Visual Studio Solution File, Format Version 11.00
" + s_visualStudio2010 + @"
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") =""ConsoleApplication1"" ,    ""Console Application1\ConsoleApplication1.vbproj"",""{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x86 = Debug|x86
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.ActiveCfg = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.Build.0 = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.ActiveCfg = Release|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.Build.0 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

            var file = Parse(new StringReader(vbConsoleApplicationSolution));
            
            var projectBlock = file.ProjectBlocks.Single();
            Assert.Equal(Guid.Parse("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"), projectBlock.ProjectTypeGuid);
            Assert.Equal("ConsoleApplication1", projectBlock.ProjectName);
            Assert.Equal(@"Console Application1\ConsoleApplication1.vbproj", projectBlock.ProjectPath);
            Assert.Equal(Guid.Parse("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}"), projectBlock.ProjectGuid);
            Assert.Empty(projectBlock.ProjectSections);
        }

        [Fact]
        [WorkItem(708726, "DevDiv")]
        public void ParseSolutionFileWithVisualStudioVersion()
        {
            var vbConsoleApplicationSolution = @"
Microsoft Visual Studio Solution File, Format Version 12.00
" + s_visualStudio2012 + @"
VisualStudioVersion = 12.0.20430.1 PREVIEW
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") =""ConsoleApplication1"" ,    ""Console Application1\ConsoleApplication1.vbproj"",""{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x86 = Debug|x86
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.ActiveCfg = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Debug|x86.Build.0 = Debug|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.ActiveCfg = Release|x86
		{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}.Release|x86.Build.0 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

            var file = Parse(new StringReader(vbConsoleApplicationSolution));

            var vsVersionLine = file.VisualStudioVersionLineOpt;
            Assert.Equal(vsVersionLine, "VisualStudioVersion = 12.0.20430.1 PREVIEW");
            var minimumVersionLine = file.MinimumVisualStudioVersionLineOpt;
            Assert.Equal(minimumVersionLine, "MinimumVisualStudioVersion = 10.0.40219.1");
            
            var projectBlock = file.ProjectBlocks.Single();
            Assert.Equal(Guid.Parse("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"), projectBlock.ProjectTypeGuid);
            Assert.Equal("ConsoleApplication1", projectBlock.ProjectName);
            Assert.Equal(@"Console Application1\ConsoleApplication1.vbproj", projectBlock.ProjectPath);
            Assert.Equal(Guid.Parse("{09BC9F5A-FBFA-4BEE-A13C-77A99C95D06B}"), projectBlock.ProjectGuid);
            Assert.Empty(projectBlock.ProjectSections);
        }

        private Microsoft.CodeAnalysis.MSBuild.SolutionFile Parse(StringReader stringReader)
        {
            return Microsoft.CodeAnalysis.MSBuild.SolutionFile.Parse(stringReader);
        }
#endif
    }
}
