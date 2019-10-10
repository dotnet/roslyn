// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBEmbeddedSourceTests : CSharpTestBase
    {
        [Theory]
        [InlineData(DebugInformationFormat.PortablePdb)]
        [InlineData(DebugInformationFormat.Pdb)]
        [WorkItem(28045, "https://github.com/dotnet/roslyn/issues/28045")]
        public void StandalonePdb(DebugInformationFormat format)
        {
            string source1 = WithWindowsLineBreaks(@"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
");
            string source2 = WithWindowsLineBreaks(@"
// no code
");

            var tree1 = Parse(source1, "f:/build/goo.cs");
            var tree2 = Parse(source2, "f:/build/nocode.cs");
            var c = CreateCompilation(new[] { tree1, tree2 }, options: TestOptions.DebugDll);
            var embeddedTexts = new[]
            {
                EmbeddedText.FromSource(tree1.FilePath, tree1.GetText()),
                EmbeddedText.FromSource(tree2.FilePath, tree2.GetText())
            };

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""f:/build/goo.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""5D-7D-CF-1B-79-12-0E-0A-80-13-E0-98-7E-5C-AA-3B-63-D8-7E-4F""><![CDATA[﻿
using System;
class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
]]></file>
    <file id=""2"" name=""f:/build/nocode.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8B-1D-3F-75-E0-A8-8F-90-B2-D3-52-CF-71-9B-17-29-3C-70-7A-42""><![CDATA[﻿
// no code
]]></file>
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""29"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>
", embeddedTexts, format: format);
        }

        [Fact]
        public void EmbeddedPdb()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var tree = Parse(source, "f:/build/goo.cs");
            var c = CreateCompilation(tree, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peBlob = c.EmitToArray(
                EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded),
                embeddedTexts: new[] { EmbeddedText.FromSource(tree.FilePath, tree.GetText()) });
            pdbStream.Position = 0;

            using (var peReader = new PEReader(peBlob))
            {
                var embeddedEntry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

                using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry))
                {
                    var pdbReader = embeddedMetadataProvider.GetMetadataReader();

                    var embeddedSource =
                        (from documentHandle in pdbReader.Documents
                         let document = pdbReader.GetDocument(documentHandle)
                         select new
                         {
                             FilePath = pdbReader.GetString(document.Name),
                             Text = pdbReader.GetEmbeddedSource(documentHandle)
                         }).Single();

                    Assert.Equal("f:/build/goo.cs", embeddedSource.FilePath);
                    Assert.Equal(source, embeddedSource.Text.ToString());
                }
            }
        }
    }
}
