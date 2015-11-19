// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeVariableManipulationTests : AbstractFileCodeElementTests
    {
        public FileCodeVariableManipulationTests()
            : base(@"class Foo
{
    private int bar;
}")
        {
        }

        private async Task<CodeVariable> GetCodeVariableAsync(params object[] path)
        {
            return (CodeVariable)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task DeleteField()
        {
            CodeClass c = (CodeClass)await GetCodeElementAsync("Foo");
            c.RemoveMember(c.Members.Item("bar"));

            Assert.Equal(@"class Foo
{
}", await GetFileTextAsync());
        }
    }
}
