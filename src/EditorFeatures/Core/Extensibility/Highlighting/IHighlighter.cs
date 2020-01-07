// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IHighlighter
    {
        void AddHighlights(SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken);
    }
}
