﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeVariableManipulationTests : AbstractFileCodeElementTests
    {
        public FileCodeVariableManipulationTests()
            : base(@"class Goo
{
    private int bar;
}")
        {
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DeleteField()
        {
            var c = (CodeClass)GetCodeElement("Goo");
            c.RemoveMember(c.Members.Item("bar"));

            Assert.Equal(@"class Goo
{
}", GetFileText());
        }
    }
}
