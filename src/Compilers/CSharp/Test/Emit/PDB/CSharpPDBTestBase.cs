// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
    }
}
