// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpCreateTestAccessor>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicCreateTestAccessor>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CreateTestAccessorTests
    {
        [Theory]
        [InlineData("$$class TestClass ")]
        [InlineData("class $$TestClass ")]
        [InlineData("class TestClass$$ ")]
        [InlineData("class [|TestClass|] ")]
        [InlineData("[|class TestClass|] ")]
        public async Task CreateTestAccessorCSharpAsync(string typeHeader)
        {
            var source = typeHeader + """
                {
                }
                """;
            var fixedSourceBody = """
                {
                    internal TestAccessor GetTestAccessor()
                    {
                        return new TestAccessor(this);
                    }

                    internal readonly struct TestAccessor
                    {
                        private readonly TestClass _instance;

                        internal TestAccessor(TestClass instance)
                        {
                            _instance = instance;
                        }
                    }
                }
                """;

            var fixedSource = "class TestClass " + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData("$$struct TestStruct ")]
        [InlineData("struct $$TestStruct ")]
        [InlineData("struct TestStruct$$ ")]
        [InlineData("struct [|TestStruct|] ")]
        [InlineData("[|struct TestStruct|] ")]
        public async Task CreateTestAccessorStructCSharpAsync(string typeHeader)
        {
            var source = typeHeader + """
                {
                }
                """;
            var fixedSourceBody = """
                {
                    internal TestAccessor GetTestAccessor()
                    {
                        return new TestAccessor(this);
                    }

                    internal readonly struct TestAccessor
                    {
                        private readonly TestStruct _instance;

                        internal TestAccessor(TestStruct instance)
                        {
                            _instance = instance;
                        }
                    }
                }
                """;

            var fixedSource = "struct TestStruct " + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory(Skip = "Needs Roslyn 16.9 Preview 1: https://github.com/dotnet/roslyn/pull/48096")]
        [InlineData("$$record TestRecord ")]
        [InlineData("record $$TestRecord ")]
        [InlineData("record TestRecord$$ ")]
        [InlineData("record [|TestRecord|] ")]
        [InlineData("[|record TestRecord|] ")]
        public async Task CreateTestAccessorRecordCSharpAsync(string typeHeader)
        {
            var source = typeHeader + """
                {
                }
                """;
            var fixedSourceBody = """
                {
                    internal TestAccessor GetTestAccessor()
                    {
                        return new TestAccessor(this);
                    }

                    internal readonly struct TestAccessor
                    {
                        private readonly TestRecord _instance;

                        internal TestAccessor(TestRecord instance)
                        {
                            _instance = instance;
                        }
                    }
                }
                """;

            var fixedSource = "record TestRecord " + fixedSourceBody;
            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = fixedSource,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Theory]
        [InlineData(TypeKind.Delegate)]
        [InlineData(TypeKind.Enum)]
        [InlineData(TypeKind.Interface)]
        public async Task UnsupportedTypeCSharpAsync(TypeKind typeKind)
        {
            var declaration = typeKind switch
            {
                TypeKind.Delegate => "delegate void $$Method();",
                TypeKind.Enum => "public enum $$SomeType { }",
                TypeKind.Interface => "public interface $$SomeType { }",
                _ => throw new NotSupportedException(),
            };

            await VerifyCS.VerifyRefactoringAsync(declaration, declaration);
        }

        [Theory]
        [InlineData("$$Class TestClass")]
        [InlineData("Class $$TestClass")]
        [InlineData("Class TestClass$$")]
        [InlineData("Class [|TestClass|]")]
        [InlineData("[|Class TestClass|]")]
        public async Task CreateTestAccessorVisualBasicAsync(string typeHeader)
        {
            var fixedSourceBody = """

                    Friend Function GetTestAccessor() As TestAccessor
                        Return New TestAccessor(Me)
                    End Function

                    Friend Structure TestAccessor
                        Private ReadOnly _instance As TestClass

                        Friend Sub New(instance As TestClass)
                            _instance = instance
                        End Sub
                    End Structure
                End Class
                """;

            var fixedSource = "Class TestClass" + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync($"""
                {typeHeader}
                End Class
                """, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData("$$Structure TestStructure")]
        [InlineData("Structure $$TestStructure")]
        [InlineData("Structure TestStructure$$")]
        [InlineData("Structure [|TestStructure|]")]
        [InlineData("[|Structure TestStructure|]")]
        public async Task CreateTestAccessorStructureVisualBasicAsync(string typeHeader)
        {
            var fixedSourceBody = """

                    Friend Function GetTestAccessor() As TestAccessor
                        Return New TestAccessor(Me)
                    End Function

                    Friend Structure TestAccessor
                        Private ReadOnly _instance As TestStructure

                        Friend Sub New(instance As TestStructure)
                            _instance = instance
                        End Sub
                    End Structure
                End Structure
                """;

            var fixedSource = "Structure TestStructure" + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync($"""
                {typeHeader}
                End Structure
                """, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData(TypeKind.Delegate)]
        [InlineData(TypeKind.Enum)]
        [InlineData(TypeKind.Interface)]
        [InlineData(TypeKind.Module)]
        public async Task UnsupportedTypeVisualBasicAsync(TypeKind typeKind)
        {
            var declaration = typeKind switch
            {
                TypeKind.Delegate => "Delegate Function $$SomeType() As Integer",
                TypeKind.Enum => "Enum $$SomeType\r\n    Member\r\nEnd Enum",
                TypeKind.Interface => "Interface $$SomeType\r\nEnd Interface",
                TypeKind.Module => "Module $$SomeType\r\nEnd Module",
                _ => throw new NotSupportedException(),
            };

            await VerifyVB.VerifyRefactoringAsync(declaration, declaration);
        }
    }
}
