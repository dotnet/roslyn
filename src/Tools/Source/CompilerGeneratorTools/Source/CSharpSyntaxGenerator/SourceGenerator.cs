// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We only build the Source Generator in the netstandard target
#if NETSTANDARD

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CSharpSyntaxGenerator
{
    [Generator]
    public sealed class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxXml = context.AdditionalFiles.SingleOrDefault(a => Path.GetFileName(a.Path) == "Syntax.xml");

            if (syntaxXml == null)
            {
                return;
            }

            var syntaxXmlText = syntaxXml.GetText();

            if (syntaxXmlText == null)
            {
                // TODO: report a diagnostic here
                return;
            }

            var reader = XmlReader.Create(new SourceTextReader(syntaxXmlText), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var serializer = new XmlSerializer(typeof(Tree));
            var tree = (Tree)serializer.Deserialize(reader);

            TreeFlattening.FlattenChildren(tree);

            AddResult(writer => SourceWriter.WriteMain(writer, tree, context.CancellationToken), "Syntax.xml.Main.Generated.cs");
            AddResult(writer => SourceWriter.WriteInternal(writer, tree, context.CancellationToken), "Syntax.xml.Internal.Generated.cs");
            AddResult(writer => SourceWriter.WriteSyntax(writer, tree, context.CancellationToken), "Syntax.xml.Syntax.Generated.cs");

            void AddResult(Action<TextWriter> writeFunction, string hintName)
            {
                // Write out the contents to a StringBuilder to avoid creating a single large string
                // in memory
                var stringBuilder = new StringBuilder();
                using (var textWriter = new StringWriter(stringBuilder))
                {
                    writeFunction(textWriter);
                }

                // And create a SourceText from the StringBuilder, once again avoiding allocating a single massive string
                context.AddSource(hintName, SourceText.From(new StringBuilderReader(stringBuilder), stringBuilder.Length, encoding: Encoding.UTF8));
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
