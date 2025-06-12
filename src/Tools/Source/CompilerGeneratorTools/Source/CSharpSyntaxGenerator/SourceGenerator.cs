// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We only build the Source Generator in the netstandard target
#if NETSTANDARD

#nullable enable

using System;
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
    public sealed class SourceGenerator : IIncrementalGenerator
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
            var syntaxXmlFiles = context.AdditionalTextsProvider.Where(at => Path.GetFileName(at.Path) == "Syntax.xml").Collect();

            context.RegisterSourceOutput(syntaxXmlFiles, static (context, syntaxXmlFiles) =>
            {
                var input = syntaxXmlFiles.SingleOrDefault();

                if (input == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_MissingSyntaxXml, location: null));
                    return;
                }

                var inputText = input.GetText();
                if (inputText == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_UnableToReadSyntaxXml, location: null));
                    return;
                }

                Tree tree;

                try
                {
                    var reader = XmlReader.Create(new SourceTextReader(inputText), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                    var serializer = new XmlSerializer(typeof(Tree));
                    tree = (Tree)serializer.Deserialize(reader);
                }
                catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
                {
                    var xmlException = (XmlException)ex.InnerException;

                    var line = inputText.Lines[xmlException.LineNumber - 1]; // LineNumber is one-based.
                    int offset = xmlException.LinePosition - 1; // LinePosition is one-based
                    var position = line.Start + offset;
                    var span = new TextSpan(position, 0);
                    var lineSpan = inputText.Lines.GetLinePositionSpan(span);

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            s_SyntaxXmlError,
                            location: Location.Create(input.Path, span, lineSpan),
                            xmlException.Message));

                    return;

                }

                DoGeneration(tree, context, context.CancellationToken);
            });
        }

        private static void DoGeneration(
            Tree tree,
            SourceProductionContext context,
            CancellationToken cancellationToken)
        {
            TreeFlattening.FlattenChildren(tree);

            var sourcesBuilder = ImmutableArray.CreateBuilder<(string hintName, SourceText sourceText)>();
            addResult(writer => SourceWriter.WriteMain(writer, tree, cancellationToken), "Syntax.xml.Main.Generated.cs");
            addResult(writer => SourceWriter.WriteInternal(writer, tree, cancellationToken), "Syntax.xml.Internal.Generated.cs");
            addResult(writer => SourceWriter.WriteSyntax(writer, tree, cancellationToken), "Syntax.xml.Syntax.Generated.cs");

            void addResult(Action<TextWriter> writeFunction, string hintName)
            {
                // Write out the contents to a StringBuilder to avoid creating a single large string
                // in memory
                var stringBuilder = new StringBuilder();
                using (var textWriter = new StringWriter(stringBuilder))
                {
                    writeFunction(textWriter);
                }

                // And create a SourceText from the StringBuilder, once again avoiding allocating a single massive string
                var sourceText = new StringBuilderText(stringBuilder, encoding: Encoding.UTF8);
                context.AddSource(hintName, sourceText);
            }
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

        private sealed class StringBuilderText : SourceText
        {
            private readonly StringBuilder _builder;
            private readonly Encoding? _encoding;

            public StringBuilderText(StringBuilder builder, Encoding? encoding)
            {
                _builder = builder;
                _encoding = encoding;
            }

            public override Encoding? Encoding => _encoding;
            public override int Length => _builder.Length;
            public override char this[int position] => _builder[position];

            public override string ToString(TextSpan span)
                => _builder.ToString(span.Start, span.Length);

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
                => _builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}

#endif
