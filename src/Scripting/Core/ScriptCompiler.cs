﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal abstract class ScriptCompiler
    {
        public abstract Compilation CreateSubmission(Script script);
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }
        public abstract StringComparer IdentifierComparer { get; }

        public abstract SyntaxTree ParseSubmission(SourceText text, CancellationToken cancellationToken);
        public abstract bool IsCompleteSubmission(SyntaxTree tree);

        public abstract ImmutableArray<string> GetGlobalImportStrings(Script script);
        public abstract ImmutableArray<string> GetLocalImportStrings(Script script);
    }
}
