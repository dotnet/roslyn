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
        public static async Task<FileCodeVariableManipulationTests> CreateAsync()
        {
            var pair = await CreateWorkspaceAndFileCodeModelAsync(@"class Foo
{
    private int bar;
}");

            return new FileCodeVariableManipulationTests(pair);
        }

        public FileCodeVariableManipulationTests(Tuple<TestWorkspace, EnvDTE.FileCodeModel> pair)
            : base(pair)
        {
        }

        private CodeVariable GetCodeVariable(params object[] path)
        {
            return (CodeVariable)GetCodeElement(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DeleteField()
        {
            CodeClass c = (CodeClass)GetCodeElement("Foo");
            c.RemoveMember(c.Members.Item("bar"));

            Assert.Equal(@"class Foo
{
}", GetFileText());
        }
    }
}
