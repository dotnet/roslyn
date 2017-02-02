// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeMethodStatic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MakeMethodStatic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeMethodStatic
{
    public partial class MakeMethodStaticTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpMakeMethodStaticDiagnosticAnalyzer(), new CSharpMakeMethodStaticCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodStatic)]
        public async Task Test()
        {
            await TestAsync(
@"class C
{
    void [||]Method() { }
    void Usages(C instance) 
    {
        Method();
        this.Method();
        instance.Method();
        new C().Method();

        Action _ = Method;
        Action _ = this.Method;
        Action _ = instance.Method;
        Action _ = new C().Method;
    }
}",
@"class C
{
    static void Method() { }
    void Usages(C instance) 
    {
        Method();
        Method();
        Method();
        Method();

        Action _ = Method;
        Action _ = Method;
        Action _ = Method;
        Action _ = Method;
    }
}");
        }
    }
}