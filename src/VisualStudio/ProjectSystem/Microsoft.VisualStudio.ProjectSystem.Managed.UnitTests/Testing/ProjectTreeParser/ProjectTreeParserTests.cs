
using System;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Testing
{
    [UnitTestTrait]
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
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("   ")]
        [InlineData("    ")]
        public void Parse_WhitespaceAsValue_ThrowsFormat(string input)
        {
            var parser = CreateInstance(input);

            Assert.Throws<FormatException>(() => {

                parser.Parse();
            });
        }

        [Theory]
        [InlineData(@"R",                                                                           @"R[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""" )]
        [InlineData(@"Ro",                                                                          @"Ro[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Root",                                                                        @"Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
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

        [Theory]
        [InlineData(@"Project Root (capabilities: {})",                                             @"Project Root[caption] (visibility: visible, capabilities: {}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A})",                                            @"Project Root[caption] (visibility: visible, capabilities: {A[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A B})",                                          @"Project Root[caption] (visibility: visible, capabilities: {A[capability] B[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {A B C})",                                        @"Project Root[caption] (visibility: visible, capabilities: {A[capability] B[capability] C[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {Folder})",                                       @"Project Root[caption] (visibility: visible, capabilities: {Folder[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {Folder IncludeInProjectCandidate})",             @"Project Root[caption] (visibility: visible, capabilities: {Folder[capability] IncludeInProjectCandidate[capability]}), FilePath: ""[filepath]""")]
        [InlineData(@"Project Root (capabilities: {AppDesigner Folder IncludeInProjectCandidate})", @"Project Root[caption] (visibility: visible, capabilities: {AppDesigner[capability] Folder[capability] IncludeInProjectCandidate[capability]}), FilePath: ""[filepath]""")]
        public void Parse_RootWithCapabilities_CanParse(string input, string expected)
        {
            AssertProjectTree(input, expected);
        }

        private static ProjectTreeParser CreateInstance(string value)
        {
            return new ProjectTreeParser(value);
        }

        private void AssertProjectTree(string input, string expected)
        {
            // Remove the newlines from the start and end of input and expected so that 
            // it makes it easier inside the test to layout the repro.
            input = input.Trim(new[] { '\n', '\r' });
            expected = expected.Trim(new[] { '\n', '\r' });

            var parser = new ProjectTreeParser(input);
            var writer = new ProjectTreeWriter(parser.Parse(), tagElements: true);

            string result = writer.WriteToString();

            Assert.Equal(expected, result);
        }
    }
}
