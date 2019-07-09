// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateType;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateTypeTests
{
    public partial class GenerateTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new GenerateTypeCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> codeActions)
            => FlattenActions(codeActions);

        #region Generate Class

        #region Generics

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeParameterFromArgumentInferT()
        {
            await TestInRegular73AndScriptAsync(
@"class Program
{
    void Main()
    {
        [|Goo<int>|] f;
    }
}",
@"class Program
{
    void Main()
    {
        Goo<int> f;
    }
}

internal class Goo<T>
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromTypeParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    System.Action<[|Employee|]> employees;
}",
@"class Class
{
    System.Action<Employee> employees;

    private class Employee
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClause()
        {
            await TestInRegular73AndScriptAsync(
@"class EmployeeList<T> where T : [|Employee|], new()
{
}",
@"class EmployeeList<T> where T : Employee, new()
{
}

internal class Employee
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGeneratePublicClassFromASingleConstraintClause()
        {
            await TestInRegular73AndScriptAsync(
@"public class EmployeeList<T> where T : [|Employee|], new()
{
}",
@"public class EmployeeList<T> where T : Employee, new()
{
}

public class Employee
{
}",
index: 1);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassFromConstructorConstraint()
        {
            await TestMissingInRegularAndScriptAsync(
@"class EmployeeList<T> where T : Employee, [|new()|]
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromMultipleTypeConstraintClauses()
        {
            await TestInRegular73AndScriptAsync(
@"class Derived<T, U>
    where U : struct
    where T : [|Base|], new()
{
}",
@"class Derived<T, U>
    where U : struct
    where T : Base, new()
{
}

internal class Base
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGeneratePublicClassFromMultipleTypeConstraintClauses()
        {
            await TestInRegular73AndScriptAsync(
@"public class Derived<T, U>
    where U : struct
    where T : [|Base|], new()
{
}",
@"public class Derived<T, U>
    where U : struct
    where T : Base, new()
{
}

public class Base
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassFromClassOrStructConstraint()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Derived<T, U>
    where U : [|struct|]
    where T : Base, new()
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAbsenceOfGenerateIntoInvokingTypeForConstraintList()
        {
            await TestActionCountAsync(
@"class EmployeeList<T> where T : [|Employee|]
{
}",
count: 3,
parameters: new TestParameters(Options.Regular));
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseInterface()
        {
            await TestInRegular73AndScriptAsync(
@"interface IEmployeeList<T> where T : [|Employee|], new()
{
}",
@"interface IEmployeeList<T> where T : Employee, new()
{
}

internal class Employee
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGeneratePublicClassFromASingleConstraintClausePublicInterface()
        {
            await TestInRegular73AndScriptAsync(
@"public interface IEmployeeList<T> where T : [|Employee|], new()
{
}",
@"public interface IEmployeeList<T> where T : Employee, new()
{
}

public class Employee
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseInternalDelegate()
        {
            await TestInRegular73AndScriptAsync(
@"class Employee
{
    internal delegate void Action<T>() where T : [|Command|];
}",
@"class Employee
{
    internal delegate void Action<T>() where T : Command;
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClausePublicDelegate()
        {
            await TestInRegular73AndScriptAsync(
@"class Employee
{
    public delegate void Action<T>() where T : [|Command|];
}",
@"class Employee
{
    public delegate void Action<T>() where T : Command;
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseInternalMethod()
        {
            await TestInRegular73AndScriptAsync(
@"class Employee
{
    internal void Action<T>() where T : [|Command|] {}
}",
@"class Employee
{
    internal void Action<T>() where T : Command {}
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClausePublicMethod()
        {
            await TestInRegular73AndScriptAsync(
@"class Employee
{
    public void Action<T>() where T : [|Command|] {}
}",
@"class Employee
{
    public void Action<T>() where T : Command {}
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseMethod()
        {
            await TestInRegular73AndScriptAsync(
@"class Employee
{
    void Action<T>() where T : [|Command|] {}
}",
@"class Employee
{
    void Action<T>() where T : Command {}
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseMethodInInterface()
        {
            await TestInRegular73AndScriptAsync(
@"interface Employee
{
    void Action<T>() where T : [|Command|] {}
}",
@"interface Employee
{
    void Action<T>() where T : Command {}
}

internal class Command
{
}",
index: 1);
        }

        [WorkItem(18240, "https://github.com/dotnet/roslyn/issues/18240")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        [InlineData("public", "internal", "internal")]
        [InlineData("public", "private", "internal")]
        [InlineData("internal", "protected", "internal")]
        [InlineData("public", "protected internal", "public")]
        [InlineData("protected", "protected", "public")]
        [InlineData("protected internal", "protected", "public")]
        [InlineData("protected", "protected private", "internal")]
        [InlineData("protected private", "protected", "internal")]
        public async Task TestGenerateInternalClassFromASingleConstraintClauseNestedClass(string middleAccessibility, string accessibility, string generatedAccessibility)
        {
            await TestInRegular73AndScriptAsync(
$@"public class A
{{
    {middleAccessibility} class B
    {{
        {accessibility} class C<T> where T : [|D|]
        {{

        }}
    }}
}}",
$@"public class A
{{
    {middleAccessibility} class B
    {{
        {accessibility} class C<T> where T : D
        {{

        }}
    }}
}}

{generatedAccessibility} class D
{{
}}",
index: 1);
        }

        #endregion

        #region Lambdas

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromParenthesizedLambdaExpressionsParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    Func<Employee, int, bool> l = ([|Employee|] e, int age) => e.Age > age;
}",
@"class Class
{
    Func<Employee, int, bool> l = (Employee e, int age) => e.Age > age;

    private class Employee
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromParenthesizedLambdaExpressionsBody()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    System.Action<Class, int> l = (Class e, int age) => {
        [|Wage|] w;
    };
}",
@"class Class
{
    System.Action<Class, int> l = (Class e, int age) => {
        Wage w;
    };

    private class Wage
    {
    }
}",
index: 2);
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoSameType()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|Goo|] f;
}",
@"class Class
{
    Goo f;

    private class Goo
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromNullableFieldDeclarationIntoSameType()
        {
            await TestInRegular73AndScriptAsync(
@"#nullable enable
class Class
{
    [|Goo?|] f;
}",
@"#nullable enable
class Class
{
    Goo? f;

    private class Goo
    {
    }
}",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoGlobalNamespace()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"class Program { void Main ( ) { [|Goo|] f ; } } ",
@"internal class Goo
{
}",
expectedContainers: ImmutableArray<string>.Empty,
expectedDocumentName: "Goo.cs");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoCustomNamespace()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"class Class { [|TestNamespace|].Goo f; }",
@"namespace TestNamespace
{
    internal class Goo
    {
    }
}",
expectedContainers: ImmutableArray.Create("TestNamespace"),
expectedDocumentName: "Goo.cs");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromFieldDeclarationIntoSameNamespace()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|Goo|] f;
}",
@"class Class
{
    Goo f;
}

internal class Goo
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWithCtorFromObjectCreation()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    Goo f = new [|Goo|]();
}",
@"class Class
{
    Goo f = new Goo();

    private class Goo
    {
        public Goo()
        {
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWithCtorFromObjectCreationWithTuple()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    var f = new [|Generated|]((1, 2));
}",
@"class Class
{
    var f = new Generated((1, 2));

    private class Generated
    {
        private (int, int) p;

        public Generated((int, int) p)
        {
            this.p = p;
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWithCtorFromObjectCreationWithTupleWithNames()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    var f = new [|Generated|]((a: 1, b: 2, 3));
}",
@"class Class
{
    var f = new Generated((a: 1, b: 2, 3));

    private class Generated
    {
        private (int a, int b, int) p;

        public Generated((int a, int b, int) p)
        {
            this.p = p;
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromBaseList()
        {
            await TestInRegular73AndScriptAsync(
@"class Class : [|BaseClass|]
{
}",
@"class Class : BaseClass
{
}

internal class BaseClass
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromMethodParameters()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method([|Goo|] f)
    {
    }
}",
@"class Class
{
    void Method(Goo f)
    {
    }

    private class Goo
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromMethodReturnType()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|Goo|] Method()
    {
    }
}",
@"class Class
{
    Goo Method()
    {
    }

    private class Goo
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromAttribute()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [[|Obsolete|]]
    void Method()
    {
    }
}",
@"using System;

class Class
{
    [Obsolete]
    void Method()
    {
    }

    private class ObsoleteAttribute : Attribute
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromExpandedAttribute()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [[|ObsoleteAttribute|]]
    void Method()
    {
    }
}",
@"using System;

class Class
{
    [ObsoleteAttribute]
    void Method()
    {
    }

    private class ObsoleteAttribute : Attribute
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromCatchClause()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        try
        {
        }
        catch ([|ExType|])
        {
        }
    }
}",
@"using System;
using System.Runtime.Serialization;

class Class
{
    void Method()
    {
        try
        {
        }
        catch (ExType)
        {
        }
    }

    [Serializable]
    private class ExType : Exception
    {
        public ExType()
        {
        }

        public ExType(string message) : base(message)
        {
        }

        public ExType(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExType(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatement()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        throw new [|ExType|]();
    }
}",
@"using System;
using System.Runtime.Serialization;

class Class
{
    void Method()
    {
        throw new ExType();
    }

    [Serializable]
    private class ExType : Exception
    {
        public ExType()
        {
        }

        public ExType(string message) : base(message)
        {
        }

        public ExType(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExType(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatementWithDifferentArg()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        throw new [|ExType|](1);
    }
}",
@"using System;
using System.Runtime.Serialization;

class Class
{
    void Method()
    {
        throw new ExType(1);
    }

    [Serializable]
    private class ExType : Exception
    {
        private int v;

        public ExType()
        {
        }

        public ExType(int v)
        {
            this.v = v;
        }

        public ExType(string message) : base(message)
        {
        }

        public ExType(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExType(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromThrowStatementWithMatchingArg()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        throw new [|ExType|](""message"");
    }
}",
@"using System;
using System.Runtime.Serialization;

class Class
{
    void Method()
    {
        throw new ExType(""message"");
    }

    [Serializable]
    private class ExType : Exception
    {
        public ExType()
        {
        }

        public ExType(string message) : base(message)
        {
        }

        public ExType(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExType(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAbsenceOfGenerateIntoInvokingTypeForBaseList()
        {
            await TestActionCountAsync(
@"class Class : [|BaseClass|]
{
}",
count: 3,
parameters: new TestParameters(Options.Regular));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromUsingStatement()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        using ([|Goo|] f = new Goo())
        {
        }
    }
}",
@"class Class
{
    void Method()
    {
        using (Goo f = new Goo())
        {
        }
    }

    private class Goo
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromForeachStatement()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        foreach ([|Employee|] e in empList)
        {
        }
    }
}",
@"class Class
{
    void Method()
    {
        foreach (Employee e in empList)
        {
        }
    }

    private class Employee
    {
    }
}",
index: 2);
        }

        [WorkItem(538346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538346")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassWhereKeywordBecomesTypeName()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|@class|] c;
}",
@"class Class
{
    @class c;

    private class @class
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassOnContextualKeyword()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|@Goo|] c;
}",
@"class Class
{
    @Goo c;

    private class Goo
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateClassOnFrameworkTypes()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|System|].Console.Write(5);
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        System.[|Console|].Write(5);
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        System.Console.[|Write|](5);
    }
}");
        }

        [WorkItem(538409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538409")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateIntoRightPart()
        {
            await TestInRegular73AndScriptAsync(
@"partial class Class
{
}

partial class Class
{
    [|C|] c;
}",
@"partial class Class
{
}

partial class Class
{
    C c;

    private class C
    {
    }
}",
index: 2);
        }

        [WorkItem(538408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538408")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoCompilationUnit()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|C|] c;

    void Main()
    {
    }
}",
@"class Class
{
    C c;

    void Main()
    {
    }
}

internal class C
{
}",
index: 1);
        }

        [WorkItem(538408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538408")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoNamespace()
        {
            await TestInRegular73AndScriptAsync(
@"namespace N
{
    class Class
    {
        [|C|] c;

        void Main()
        {
        }
    }
}",
@"namespace N
{
    class Class
    {
        C c;

        void Main()
        {
        }
    }

    internal class C
    {
    }
}",
index: 1);
        }

        [WorkItem(538115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538115")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithPreprocessor()
        {
            await TestInRegular73AndScriptAsync(
@"class C
{
#if true 
    void Goo([|A|] x) { }
#else
#endif
}",
@"class C
{
#if true 
    void Goo(A x) { }

    private class A
    {
    }
#else
#endif
}",
index: 2);
        }

        [WorkItem(538495, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538495")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoContainingNamespace()
        {
            await TestInRegular73AndScriptAsync(
@"namespace N
{
    class Class
    {
        N.[|C|] c;
    }
}",
@"namespace N
{
    class Class
    {
        N.C c;
    }

    internal class C
    {
    }
}",
index: 1);
        }

        [WorkItem(538516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateClassFromIntoNewNamespace()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"class Class { static void Main(string[] args) { [|N|].C c; } }",
@"namespace N
{
    internal class C
    {
    }
}",
expectedContainers: ImmutableArray.Create("N"),
expectedDocumentName: "C.cs");
        }

        [WorkItem(538558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538558")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGlobalAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|global|]::System.String s;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        global::[|System|].String s;
    }
}");
        }

        [WorkItem(538069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538069")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation1()
        {
            await TestAsync(
@"class A
{
    void Goo()
    {
        A[] x = new [|C|][] { };
    }
}",
@"class A
{
    void Goo()
    {
        A[] x = new C[] { };
    }
}

internal class C : A
{
}",
index: 1,
parseOptions: null);
        }

        [WorkItem(538069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538069")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation2()
        {
            await TestAsync(
@"class A
{
    void Goo()
    {
        A[][] x = new [|C|][][] { };
    }
}",
@"class A
{
    void Goo()
    {
        A[][] x = new C[][] { };
    }
}

internal class C : A
{
}",
index: 1,
parseOptions: null);
        }

        [WorkItem(538069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538069")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeFromArrayCreation3()
        {
            await TestAsync(
@"class A
{
    void Goo()
    {
        A[] x = new [|C|][][] { };
    }
}",
@"class A
{
    void Goo()
    {
        A[] x = new C[][] { };
    }
}

internal class C
{
}",
index: 1,
parseOptions: null);
        }

        [WorkItem(539329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539329")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestNotInUsingDirective()
        {
            await TestMissingInRegularAndScriptAsync(
@"using [|A|];");

            await TestMissingInRegularAndScriptAsync(
@"using [|A.B|];");

            await TestMissingInRegularAndScriptAsync(
@"using [|A|].B;");

            await TestMissingInRegularAndScriptAsync(
@"using A.[|B|];");

            await TestMissingInRegularAndScriptAsync(
@"using X = [|A|];");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateSimpleConstructor()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M()
    {
        new [|T|]();
    }
}",
@"class Class
{
    void M()
    {
        new T();
    }
}

internal class T
{
    public T()
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithValueParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M()
    {
        new [|T|](1);
    }
}",
@"class Class
{
    void M()
    {
        new T(1);
    }
}

internal class T
{
    private int v;

    public T(int v)
    {
        this.v = v;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithTwoValueParameters()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M()
    {
        new [|T|](1, """");
    }
}",
@"class Class
{
    void M()
    {
        new T(1, """");
    }
}

internal class T
{
    private int v1;
    private string v2;

    public T(int v1, string v2)
    {
        this.v1 = v1;
        this.v2 = v2;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNullableParameter()
        {
            await TestInRegular73AndScriptAsync(
@"#nullable enable
class Class
{
    void M()
    {
        string? s = null;
        new [|T|](s);
    }
}",
@"#nullable enable
class Class
{
    void M()
    {
        string? s = null;
        new [|T|](s);
    }
}

internal class T
{
    private string? s;

    public T(string? s)
    {
        this.s = s;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNullableParameterThatIsNotNull()
        {
            await TestInRegular73AndScriptAsync(
@"#nullable enable
class Class
{
    void M()
    {
        string? s = ""asdf"";
        new [|T|](s);
    }
}",
@"#nullable enable
class Class
{
    void M()
    {
        string? s = ""asdf"";
        new [|T|](s);
    }
}

internal class T
{
    private string s;

    public T(string s)
    {
        this.s = s;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNamedParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M()
    {
        new [|T|](arg: 1);
    }
}",
@"class Class
{
    void M()
    {
        new T(arg: 1);
    }
}

internal class T
{
    private int arg;

    public T(int arg)
    {
        this.arg = arg;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithRefParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        new [|T|](ref i);
    }
}",
@"class Class
{
    void M(int i)
    {
        new T(ref i);
    }
}

internal class T
{
    private int i;

    public T(ref int i)
    {
        this.i = i;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameter()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i, bool b)
    {
        new [|T|](out i, ref b, null);
    }
}",
@"class Class
{
    void M(int i, bool b)
    {
        new T(out i, ref b, null);
    }
}

internal class T
{
    private bool b;
    private object p;

    public T(out int i, ref bool b, object p)
    {
        i = 0;
        this.b = b;
        this.p = p;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(string s)
    {
        new [|T|](out s);
    }
}",
@"class Class
{
    void M(string s)
    {
        new T(out s);
    }
}

internal class T
{
    public T(out string s)
    {
        s = null;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters2_CSharp7()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void M(DateTime d)
    {
        new [|T|](out d);
    }
}",
@"using System;

class Class
{
    void M(DateTime d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out DateTime d)
    {
        d = default(DateTime);
    }
}",
index: 1,
parseOptions: TestOptions.Regular7);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters2()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class Class
{
    void M(DateTime d)
    {
        new [|T|](out d);
    }
}",
@"using System;

class Class
{
    void M(DateTime d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out DateTime d)
    {
        d = default;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters3()
        {
            await TestInRegular73AndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    void M(IList<int> d)
    {
        new [|T|](out d);
    }
}",
@"using System.Collections.Generic;

class Class
{
    void M(IList<int> d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out IList<int> d)
    {
        d = null;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters4()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int? d)
    {
        new [|T|](out d);
    }
}",
@"class Class
{
    void M(int? d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out int? d)
    {
        d = null;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters5()
        {
            await TestInRegular73AndScriptAsync(
@"class Class<X>
{
    void M(X d)
    {
        new [|T|](out d);
    }
}",
@"class Class<X>
{
    void M(X d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out object d)
    {
        d = null;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters6_CSharp7()
        {
            await TestInRegularAndScriptAsync(
@"class Class<X>
{
    void M(X d)
    {
        new [|T|](out d);
    }
}",
@"class Class<X>
{
    void M(X d)
    {
        new T(out d);
    }

    private class T
    {
        public T(out X d)
        {
            d = default(X);
        }
    }
}",
index: 2,
parseOptions: TestOptions.Regular7);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters6()
        {
            await TestInRegular73AndScriptAsync(
@"class Class<X>
{
    void M(X d)
    {
        new [|T|](out d);
    }
}",
@"class Class<X>
{
    void M(X d)
    {
        new T(out d);
    }

    private class T
    {
        public T(out X d)
        {
            d = default;
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters7()
        {
            await TestInRegular73AndScriptAsync(
@"class Class<X> where X : class
{
    void M(X d)
    {
        new [|T|](out d);
    }
}",
@"class Class<X> where X : class
{
    void M(X d)
    {
        new T(out d);
    }
}

internal class T
{
    public T(out object d)
    {
        d = null;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithOutParameters8()
        {
            await TestInRegular73AndScriptAsync(
@"class Class<X> where X : class
{
    void M(X d)
    {
        new [|T|](out d);
    }
}",
@"class Class<X> where X : class
{
    void M(X d)
    {
        new T(out d);
    }

    private class T
    {
        public T(out X d)
        {
            d = null;
        }
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithMethod()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    string M(int i)
    {
        new [|T|](M);
    }
}",
@"using System;

class Class
{
    string M(int i)
    {
        new T(M);
    }
}

internal class T
{
    private Func<int, string> m;

    public T(Func<int, string> m)
    {
        this.m = m;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithLambda()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    string M(int i)
    {
        new [|T|](a => a.ToString());
    }
}",
@"using System;

class Class
{
    string M(int i)
    {
        new T(a => a.ToString());
    }
}

internal class T
{
    private Func<object, object> p;

    public T(Func<object, object> p)
    {
        this.p = p;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](1);
    }
}

class Base
{
    protected Base(int i)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(1);
    }
}

internal class T : Base
{
    public T(int i) : base(i)
    {
    }
}

class Base
{
    protected Base(int i)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor2()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](1);
    }
}

class Base
{
    protected Base(object i)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(1);
    }
}

internal class T : Base
{
    public T(object i) : base(i)
    {
    }
}

class Base
{
    protected Base(object i)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor3()
        {
            await TestInRegular73AndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    void M()
    {
        Base b = new [|T|](new List<int>());
    }
}

class Base
{
    protected Base(IEnumerable<int> values)
    {
    }
}",
@"using System.Collections.Generic;

class Class
{
    void M()
    {
        Base b = new T(new List<int>());
    }
}

internal class T : Base
{
    public T(IEnumerable<int> values) : base(values)
    {
    }
}

class Base
{
    protected Base(IEnumerable<int> values)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor4()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](ref i);
    }
}

class Base
{
    protected Base(ref int o)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(ref i);
    }
}

internal class T : Base
{
    public T(ref int o) : base(ref o)
    {
    }
}

class Base
{
    protected Base(ref int o)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor5()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](a => a.ToString());
    }
}

class Base
{
    protected Base(System.Func<int, string> f)
    {
    }
}",
@"using System;

class Class
{
    void M(int i)
    {
        Base b = new T(a => a.ToString());
    }
}

internal class T : Base
{
    public T(Func<int, string> f) : base(f)
    {
    }
}

class Base
{
    protected Base(System.Func<int, string> f)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructor6()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](out i);
    }
}

class Base
{
    protected Base(out int o)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(out i);
    }
}

internal class T : Base
{
    public T(out int o) : base(out o)
    {
    }
}

class Base
{
    protected Base(out int o)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithDelegatingConstructorAssigningToNullableField()
        {
            await TestInRegular73AndScriptAsync(
@"#nullable enable
class Class
{
    void M()
    {
        Base? b = new [|T|]();
    }
}

class Base
{
}",
@"#nullable enable
class Class
{
    void M()
    {
        Base? b = new [|T|]();
    }
}

internal class T : Base
{
}

class Base
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](1);
    }
}

class Base
{
    protected Base(string i)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(1);
    }
}

internal class T : Base
{
    private int v;

    public T(int v)
    {
        this.v = v;
    }
}

class Base
{
    protected Base(string i)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor2()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](ref i);
    }
}

class Base
{
    protected Base(out int o)
    {
    }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(ref i);
    }
}

internal class T : Base
{
    private int i;

    public T(ref int i)
    {
        this.i = i;
    }
}

class Base
{
    protected Base(out int o)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor3()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i, bool f)
    {
        Base b = new [|T|](out i, out f);
    }
}

class Base
{
    protected Base(ref int o, out bool b)
    {
    }
}",
@"class Class
{
    void M(int i, bool f)
    {
        Base b = new T(out i, out f);
    }
}

internal class T : Base
{
    public T(out int i, out bool f)
    {
        i = 0;
        f = false;
    }
}

class Base
{
    protected Base(ref int o, out bool b)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithNonDelegatingConstructor4()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M()
    {
        Base b = new [|T|](1);
    }
}

class Base
{
    private Base(int i)
    {
    }
}",
@"class Class
{
    void M()
    {
        Base b = new T(1);
    }
}

internal class T : Base
{
    private int v;

    public T(int v)
    {
        this.v = v;
    }
}

class Base
{
    private Base(int i)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected int i;
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        this.i = i;
    }
}

class Base
{
    protected int i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField2()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(string i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected object i;
}",
@"class Class
{
    void M(string i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(string i)
    {
        this.i = i;
    }
}

class Base
{
    protected object i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField3()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(string i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected bool i;
}",
@"class Class
{
    void M(string i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private string i;

    public T(string i)
    {
        this.i = i;
    }
}

class Base
{
    protected bool i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField4()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(bool i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected bool ii;
}",
@"class Class
{
    void M(bool i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private bool i;

    public T(bool i)
    {
        this.i = i;
    }
}

class Base
{
    protected bool ii;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField5()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(bool i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    private bool i;
}",
@"class Class
{
    void M(bool i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private bool i;

    public T(bool i)
    {
        this.i = i;
    }
}

class Base
{
    private bool i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField6()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(bool i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected readonly bool i;
}",
@"class Class
{
    void M(bool i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private bool i;

    public T(bool i)
    {
        this.i = i;
    }
}

class Base
{
    protected readonly bool i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField7()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected int I;
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        I = i;
    }
}

class Base
{
    protected int I;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField7WithQualification()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected int I;
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        this.I = i;
    }
}

class Base
{
    protected int I;
}",
index: 1,
options: Option(CodeStyleOptions.QualifyFieldAccess, true, NotificationOption.Error));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField8()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    private int I;
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private int i;

    public T(int i)
    {
        this.i = i;
    }
}

class Base
{
    private int I;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField9()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    public static int i;
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private int i;

    public T(int i)
    {
        this.i = i;
    }
}

class Base
{
    public static int i;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToField10()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        D d = new [|T|](i);
    }
}

class D : B
{
    protected int I;
}

class B
{
    protected int i }",
@"class Class
{
    void M(int i)
    {
        D d = new T(i);
    }
}

internal class T : D
{
    public T(int i)
    {
        this.i = i;
    }
}

class D : B
{
    protected int I;
}

class B
{
    protected int i }",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    public int I { get; private set; }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    private int i;

    public T(int i)
    {
        this.i = i;
    }
}

class Base
{
    public int I { get; private set; }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty2()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    public int I { get; protected set; }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        I = i;
    }
}

class Base
{
    public int I { get; protected set; }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty2WithQualification()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    public int I { get; protected set; }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        this.I = i;
    }
}

class Base
{
    public int I { get; protected set; }
}",
index: 1,
options: Option(CodeStyleOptions.QualifyPropertyAccess, true, NotificationOption.Error));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty3()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected int I { get; set; }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        I = i;
    }
}

class Base
{
    protected int I { get; set; }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateWithCallToProperty3WithQualification()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        Base b = new [|T|](i);
    }
}

class Base
{
    protected int I { get; set; }
}",
@"class Class
{
    void M(int i)
    {
        Base b = new T(i);
    }
}

internal class T : Base
{
    public T(int i)
    {
        this.I = i;
    }
}

class Base
{
    protected int I { get; set; }
}",
index: 1,
options: Option(CodeStyleOptions.QualifyPropertyAccess, true, NotificationOption.Error));
        }

        [WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithPreferIntrinsicPredefinedKeywordFalse()
        {
            await TestInRegular73AndScriptAsync(
@"class Class {
    void M(int i) 
    {
        var b = new [|T|](i);
    }
}",
@"class Class {
    void M(int i) 
    {
        var b = new T(i);
    }
}

internal class T
{
    private System.Int32 i;

    public T(System.Int32 i)
    {
        this.i = i;
    }
}",
index: 1,
options: Option(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption.Error));
        }

        #endregion

        #region Generate Interface

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"class EmployeeList<T> where T : Employee, [|IEmployee|], new()
{
}",
@"class EmployeeList<T> where T : Employee, IEmployee, new()
{
}

internal interface IEmployee
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraints()
        {
            await TestInRegular73AndScriptAsync(
@"class EmployeeList<T> where T : Employee, IEmployee, [|IComparable<T>|], new()
{
}",
@"class EmployeeList<T> where T : Employee, IEmployee, IComparable<T>, new()
{
}

internal interface IComparable<T> where T : Employee, IEmployee, IComparable<T>, new()
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NegativeTestGenerateInterfaceFromTypeConstraint()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class EmployeeList<T> where T : Employee, IEmployee, [|IComparable<T>|], new()
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList1()
        {
            await TestInRegular73AndScriptAsync(
@"interface A : [|B|]
{
}",
@"interface A : B
{
}

internal interface B
{
}",
index: 1);
        }

        [WorkItem(538519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538519")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList2()
        {
            await TestInRegular73AndScriptAsync(
@"class Test : [|ITest|]
{
}",
@"class Test : ITest
{
}

internal interface ITest
{
}",
index: 1);
        }

        [WorkItem(538519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538519")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromTypeConstraints2()
        {
            await TestInRegular73AndScriptAsync(
@"class Test<T> where T : [|ITest|]
{
}",
@"class Test<T> where T : ITest
{
}

internal interface ITest
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInterfaceFromBaseList3()
        {
            await TestInRegular73AndScriptAsync(
@"class A : object, [|B|]
{
}",
@"class A : object, B
{
}

internal interface B
{
}",
index: 1);
        }

        #endregion

        [WorkItem(539339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539339")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NotInLeftSideOfAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void M(int i)
    {
        [|Goo|] = 2;
    }
}");
        }

        [WorkItem(539339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539339")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task InLeftSideOfAssignment()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        [|Goo|].Bar = 2;
    }
}",
@"class Class
{
    void M(int i)
    {
        Goo.Bar = 2;
    }
}

internal class Goo
{
}",
index: 1);
        }

        [WorkItem(539339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539339")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task NotInRightSideOfAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void M(int i)
    {
        x = [|Goo|];
    }
}");
        }

        [WorkItem(539339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539339")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task InRightSideOfAssignment()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void M(int i)
    {
        x = [|Goo|].Bar;
    }
}",
@"class Class
{
    void M(int i)
    {
        x = Goo.Bar;
    }
}

internal class Goo
{
}",
index: 1);
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEscapedName()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|@Goo|] f;
}",
@"class Class
{
    @Goo f;
}

internal class Goo
{
}",
index: 1);
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEscapedKeyword()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    [|@int|] f;
}",
@"class Class
{
    @int f;
}

internal class @int
{
}",
index: 1);
        }

        [WorkItem(539535, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539535")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateIntoNewFile()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"class Class { void F() { new [|Goo|].Bar(); } }",
@"namespace Goo
{
    internal class Bar
    {
        public Bar()
        {
        }
    }
}",
expectedContainers: ImmutableArray.Create("Goo"),
expectedDocumentName: "Bar.cs");
        }

        [WorkItem(539620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539620")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDeclarationSpan()
        {
            await TestSpansAsync(
@"class Class
{
    void Goo()
    {
        [|Bar|] b;
    }
}",
index: 1);
        }

        [WorkItem(539674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539674")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInEnumBaseList()
        {
            await TestMissingInRegularAndScriptAsync(
@"enum E : [|A|]
{
}");
        }

        [WorkItem(539681, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539681")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInConditional()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        if ([|IsTrue|])
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInUsing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        using ([|Goo|] f = bar())
        {
        }
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        using (Goo f = bar())
        {
        }
    }
}

internal class Goo
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInDelegateConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
@"delegate void D(int x);

class C
{
    void M()
    {
        D d = new D([|Test|]);
    }
}");
        }

        [WorkItem(539754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539754")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|var|] x = new Program();
    }
}");
        }

        [WorkItem(539765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539765")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestElideDefaultConstructor()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    void M()
    {
        C test = new [|B|]();
    }
}

internal class C
{
}",
@"class A
{
    void M()
    {
        C test = new B();
    }
}

internal class B : C
{
}

internal class C
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        [WorkItem(539783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539783")]
        public async Task RegressionFor5867ErrorToleranceTopLevel()
        {
            await TestMissingAsync(
@"[|this|] . f = f ; ",
new TestParameters(GetScriptOptions()));
        }

        [WorkItem(539799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestOnInaccessibleType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private class D
    {
    }
}

class A
{
    void M()
    {
        C.[|D|] d = new C.D();
    }
}");
        }

        [WorkItem(539794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539794")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDefaultConstructorInTypeDerivingFromInterface()
        {
            await TestInRegular73AndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        I obj = new [|A|]();
    }
}

interface I
{
}",
@"class Program
{
    static void Main(string[] args)
    {
        I obj = new A();
    }
}

internal class A : I
{
}

interface I
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateWithThrow()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void M()
    {
        throw new [|NotFoundException|]();
    }
}",
@"using System;
using System.Runtime.Serialization;

class C
{
    void M()
    {
        throw new NotFoundException();
    }
}

[Serializable]
internal class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInTryCatch()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void M()
    {
        try
        {
        }
        catch ([|NotFoundException|] ex)
        {
        }
    }
}",
@"using System;
using System.Runtime.Serialization;

class C
{
    void M()
    {
        try
        {
        }
        catch (NotFoundException ex)
        {
        }
    }
}

[Serializable]
internal class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}",
index: 1);
        }

        [WorkItem(539739, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539739")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNotGenerateInDelegateConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

delegate void D(int x);

class C
{
    void M()
    {
        D d = new D([|Test|]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInStructBaseList()
        {
            await TestInRegular73AndScriptAsync(
@"struct S : [|A|]
{
}",
@"struct S : A
{
}

internal interface A
{
}",
index: 1);
        }

        [WorkItem(539870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539870")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenericWhenNonGenericExists()
        {
            await TestInRegular73AndScriptAsync(
@"class C
{
    void Goo()
    {
        [|A<T>|] a;
    }
}

class A
{
}",
@"class C
{
    void Goo()
    {
        A<T> a;
    }
}

internal class A<T>
{
}

class A
{
}",
index: 1);
        }

        [WorkItem(539930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539930")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestInheritedTypeParameters()
        {
            await TestInRegular73AndScriptAsync(
@"class C<T, R>
{
    void M()
    {
        I<T, R> i = new [|D<T, R>|]();
    }
}

interface I<T, R>
{
}",
@"class C<T, R>
{
    void M()
    {
        I<T, R> i = new D<T, R>();
    }
}

internal class D<T, R> : I<T, R>
{
}

interface I<T, R>
{
}",
index: 1);
        }

        [WorkItem(539971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDoNotUseOuterTypeParameters()
        {
            await TestInRegular73AndScriptAsync(
@"class C<T1, T2>
{
    public void Goo()
    {
        [|D<int, string>|] d;
    }
}",
@"class C<T1, T2>
{
    public void Goo()
    {
        D<int, string> d;
    }

    private class D<T3, T4>
    {
    }
}",
index: 2);
        }

        [WorkItem(539970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539970")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters1()
        {
            await TestInRegular73AndScriptAsync(
@"class M<T, R>
{
    public void Goo()
    {
        I<T, R> i = new [|C<int, string>|]();
    }
}

interface I<T, R>
{
}",
@"class M<T, R>
{
    public void Goo()
    {
        I<T, R> i = new C<int, string>();
    }
}

internal class C<T1, T2> : I<object, object>
{
}

interface I<T, R>
{
}",
index: 1);
        }

        [WorkItem(539970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539970")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters2()
        {
            await TestInRegular73AndScriptAsync(
@"class M<T, R>
{
    public void Goo()
    {
        I<T, R> i = new [|C<int, string>|]();
    }
}

interface I<T, R>
{
}",
@"class M<T, R>
{
    public void Goo()
    {
        I<T, R> i = new C<int, string>();
    }

    private class C<T1, T2> : I<T, R>
    {
    }
}

interface I<T, R>
{
}",
index: 2);
        }

        [WorkItem(539972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539972")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters3()
        {
            await TestInRegular73AndScriptAsync(
@"class C<T1, T2>
{
    public void Goo(T1 t1, T2 t2)
    {
        A a = new [|A|](t1, t2);
    }
}",
@"class C<T1, T2>
{
    public void Goo(T1 t1, T2 t2)
    {
        A a = new A(t1, t2);
    }
}

internal class A
{
    private object t1;
    private object t2;

    public A(object t1, object t2)
    {
        this.t1 = t1;
        this.t2 = t2;
    }
}",
index: 1);
        }

        [WorkItem(539972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539972")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestReferencingTypeParameters4()
        {
            await TestInRegular73AndScriptAsync(
@"class C<T1, T2>
{
    public void Goo(T1 t1, T2 t2)
    {
        A a = new [|A|](t1, t2);
    }
}",
@"class C<T1, T2>
{
    public void Goo(T1 t1, T2 t2)
    {
        A a = new A(t1, t2);
    }

    private class A
    {
        private T1 t1;
        private T2 t2;

        public A(T1 t1, T2 t2)
        {
            this.t1 = t1;
            this.t2 = t2;
        }
    }
}",
index: 2);
        }

        [WorkItem(539992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539992")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotPassingEmptyIssueListToCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main()
    {
        Enumerable.[|T|] Enumerable . Select(Enumerable.Range(0, 9), i => char.Parse(i.ToString())) }
}");
        }

        [WorkItem(540644, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540644")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateWithVoidArg()
        {
            await TestInRegular73AndScriptAsync(
@"class Program
{
    void M()
    {
        C c = new [|C|](M());
    }
}",
@"class Program
{
    void M()
    {
        C c = new C(M());
    }
}

internal class C
{
    private object v;

    public C(object v)
    {
        this.v = v;
    }
}",
index: 1);
        }

        [WorkItem(540989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540989")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInaccessibleType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Outer
{
    class Inner
    {
    }
}

class A
{
    Outer.[|Inner|] inner;
}");
        }

        [WorkItem(540766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540766")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInvalidGlobalCode()
        {
            await TestMissingAsync(
@"[|a|] test ");
        }

        [WorkItem(539985, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539985")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDoNotInferTypeWithWrongArity()
        {
            await TestInRegular73AndScriptAsync(
@"class C<T1>
{
    public void Test()
    {
        C c = new [|C|]();
    }
}",
@"class C<T1>
{
    public void Test()
    {
        C c = new C();
    }
}

internal class C
{
    public C()
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMissingOnInvalidConstructorToExistingType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        new [|Program|](1);
    }
}");
        }

        [WorkItem(541263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541263")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public static class MyExtension
{
    public static int ExtensionMethod(this String s, [|D|] d)
    {
        return 10;
    }
}",
@"public static class MyExtension
{
    public static int ExtensionMethod(this String s, D d)
    {
        return 10;
    }
}

public class D
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestBaseTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C : [|D|]
{
}",
@"public class C : D
{
}

public class D
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestBaseInterfaceAccessibilityConstraint1()
        {
            await TestInRegular73AndScriptAsync(
@"public class C : X, [|IGoo|]
{
}",
@"public class C : X, IGoo
{
}

internal interface IGoo
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint2()
        {
            await TestInRegular73AndScriptAsync(
@"public interface C : [|IBar|], IGoo
{
}",
@"public interface C : IBar, IGoo
{
}

public interface IBar
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityConstraint3()
        {
            await TestInRegular73AndScriptAsync(
@"public interface C : IBar, [|IGoo|]
{
}",
@"public interface C : IBar, IGoo
{
}

public interface IGoo
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDelegateReturnTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public delegate [|D|] Goo();",
@"public delegate D Goo();

public class D
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDelegateParameterAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public delegate D Goo([|S|] d);",
@"public delegate D Goo(S d);

public class S
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMethodParameterAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C
{
    public void Goo([|F|] f);
}",
@"public class C
{
    public void Goo(F f);
}

public class F
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestMethodReturnTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C
{
    public [|F|] Goo(Bar f);
}",
@"public class C
{
    public F Goo(Bar f);

    public class F
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestPropertyTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C
{
    public [|F|] Goo { get; }
}",
@"public class C
{
    public F Goo { get; }

    public class F
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestFieldEventTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C
{
    public event [|F|] E;
}",
@"public class C
{
    public event F E;

    public class F
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestEventTypeAccessibilityConstraint()
        {
            await TestInRegular73AndScriptAsync(
@"public class C
{
    public event [|F|] E
    {
        add
        {
        }

        remove
        {
        }
    }
}",
@"public class C
{
    public event F E
    {
        add
        {
        }

        remove
        {
        }
    }
}

public class F
{
}",
index: 1);
        }

        [WorkItem(541654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541654")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateVarType()
        {
            await TestInRegular73AndScriptAsync(
@"class C
{
    public static void Main()
    {
        [|@var|] v;
    }
}",
@"class C
{
    public static void Main()
    {
        @var v;
    }
}

internal class var
{
}",
index: 1);
        }

        [WorkItem(541641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541641")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestOnBadAttribute()
        {
            await TestInRegular73AndScriptAsync(
@"[[|AttClass|]()]
class C
{
}

internal class AttClassAttribute
{
}",
@"using System;

[AttClass()]
class C
{
}

internal class AttClassAttribute : Attribute
{
}

internal class AttClassAttribute
{
}",
index: 1);
        }

        [WorkItem(542528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct1()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class A<T> where T : struct
{
}

class Program
{
    static void Main()
    {
        new A<[|S|]>();
    }
}",
@"using System;

class A<T> where T : struct
{
}

class Program
{
    static void Main()
    {
        new A<S>();
    }
}

internal struct S
{
}",
index: 1);
        }

        [WorkItem(542480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestCopyConstraints1()
        {
            await TestInRegular73AndScriptAsync(
@"class A<T> where T : class
{
}

class Program
{
    static void Goo<T>() where T : class
    {
        A<T> a = new [|B<T>|]();
    }
}",
@"class A<T> where T : class
{
}

class Program
{
    static void Goo<T>() where T : class
    {
        A<T> a = new B<T>();
    }
}

internal class B<T> : A<T> where T : class
{
}",
index: 1);
        }

        [WorkItem(542528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct2()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class A<T> where T : struct
{
}

class Program
{
    static void Main()
    {
        new A<Program.[|S|]>();
    }
}",
@"using System;

class A<T> where T : struct
{
}

class Program
{
    static void Main()
    {
        new A<Program.S>();
    }

    private struct S
    {
    }
}");
        }

        [WorkItem(542528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateStruct3()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Goo<Program.[|S|]>();
    }

    static void Goo<T>() where T : struct
    {
    }
}",
@"using System;

class Program
{
    static void Main()
    {
        Goo<Program.S>();
    }

    static void Goo<T>() where T : struct
    {
    }

    private struct S
    {
    }
}");
        }

        [WorkItem(542761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542761")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateOpenType1()
        {
            await TestInRegular73AndScriptAsync(
@"class Program
{
    static void Main()
    {
        var x = typeof([|C<,>|]);
    }
}",
@"class Program
{
    static void Main()
    {
        var x = typeof(C<,>);
    }
}

internal class C<T1, T2>
{
}",
index: 1);
        }

        [WorkItem(542766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542766")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateAttributeInGenericType()
        {
            await TestActionCountAsync(
@"using System;

class A<T>
{
    [[|C|]]
    void Goo()
    {
    }
}",
count: 6);
        }

        [WorkItem(543061, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543061")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNestedGenericAccessibility()
        {
            await TestInRegular73AndScriptAsync(
@"using System.Collections.Generic;

public class C
{
    public void Goo(List<[|NewClass|]> x)
    {
    }
}",
@"using System.Collections.Generic;

public class C
{
    public void Goo(List<NewClass> x)
    {
    }
}

public class NewClass
{
}",
index: 1);
        }

        [WorkItem(543493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543493")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task MissingIfNotInTypeStatementOrExpressionContext()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        a [|b|] c d }
}");
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        a b [|c|] d }
}");
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        a b c [|d|] }
}");
        }

        [WorkItem(542641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542641")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAttributeSuffixOnAttributeSubclasses()
        {
            await TestInRegular73AndScriptAsync(
@"using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        CustomConstantAttribute a = new [|GooAttribute|]();
    }
}",
@"using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        CustomConstantAttribute a = new GooAttribute();
    }
}

internal class GooAttribute : CustomConstantAttribute
{
}",
index: 1);
        }

        [WorkItem(543853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543853")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestDisplayStringForGlobalNamespace()
        {
            await TestSmartTagTextAsync(
@"class C : [|Goo|]",
string.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Goo", FeaturesResources.Global_Namespace));
        }

        [WorkItem(543853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543853")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAddDocumentForGlobalNamespace()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"class C : [|Goo|]",
@"internal class Goo
{
}",
ImmutableArray<string>.Empty,
"Goo.cs");
        }

        [WorkItem(543886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543886")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestVerbatimAttribute()
        {
            await TestInRegular73AndScriptAsync(
@"[[|@X|]]
class Class3
{
}",
@"using System;

[@X]
class Class3
{
}

internal class X : Attribute
{
}",
index: 1);
        }

        [WorkItem(531220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531220")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task CompareIncompleteMembersToEqual()
        {
            await TestInRegular73AndScriptAsync(
@"class C
{
    X.X,X class X
    {
        X
    }

    X void X<X void X
    x, [|x|])",
@"class C
{
    X.X,X class X
    {
        X
    }

    X void X<X void X
    x, x)private class x
    {
    }
}
",
index: 2);
        }

        [WorkItem(544168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544168")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotOnAbstractClassCreation()
        {
            await TestMissingInRegularAndScriptAsync(
@"abstract class Goo
{
}

class SomeClass
{
    void goo()
    {
        var q = new [|Goo|]();
    }
}");
        }

        [WorkItem(545362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateInVenus1()
        {
            var code = @"
#line hidden
#line 1 ""Default.aspx""
class Program
{
    static void Main(string[] args)
    {
        [|Goo|] f;
#line hidden
#line 2 ""Default.aspx""
    }
}
";

            await TestExactActionSetOfferedAsync(code,
                new[]
                {
                    string.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Goo", FeaturesResources.Global_Namespace),
                    string.Format(FeaturesResources.Generate_nested_0_1, "class", "Goo", "Program"),
                    FeaturesResources.Generate_new_type
                });

            await TestInRegular73AndScriptAsync(code,
@"
#line hidden
#line 1 ""Default.aspx""
class Program
{
    static void Main(string[] args)
    {
        [|Goo|] f;
#line hidden
#line 2 ""Default.aspx""
    }

    private class Goo
    {
    }
}
", index: 1);
        }

        [WorkItem(869506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869506")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeOutsideCurrentProject()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <ProjectReference>Assembly2</ProjectReference>
                        <Document FilePath=""Test1.cs"">
class Program
{
    static void Main(string[] args)
    {
        [|A.B.C$$|].D f;
    }
}

namespace A
{

}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document FilePath=""Test2.cs"">
namespace A
{
    public class B
    {
    }
}
</Document>
                    </Project>
                </Workspace>";

            var expected = @"
namespace A
{
    public class B
    {
        public class C
        {
        }
    }
}
";

            await TestInRegular73AndScriptAsync(code, expected);
        }

        [WorkItem(932602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932602")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeInFolderNotDefaultNamespace_0()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DefaultNamespace = ""Namespace1.Namespace2"">
                        <Document FilePath=""Test1.cs"">
namespace Namespace1.Namespace2
{
    public class ClassA : [|$$ClassB|]
    {
    }
}
                        </Document>
                    </Project>
                </Workspace>";

            var expected = @"namespace Namespace1.Namespace2
{
    public class ClassB
    {
    }
}";

            await TestAddDocumentInRegularAndScriptAsync(code,
                expected,
                expectedContainers: ImmutableArray<string>.Empty,
                expectedDocumentName: "ClassB.cs");
        }

        [WorkItem(932602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932602")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeInFolderNotDefaultNamespace_1()
        {
            var code = @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DefaultNamespace = ""Namespace1.Namespace2"" >
                        <Document FilePath=""Test1.cs"" Folders=""Namespace1\Namespace2"">
namespace Namespace1.Namespace2.Namespace3
{
    public class ClassA : [|$$ClassB|]
    {
    }
}
                        </Document>
                    </Project>
                </Workspace>";

            var expected = @"namespace Namespace1.Namespace2.Namespace3
{
    public class ClassB
    {
    }
}";

            await TestAddDocumentInRegularAndScriptAsync(code,
                expected,
                expectedContainers: ImmutableArray.Create("Namespace1", "Namespace2"),
                expectedDocumentName: "ClassB.cs");
        }

        [WorkItem(612700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612700")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestGenerateTypeWithNoBraces()
        {
            var code = @"class Test : [|Base|]";

            var expected = @"class Test : Base
internal class Base
{
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(940003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/940003")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties1()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = ""John"", Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = ""John"", Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public string Name { get; set; }
    public DateTime Age { get; set; }
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(940003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/940003")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties2()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = null, Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = null, Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public object Name { get; set; }
    public DateTime Age { get; set; }
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(940003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/940003")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties3()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|](x: 1, y: ""Hello"") {Name = Goo, Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer(x: 1, y: ""Hello"") {Name = Goo, Age = DateTime.Today};
    }
}

internal class Customer
{
    private int x;
    private string y;

    public Customer(int x, string y)
    {
        this.x = x;
        this.y = y;
    }

    public object Name { get; set; }
    public DateTime Age { get; set; }
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(1082031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082031")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithProperties4()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new [|Customer|] {Name = ""John"", Age = DateTime.Today};
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        var c = new Customer {Name = ""John"", Age = DateTime.Today};
    }
}

internal class Customer
{
    public string Name { get; set; }
    public DateTime Age { get; set; }
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithNameOf()
        {
            var code = @"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}
";

            var expected = @"class C
{
    void M()
    {
        var x = nameof(Z);
    }
}

internal class Z
{
}";

            await TestInRegular73AndScriptAsync(code, expected, index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176"), WorkItem(1073099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithNameOf2()
        {
            var code = @"class C
{
    void M()
    {
        var x = nameof([|C.Test|]);
    }
}";

            var expected = @"class C
{
    void M()
    {
        var x = nameof(C.Test);
    }

    private class Test
    {
    }
}";

            await TestInRegular73AndScriptAsync(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithUsingStatic()
        {
            await TestInRegular73AndScriptAsync(
@"using static [|Sample|];",
@"using static Sample;

internal class Sample
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestWithUsingStatic2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using [|Sample|];");
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B b = new [|B|]();
}",
@"public class B
{
    public B()
    {
    }
}");
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields2()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B b = new [|B|]();
}",
@"class A
{
    public B b = new B();
}

public class B
{
    public B()
    {
    }
}",
index: 1);
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields3()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B b = new [|B|]();
}",
@"class A
{
    public B b = new B();

    public class B
    {
        public B()
        {
        }
    }
}",
index: 2);
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields4()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B<int> b = new [|B|]<int>();
}",
@"public class B<T>
{
    public B()
    {
    }
}");
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields5()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B<int> b = new [|B|]<int>();
}",
@"class A
{
    public B<int> b = new B<int>();
}

public class B<T>
{
    public B()
    {
    }
}",
index: 1);
        }

        [WorkItem(1107929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestAccessibilityForPublicFields6()
        {
            await TestInRegular73AndScriptAsync(
@"class A
{
    public B<int> b = new [|B|]<int>();
}",
@"class A
{
    public B<int> b = new B<int>();

    public class B<T>
    {
        public B()
        {
        }
    }
}",
index: 2);
        }

        [WorkItem(17361, "https://github.com/dotnet/roslyn/issues/17361")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestPreserveFileBanner1()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"// I am a banner

class Program
{
    void Main ( )
    {
        [|Goo|] f ;
    }
} ",
@"// I am a banner

internal class Goo
{
}",
expectedContainers: ImmutableArray<string>.Empty,
expectedDocumentName: "Goo.cs");
        }

        [WorkItem(17361, "https://github.com/dotnet/roslyn/issues/17361")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestPreserveFileBanner2()
        {
            await TestAddDocumentInRegularAndScriptAsync(
@"/// I am a doc comment
class Program
{
    void Main ( )
    {
        [|Goo|] f ;
    }
} ",
@"internal class Goo
{
}",
expectedContainers: ImmutableArray<string>.Empty,
expectedDocumentName: "Goo.cs");
        }

        [WorkItem(17361, "https://github.com/dotnet/roslyn/issues/17361")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestPreserveFileBanner3()
        {
            await TestAddDocumentInRegularAndScriptAsync(
    @"// I am a banner
using System;

class Program
{
    void Main (StackOverflowException e)
    {
        var f = new [|Goo|](e);
    }
}",
    @"// I am a banner
using System;

internal class Goo
{
    private StackOverflowException e;

    public Goo(StackOverflowException e)
    {
        this.e = e;
    }
}",
    expectedContainers: ImmutableArray<string>.Empty,
    expectedDocumentName: "Goo.cs");
        }

        [WorkItem(22293, "https://github.com/dotnet/roslyn/issues/22293")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        [InlineData("void")]
        [InlineData("int")]
        public async Task TestMethodGroupWithMissingSystemActionAndFunc(string returnType)
        {
            await TestInRegular73AndScriptAsync(
    $@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""false"">
        <Document><![CDATA[
class C
{{
    void M()
    {{
        new [|Class|](Method);
    }}

    {returnType} Method()
    {{
    }}
}}
]]>
        </Document>
    </Project>
</Workspace>",
    $@"
class C
{{
    void M()
    {{
        new Class(Method);
    }}

    {returnType} Method()
    {{
    }}
}}

internal class Class
{{
    private global::System.Object method;

    public Class(global::System.Object method)
    {{
        this.method = method;
    }}
}}",
    index: 1);
        }
    }

    public partial class GenerateTypeWithUnboundAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUnboundIdentifiersDiagnosticAnalyzer(), new GenerateTypeCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> codeActions)
            => FlattenActions(codeActions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        [WorkItem(13211, "https://github.com/dotnet/roslyn/issues/13211")]
        public async Task TestGenerateOffOfIncompleteMember()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    public [|Goo|]
}",
@"class Class
{
    public Goo
}

internal class Goo
{
}",
index: 1);
        }
    }
}
