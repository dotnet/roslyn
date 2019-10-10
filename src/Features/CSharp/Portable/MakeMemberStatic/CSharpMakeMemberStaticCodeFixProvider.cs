// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.MakeMemberStatic;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.MakeMemberStatic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMakeMemberStaticCodeFixProvider)), Shared]
    internal sealed class CSharpMakeMemberStaticCodeFixProvider : AbstractMakeMemberStaticCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS0708" // 'MyMethod': cannot declare instance members in a static class
            );
    }
}
