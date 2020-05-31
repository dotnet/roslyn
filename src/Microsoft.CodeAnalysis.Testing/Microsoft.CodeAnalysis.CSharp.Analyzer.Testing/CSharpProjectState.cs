// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
