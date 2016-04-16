// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    internal static class Options
    {
        internal static readonly CSharpParseOptions Script = new CSharpParseOptions(kind: SourceCodeKind.Script);
        internal static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular);
    }
}
