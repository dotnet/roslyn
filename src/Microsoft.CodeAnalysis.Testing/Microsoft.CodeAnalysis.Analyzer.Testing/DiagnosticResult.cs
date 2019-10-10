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
    public readonly struct DiagnosticResult
    {
        public static readonly DiagnosticResult[] EmptyDiagnosticResults = { };

        private static readonly object[] EmptyArguments = new object[0];

        private readonly ImmutableArray<DiagnosticLocation> _spans;
        private readonly bool _suppressMessage;
        private readonly string? _message;

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

        private DiagnosticResult(
            ImmutableArray<DiagnosticLocation> spans,
            bool suppressMessage,
            string? message,
            DiagnosticSeverity severity,
            string id,
            LocalizableString messageFormat,
            object[] messageArguments)
        {
            _spans = spans;
            _suppressMessage = suppressMessage;
            _message = message;
            Severity = severity;
            Id = id;
            MessageFormat = messageFormat;
            MessageArguments = messageArguments;
        }

        public ImmutableArray<DiagnosticLocation> Spans => _spans.IsDefault ? ImmutableArray<DiagnosticLocation>.Empty : _spans;

        public DiagnosticSeverity Severity { get; }

        public string Id { get; }

        public string? Message
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
                    try
                    {
                        return string.Format(MessageFormat.ToString(), MessageArguments ?? EmptyArguments);
                    }
                    catch (FormatException)
                    {
                        return MessageFormat.ToString();
                    }
                }

                return null;
            }
        }

        public LocalizableString MessageFormat { get; }

        public object[] MessageArguments { get; }

        public bool HasLocation => !Spans.IsEmpty;

        public static DiagnosticResult CompilerError(string identifier)
            => new DiagnosticResult(identifier, DiagnosticSeverity.Error);

        public static DiagnosticResult CompilerWarning(string identifier)
            => new DiagnosticResult(identifier, DiagnosticSeverity.Warning);

        public DiagnosticResult WithSeverity(DiagnosticSeverity severity)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
        }

        public DiagnosticResult WithArguments(params object[] arguments)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: arguments);
        }

        public DiagnosticResult WithMessage(string? message)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: message is null,
                message: message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
        }

        public DiagnosticResult WithMessageFormat(LocalizableString messageFormat)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: messageFormat,
                messageArguments: MessageArguments);
        }

        public DiagnosticResult WithNoLocation()
        {
            return new DiagnosticResult(
                spans: ImmutableArray<DiagnosticLocation>.Empty,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
        }

        public DiagnosticResult WithLocation(int line, int column)
            => WithLocation(path: string.Empty, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(LinePosition location)
            => WithLocation(path: string.Empty, location);

        public DiagnosticResult WithLocation(string path, int line, int column)
            => WithLocation(path, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(string path, LinePosition location)
            => AppendSpan(new FileLinePositionSpan(path, location, location), DiagnosticLocationOptions.IgnoreLength);

        public DiagnosticResult WithLocation(string path, LinePosition location, DiagnosticLocationOptions options)
            => AppendSpan(new FileLinePositionSpan(path, location, location), options | DiagnosticLocationOptions.IgnoreLength);

        public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
            => WithSpan(path: string.Empty, startLine, startColumn, endLine, endColumn);

        public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
            => AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1)), DiagnosticLocationOptions.None);

        public DiagnosticResult WithSpan(FileLinePositionSpan span)
            => AppendSpan(span, DiagnosticLocationOptions.None);

        public DiagnosticResult WithSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
            => AppendSpan(span, options);

        public DiagnosticResult WithDefaultPath(string path)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var spans = Spans.ToBuilder();
            for (var i = 0; i < spans.Count; i++)
            {
                if (spans[i].Span.Path == string.Empty)
                {
                    spans[i] = new DiagnosticLocation(new FileLinePositionSpan(path, spans[i].Span.Span), spans[i].Options);
                }
            }

            return new DiagnosticResult(
                spans: spans.MoveToImmutable(),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
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
                var newStartLinePosition = new LinePosition(result.Spans[i].Span.StartLinePosition.Line + offset, result.Spans[i].Span.StartLinePosition.Character);
                var newEndLinePosition = new LinePosition(result.Spans[i].Span.EndLinePosition.Line + offset, result.Spans[i].Span.EndLinePosition.Character);

                spansBuilder[i] = new DiagnosticLocation(new FileLinePositionSpan(result.Spans[i].Span.Path, newStartLinePosition, newEndLinePosition), result.Spans[i].Options);
            }

            return new DiagnosticResult(
                spans: spansBuilder.MoveToImmutable(),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
        }

        private DiagnosticResult AppendSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
        {
            return new DiagnosticResult(
                spans: Spans.Add(new DiagnosticLocation(span, options)),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (HasLocation)
            {
                var location = Spans[0];
                builder.Append(location.Span.Path == string.Empty ? "?" : location.Span.Path);
                builder.Append("(");
                builder.Append(location.Span.StartLinePosition.Line + 1);
                builder.Append(",");
                builder.Append(location.Span.StartLinePosition.Character + 1);
                if (!location.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
                {
                    builder.Append(",");
                    builder.Append(location.Span.EndLinePosition.Line + 1);
                    builder.Append(",");
                    builder.Append(location.Span.EndLinePosition.Character + 1);
                }

                builder.Append("): ");
            }

            builder.Append(Severity.ToString().ToLowerInvariant());
            builder.Append(" ");
            builder.Append(Id);

            var message = Message;
            if (message != null)
            {
                builder.Append(": ").Append(message);
            }

            return builder.ToString();
        }
    }
}
