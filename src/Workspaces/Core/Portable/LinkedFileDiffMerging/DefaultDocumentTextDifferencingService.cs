// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    [ExportWorkspaceService(typeof(IDocumentTextDifferencingService), ServiceLayer.Default), Shared]
    internal class DefaultDocumentTextDifferencingService : IDocumentTextDifferencingService
    {
        [ImportingConstructor]
        public DefaultDocumentTextDifferencingService()
        {
        }

        public Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            return GetTextChangesAsync(oldDocument, newDocument, TextDifferenceTypes.Word, cancellationToken);
        }

        public async Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, TextDifferenceTypes preferredDifferenceType, CancellationToken cancellationToken)
        {
            var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
            return changes.ToImmutableArray();
        }
    }
}
