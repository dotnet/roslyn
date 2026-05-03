// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpExposeMemberForTesting>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicExposeMemberForTesting>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ExposeMemberForTestingTests
    {
        [Fact]
        public async Task ExposeFieldCSharpAsync()
        {
            var source = """
                class TestClass {
                    private int _field;
                    internal readonly struct [|TestAccessor|] {
                        private readonly TestClass _testClass;
                        internal TestAccessor(TestClass testClass) => _testClass = testClass;
                    }
                }
                """;
            var fixedSource = """
                class TestClass {
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
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "F:TestClass._field",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeFieldVisualBasicAsync()
        {
            var source = """
                Class TestClass
                    Private Dim _field As Integer
                    Friend Structure [|TestAccessor|]
                        Private Dim ReadOnly _testClass As TestClass
                        Friend Sub New(testClass As TestClass)
                            _testClass = testClass
                        End Sub
                    End Structure
                End Class
                """;
            var fixedSource = """
                Class TestClass
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
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "F:TestClass._field",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyFieldCSharpAsync()
        {
            var source = """
                class TestClass {
                    private readonly int _field;
                    internal readonly struct [|TestAccessor|] {
                        private readonly TestClass _testClass;
                        internal TestAccessor(TestClass testClass) => _testClass = testClass;
                    }
                }
                """;
            var fixedSource = """
                class TestClass {
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
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "F:TestClass._field",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyFieldVisualBasicAsync()
        {
            var source = """
                Class TestClass
                    Private ReadOnly _field As Integer
                    Friend Structure [|TestAccessor|]
                        Private Dim ReadOnly _testClass As TestClass
                        Friend Sub New(testClass As TestClass)
                            _testClass = testClass
                        End Sub
                    End Structure
                End Class
                """;
            var fixedSource = """
                Class TestClass
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
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "F:TestClass._field",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposePropertyCSharpAsync()
        {
            var source = """
                class TestClass {
                    private int Property { get; set; }
                    internal readonly struct [|TestAccessor|] {
                        private readonly TestClass _testClass;
                        internal TestAccessor(TestClass testClass) => _testClass = testClass;
                    }
                }
                """;
            var fixedSource = """
                class TestClass {
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
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.Property",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposePropertyVisualBasicAsync()
        {
            var source = """
                Class TestClass
                    Private Property TestProperty As Integer
                    Friend Structure [|TestAccessor|]
                        Private Dim ReadOnly _testClass As TestClass
                        Friend Sub New(testClass As TestClass)
                            _testClass = testClass
                        End Sub
                    End Structure
                End Class
                """;
            var fixedSource = """
                Class TestClass
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
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.TestProperty",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyPropertyCSharpAsync()
        {
            var source = """
                class TestClass {
                    private int Property { get; }
                    internal readonly struct [|TestAccessor|] {
                        private readonly TestClass _testClass;
                        internal TestAccessor(TestClass testClass) => _testClass = testClass;
                    }
                }
                """;
            var fixedSource = """
                class TestClass {
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
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.Property",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeReadOnlyPropertyVisualBasicAsync()
        {
            var source = """
                Class TestClass
                    Private ReadOnly Property TestProperty As Integer
                    Friend Structure [|TestAccessor|]
                        Private Dim ReadOnly _testClass As TestClass
                        Friend Sub New(testClass As TestClass)
                            _testClass = testClass
                        End Sub
                    End Structure
                End Class
                """;
            var fixedSource = """
                Class TestClass
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
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.TestProperty",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeWriteOnlyPropertyCSharpAsync()
        {
            var source = """
                class TestClass {
                    private int Property { set { } }
                    internal readonly struct [|TestAccessor|] {
                        private readonly TestClass _testClass;
                        internal TestAccessor(TestClass testClass) => _testClass = testClass;
                    }
                }
                """;
            var fixedSource = """
                class TestClass {
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
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.Property",
            }.RunAsync();
        }

        [Fact]
        public async Task ExposeWriteOnlyPropertyVisualBasicAsync()
        {
            var source = """
                Class TestClass
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
                End Class
                """;
            var fixedSource = """
                Class TestClass
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
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                CodeActionEquivalenceKey = "P:TestClass.TestProperty",
            }.RunAsync();
        }
    }
}
