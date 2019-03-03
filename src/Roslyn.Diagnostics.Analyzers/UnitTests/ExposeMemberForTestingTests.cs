// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using CSLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VBLanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ExposeMemberForTesting,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpExposeMemberForTestingFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ExposeMemberForTesting,
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicExposeMemberForTestingFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ExposeMemberForTestingTests
    {
        [Fact]
        public async Task ExposeFieldCSharp()
        {
            var source = @"class TestClass {
    private int _field;
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;
    }
}";
            var fixedSource = @"class TestClass {
    private int _field;
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;

        internal ref int Field
        {
            get
            {
                return ref _testClass._field;
            }
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "F:TestClass._field",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeFieldVisualBasic()
        {
            var source = @"Class TestClass
    Private Dim _field As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";
            var fixedSource = @"Class TestClass
    Private Dim _field As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Friend Property Field As Integer
            Get
                Return _testClass._field
            End Get
            Set(value As Integer)
                _testClass._field = value
            End Set
        End Property
    End Structure
End Class";

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "F:TestClass._field",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyFieldCSharp()
        {
            var source = @"class TestClass {
    private readonly int _field;
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;
    }
}";
            var fixedSource = @"class TestClass {
    private readonly int _field;
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;

        internal ref readonly int Field
        {
            get
            {
                return ref _testClass._field;
            }
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "F:TestClass._field",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyFieldVisualBasic()
        {
            var source = @"Class TestClass
    Private ReadOnly _field As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";
            var fixedSource = @"Class TestClass
    Private ReadOnly _field As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Friend ReadOnly Property Field As Integer
            Get
                Return _testClass._field
            End Get
        End Property
    End Structure
End Class";

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "F:TestClass._field",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposePropertyCSharp()
        {
            var source = @"class TestClass {
    private int Property { get; set; }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;
    }
}";
            var fixedSource = @"class TestClass {
    private int Property { get; set; }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;

        internal int Property
        {
            get
            {
                return _testClass.Property;
            }

            set
            {
                _testClass.Property = value;
            }
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.Property",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposePropertyVisualBasic()
        {
            var source = @"Class TestClass
    Private Property TestProperty As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";
            var fixedSource = @"Class TestClass
    Private Property TestProperty As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Friend Property TestProperty As Integer
            Get
                Return _testClass.TestProperty
            End Get
            Set(value As Integer)
                _testClass.TestProperty = value
            End Set
        End Property
    End Structure
End Class";

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.TestProperty",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyPropertyCSharp()
        {
            var source = @"class TestClass {
    private int Property { get; }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;
    }
}";
            var fixedSource = @"class TestClass {
    private int Property { get; }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;

        internal int Property
        {
            get
            {
                return _testClass.Property;
            }
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.Property",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyPropertyVisualBasic()
        {
            var source = @"Class TestClass
    Private ReadOnly Property TestProperty As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";
            var fixedSource = @"Class TestClass
    Private ReadOnly Property TestProperty As Integer
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Friend ReadOnly Property TestProperty As Integer
            Get
                Return _testClass.TestProperty
            End Get
        End Property
    End Structure
End Class";

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.TestProperty",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeWriteOnlyPropertyCSharp()
        {
            var source = @"class TestClass {
    private int Property { set { } }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;
    }
}";
            var fixedSource = @"class TestClass {
    private int Property { set { } }
    internal readonly struct [|TestAccessor|] {
        private readonly TestClass _testClass;
        internal TestAccessor(TestClass testClass) => _testClass = testClass;

        internal int Property
        {
            set
            {
                _testClass.Property = value;
            }
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.Property",
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task ExposeWriteOnlyPropertyVisualBasic()
        {
            var source = @"Class TestClass
    Private WriteOnly Property TestProperty As Integer
        Set(value As Integer)
        End Set
    End Property
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";
            var fixedSource = @"Class TestClass
    Private WriteOnly Property TestProperty As Integer
        Set(value As Integer)
        End Set
    End Property
    Friend Structure [|TestAccessor|]
        Private Dim ReadOnly _testClass As TestClass
        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Friend WriteOnly Property TestProperty As Integer
            Set(value As Integer)
                _testClass.TestProperty = value
            End Set
        End Property
    End Structure
End Class";

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
                CodeFixEquivalenceKey = "P:TestClass.TestProperty",
            };

            await test.RunAsync();
        }
    }
}
