// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
    EmptyCodeFixProvider>;

public sealed class CSharpImplementNotImplementedExceptionDiagnosticAnalyzerTests
{
    [Fact]
    public async Task TestThrowNotImplementedExceptionInStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                int P => {|IDE3000:throw new NotImplementedException()|};
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public {|IDE3000:C|}()
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInDestructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                ~{|IDE3000:C|}()
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInIndexer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public int this[int index]
                {
                    {|IDE3000:get|} { {|IDE3000:throw new NotImplementedException();|} }
                    {|IDE3000:set|} { {|IDE3000:throw new NotImplementedException();|} }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInEvent()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public event EventHandler MyEvent
                {
                    {|IDE3000:add|} { {|IDE3000:throw new NotImplementedException();|} }
                    {|IDE3000:remove|} { {|IDE3000:throw new NotImplementedException();|} }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInOperator()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public static C operator {|IDE3000:+|}(C a, C b)
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestDifferentFlavorsOfThrowNotImplementedException()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                void {|IDE3000:M1|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void {|IDE3000:M1WithComment|}()
                {
                    // Some comment
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void {|IDE3000:M2|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                int P1
                {
                    {|IDE3000:get|} { {|IDE3000:throw new NotImplementedException();|} }
                }

                int P2
                {
                    {|IDE3000:get|} { {|IDE3000:throw new NotImplementedException();|} }
                    {|IDE3000:set|} { {|IDE3000:throw new NotImplementedException();|} }
                }

                int this[int index]
                {
                    {|IDE3000:get|} { {|IDE3000:throw new NotImplementedException();|} }
                    {|IDE3000:set|} { {|IDE3000:throw new NotImplementedException();|} }
                }
            
                int P11
                {
                    {|IDE3000:get|} { {|IDE3000:throw new NotImplementedException();|} /*I am a comment*/ }
                }
            
                void {|IDE3000:M6|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            
                void {|IDE3000:M7|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }
            
                public double {|IDE3000:CalculateSquareRoot|}(double number) => {|IDE3000:throw new NotImplementedException("CalculateSquareRoot method not implemented")|};

                private string _name;
                public string Name
                {
                    get => _name;
                    // Should NOT report - throw is conditionally inside a lambda
                    set => _name = value ?? throw new NotImplementedException();
                }

                // Should NOT report - throw is inside a function
                void LambdaThrowWithFunc()
                {
                    Func<int> func = () => throw new NotImplementedException();
                    func();
                }

                // Should NOT report - throw is inside a lambda
                void LambdaThrow()
                {
                    Action action = () => throw new NotImplementedException();
                    action();
                }

                // Should NOT report - throw is inside an anonymous method
                void AnonymousMethodThrow()
                {
                    Action action = delegate 
                    { 
                        throw new NotImplementedException(); 
                    };
                    action();
                }

                // Should NOT report - throw is inside a local function
                void LocalFunctionThrow()
                {
                    void Local() 
                    { 
                        throw new NotImplementedException(); 
                    }

                    Local();
                }

                // Should NOT report - throw is inside a nested block
                void NestedBlockThrow()
                {
                    if (true)
                    {
                        throw new NotImplementedException();
                    }
                }

                // Should NOT report - throw is inside a loop
                void LoopThrow()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        throw new NotImplementedException();
                    }
                }

                // Should NOT report - throw is inside a switch
                void SwitchThrow(int value)
                {
                    switch (value)
                    {
                        case 1:
                            throw new NotImplementedException();
                    }
                }
            
                // Should NOT report - throw is inside a switch expression arm
                public int GetValue(string type) =>
                    type switch
                    {
                        "A" => 1,
                        "B" => 2,
                        _ => throw new NotImplementedException($"Type '{type}' not implemented")
                    };

                // Should NOT report - throw is inside a using block
                void UsingThrow()
                {
                    using (var resource = new System.IO.MemoryStream())
                    {
                        throw new NotImplementedException();
                    }
                }
            
                // Should NOT report - throw is inside a try-catch block
                void M3()
                {
                    try
                    {
                        // Some code
                    }
                    catch (Exception)
                    {
                        throw new NotImplementedException();
                    }
                }

                // Should NOT report - throw is inside a lock
                void LockThrow()
                {
                    lock (new object())
                    {
                        throw new NotImplementedException();
                    }
                }
            
                // Should NOT report - throw is inside a ternary
                void TernaryThrow(bool condition)
                {
                    var result = condition ? 1 : throw new NotImplementedException();
                }
            
                // Should NOT report - throw is inside an anonymous type with lambda
                void AnonymousTypeWithLambdaThrow()
                {
                    var result = new { Value = (Func<int>)(() => throw new NotImplementedException()) };
                }
            
                // Should NOT report - throw is inside a LINQ query/expression
                void LinqThrow()
                {
                    var result = new[] { 1, 2, 3 }.Select(x => x > 0 ? x : throw new NotImplementedException());
                }
            
                // Should NOT report - throw is inside a complex LINQ query with multiple nestings
                public int[] ComplexQuery()
                {
                    return new[] { 1, 2, 3 }
                        .Where(x => x > 0)
                        .Select(x => x * 2)
                        .Where(x => x > 0 ? true : throw new NotImplementedException())
                        .ToArray();
                }
            
                // Switch expression in lambda in a method
                public void ProcessData(List<object> data)
                {
                    var result = data.Select(item => item switch 
                    {
                        string s => s.ToUpper(),
                        int i => i.ToString(),
                        DateTime d => d.ToShortDateString(),
                        _ => throw new NotImplementedException("Unsupported data type")
                    });
                }
            
                // Object initializer with conditional expressions
                internal Person CreatePerson(string name, int age)
                {
                    return new Person
                    {
                        Name = name ?? throw new NotImplementedException("Name cannot be null"),
                        Age = age < 0 ? throw new NotImplementedException("Age must be positive") : age,
                        Skills = new() { "C#", "F#" }
                    };
                }
            
                // Local method with throw
                public void ProcessWithLocalMethod(string input)
                {
                    string ParseInput(string text)
                    {
                        return text?.Length > 5 ? text : throw new NotImplementedException("Input too short");
                    }
                }
            
                // Anonymous method with throw
                public Func<int, int> GetCalculator(string operation)
                {
                    return operation switch
                    {
                        "square" => x => x * x,
                        "double" => x => x * 2,
                        _ => throw new NotImplementedException($"Operation {operation} not implemented")
                    };
                }
            
                // Nested delegated expressions
                public void ProcessWithNestedDelegates()
                {
                    // Anonymous function that returns another function
                    Func<int, Func<int, int>> createOperation = x => 
                        y => x > 0 ? x + y : throw new NotImplementedException("Negative values not implemented");
                }
            
                // Async method with complex initialization
                public async Task<Person> GetPersonAsync(int id)
                {
                    var supervisor = id > 100 
                        ? new Person { Name = "Manager" } 
                        : throw new NotImplementedException("Non-manager employees not implemented");
                }
            
                internal class Person
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                    public List<string> Skills { get; set; }
                    public Person Supervisor { get; set; }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task ShouldNotThrowButCurrentlyIs()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                // Async method with complex initialization
                public async Task<Person> GetPersonAsync(int id)
                {
                    var supervisor = id > 100 
                        ? new Person { Name = "Manager" } 
                        : throw new NotImplementedException("Non-manager employees not implemented");

                    return supervisor;
                }
            
                // Should NOT report - throw is inside a ternary
                internal void {|IDE3000:TernaryThrow|}(bool condition)
                {
                    var result = condition ? 1 : throw new NotImplementedException();
            
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }
            
                // Should NOT report - throw is inside a ternary
                internal void {|IDE3000:ArbitraryThrow|}(bool condition)
                {
                    var result = condition ? 1 : 2;
            
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }
            
                internal class Person
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                    public List<string> Skills { get; set; }
                    public Person Supervisor { get; set; }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }
}
