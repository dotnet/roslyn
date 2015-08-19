// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
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
