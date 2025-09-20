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
    public Task TestThrowNotImplementedExceptionInStatement()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInExpression()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInConstructor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInDestructor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInIndexer()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInEvent()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestThrowNotImplementedExceptionInOperator()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestDifferentFlavorsOfThrowNotImplementedException()
        => new VerifyCS.Test
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

                internal void ThrowOnAllStatements(bool condition)
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact]
    public Task WhenShouldNotReportOnMember()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            class C
            {
                private string _name;
                public string Name
                {
                    get => _name;
                    set => _name = value ?? {|IDE3000:throw new NotImplementedException()|};
                }

                void LambdaThrowWithFunc()
                {
                    Func<int> func = () => {|IDE3000:throw new NotImplementedException()|};
                    func();
                }

                void LambdaThrow()
                {
                    Action action = () => {|IDE3000:throw new NotImplementedException()|};
                    action();
                }

                void AnonymousMethodThrow()
                {
                    Action action = delegate 
                    { 
                        {|IDE3000:throw new NotImplementedException();|}
                    };
                    action();
                }

                void LocalFunctionThrow()
                {
                    void Local() 
                    { 
                        {|IDE3000:throw new NotImplementedException();|}
                    }

                    Local();
                }

                void NestedBlockThrow()
                {
                    if (true)
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                void LoopThrow()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                public int GetValue(string type) =>
                    type switch
                    {
                        "A" => 1,
                        "B" => 2,
                        _ => {|IDE3000:throw new NotImplementedException($"Type '{type}' not implemented")|}
                    };

                void UsingThrow()
                {
                    using (var resource = new System.IO.MemoryStream())
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                void TryCatchThrow()
                {
                    try
                    {
                        // Some code
                    }
                    catch (Exception)
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                void LockThrow()
                {
                    lock (new object())
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                void TernaryThrow(bool condition)
                {
                    var result = condition ? 1 : {|IDE3000:throw new NotImplementedException()|};
                }

                void AnonymousTypeWithLambdaThrow()
                {
                    var result = new { Value = (Func<int>)(() => {|IDE3000:throw new NotImplementedException()|}) };
                }

                void LinqThrow()
                {
                    var result = new[] { 1, 2, 3 }.Select(x => x > 0 ? x : {|IDE3000:throw new NotImplementedException()|});
                }

                public int[] ComplexQuery()
                {
                    return new[] { 1, 2, 3 }
                        .Where(x => x > 0)
                        .Select(x => x * 2)
                        .Where(x => x > 0 ? true : {|IDE3000:throw new NotImplementedException()|})
                        .ToArray();
                }

                public void ProcessData(List<object> data)
                {
                    var result = data.Select(item => item switch 
                    {
                        string s => s.ToUpper(),
                        int i => i.ToString(),
                        DateTime d => d.ToShortDateString(),
                        _ => {|IDE3000:throw new NotImplementedException("Unsupported data type")|}
                    });
                }

                internal Person CreatePerson(string name, int age)
                {
                    return new Person
                    {
                        Name = name ?? {|IDE3000:throw new NotImplementedException("Name cannot be null")|},
                        Age = age < 0 ? {|IDE3000:throw new NotImplementedException("Age must be positive")|} : age,
                        Skills = new() { "C#", "F#" }
                    };
                }

                public void ProcessWithLocalMethod(string input)
                {
                    string ParseInput(string text)
                    {
                        return text?.Length > 5 ? text : {|IDE3000:throw new NotImplementedException("Input too short")|};
                    }
                }

                public Func<int, int> GetCalculator(string operation)
                {
                    return operation switch
                    {
                        "square" => x => x * x,
                        "double" => x => x * 2,
                        _ => {|IDE3000:throw new NotImplementedException($"Operation {operation} not implemented")|}
                    };
                }

                public void ProcessWithNestedDelegates()
                {
                    Func<int, Func<int, int>> createOperation = x => 
                        y => x > 0 ? x + y : {|IDE3000:throw new NotImplementedException("Negative values not implemented")|};
                }

                public async Task<Person> GetPersonAsync(int id)
                {
                    var supervisor = id > 100 
                        ? new Person { Name = "Manager" } 
                        : {|IDE3000:throw new NotImplementedException("Non-manager employees not implemented")|};

                    return supervisor;
                }

                void SwitchThrow(int value)
                {
                    switch (value)
                    {
                        case 1:
                            {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                internal void WontReportOnMemberWhenThrowIsNotDirect(bool condition)
                {
                    var result = condition ? 1 : {|IDE3000:throw new NotImplementedException()|};

                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                internal void WontReportOnMemberWhenNonThrowStatementsExist(bool condition)
                {
                    Console.WriteLine(condition ? 1 : 0);

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
