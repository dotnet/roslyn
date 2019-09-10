// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(ITextFactoryService), ServiceLayer.Default), Shared]
    internal class TextFactoryService : ITextFactoryService
    {
        [ImportingConstructor]
        public TextFactoryService()
        {
        }

        public SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return EncodedStringText.Create(stream, defaultEncoding);
        }

        public SourceText CreateText(TextReader reader, Encoding encoding, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var textReaderWithLength = reader as TextReaderWithLength;
            if (textReaderWithLength != null)
            {
                return SourceText.From(textReaderWithLength, textReaderWithLength.Length, encoding);
            }

            return SourceText.From(reader.ReadToEnd(), encoding);
        }
    }
}

