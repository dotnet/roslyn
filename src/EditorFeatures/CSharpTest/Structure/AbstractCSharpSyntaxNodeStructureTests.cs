// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public abstract class AbstractCSharpSyntaxNodeStructureTests<TSyntaxNode> :
        AbstractSyntaxNodeStructureProviderTests<TSyntaxNode>
        where TSyntaxNode : SyntaxNode
    {
        protected sealed override string LanguageName => LanguageNames.CSharp;
    }
}
