// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class CSharpPDBTestBase : CSharpTestBase
    {
        public static void TestLambdasAndClosures(string input, string methodStartSpanName = "method", string closureSpanName = "closure", string lambdaSpanName = "lambda")
        {
            MarkupTestFile.GetSpans(input, out var source, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            var pdb = PdbValidation.GetPdbXml(compilation);

            //TODO: Should we support having more than one method under test? Or does that make for tests that do too much?
            var methodStart = spans[methodStartSpanName].Single().Start;

            // We need to offset the closures and lambdas by the method start point
            var closures = spans[closureSpanName].SelectAsArray(c => new TextSpan(c.Start - methodStart, c.Length));
            var lambdas = spans[lambdaSpanName].SelectAsArray(c => new TextSpan(c.Start - methodStart, c.Length));

            CheckClosuresAndLambdas(pdb, closures, lambdas);
        }

        private static void CheckClosuresAndLambdas(string pdb, ImmutableArray<TextSpan> closures, ImmutableArray<TextSpan> lambdas)
        {
            var pdbXml = XElement.Parse(pdb);

            // Compare as strings so the developer gets nicer failure messages
            var actualClosures = pdbXml.DescendantsAndSelf("closure").Select(c => c.Attribute("offset").Value).Select(int.Parse);

            // Verify number and start position of closures
            var actualClosuresDescription = GetOffsetsDescription("Closure", actualClosures);
            var expectedClosuresDescription = GetOffsetsDescription("Closure", closures.Select(c => c.Start));
            Assert.Equal(expectedClosuresDescription, actualClosuresDescription);

            var actualLambdas = pdbXml.DescendantsAndSelf("lambda").Select(c => new
            {
                Offset = int.Parse(c.Attribute("offset").Value),
                Closure = int.Parse(c.Attribute("closure").Value)
            });

            // Verify number and start position of lambdas
            var actualLambdasDescription = GetOffsetsDescription("Lambda", actualLambdas.Select(l => l.Offset));
            var expectedLambdasDescription = GetOffsetsDescription("Lambda", lambdas.Select(l => l.Start));
            Assert.Equal(actualLambdasDescription, expectedLambdasDescription);

            // Verify that lambdas are inside their expected closures

            // The markup system has these ordered by span end, but PDB xml indexes by start
            var orderedClosures = closures.OrderBy(c => c.Start).ToArray();
            foreach (var lambda in actualLambdas)
            {
                var closure = orderedClosures[lambda.Closure];
                Assert.True(closure.Contains(lambda.Offset), $"Lambda at offset {lambda.Offset} was expected to be contained in closure {lambda.Closure} but that closure is {closure}");
            }

            static string GetOffsetsDescription(string item, IEnumerable<int> actualClosures)
            {
                return item + " offsets: " + string.Join(", ", actualClosures.OrderBy(i => i));
            }
        }

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
