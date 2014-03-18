// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


#if FALSE
public class VBSyntaxNodeKindProvider : ISyntaxNodeKindProvider
{

    public String get_Kind(SyntaxNode node)
    {
        return ((CSharpSyntaxNode)node).Kind.ToString();
    }

}
#endif