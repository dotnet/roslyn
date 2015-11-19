// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PortablePdbTests : CSharpPDBTestBase
    {
        [Fact]
        public void SequencePointBlob()
        {
            string source = @"
class C
{
    public static void Main()
    {
        if (F())
        {
            System.Console.WriteLine(1);
        }
    }

    public static bool F() => false;
}
";
            var c = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);

            using (var peReader = new PEReader(peBlob))
            using (var pdbMetadata = new PinnedMetadata(pdbStream.ToImmutable()))
            {
                var mdReader = peReader.GetMetadataReader();
                var pdbReader = pdbMetadata.Reader;

                foreach (var methodHandle in mdReader.MethodDefinitions)
                {
                    var method = mdReader.GetMethodDefinition(methodHandle);
                    var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodHandle);

                    var name = mdReader.GetString(method.Name);

                    TextWriter writer = new StringWriter();
                    foreach (var sp in methodDebugInfo.GetSequencePoints())
                    {
                        if (sp.IsHidden)
                        {
                            writer.WriteLine($"{sp.Offset}: <hidden>");
                        }
                        else
                        {
                            writer.WriteLine($"{sp.Offset}: ({sp.StartLine},{sp.StartColumn})-({sp.EndLine},{sp.EndColumn})");
                        }
                    }

                    var spString = writer.ToString();
                    var spBlob = pdbReader.GetBlobBytes(methodDebugInfo.SequencePointsBlob);

                    switch (name)
                    {
                        case "Main":
                            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
0: (5,5)-(5,6)
1: (6,9)-(6,17)
7: <hidden>
10: (7,9)-(7,10)
11: (8,13)-(8,41)
18: (9,9)-(9,10)
19: (10,5)-(10,6)
", spString);
                            AssertEx.Equal(new byte[] 
                            {
                                0x01, // local signature

                                0x00, // IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x05, // Start Line
                                0x05, // Start Column

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x08, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x08, // delta Start Column (signed compressed)

                                0x06, // delta IL offset
                                0x00, // hidden
                                0x00, // hidden

                                0x03, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x00, // delta Start Column (signed compressed)

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x1C, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x08, // delta Start Column (signed compressed)

                                0x07, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x79, // delta Start Column (signed compressed)

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x79, // delta Start Column (signed compressed)
                            }, spBlob);
                            break;

                        case "F":
                            AssertEx.AssertEqualToleratingWhitespaceDifferences("0: (12,31)-(12,36)", spString);
                            AssertEx.Equal(new byte[] 
                            {
                                0x00, // local signature

                                0x00, // delta IL offset
                                0x00, // Delta Lines
                                0x05, // Delta Columns
                                0x0C, // Start Line
                                0x1F  // Start Column
                            }, spBlob);
                            break;
                    }
                }
            }
        }
    }
}