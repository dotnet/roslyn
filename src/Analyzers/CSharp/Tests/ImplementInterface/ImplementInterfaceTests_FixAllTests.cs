// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ImplementInterface;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpImplementInterfaceCodeFixProvider>;

public class ImplementInterfaceTests_FixAllTests
{
    #region "Fix all occurrences tests"

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInDocument()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            @"class B3 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C3 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                        },
                        AdditionalProjectReferences = { "TestProject" },
                    },
                },
            },
            FixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllInProjectCheck | CodeFixTestBehaviors.SkipFixAllInSolutionCheck,
            CodeActionEquivalenceKey = "False;False;True:global::I1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 0,
        }.RunAsync();
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInProject()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            @"class B3 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C3 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                        },
                        AdditionalProjectReferences = { "TestProject" },
                    },
                },
            },
            FixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                    @"class B2 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : I1, I2
    {
        public void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeFixTestBehaviors.SkipFixAllInSolutionCheck,
            CodeActionEquivalenceKey = "False;False;True:global::I1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 0,
        }.RunAsync();
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInSolution()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            @"class B3 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C3 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                        },
                        AdditionalProjectReferences = { "TestProject" },
                    },
                },
            },
            FixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                    @"class B2 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : {|CS0535:I1|}, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            @"class B3 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C3 : {|CS0535:I1|}, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                        },
                        AdditionalProjectReferences = { "TestProject" },
                    },
                },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeFixTestBehaviors.SkipFixAllInProjectCheck,
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeActionEquivalenceKey = "True;False;False:global::I2;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInSolution_DifferentAssemblyWithSameTypeName()
    {
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B3 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C3 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                        },
                    },
                },
            },
            FixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                    @"class B2 : {|CS0535:I1|}, {|CS0535:I2|}
{
    class C2 : {|CS0535:I1|}, {|CS0535:I2|}
    {
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedState =
            {
                Sources =
                {
                    @"public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : {|CS0535:I1|}, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                    @"class B2 : {|CS0535:I1|}, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : {|CS0535:I1|}, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}",
                },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeFixTestBehaviors.SkipFixAllInProjectCheck,
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeActionEquivalenceKey = "True;False;False:global::I2;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 1,
        }.RunAsync();
    }

    #endregion
}
