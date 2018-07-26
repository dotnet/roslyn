// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(IPeekResultFactory))]
    [Shared]
    [PartNotDiscoverable]
    internal sealed class FakePeekResultFactory : IPeekResultFactory
    {
        public IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idLine, int idIndex)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idLine, int idIndex, bool isReadOnly)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idStartLine, int idStartIndex, int idEndLine, int idEndIndex)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idStartLine, int idStartIndex, int idEndLine, int idEndIndex, bool isReadOnly)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idStartLine, int idStartIndex, int idEndLine, int idEndIndex, bool isReadOnly, Guid editorDestination)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo2 displayInfo, ImageMoniker image, string filePath, int startLine, int startIndex, int endLine, int endIndex, int idStartLine, int idStartIndex, int idEndLine, int idEndIndex, bool isReadOnly, Guid editorDestination, Action<IPeekResult, object, object> postNavigationCallback)
        {
            throw new NotImplementedException();
        }

        public IDocumentPeekResult Create(IPeekResultDisplayInfo displayInfo, string filePath, Span eoiSpan, int idPosition, bool isReadOnly)
        {
            throw new NotImplementedException();
        }

        public IExternallyBrowsablePeekResult Create(IPeekResultDisplayInfo displayInfo, Action browseAction)
        {
            throw new NotImplementedException();
        }
    }
}
