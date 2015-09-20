// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Xunit;

namespace Microsoft.VisualStudio.Testing
{
    [UnitTestTrait]
    public class ProjectTreeProviderTests
    {
        [Fact]
        public void Parse_Empty()
        {
            string input = @"";
            string expected = @"
Root (capabilities: {ProjectRoot})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_PathSeparator()
        {
            string input = @"
|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    <Unnamed> (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_ComponentSeparator()
        {
            string input = @"
\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    <Unnamed> (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_File()
        {
            string input = @"
File";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File (capabilities: {})";

            AssertProjectTree(input, expected);
        }


        [Fact]
        public void Parse_FileWithTrailingPathSeparator()
        {
            string input = @"
File|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FileWithTrailingComponentSeparator()
        {
            string input = @"
File\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_TwoFiles()
        {
            string input = @"
File1|
File2";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_TwoFilesWithTrailingPathSeparator()
        {
            string input = @"
File1|
File2|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_TwoFilesWithTrailingComponentSeparator()
        {
            string input = @"
File1|
File2\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_ThreeFiles()
        {
            string input = @"
File1|
File2|
File3";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})
    File3 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_ThreeFilesWithTrailingPathSeparator()
        {
            string input = @"
File1|
File2|
File3|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})
    File3 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_ThreeFilesWithTrailingComponentSeparator()
        {
            string input = @"
File1|
File2|
File3\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    File1 (capabilities: {})
    File2 (capabilities: {})
    File3 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithSingleFile()
        {
            string input = @"
Folder\File";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithSingleFileWithTrailingPathSeparator()
        {
            string input = @"
Folder\File|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithSingleFileWithTrailingComponentSeparator()
        {
            string input = @"
Folder\File\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithMultipleFiles()
        {
            string input = @"
Folder\File1|
Folder\File2";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File1 (capabilities: {})
        File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithMultipleFilesWithTrailingPathSeparator()
        {
            string input = @"
Folder\File1|
Folder\File2|";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File1 (capabilities: {})
        File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_FolderWithMultipleFilesWithTrailingComponentSeparator()
        {
            string input = @"
Folder\File1|
Folder\File2\";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        File1 (capabilities: {})
        File2 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        [Fact]
        public void Parse_MultipleWithMultipleFiles()
        {
            string input = @"
Folder1\File1|
Folder1\File2|
Folder1\Folder4\File7|
Folder1\Folder4\Folder7\File10|
Folder2\File3|
Folder2\File4|
Folder2\Folder5\File8|
Folder2\Folder5\Folder8\File11|
Folder3\File5|
Folder3\File6|
Folder3\Folder6\File9|
Folder3\Folder6\Folder9\File12";

            string expected = @"
Root (capabilities: {ProjectRoot})
    Folder1 (capabilities: {Folder})
        File1 (capabilities: {})
        File2 (capabilities: {})
        Folder4 (capabilities: {Folder})
            File7 (capabilities: {})
            Folder7 (capabilities: {Folder})
                File10 (capabilities: {})
    Folder2 (capabilities: {Folder})
        File3 (capabilities: {})
        File4 (capabilities: {})
        Folder5 (capabilities: {Folder})
            File8 (capabilities: {})
            Folder8 (capabilities: {Folder})
                File11 (capabilities: {})
    Folder3 (capabilities: {Folder})
        File5 (capabilities: {})
        File6 (capabilities: {})
        Folder6 (capabilities: {Folder})
            File9 (capabilities: {})
            Folder9 (capabilities: {Folder})
                File12 (capabilities: {})";

            AssertProjectTree(input, expected);
        }

        private static void AssertProjectTree(string input, string expected)
        {
            IProjectTree tree = ProjectTreeProvider.Parse(input);

            string result = GetStringRepresentation(tree);

            // Remove the first line so that the test can lay it out a little better
            expected = expected.TrimStart(new[] { '\n', '\r' });

            Assert.Equal(expected, result, ignoreCase:false, ignoreLineEndingDifferences:true);
        }

        private static string GetStringRepresentation(IProjectTree tree)
        {
            StringWriter writer = new StringWriter();

            ProjectTreeWriter.WriteTo(writer, tree);

            return writer.ToString();   
        }
    }
}
