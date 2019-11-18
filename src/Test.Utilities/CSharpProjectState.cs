// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    /// <summary>
    /// This class is a copy-paste from
    /// https://github.com/dotnet/roslyn-sdk/blob/master/src/Microsoft.CodeAnalysis.Testing/Microsoft.CodeAnalysis.CSharp.Analyzer.Testing/CSharpProjectState.cs
    /// </summary>
    public class CSharpProjectState : ProjectState
    {
        public CSharpProjectState(string name)
            : base(name, defaultPrefix: "Test", defaultExtension: "cs")
        {
        }

        public override string Language => LanguageNames.CSharp;
    }
}