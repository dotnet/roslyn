// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal interface IUnifiedArgumentSyntax
    {
        bool IsDefault { get; }
        bool IsNamed { get; }
        string GetName();
        IUnifiedArgumentSyntax WithName(string name);
        IUnifiedArgumentSyntax WithAdditionalAnnotations(SyntaxAnnotation annotation);
    }
}
