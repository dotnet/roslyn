// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Helper to check compilations with test instrumentation.
    ///
    /// Use in three steps:
    /// 1. Initialize with expectations about methods, what spans they include and which spans were covered.
    ///     This is done with calls to Method followed by a call to True or False for each span within a method.
    /// 2. Use the ExpectedOutput to check that executing the compilation produces the expected instrumentation data.
    /// 3. Use CompleteCheck to check that your expectations about spans were correct.
    ///
    /// If you only do the third step, the test output will provide you with a template of code for the first step.
    /// </summary>
    internal sealed class CSharpInstrumentationChecker : BaseInstrumentationChecker
    {
        public string ExpectedOutput { get { return _consoleExpectations.ToString(); } }

        internal override void AddConsoleExpectation(string text)
        {
            _consoleExpectations.AppendLine(text);
        }

        internal override void Dump(DynamicAnalysisDataReader reader, string[] sourceLines)
        {
            var output = PooledStringBuilder.GetInstance();
            output.Builder.AppendLine("Template code for checking instrumentation results:");

            for (int method = 1; method <= reader.Methods.Length; method++)
            {
                var snippets = GetActualSnippets(method, reader, sourceLines);
                if (snippets.Length == 0)
                {
                    continue;
                }

                string methodTermination = GetTermination(0, snippets.Length);
                if (snippets[0] == null)
                {
                    output.Builder.AppendLine($"checker.Method({method}, 1){methodTermination}");
                }
                else
                {
                    output.Builder.AppendLine($"checker.Method({method}, 1, \"{snippets[0]}\"){methodTermination}");
                }

                for (int index = 1; index < snippets.Length; index++)
                {
                    string termination = GetTermination(index, snippets.Length);
                    if (snippets[index] == null)
                    {
                        output.Builder.AppendLine($"    .True(){termination}");
                    }
                    else
                    {
                        output.Builder.AppendLine($"    .True(\"{snippets[index]}\"){termination}");
                    }
                }
            }
            AssertEx.Fail(output.ToStringAndFree());
        }

        private static string GetTermination(int index, int length)
        {
            return (index == length - 1) ? ";" : "";
        }

        public static readonly string InstrumentationHelperSource = @"
namespace Microsoft.CodeAnalysis.Runtime
{
    public static class Instrumentation
    {
        private static bool[][] _payloads;
        private static int[][] _fileIndices;
        private static System.Guid _mvid;

        public static bool[] CreatePayload(System.Guid mvid, int methodIndex, int[] fileIndices, ref bool[] payload, int payloadLength)
        {
            if (_mvid != mvid)
            {
                _payloads = new bool[100][];
                _fileIndices = new int[100][];
                _mvid = mvid;
            }

            if (System.Threading.Interlocked.CompareExchange(ref payload, new bool[payloadLength], null) == null)
            {
                _payloads[methodIndex] = payload;
                _fileIndices[methodIndex] = fileIndices;
                return payload;
            }

            return _payloads[methodIndex];
        }

        public static void FlushPayload()
        {
            System.Console.WriteLine(""Flushing"");
            if (_payloads == null)
            {
                return;
            }
            for (int i = 0; i < _payloads.Length; i++)
            {
                bool[] payload = _payloads[i];
                if (payload != null)
                {
                    System.Console.WriteLine(""Method "" + i.ToString());
                    for (int j = 0; j < _fileIndices[i].Length; j++)
                    {
                        System.Console.WriteLine(""File "" + _fileIndices[i][j].ToString());
                    }
                    for (int j = 0; j < payload.Length; j++)
                    {
                        System.Console.WriteLine(payload[j]);
                        payload[j] = false;
                    }
                }
            }
        }

        public static bool[] CreatePayload(System.Guid mvid, int methodIndex, int fileIndex, ref bool[] payload, int payloadLength)
        {
            return CreatePayload(mvid, methodIndex, new[] { fileIndex }, ref payload, payloadLength);
        }
    }
}
";
    }

    /// <summary>
    /// Helper to check compilations with test instrumentation.
    ///
    /// Use in three steps:
    /// 1. Initialize with expectations about methods, what spans they include and which spans were covered.
    ///     This is done with calls to Method followed by a call to True or False for each span within a method.
    /// 2. Use the ExpectedOutput to check that executing the compilation produces the expected instrumentation data.
    /// 3. Use CompleteCheck to check that your expectations about spans were correct.
    ///
    /// If you only do the third step, the test output will provide you with a template of code for the first step.
    /// </summary>
    public sealed class VBInstrumentationChecker : BaseInstrumentationChecker
    {
        private readonly string tab = "    ";

        public XCData ExpectedOutput { get { return new XCData(_consoleExpectations.ToString()); } }

        internal override void AddConsoleExpectation(string text)
        {
            _consoleExpectations.Append(text);
            _consoleExpectations.Append('\n');
        }

        public void CompleteCheck(Compilation compilation, XElement source)
        {
            CompleteCheck(compilation, (source.FirstNode as XText).Value.Trim());
        }

        internal override void Dump(DynamicAnalysisDataReader reader, string[] sourceLines)
        {
            var output = PooledStringBuilder.GetInstance();
            output.Builder.AppendLine("Template code for checking instrumentation results:");

            for (int method = 1; method <= reader.Methods.Length; method++)
            {
                var snippets = GetActualSnippets(method, reader, sourceLines);
                if (snippets.Length == 0)
                {
                    continue;
                }

                var methodTermination = GetTermination(0, snippets.Length);
                if (snippets[0] == null)
                {
                    output.Builder.AppendLine($"{tab}{tab}{tab}checker.Method({method}, 1){methodTermination}");
                }
                else
                {
                    output.Builder.AppendLine($"{tab}{tab}{tab}checker.Method({method}, 1, \"{snippets[0]}\"){methodTermination}");
                }

                for (int index = 1; index < snippets.Length; index++)
                {
                    string termination = GetTermination(index, snippets.Length);
                    if (snippets[index] == null)
                    {
                        output.Builder.AppendLine($"{tab}{tab}{tab}{tab}True(){termination}");
                    }
                    else
                    {
                        output.Builder.AppendLine($"{tab}{tab}{tab}{tab}True(\"{snippets[index]}\"){termination}");
                    }
                }
            }
            AssertEx.Fail(output.ToStringAndFree());
        }

        private static string GetTermination(int index, int length)
        {
            return (index == length - 1) ? "" : ".";
        }

        public static readonly string InstrumentationHelperSourceStr = @"
Namespace Microsoft.CodeAnalysis.Runtime

    Public Class Instrumentation

        Private Shared _payloads As Boolean()()
        Private Shared _fileIndices As Integer()()
        Private Shared _mvid As System.Guid

        Public Shared Function CreatePayload(mvid As System.Guid, methodIndex As Integer, fileIndices As Integer(), ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            If _mvid <> mvid Then
                _payloads = New Boolean(100)() {}
                _fileIndices = New Integer(100)() {}
                _mvid = mvid
            End If

            If System.Threading.Interlocked.CompareExchange(payload, new Boolean(payloadLength - 1) {}, Nothing) Is Nothing Then    
                If _payloads(methodIndex) IsNot Nothing Then
                    Throw New System.ArgumentException(""Overwriting existing payload array."")
                End If
                _payloads(methodIndex) = payload
                _fileIndices(methodIndex) = fileIndices
                Return payload
            End If

            Return _payloads(methodIndex)
        End Function

        Public Shared Sub FlushPayload()
            System.Console.WriteLine(""Flushing"")
            If _payloads Is Nothing Then
                Return
            End If
            For i As Integer = 0 To _payloads.Length - 1
                Dim payload As Boolean() = _payloads(i)
                if payload IsNot Nothing
                    System.Console.WriteLine(""Method "" & i.ToString())
                    For j As Integer = 0 To _fileIndices(i).Length - 1
                        System.Console.WriteLine(""File "" & _fileIndices(i)(j).ToString())
                    Next
                    For j As Integer = 0 To payload.Length - 1
                        System.Console.WriteLine(payload(j))
                        payload(j) = False
                    Next
                End If
            Next
        End Sub

        Public Shared Function CreatePayload(mvid As System.Guid, methodIndex As Integer, fileIndex As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            Return CreatePayload(mvid, methodIndex, { fileIndex }, payload, payloadLength)
        End Function
    End Class
End Namespace
";
        public static readonly XElement InstrumentationHelperSource = new XElement("file", new XAttribute("name", "c.vb"), InstrumentationHelperSourceStr);
    }

    public abstract class BaseInstrumentationChecker
    {
        protected StringBuilder _consoleExpectations = new StringBuilder();
        private readonly Dictionary<int /*method*/, MethodChecker> _spanExpectations = new Dictionary<int, MethodChecker>();

        protected BaseInstrumentationChecker()
        {
            AddConsoleExpectation($"Flushing");
        }

        /// <summary>
        /// Start recording expectations for a method.
        /// They need to be recorded in order, from method with smallest identifier to largest.
        /// </summary>
        /// <param name="method">The 1-based cardinal for referring to methods in data emitted and instrumentation data collected.</param>
        /// <param name="file">The 1-based cardinal identifying a source file, as collected by the instrumentation.</param>
        /// <param name="snippet">
        /// A short snippet of code capturing the what you expect the span for this method to look like.
        /// It will be verified against the start of the actual method source span.
        /// If no snippet is passed in, then snippet validation will be disabled for the whole method (subsequent calls to <c>True</c> or <c>False</c>).
        /// </param>
        public MethodChecker Method(int method, int file, string snippet = null, bool expectBodySpan = true)
        {
            AddConsoleExpectation($"Method {method}");
            AddConsoleExpectation($"File {file}");
            var result = new MethodChecker(this, noSnippets: snippet == null);

            // Most methods have a span that indicates that the method has been entered.
            if (expectBodySpan)
            {
                result = result.True(snippet);
            }

            if (snippet != null)
            {
                _spanExpectations.Add(method, result);
            }

            return result;
        }

        /// <summary>
        /// Verify the recorded expectations.
        /// </summary>
        public void CompleteCheck(Compilation compilation, string source)
        {
            var peImage = compilation.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");
            string[] sourceLines = source.Split('\n');

            if (_spanExpectations.Count == 0)
            {
                Dump(reader, sourceLines);
                return;
            }

            foreach (int method in _spanExpectations.Keys)
            {
                var actualSnippets = GetActualSnippets(method, reader, sourceLines);
                var expectedSnippets = _spanExpectations[method].SnippetExpectations;

                AssertEx.Equal(expectedSnippets, actualSnippets, new SnippetComparer(), $"Validation of method {method} failed.");
            }
        }

        // Fails the test and outputs a template for updating the test code with appropriate expectations.
        internal abstract void Dump(DynamicAnalysisDataReader reader, string[] sourceLines);

        internal abstract void AddConsoleExpectation(string text);

        internal static ImmutableArray<string> GetActualSnippets(int method, DynamicAnalysisDataReader reader, string[] sourceLines)
        {
            var actualSpans = reader.GetSpans(reader.Methods[method - 1].Blob);

            return actualSpans.SelectAsArray(map: (span, lines) =>
            {
                if (span.StartLine >= lines.Length)
                {
                    return null;
                }
                return lines[span.StartLine].Substring(span.StartColumn).TrimEnd(new[] { '\r', '\n', ' ' });
            }, arg: sourceLines);
        }

        public class MethodChecker
        {
            private readonly List<string> _snippetExpectations;
            private readonly BaseInstrumentationChecker _checker;

            public MethodChecker(BaseInstrumentationChecker checker, bool noSnippets = false)
            {
                _checker = checker;

                if (!noSnippets)
                {
                    _snippetExpectations = new List<string>();
                }
            }

            public string[] SnippetExpectations { get { return _snippetExpectations.ToArray(); } }

            /// <summary>
            /// Records the expectation that the following span will be covered and resembles the provided snippet.
            /// </summary>
            public MethodChecker True(string expectedSourceSnippet = null)
            {
                return Expect("True", expectedSourceSnippet);
            }

            /// <summary>
            /// Records the expectation that the following span will *not* be covered and resembles the provided snippet.
            /// </summary>
            public MethodChecker False(string expectedSourceSnippet = null)
            {
                return Expect("False", expectedSourceSnippet);
            }

            private MethodChecker Expect(string text, string expectedSourceSnippet)
            {
                _checker.AddConsoleExpectation(text);
                if (_snippetExpectations != null)
                {
                    _snippetExpectations.Add(expectedSourceSnippet);
                }
                else
                {
                    Assert.True(expectedSourceSnippet == null,
                        "You must pass a snippet when checking the method with M if you intend to verify snippets within the method.");
                }

                return this;
            }
        }

        private class SnippetComparer : IEqualityComparer<string>
        {
            public bool Equals(string expected, string actual)
            {
                if (string.IsNullOrWhiteSpace(expected))
                {
                    return false;
                }
                return actual.StartsWith(expected);
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
