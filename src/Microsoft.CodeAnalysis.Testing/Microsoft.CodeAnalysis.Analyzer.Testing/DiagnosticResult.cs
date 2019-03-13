// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Structure that stores information about a <see cref="Diagnostic"/> appearing in a source.
    /// </summary>
    public struct DiagnosticResult
    {
        public static readonly DiagnosticResult[] EmptyDiagnosticResults = { };

        private static readonly object[] EmptyArguments = new object[0];

        private ImmutableArray<FileLinePositionSpan> _spans;
        private bool _suppressMessage;
        private string _message;

        public DiagnosticResult(string id, DiagnosticSeverity severity)
            : this()
        {
            Id = id;
            Severity = severity;
        }

        public DiagnosticResult(DiagnosticDescriptor descriptor)
            : this()
        {
            Id = descriptor.Id;
            Severity = descriptor.DefaultSeverity;
            MessageFormat = descriptor.MessageFormat;
        }

        public ImmutableArray<FileLinePositionSpan> Spans => _spans.IsDefault ? ImmutableArray<FileLinePositionSpan>.Empty : _spans;

        public DiagnosticSeverity Severity { get; private set; }

        public string Id { get; }

        public string Message
        {
            get
            {
                if (_suppressMessage)
                {
                    return null;
                }

                if (_message != null)
                {
                    return _message;
                }

                if (MessageFormat != null)
                {
                    return string.Format(MessageFormat.ToString(), MessageArguments ?? EmptyArguments);
                }

                return null;
            }
        }

        public LocalizableString MessageFormat { get; private set; }

        public object[] MessageArguments { get; private set; }

        public bool HasLocation => !Spans.IsEmpty;

        public static DiagnosticResult CompilerError(string identifier)
            => new DiagnosticResult(identifier, DiagnosticSeverity.Error);

        public static DiagnosticResult CompilerWarning(string identifier)
            => new DiagnosticResult(identifier, DiagnosticSeverity.Warning);

        public DiagnosticResult WithSeverity(DiagnosticSeverity severity)
        {
            var result = this;
            result.Severity = severity;
            return result;
        }

        public DiagnosticResult WithArguments(params object[] arguments)
        {
            var result = this;
            result.MessageArguments = arguments;
            return result;
        }

        public DiagnosticResult WithMessage(string message)
        {
            var result = this;
            result._message = message;
            result._suppressMessage = message is null;
            return result;
        }

        public DiagnosticResult WithMessageFormat(LocalizableString messageFormat)
        {
            var result = this;
            result.MessageFormat = messageFormat;
            return result;
        }

        public DiagnosticResult WithLocation(int line, int column)
            => WithLocation(path: string.Empty, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(LinePosition location)
            => WithLocation(path: string.Empty, location);

        public DiagnosticResult WithLocation(string path, int line, int column)
            => WithLocation(path, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(string path, LinePosition location)
            => AppendSpan(new FileLinePositionSpan(path, location, location));

        public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
            => WithSpan(path: string.Empty, startLine, startColumn, endLine, endColumn);

        public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
            => AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1)));

        public DiagnosticResult WithSpan(FileLinePositionSpan span)
            => AppendSpan(span);

        public DiagnosticResult WithDefaultPath(string path)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var result = this;
            var spans = Spans.ToBuilder();
            for (var i = 0; i < spans.Count; i++)
            {
                if (spans[i].Path == string.Empty)
                {
                    spans[i] = new FileLinePositionSpan(path, spans[i].Span);
                }
            }

            result._spans = spans.MoveToImmutable();
            return result;
        }

        public DiagnosticResult WithLineOffset(int offset)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var result = this;
            var spansBuilder = result.Spans.ToBuilder();
            for (var i = 0; i < result.Spans.Length; i++)
            {
                var newStartLinePosition = new LinePosition(result.Spans[i].StartLinePosition.Line + offset, result.Spans[i].StartLinePosition.Character);
                var newEndLinePosition = new LinePosition(result.Spans[i].EndLinePosition.Line + offset, result.Spans[i].EndLinePosition.Character);

                spansBuilder[i] = new FileLinePositionSpan(result.Spans[i].Path, newStartLinePosition, newEndLinePosition);
            }

            result._spans = spansBuilder.MoveToImmutable();
            return result;
        }

        private DiagnosticResult AppendSpan(FileLinePositionSpan span)
        {
            var result = this;
            result._spans = Spans.Add(span);
            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (HasLocation)
            {
                var location = Spans[0];
                builder.Append(location.Path == string.Empty ? "?" : location.Path);
                builder.Append("(");
                builder.Append(location.StartLinePosition.Line + 1);
                builder.Append(",");
                builder.Append(location.StartLinePosition.Character + 1);
                if (location.EndLinePosition != location.StartLinePosition)
                {
                    builder.Append(",");
                    builder.Append(location.EndLinePosition.Line + 1);
                    builder.Append(",");
                    builder.Append(location.EndLinePosition.Character + 1);
                }

                builder.Append("): ");
            }

            builder.Append(Severity.ToString().ToLowerInvariant());
            builder.Append(" ");
            builder.Append(Id);

            try
            {
                var message = Message;
                if (message != null)
                {
                    builder.Append(": ").Append(message);
                }
            }
            catch (FormatException)
            {
                // A message format is provided without arguments, so we print the unformatted string
                Debug.Assert(MessageFormat != null, $"Assertion failed: {nameof(MessageFormat)} != null");
                builder.Append(": ").Append(MessageFormat);
            }

            return builder.ToString();
        }
    }
}
