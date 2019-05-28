// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpProjectState : ProjectState
    {
        public CSharpProjectState(string name)
            : base(name, defaultPrefix: "Test", defaultExtension: "cs")
        {
        }

        public override string Language => LanguageNames.CSharp;
    }
}
