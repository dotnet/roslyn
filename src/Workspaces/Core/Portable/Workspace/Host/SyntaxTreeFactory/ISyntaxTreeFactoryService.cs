﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Factory service for creating syntax trees.
    /// </summary>
    internal interface ISyntaxTreeFactoryService : ILanguageService
    {
        ParseOptions GetDefaultParseOptions();

        // new tree from root node
        SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root);

        // new tree from text
        SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);

        bool CanCreateRecoverableTree(SyntaxNode root);

        // new recoverable tree from root node
        SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, Encoding encoding, SyntaxNode root);

        SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);
    }
}
