// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [Export(typeof(IDocumentOptionsProviderFactory))]
    class EditorConfigDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        public IDocumentOptionsProvider Create(Workspace workspace)
        {
            return new EditorConfigDocumentOptionsProvider(workspace);
        }
    }
}
