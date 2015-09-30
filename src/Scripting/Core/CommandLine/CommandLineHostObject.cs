// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public sealed class CommandLineHostObject
    {
        private readonly TextWriter _outputWriter;
        private ObjectFormattingOptions _formattingOptions;
        private readonly ObjectFormatter _objectFormatter;

        public readonly CancellationToken CancellationToken;

        public string[] Args;
        public int ExitCode;

        // TODO (tomat): Add ReferencePaths, SourcePaths

        internal CommandLineHostObject(TextWriter outputWriter, ObjectFormatter objectFormatter, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;

            _formattingOptions = new ObjectFormattingOptions(
                memberFormat: MemberDisplayFormat.Inline,
                quoteStrings: true,
                useHexadecimalNumbers: false,
                maxOutputLength: 1024,
                memberIndentation: "  ");

            _outputWriter = outputWriter;
            _objectFormatter = objectFormatter;
        }

        public void Print(object value)
        {
            _outputWriter.WriteLine(_objectFormatter.FormatObject(value, _formattingOptions));
        }
    }
}
