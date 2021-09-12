// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class CSharpPDBTestBase : CSharpTestBase
    {
        public static void TestSequencePoints(string markup, CSharpCompilationOptions compilationOptions, CSharpParseOptions parseOptions = null, string methodName = "")
        {
            int? position;
            TextSpan? expectedSpan;
            string source;
            MarkupTestFile.GetPositionAndSpan(markup, out source, out position, out expectedSpan);

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: compilationOptions, parseOptions: parseOptions);
            compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            var pdb = PdbValidation.GetPdbXml(compilation, qualifiedMethodName: methodName);
            bool hasBreakpoint = CheckIfSpanWithinSequencePoints(expectedSpan.GetValueOrDefault(), source, pdb);

            Assert.True(hasBreakpoint);
        }

        public static bool CheckIfSpanWithinSequencePoints(TextSpan span, string source, string pdb)
        {
            // calculate row and column from span
            var text = SourceText.From(source);
            var startLine = text.Lines.GetLineFromPosition(span.Start);
            var startRow = startLine.LineNumber + 1;
            var startColumn = span.Start - startLine.Start + 1;

            var endLine = text.Lines.GetLineFromPosition(span.End);
            var endRow = endLine.LineNumber + 1;
            var endColumn = span.End - endLine.Start + 1;

            var doc = new XmlDocument() { XmlResolver = null };
            using (var reader = new XmlTextReader(new StringReader(pdb)) { DtdProcessing = DtdProcessing.Prohibit })
            {
                doc.Load(reader);
            }

            foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
            {
                foreach (XmlElement item in entry.ChildNodes)
                {
                    if (startRow.ToString() == item.GetAttribute("startLine") &&
                        startColumn.ToString() == item.GetAttribute("startColumn") &&
                        endRow.ToString() == item.GetAttribute("endLine") &&
                        endColumn.ToString() == item.GetAttribute("endColumn"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void TestTypeDocuments(string[] sources, params (string typeName, string documentName)[] expected)
        {
            var trees = sources.Select((s, i) => SyntaxFactory.ParseSyntaxTree(s, path: $"{i + 1}.cs", encoding: Encoding.UTF8)).ToArray();
            var compilation = CreateCompilation(trees, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var pe = compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            var metadata = ModuleMetadata.CreateFromImage(pe);
            var metadataReader = metadata.GetMetadataReader();

            var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();

            var actual = from handle in pdbReader.CustomDebugInformation
                         let entry = pdbReader.GetCustomDebugInformation(handle)
                         where pdbReader.GetGuid(entry.Kind).Equals(PortableCustomDebugInfoKinds.TypeDocument)
                         select (typeName: GetTypeName(entry.Parent), documentName: GetDocumentNames(entry.Value));

            AssertEx.Equal(expected, actual, itemSeparator: ", ", itemInspector: i => $"(\"{i.typeName}\", \"{i.documentName}\")");

            string GetTypeName(EntityHandle handle)
            {
                var typeHandle = (TypeDefinitionHandle)handle;
                var type = metadataReader.GetTypeDefinition(typeHandle);
                return metadataReader.GetString(type.Name);
            }

            string GetDocumentNames(BlobHandle value)
            {
                var result = new List<string>();

                var reader = pdbReader.GetBlobReader(value);
                while (reader.RemainingBytes > 0)
                {
                    var documentRow = reader.ReadCompressedInteger();
                    if (documentRow > 0)
                    {
                        var doc = pdbReader.GetDocument(MetadataTokens.DocumentHandle(documentRow));
                        result.Add(pdbReader.GetString(doc.Name));
                    }
                }

                return string.Join(", ", result);
            }
        }
    }
}
