// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

public sealed class FileCodeVariableManipulationTests : AbstractFileCodeElementTests
{
    public FileCodeVariableManipulationTests()
        : base("""
            class Goo
            {
                private int bar;
            }
            """)
    {
    }

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.CodeModel)]
    public void DeleteField()
    {
        var c = (CodeClass)GetCodeElement("Goo");
        c.RemoveMember(c.Members.Item("bar"));

        Assert.Equal("""
            class Goo
            {
            }
            """, GetFileText());
    }
}
