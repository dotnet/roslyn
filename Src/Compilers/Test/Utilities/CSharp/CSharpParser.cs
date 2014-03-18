// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


// Disabling this part of code which will be fixed once ParseTreeVisualizer changes are completed
#if FALSE
public class CSharpParser : IParser
{

    public SyntaxNode Parse(String code)
    {
        return Parse(code, null);
    }

    public SyntaxNode Parse(String code, ParseOptions options)
    {
        return Syntax.ParseCompilationUnit(code);
    }

}
#endif