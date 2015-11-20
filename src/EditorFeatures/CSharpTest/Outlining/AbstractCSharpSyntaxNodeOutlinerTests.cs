// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public abstract class AbstractCSharpSyntaxNodeOutlinerTests<TSyntaxNode> : AbstractSyntaxNodeOutlinerTests<TSyntaxNode>
        where TSyntaxNode : SyntaxNode
    {
        protected override string LanguageName => LanguageNames.CSharp;
    }
}
