// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddParameterCheck;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddParameterCheck
{
    public class AddParameterCheckTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpAddParameterCheckCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterCheck)]
        public async Task TestSimpleReferenceType()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }
    }
}