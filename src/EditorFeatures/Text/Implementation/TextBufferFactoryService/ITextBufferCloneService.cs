// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal interface ITextBufferCloneService : IWorkspaceService
    {
        /// <summary>
        /// get new <see cref="ITextBuffer"/> from <see cref="SnapshotSpan"/> with <see cref="IContentTypeRegistryService.UnknownContentType"/>
        /// 
        /// it is explicitly marked with unknown content type so that it can't be used with editor directly
        /// </summary>
        ITextBuffer CloneWithUnknownContentType(SnapshotSpan span);

        /// <summary>
        /// get new <see cref="ITextBuffer"/> from <see cref="ITextImage"/> with <see cref="IContentTypeRegistryService.UnknownContentType"/>
        /// 
        /// it is explicitly marked with unknown content type so that it can't be used with editor directly
        /// </summary>
        ITextBuffer CloneWithUnknownContentType(ITextImage textImage);

        /// <summary>
        /// get new <see cref="ITextBuffer"/> from <see cref="SourceText"/> with <see cref="ContentTypeNames.RoslynContentType"/>
        /// </summary>
        ITextBuffer CloneWithRoslynContentType(SourceText sourceText);

        /// <summary>
        /// get new <see cref="ITextBuffer"/> from <see cref="SourceText"/> with <see cref="IContentType"/>
        /// </summary>
        ITextBuffer Clone(SourceText sourceText, IContentType contentType);
    }
}
