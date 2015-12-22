// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.Testing
{
    [ProjectSystemTrait]
    public class ProjectTreeParserTests
    {
        [Fact]
        public void Constructor_NullAsValue_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("value", () => {

                new ProjectTreeParser((string)null);
            });
        }

        [Fact]
        public void Constructor_EmptyAsValue_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>("value", () => {

                new ProjectTreeParser("");
            });
        }

        [Theory]
        [InlineData(@" ")]
        [InlineData(@"  ")]
        [InlineData(@"   ")]
        [InlineData(@"    ")]
        public void Parse_IdExpected_EncounteredOnlyWhiteSpace_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.IdExpected_EncounteredOnlyWhiteSpace);
        }

        [Theory]
        [InlineData(@"(")]
        [InlineData(@",")]
        [InlineData(@"Root,  ")]
        [InlineData(@"Root,   ")]
        [InlineData(@"Root ( ")]
        [InlineData(@"Root (visibility:  ")]
        [InlineData(@"Root (visibility: visible,  ")]
        [InlineData(@"Root (visibility: visible,   ")]
        [InlineData(@"Root (capabilities: { ")]
        [InlineData(@"Root (capabilities: {  ")]
        public void Parse_IdExpected_EncounteredDelimiter_ThrowsFormat(string input)
        {   
            AssertThrows(input, ProjectTreeFormatError.IdExpected_EncounteredDelimiter);
        }

        [Theory]
        [InlineData(@"Root, ")]
        [InlineData(@"Root (")]
        [InlineData(@"Root (visibility: ")]
        [InlineData(@"Root (visibility: visible, ")]
        [InlineData(@"Root (capabilities: {")]
        public void Parse_IdExpected_EncounteredEndOfString_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.IdExpected_EncounteredEndOfString);
        }


        [Theory]
        [InlineData(@"Root, FilePath:")]
        [InlineData(@"Root (),")]
        [InlineData(@"Root (visibility")]
        [InlineData(@"Root (visibility:")]
        [InlineData(@"Root (capabilities")]
        [InlineData(@"Root (capabilities:")]
        [InlineData(@"Root (capabilities: ")]
        [InlineData(@"Root (capabilities: {}")]
        public void Parse_DelimiterExpected_EncounteredEndOfString_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.DelimiterExpected_EncounteredEndOfString);
        }

        [Theory]
        [InlineData(@"Root, FilePath ")]
        [InlineData(@"Root,FilePath: """"")]
        [InlineData(@"Root, FilePath:""""")]
        [InlineData(@"Root, FilePath:,")]
        [InlineData(@"Root (),,")]
        [InlineData(@"Root (visibility ")]
        public void Parse_DelimiterExpected_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.DelimiterExpected);
        }


        [Theory]
        [InlineData(@"Root, FilePath,")]
        [InlineData(@"Root (Foo:")]
        [InlineData(@"Root (Visibility:")]
        [InlineData(@"Root (Capabilities: ")]
        [InlineData(@"Root (Foo")]
        [InlineData(@"Root (Visibility")]
        [InlineData(@"Root (Capabilities ")]
        [InlineData(@"Root (), File")]
        [InlineData(@"Root (), Filepath")]
        [InlineData(@"Root (), filepath")]
        [InlineData(@"Root (), filepath:")]
        public void Parse_UnrecognizedPropertyName_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.UnrecognizedPropertyName);
        }

        [Theory]
        [InlineData(@"Root (visibility: Visible")]
        [InlineData(@"Root (visibility: Invisible")]
        [InlineData(@"Root (visibility: VISIBLE")]
        [InlineData(@"Root (visibility: INVISIBLE")]
        [InlineData(@"Root (visibility: v")]
        [InlineData(@"Root (visibility: i")]
        public void Parse_UnrecognizedPropertyValue_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.UnrecognizedPropertyValue);
        }

        [Theory]
        [InlineData(@"Root, FilePath: ""C:\Temp"",")]
        [InlineData(@"Root, FilePath: ""C:\Temp""""")]
        [InlineData(@"Root, FilePath: ""C:\Temp""A")]
        public void Parse_EndOfStringExpected_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.EndOfStringExpected);
        }

        // Input                                                                                    // Expected
        [Theory]
        [InlineData(@"R",                                                                           @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""" )]
        [InlineData(@"Ro",                                                                          @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root",                                                                        @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project:Root",                                                                @"Project:Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root",                                                                @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root",                                                    @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R, FilePath: """"",                                                           @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro, FilePath: """"",                                                          @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root, FilePath: """"",                                                        @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root, FilePath: """"",                                                @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root, FilePath: """"",                                    @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        public void Parse_RootWithNoProperties_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        // Input                                                                                    // Expected
        [Theory]
        [InlineData(@"R()",                                                                         @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro()",                                                                        @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root()",                                                                      @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root()",                                                              @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root()",                                                  @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R ()",                                                                        @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro ()",                                                                       @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root ()",                                                                     @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root ()",                                                             @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root ()",                                                 @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R(), FilePath: """"",                                                         @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro(), FilePath: """"",                                                        @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root(), FilePath: """"",                                                      @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root(), FilePath: """"",                                              @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root(), FilePath: """"",                                  @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R (), FilePath: """"",                                                        @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro (), FilePath: """"",                                                       @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root (), FilePath: """"",                                                     @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (), FilePath: """"",                                             @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root (), FilePath: """"",                                 @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        
        public void Parse_RootWithEmptyProperties_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        // Input                                                                                    // Expected
        [Theory]
        [InlineData(@"R(visibility: visible)",                                                      @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro(visibility: visible)",                                                     @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root(visibility: visible)",                                                   @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root(visibility: visible)",                                           @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root(visibility: visible)",                               @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R (visibility: visible)",                                                     @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro (visibility: visible)",                                                    @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root (visibility: visible)",                                                  @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (visibility: visible)",                                          @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root (visibility: visible)",                              @"This is the project root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R(visibility: invisible)",                                                    @"R[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro(visibility: invisible)",                                                   @"Ro[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root(visibility: invisible)",                                                 @"Root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root(visibility: invisible)",                                         @"Project Root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root(visibility: invisible)",                             @"This is the project root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"R (visibility: invisible)",                                                   @"R[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Ro (visibility: invisible)",                                                  @"Ro[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root (visibility: invisible)",                                                @"Root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (visibility: invisible)",                                        @"Project Root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"This is the project root (visibility: invisible)",                            @"This is the project root[caption] (visibility: invisible, capabilities: {}), FilePath: ""[filepath]""")]
        public void Parse_RootWithVisibility_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        // Input                                                                                    // Expected
        [Theory]
        [InlineData(@"Project Root (capabilities: {})",                                             @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A})",                                            @"Project Root[caption] (visibility: visible, capabilities: {A[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A B})",                                          @"Project Root[caption] (visibility: visible, capabilities: {A[capability] B[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A B C})",                                        @"Project Root[caption] (visibility: visible, capabilities: {A[capability] B[capability] C[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {Folder})",                                       @"Project Root[caption] (visibility: visible, capabilities: {Folder[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {Folder IncludeInProjectCandidate})",             @"Project Root[caption] (visibility: visible, capabilities: {Folder[capability] IncludeInProjectCandidate[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {AppDesigner Folder IncludeInProjectCandidate})", @"Project Root[caption] (visibility: visible, capabilities: {AppDesigner[capability] Folder[capability] IncludeInProjectCandidate[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {App:Designer})",                                 @"Project Root[caption] (visibility: visible, capabilities: {App:Designer[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (visibility: visible, capabilities: {App:Designer})",            @"Project Root[caption] (visibility: visible, capabilities: {App:Designer[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {App:Designer}, visibility: visible)",            @"Project Root[caption] (visibility: visible, capabilities: {App:Designer[capability]}), FilePath: ""[filepath]""")]
        public void Parse_RootWithCapabilities_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        // Input                                                                                    // Expected
        [Theory]
        [InlineData(@"Project Root (), FilePath: """"",                                             @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""C""",                                            @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""C[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""C:""",                                           @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""C:[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""C:\""",                                          @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""C:\[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""C:\Project""",                                   @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""C:\Project[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""C:\Project Root""",                              @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""C:\Project Root[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""Project Root""",                                 @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""Project Root[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""Project Root.csproj""",                          @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""Project Root.csproj[filepath]""")]
        [InlineData(@"Project Root (), FilePath: ""Folder\Project Root.csproj""",                   @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""Folder\Project Root.csproj[filepath]""")]
        public void Parse_RootWithFilePath_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        [Theory]
        [InlineData(@"
Root
    Parent
",
            @"
Root[caption]
[indent]Parent[caption]
")]

        [InlineData(@"
Root
    Parent1
    Parent2
",
            @"
Root[caption]
[indent]Parent1[caption]
[indent]Parent2[caption]
")]

        [InlineData(@"
Root
    Parent1
        Child
    Parent2
",
            @"
Root[caption]
[indent]Parent1[caption]
[indent][indent]Child[caption]
[indent]Parent2[caption]
")]

        [InlineData(@"
Root
    Parent1
        Child1
        Child2
    Parent2
    Parent3
        Child3
        Child4
            Grandchild
    Parent4
",
            @"
Root[caption]
[indent]Parent1[caption]
[indent][indent]Child1[caption]
[indent][indent]Child2[caption]
[indent]Parent2[caption]
[indent]Parent3[caption]
[indent][indent]Child3[caption]
[indent][indent]Child4[caption]
[indent][indent][indent]Grandchild[caption]
[indent]Parent4[caption]
")]

        public void Parse_RootWithChildren_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected, ProjectTreeWriterOptions.None);
        }


        [Theory]
        [InlineData(@"
Root
Root
")]
        [InlineData(@"
Root
    Parent
Root
")]
        [InlineData(@"
Root
    Parent
        Child
Root
")]
        [InlineData(@"
Root
    Parent
        Child
        Child
Root
")]
        public void Parse_MultipleRoots_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.MultipleRoots);
        }

        [Theory]
        [InlineData(@"
Root
        Parent
")]
        [InlineData(@"
Root
    Parent 
            Parent
")]
        [InlineData(@"
Root
            Parent
")]
        public void Parse_IndentTooManyLevels_ThrowsFormat(string input)
        {
            AssertThrows(input, ProjectTreeFormatError.IndentTooManyLevels);
        }

        private void AssertProjectTree(string input, string expected, ProjectTreeWriterOptions options = ProjectTreeWriterOptions.Capabilities | ProjectTreeWriterOptions.FilePath | ProjectTreeWriterOptions.Visibility)
        {
            // Remove the newlines from the start and end of input and expected so that 
            // it makes it easier inside the test to layout the repro.
            input = input.Trim(new[] { '\n', '\r' });
            expected = expected.Trim(new[] { '\n', '\r' });

            var parser = new ProjectTreeParser(input);
            var writer = new ProjectTreeWriter(parser.Parse(), options | ProjectTreeWriterOptions.Tags);

            string result = writer.WriteToString();

            Assert.Equal(expected, result);
        }

        private void AssertThrows(string input, ProjectTreeFormatError error)
        {
            input = input.Trim(new[] { '\n', '\r' });

            var parser = new ProjectTreeParser(input);

            var exception = Assert.Throws<ProjectTreeFormatException>(() => {

                parser.Parse();
            });


            Assert.Equal(error, exception.ErrorId);
        }
    }
}
