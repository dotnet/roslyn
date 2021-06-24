// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We only build the Source Generator in the netstandard target
#if NETSTANDARD

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CSharpSyntaxGenerator
{
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor s_MissingSyntaxXml = new DiagnosticDescriptor(
            "CSSG1001",
            title: "Syntax.xml is missing",
            messageFormat: "The Syntax.xml file was not included in the project, so we are not generating source.",
            category: "SyntaxGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_UnableToReadSyntaxXml = new DiagnosticDescriptor(
            "CSSG1002",
            title: "Syntax.xml could not be read",
            messageFormat: "The Syntax.xml file could not even be read. Does it exist?",
            category: "SyntaxGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_SyntaxXmlError = new DiagnosticDescriptor(
            "CSSG1003",
            title: "Syntax.xml has a syntax error",
            messageFormat: "{0}",
            category: "SyntaxGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // find the syntax file
            var info = context.AdditionalTextsProvider.Collect().Select((texts, _) => new SyntaxInfo(texts.SingleOrDefault(a => Path.GetFileName(a.Path) == "Syntax.xml")));

            // get the text
            info = info.Select((i, ct) => i with { Text = i.File?.GetText(ct) });

            // load the tree
            var result = info.Select<SyntaxInfo, Result>((info, ct) =>
            {
                if (info.File is null)
                {
                    return new DiagnosticResult(Diagnostic.Create(s_MissingSyntaxXml, location: null));
                }
                if (info.Text is null)
                {
                    return new DiagnosticResult(Diagnostic.Create(s_MissingSyntaxXml, location: null)); ;
                }

                using var reader = XmlReader.Create(new SourceTextReader(info.Text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                try
                {
                    var serializer = new XmlSerializer(typeof(Tree));
                    var tree = (Tree)serializer.Deserialize(reader);
                    TreeFlattening.FlattenChildren(tree);

                    return new SuccessResult(
                            buildResult(writer => SourceWriter.WriteMain(writer, tree, ct), "Syntax.xml.Main.Generated.cs"),
                            buildResult(writer => SourceWriter.WriteInternal(writer, tree, ct), "Syntax.xml.Internal.Generated.cs"),
                            buildResult(writer => SourceWriter.WriteSyntax(writer, tree, ct), "Syntax.xml.Syntax.Generated.cs"));
                }
                catch (InvalidOperationException ex) when (ex.InnerException is XmlException xmlException)
                {
                    var line = info.Text.Lines[xmlException.LineNumber - 1]; // LineNumber is one-based.
                    int offset = xmlException.LinePosition - 1; // LinePosition is one-based
                    var position = line.Start + offset;
                    var span = new TextSpan(position, 0);
                    var lineSpan = info.Text.Lines.GetLinePositionSpan(span);

                    return new DiagnosticResult(Diagnostic.Create(
                            s_SyntaxXmlError,
                            location: Location.Create(info.File.Path, span, lineSpan),
                            xmlException.Message)
                    );
                }
            }).WithComparer(new ResultComparer());

            // do the actual generation
            context.RegisterSourceOutput(result, (spc, result) =>
            {
                if (result is DiagnosticResult dr)
                {
                    spc.ReportDiagnostic(dr.Diagnostic);
                    return;
                }
                else if (result is SuccessResult sr)
                {
                    // Create a SourceText from each StringBuilder, once again avoiding allocating a single massive string
                    addResult(spc, sr.Main, "Syntax.xml.Main.Generated.cs");
                    addResult(spc, sr.Internal, "Syntax.xml.Internal.Generated.cs");
                    addResult(spc, sr.Syntax, "Syntax.xml.Syntax.Generated.cs");
                }
            });

            static StringBuilder buildResult(Action<TextWriter> writeFunction, string hintName)
            {
                // Write out the contents to a StringBuilder to avoid creating a single large string
                // in memory
                var stringBuilder = new StringBuilder();
                using (var textWriter = new StringWriter(stringBuilder))
                {
                    writeFunction(textWriter);
                }
                return stringBuilder;
            }

            static void addResult(SourceProductionContext spc, StringBuilder stringBuilder, string hintName)
            {
                // And create a SourceText from the StringBuilder, once again avoiding allocating a single massive string
                spc.AddSource(hintName, SourceText.From(new StringBuilderReader(stringBuilder), stringBuilder.Length, encoding: Encoding.UTF8));
            }
        }

        private record Result();

        private record SuccessResult(StringBuilder Main, StringBuilder Internal, StringBuilder Syntax) : Result;

        private record DiagnosticResult(Diagnostic Diagnostic) : Result;

        private record SyntaxInfo(AdditionalText? File, SourceText? Text = null);

        private sealed class ResultComparer : IEqualityComparer<Result>
        {
            public bool Equals(Result x, Result y)
            {
                if (x is SuccessResult sx && y is SuccessResult sy)
                {
                    return sx.Main.Equals(sy.Main)
                        && sx.Internal.Equals(sy.Internal)
                        && sx.Syntax.Equals(sy.Syntax);
                }
                return x.Equals(y);
            }

            public int GetHashCode(Result obj) => obj.GetHashCode();
        }

        private sealed class SourceTextReader : TextReader
        {
            private readonly SourceText _sourceText;
            private int _position;

            public SourceTextReader(SourceText sourceText)
            {
                _sourceText = sourceText;
                _position = 0;
            }

            public override int Peek()
            {
                if (_position == _sourceText.Length)
                {
                    return -1;
                }

                return _sourceText[_position];
            }

            public override int Read()
            {
                if (_position == _sourceText.Length)
                {
                    return -1;
                }

                return _sourceText[_position++];
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var charsToCopy = Math.Min(count, _sourceText.Length - _position);
                _sourceText.CopyTo(_position, buffer, index, charsToCopy);
                _position += charsToCopy;
                return charsToCopy;
            }
        }

        private sealed class StringBuilderReader : TextReader
        {
            private readonly StringBuilder _stringBuilder;
            private int _position;

            public StringBuilderReader(StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;
                _position = 0;
            }

            public override int Peek()
            {
                if (_position == _stringBuilder.Length)
                {
                    return -1;
                }

                return _stringBuilder[_position];
            }

            public override int Read()
            {
                if (_position == _stringBuilder.Length)
                {
                    return -1;
                }

                return _stringBuilder[_position++];
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var charsToCopy = Math.Min(count, _stringBuilder.Length - _position);
                _stringBuilder.CopyTo(_position, buffer, index, charsToCopy);
                _position += charsToCopy;
                return charsToCopy;
            }
        }
    }
}

#endif
