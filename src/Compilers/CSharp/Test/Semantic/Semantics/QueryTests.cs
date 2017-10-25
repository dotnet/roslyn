// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class QueryTests : CompilingTestBase
    {
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void DegenerateQueryExpression()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i;
        if (ReferenceEquals(c, r)) throw new Exception();
        // List1<int> r = c.Select(i => i);
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void FromClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from i in c select i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from i in c select i')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void QueryContinuation()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i into q select q;
        if (ReferenceEquals(c, r)) throw new Exception();
        // List1<int> r = c.Select(i => i);
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void QueryContinuation_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from i in c select i into q select q/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from i in c ...  q select q')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select q')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'select i')
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i')
                              ReturnedValue: 
                                IOperation:  (OperationKind.None) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'q')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'q')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'q')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'q')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'q')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'q')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Select()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i+1;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[2, 3, 4, 5, 6, 7, 8]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void SelectClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from i in c select i+1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from i in c select i+1')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i+1')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i+1')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i+1')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i+1')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i+1')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i+1')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i+1')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupBy01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        var r = from i in c group i by i % 2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[1, 3, 5, 7], 0:[2, 4, 6]]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void GroupByClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from i in c group i by i % 2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Linq.IGrouping<System.Int32, System.Int32>>) (Syntax: 'from i in c ...  i by i % 2')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<System.Int32, System.Int32>> System.Linq.Enumerable.GroupBy<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> keySelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Linq.IGrouping<System.Int32, System.Int32>>, IsImplicit) (Syntax: 'group i by i % 2')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'i % 2')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i % 2')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i % 2')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i % 2')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i % 2')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i % 2')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupBy02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        var r = from i in c group 10+i by i % 2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[11, 13, 15, 17], 0:[12, 14, 16]]");
        }

        [Fact]
        public void Cast()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<object> c = new List1<object>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from int i in c select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void CastInFromClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<object> c = new List<object>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from int i in c select i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from int i in c select i')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int i in c')
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int i in c')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c')
                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Object>) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void Where()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<object> c = new List1<object>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from int i in c where i < 5 select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void WhereClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<object> c = new List<object>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from int i in c where i < 5 select i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from int i  ...  5 select i')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Where<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Boolean> predicate)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'where i < 5')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int i in c')
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int i in c')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c')
                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Object>) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, IsImplicit) (Syntax: 'i < 5')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Boolean>, IsImplicit) (Syntax: 'i < 5')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i < 5')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i < 5')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i < 5')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void FromJoinSelect()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(10, 30, 40, 50, 60, 70);
        List1<int> r = from x1 in c1
                      join x2 in c2 on x1 equals x2/10
                      select x1+x2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 33, 44, 55, 77]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void FromJoinSelect_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        List<int> c2 = new List<int>() {10, 30, 40, 50, 60, 70};
        var r = /*<bind>*/from x1 in c1
                join x2 in c2 on x1 equals x2/10
                select x1+x2/*</bind>*/;
    }
}
";
            // BoundRangeVariable still doesn't have an IOperation API: https://github.com/dotnet/roslyn/issues/21238

            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from x1 in  ... elect x1+x2')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Join<System.Int32, System.Int32, System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> outer, System.Collections.Generic.IEnumerable<System.Int32> inner, System.Func<System.Int32, System.Int32> outerKeySelector, System.Func<System.Int32, System.Int32> innerKeySelector, System.Func<System.Int32, System.Int32, System.Int32> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'join x2 in  ... quals x2/10')
      Instance Receiver: 
        null
      Arguments(5):
          IArgument (ArgumentKind.Explicit, Matching Parameter: outer) (OperationKind.Argument, IsImplicit) (Syntax: 'from x1 in c1')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x1 in c1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c2')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x1')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'x1')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x1')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x1')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x1')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x2/10')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'x2/10')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x2/10')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x2/10')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x2/10')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x2/10')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'x2')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x1+x2')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32, System.Int32>, IsImplicit) (Syntax: 'x1+x2')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x1+x2')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x1+x2')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x1+x2')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x1+x2')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'x1')
                          Right: 
                            IOperation:  (OperationKind.None) (Syntax: 'x2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OrderBy()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(28, 51, 27, 84, 27, 27, 72, 64, 55, 46, 39);
        var r =
            from i in c
            orderby i/10 descending, i%10
            select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[84, 72, 64, 51, 55, 46, 39, 27, 27, 27, 28]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void OrderByClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() {1, 2, 3, 4, 5, 6, 7};
        var r = /*<bind>*/from i in c
            orderby i/10 descending, i%10
            select i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Linq.IOrderedEnumerable<System.Int32>) (Syntax: 'from i in c ... select i')
  Expression: 
    IInvocationExpression (System.Linq.IOrderedEnumerable<System.Int32> System.Linq.Enumerable.ThenBy<System.Int32, System.Int32>(this System.Linq.IOrderedEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> keySelector)) (OperationKind.InvocationExpression, Type: System.Linq.IOrderedEnumerable<System.Int32>, IsImplicit) (Syntax: 'i%10')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'i/10 descending')
            IInvocationExpression (System.Linq.IOrderedEnumerable<System.Int32> System.Linq.Enumerable.OrderByDescending<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> keySelector)) (OperationKind.InvocationExpression, Type: System.Linq.IOrderedEnumerable<System.Int32>, IsImplicit) (Syntax: 'i/10 descending')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'i/10')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i/10')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i/10')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i/10')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i/10')
                              ReturnedValue: 
                                IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i/10')
                                  Left: 
                                    IOperation:  (OperationKind.None) (Syntax: 'i')
                                  Right: 
                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'i%10')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i%10')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i%10')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i%10')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i%10')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i%10')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupJoin()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);
        List1<string> r =
            from x1 in c1
            join x2 in c2 on x1 equals x2 / 10 into g
            select x1 + "":"" + g.ToString();
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52], 7:[75]]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void GroupJoinClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{12, 34, 42, 51, 52, 66, 75};
        var r =
            /*<bind>*/from x1 in c1
            join x2 in c2 on x1 equals x2 / 10 into g
            select x1 + "":"" + g.ToString()/*</bind>*/;
    }
}
";
            // BoundRangeVariable still doesn't have an IOperation API: https://github.com/dotnet/roslyn/issues/21238

            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'from x1 in  ... .ToString()')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.GroupJoin<System.Int32, System.Int32, System.Int32, System.String>(this System.Collections.Generic.IEnumerable<System.Int32> outer, System.Collections.Generic.IEnumerable<System.Int32> inner, System.Func<System.Int32, System.Int32> outerKeySelector, System.Func<System.Int32, System.Int32> innerKeySelector, System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>, System.String> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'join x2 in  ... / 10 into g')
      Instance Receiver: 
        null
      Arguments(5):
          IArgument (ArgumentKind.Explicit, Matching Parameter: outer) (OperationKind.Argument, IsImplicit) (Syntax: 'from x1 in c1')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x1 in c1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c2')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x1')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'x1')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x1')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x1')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x1')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x2 / 10')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'x2 / 10')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x2 / 10')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x2 / 10')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x2 / 10')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x2 / 10')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'x2')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x1 + "":"" + g.ToString()')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>, System.String>, IsImplicit) (Syntax: 'x1 + "":"" + g.ToString()')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x1 + "":"" + g.ToString()')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x1 + "":"" + g.ToString()')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x1 + "":"" + g.ToString()')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'x1 + "":"" + g.ToString()')
                          Left: 
                            IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'x1 + "":""')
                              Left: 
                                IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'x1')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    IOperation:  (OperationKind.None) (Syntax: 'x1')
                              Right: 
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "":"") (Syntax: '"":""')
                          Right: 
                            IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'g.ToString()')
                              Instance Receiver: 
                                IOperation:  (OperationKind.None) (Syntax: 'g')
                              Arguments(0)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SelectMany01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> c2 = new List1<int>(10, 20, 30);
        List1<int> r = from x in c1 from y in c2 select x + y;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 21, 31, 12, 22, 32, 13, 23, 33]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void SelectMany_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{12, 34, 42, 51, 52, 66, 75};
        var r = /*<bind>*/from x in c1 from y in c2 select x + y/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from x in c ... elect x + y')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.SelectMany<System.Int32, System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>> collectionSelector, System.Func<System.Int32, System.Int32, System.Int32> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from y in c2')
      Instance Receiver: 
        null
      Arguments(3):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from x in c1')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x in c1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>>, IsImplicit) (Syntax: 'c2')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'c2')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'c2')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'c2')
                      ReturnedValue: 
                        IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c2')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x + y')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32, System.Int32>, IsImplicit) (Syntax: 'x + y')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x + y')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x + y')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + y')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'x')
                          Right: 
                            IOperation:  (OperationKind.None) (Syntax: 'y')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SelectMany02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> c2 = new List1<int>(10, 20, 30);
        List1<int> r = from x in c1 from int y in c2 select x + y;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 21, 31, 12, 22, 32, 13, 23, 33]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Let01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> r1 =
            from int x in c1
            let g = x * 10
            let z = g + x*100
            select x + z;
        System.Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[111, 222, 333]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void LetClause_IOperation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        var r = /*<bind>*/from int x in c1
            let g = x * 10
            let z = g + x*100
            select x + z/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from int x  ... elect x + z')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>, System.Int32>(this System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>> source, System.Func<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select x + z')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'let z = g + x*100')
            IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>> System.Linq.Enumerable.Select<<anonymous type: System.Int32 x, System.Int32 g>, <anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 g>> source, System.Func<<anonymous type: System.Int32 x, System.Int32 g>, <anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>>, IsImplicit) (Syntax: 'let z = g + x*100')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'let g = x * 10')
                    IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 g>> System.Linq.Enumerable.Select<System.Int32, <anonymous type: System.Int32 x, System.Int32 g>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, <anonymous type: System.Int32 x, System.Int32 g>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 g>>, IsImplicit) (Syntax: 'let g = x * 10')
                      Instance Receiver: 
                        null
                      Arguments(2):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int x in c1')
                            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int x in c1')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c1')
                                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c1')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                      Operand: 
                                        ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'x * 10')
                            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, <anonymous type: System.Int32 x, System.Int32 g>>, IsImplicit) (Syntax: 'x * 10')
                              Target: 
                                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x * 10')
                                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x * 10')
                                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x * 10')
                                      ReturnedValue: 
                                        IObjectCreationExpression (Constructor: <anonymous type: System.Int32 x, System.Int32 g>..ctor(System.Int32 x, System.Int32 g)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Int32 x, System.Int32 g>, IsImplicit) (Syntax: 'let g = x * 10')
                                          Arguments(2):
                                              IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsImplicit) (Syntax: 'let g = x * 10')
                                                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'let g = x * 10')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgument (ArgumentKind.Explicit, Matching Parameter: g) (OperationKind.Argument, IsImplicit) (Syntax: 'x * 10')
                                                IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * 10')
                                                  Left: 
                                                    IOperation:  (OperationKind.None) (Syntax: 'x')
                                                  Right: 
                                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Initializer: 
                                            null
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'g + x*100')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 g>, <anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>>, IsImplicit) (Syntax: 'g + x*100')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'g + x*100')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'g + x*100')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'g + x*100')
                              ReturnedValue: 
                                IObjectCreationExpression (Constructor: <anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>..ctor(<anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>, IsImplicit) (Syntax: 'let z = g + x*100')
                                  Arguments(2):
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: <>h__TransparentIdentifier0) (OperationKind.Argument, IsImplicit) (Syntax: 'let z = g + x*100')
                                        IParameterReferenceExpression: <>h__TransparentIdentifier0 (OperationKind.ParameterReferenceExpression, Type: <anonymous type: System.Int32 x, System.Int32 g>, IsImplicit) (Syntax: 'let z = g + x*100')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, IsImplicit) (Syntax: 'g + x*100')
                                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'g + x*100')
                                          Left: 
                                            IOperation:  (OperationKind.None) (Syntax: 'g')
                                          Right: 
                                            IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x*100')
                                              Left: 
                                                IOperation:  (OperationKind.None) (Syntax: 'x')
                                              Right: 
                                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'x + z')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: <anonymous type: System.Int32 x, System.Int32 g> <>h__TransparentIdentifier0, System.Int32 z>, System.Int32>, IsImplicit) (Syntax: 'x + z')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x + z')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x + z')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + z')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + z')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'x')
                          Right: 
                            IOperation:  (OperationKind.None) (Syntax: 'z')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_FromLet()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C c3 = new C(100, 200, 300);
        C r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            let g = x + y + z
            where (x + y / 10 + z / 100) < 6
            select g;
       Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[111, 211, 311, 121, 221, 131, 112, 212, 122, 113]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void TransparentIdentifiers_FromLet_IOperation()
        {
            string source = @"
using C = System.Collections.Generic.List<int>;
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C{1, 2, 3};
        C c2 = new C{10, 20, 30};
        C c3 = new C{100, 200, 300};
        var r1 =
            /*<bind>*/from int x in c1
            from int y in c2
            from int z in c3
            let g = x + y + z
            where (x + y / 10 + z / 100) < 6
            select g/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from int x  ... select g')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, System.Int32>(this System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>> source, System.Func<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select g')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'where (x +  ...  / 100) < 6')
            IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>>(this System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>> source, System.Func<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, System.Boolean> predicate)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>>, IsImplicit) (Syntax: 'where (x +  ...  / 100) < 6')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'let g = x + y + z')
                    IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>, <anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>>(this System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>> source, System.Func<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>, <anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>>, IsImplicit) (Syntax: 'let g = x + y + z')
                      Instance Receiver: 
                        null
                      Arguments(2):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int z in c3')
                            IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>> System.Linq.Enumerable.SelectMany<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> source, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Collections.Generic.IEnumerable<System.Int32>> collectionSelector, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>>, IsImplicit) (Syntax: 'from int z in c3')
                              Instance Receiver: 
                                null
                              Arguments(3):
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                    IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> System.Linq.Enumerable.SelectMany<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>> collectionSelector, System.Func<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'from int y in c2')
                                      Instance Receiver: 
                                        null
                                      Arguments(3):
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int x in c1')
                                            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int x in c1')
                                              Instance Receiver: 
                                                null
                                              Arguments(1):
                                                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c1')
                                                    IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c1')
                                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                      Operand: 
                                                        ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
                                            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>>, IsImplicit) (Syntax: 'c2')
                                              Target: 
                                                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'c2')
                                                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'c2')
                                                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'c2')
                                                      ReturnedValue: 
                                                        IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c2')
                                                          Instance Receiver: 
                                                            null
                                                          Arguments(1):
                                                              IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
                                                                IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c2')
                                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                                  Operand: 
                                                                    ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c2')
                                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'from int y in c2')
                                              Target: 
                                                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'from int y in c2')
                                                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'from int y in c2')
                                                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'from int y in c2')
                                                      ReturnedValue: 
                                                        IObjectCreationExpression (Constructor: <anonymous type: System.Int32 x, System.Int32 y>..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'from int y in c2')
                                                          Arguments(2):
                                                              IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                                                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from int y in c2')
                                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                              IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                                                IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from int y in c2')
                                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                          Initializer: 
                                                            null
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'c3')
                                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Collections.Generic.IEnumerable<System.Int32>>, IsImplicit) (Syntax: 'c3')
                                      Target: 
                                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'c3')
                                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'c3')
                                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'c3')
                                              ReturnedValue: 
                                                IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c3')
                                                  Instance Receiver: 
                                                    null
                                                  Arguments(1):
                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c3')
                                                        IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c3')
                                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                          Operand: 
                                                            ILocalReferenceExpression: c3 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c3')
                                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'from int z in c3')
                                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>>, IsImplicit) (Syntax: 'from int z in c3')
                                      Target: 
                                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'from int z in c3')
                                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'from int z in c3')
                                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'from int z in c3')
                                              ReturnedValue: 
                                                IObjectCreationExpression (Constructor: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>..ctor(<anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>, IsImplicit) (Syntax: 'from int z in c3')
                                                  Arguments(2):
                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: <>h__TransparentIdentifier0) (OperationKind.Argument, IsImplicit) (Syntax: 'from int z in c3')
                                                        IParameterReferenceExpression: <>h__TransparentIdentifier0 (OperationKind.ParameterReferenceExpression, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'from int z in c3')
                                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, IsImplicit) (Syntax: 'from int z in c3')
                                                        IParameterReferenceExpression: z (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from int z in c3')
                                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                  Initializer: 
                                                    null
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'x + y + z')
                            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>, <anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>>, IsImplicit) (Syntax: 'x + y + z')
                              Target: 
                                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x + y + z')
                                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x + y + z')
                                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + y + z')
                                      ReturnedValue: 
                                        IObjectCreationExpression (Constructor: <anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>..ctor(<anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, IsImplicit) (Syntax: 'let g = x + y + z')
                                          Arguments(2):
                                              IArgument (ArgumentKind.Explicit, Matching Parameter: <>h__TransparentIdentifier1) (OperationKind.Argument, IsImplicit) (Syntax: 'let g = x + y + z')
                                                IParameterReferenceExpression: <>h__TransparentIdentifier1 (OperationKind.ParameterReferenceExpression, Type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z>, IsImplicit) (Syntax: 'let g = x + y + z')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgument (ArgumentKind.Explicit, Matching Parameter: g) (OperationKind.Argument, IsImplicit) (Syntax: 'x + y + z')
                                                IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y + z')
                                                  Left: 
                                                    IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
                                                      Left: 
                                                        IOperation:  (OperationKind.None) (Syntax: 'x')
                                                      Right: 
                                                        IOperation:  (OperationKind.None) (Syntax: 'y')
                                                  Right: 
                                                    IOperation:  (OperationKind.None) (Syntax: 'z')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Initializer: 
                                            null
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, IsImplicit) (Syntax: '(x + y / 10 ...  / 100) < 6')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, System.Boolean>, IsImplicit) (Syntax: '(x + y / 10 ...  / 100) < 6')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: '(x + y / 10 ...  / 100) < 6')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: '(x + y / 10 ...  / 100) < 6')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: '(x + y / 10 ...  / 100) < 6')
                              ReturnedValue: 
                                IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(x + y / 10 ...  / 100) < 6')
                                  Left: 
                                    IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y / 10 + z / 100')
                                      Left: 
                                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y / 10')
                                          Left: 
                                            IOperation:  (OperationKind.None) (Syntax: 'x')
                                          Right: 
                                            IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'y / 10')
                                              Left: 
                                                IOperation:  (OperationKind.None) (Syntax: 'y')
                                              Right: 
                                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                                      Right: 
                                        IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'z / 100')
                                          Left: 
                                            IOperation:  (OperationKind.None) (Syntax: 'z')
                                          Right: 
                                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
                                  Right: 
                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'g')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: <anonymous type: <anonymous type: System.Int32 x, System.Int32 y> <>h__TransparentIdentifier0, System.Int32 z> <>h__TransparentIdentifier1, System.Int32 g>, System.Int32>, IsImplicit) (Syntax: 'g')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'g')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'g')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'g')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'g')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_Join01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C r1 =
            from int x in c1
            join y in c2 on x equals y/10
            let z = x+y
            select z;
        Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 22, 33]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_Join02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);
        List1<string> r1 = from x1 in c1
                      join x2 in c2 on x1 equals x2 / 10 into g
                      where x1 < 7
                      select x1 + "":"" + g.ToString();
        Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52]]");
        }

        [Fact]
        public void CodegenBug()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);

        List1<Tuple<int, List1<int>>> r1 =
            c1
            .GroupJoin(c2, x1 => x1, x2 => x2 / 10, (x1, g) => new Tuple<int, List1<int>>(x1, g))
            ;

        Func1<Tuple<int, List1<int>>, bool> condition = (Tuple<int, List1<int>> TR1) => TR1.Item1 < 7;
        List1<Tuple<int, List1<int>>> r2 =
            r1
            .Where(condition)
            ;
        Func1<Tuple<int, List1<int>>, string> map = (Tuple<int, List1<int>> TR1) => TR1.Item1.ToString() + "":"" + TR1.Item2.ToString();

        List1<string> r3 =
            r2
            .Select(map)
            ;
        string r4 = r3.ToString();
        Console.WriteLine(r4);
        return;
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52]]");
        }

        [Fact]
        public void RangeVariables01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C c3 = new C(100, 200, 300);
        C r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateStandardCompilation(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[3].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.NotEqual(MethodKind.ReducedExtension, ((IMethodSymbol)info0.CastInfo.Symbol).MethodKind);
            Assert.Null(info0.OperationInfo.Symbol);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            var y = model.GetDeclaredSymbol(q.Body.Clauses[0]);
            Assert.Equal(SymbolKind.RangeVariable, y.Kind);
            Assert.Equal("y", y.Name);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.NotEqual(MethodKind.ReducedExtension, ((IMethodSymbol)info1.OperationInfo.Symbol).MethodKind);

            var info2 = model.GetQueryClauseInfo(q.Body.Clauses[1]);
            var z = model.GetDeclaredSymbol(q.Body.Clauses[1]);
            Assert.Equal(SymbolKind.RangeVariable, z.Kind);
            Assert.Equal("z", z.Name);
            Assert.Equal("Cast", info2.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info2.OperationInfo.Symbol.Name);

            var info3 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.NotNull(info3);
            // what about info3's contents ???

            var xPyPz = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression as BinaryExpressionSyntax;
            var xPy = xPyPz.Left as BinaryExpressionSyntax;
            Assert.Equal(x, model.GetSemanticInfoSummary(xPy.Left).Symbol);
            Assert.Equal(y, model.GetSemanticInfoSummary(xPy.Right).Symbol);
            Assert.Equal(z, model.GetSemanticInfoSummary(xPyPz.Right).Symbol);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void RangeVariables_IOperation()
        {
            string source = @"
using C = System.Collections.Generic.List<int>;
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C{1, 2, 3};
        C c2 = new C{10, 20, 30};
        C c3 = new C{100, 200, 300};
        var r1 =
            /*<bind>*/from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from int x  ... t x + y + z')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.SelectMany<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> source, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Collections.Generic.IEnumerable<System.Int32>> collectionSelector, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, System.Int32> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int z in c3')
      Instance Receiver: 
        null
      Arguments(3):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
            IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> System.Linq.Enumerable.SelectMany<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>> collectionSelector, System.Func<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'from int y in c2')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from int x in c1')
                    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int x in c1')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c1')
                            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.Int32>>, IsImplicit) (Syntax: 'c2')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'c2')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'c2')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'c2')
                              ReturnedValue: 
                                IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c2')
                                  Instance Receiver: 
                                    null
                                  Arguments(1):
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c2')
                                        IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c2')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                          Operand: 
                                            ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c2')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                    IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'from int y in c2')
                      Target: 
                        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'from int y in c2')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'from int y in c2')
                            IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'from int y in c2')
                              ReturnedValue: 
                                IObjectCreationExpression (Constructor: <anonymous type: System.Int32 x, System.Int32 y>..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'from int y in c2')
                                  Arguments(2):
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from int y in c2')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, IsImplicit) (Syntax: 'from int y in c2')
                                        IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from int y in c2')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'c3')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Collections.Generic.IEnumerable<System.Int32>>, IsImplicit) (Syntax: 'c3')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'c3')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'c3')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'c3')
                      ReturnedValue: 
                        IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'c3')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'c3')
                                IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c3')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    ILocalReferenceExpression: c3 (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x + y + z')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32, System.Int32>, IsImplicit) (Syntax: 'x + y + z')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x + y + z')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x + y + z')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + y + z')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y + z')
                          Left: 
                            IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
                              Left: 
                                IOperation:  (OperationKind.None) (Syntax: 'x')
                              Right: 
                                IOperation:  (OperationKind.None) (Syntax: 'y')
                          Right: 
                            IOperation:  (OperationKind.None) (Syntax: 'z')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void RangeVariables02()
        {
            var csSource = @"
using System;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        var c1 = new int[] {1, 2, 3};
        var c2 = new int[] {10, 20, 30};
        var c3 = new int[] {100, 200, 300};
        var r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateStandardCompilation(csSource, new[] { LinqAssemblyRef });
            foreach (var dd in compilation.GetDiagnostics()) Console.WriteLine(dd);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[3].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Equal(MethodKind.ReducedExtension, ((IMethodSymbol)info0.CastInfo.Symbol).MethodKind);
            Assert.Null(info0.OperationInfo.Symbol);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            var y = model.GetDeclaredSymbol(q.Body.Clauses[0]);
            Assert.Equal(SymbolKind.RangeVariable, y.Kind);
            Assert.Equal("y", y.Name);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal(MethodKind.ReducedExtension, ((IMethodSymbol)info1.OperationInfo.Symbol).MethodKind);

            var info2 = model.GetQueryClauseInfo(q.Body.Clauses[1]);
            var z = model.GetDeclaredSymbol(q.Body.Clauses[1]);
            Assert.Equal(SymbolKind.RangeVariable, z.Kind);
            Assert.Equal("z", z.Name);
            Assert.Equal("Cast", info2.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info2.OperationInfo.Symbol.Name);

            var info3 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.NotNull(info3);
            // what about info3's contents ???

            var xPyPz = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression as BinaryExpressionSyntax;
            var xPy = xPyPz.Left as BinaryExpressionSyntax;
            Assert.Equal(x, model.GetSemanticInfoSummary(xPy.Left).Symbol);
            Assert.Equal(y, model.GetSemanticInfoSummary(xPy.Right).Symbol);
            Assert.Equal(z, model.GetSemanticInfoSummary(xPyPz.Right).Symbol);
        }

        [Fact]
        public void TestGetSemanticInfo01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C r1 =
            from int x in c1
            from int y in c2
            select x + y;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateStandardCompilation(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[2].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("x", model.GetDeclaredSymbol(q.FromClause).Name);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal("y", model.GetDeclaredSymbol(q.Body.Clauses[0]).Name);

            var info2 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            // what about info2's contents?
        }

        [Fact]
        public void TestGetSemanticInfo02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(28, 51, 27, 84, 27, 27, 72, 64, 55, 46, 39);
        var r =
            from i in c
            orderby i/10 descending, i%10
            select i;
        Console.WriteLine(r);
    }
}";
            var compilation = CreateStandardCompilation(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[1].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            Assert.Null(info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("i", model.GetDeclaredSymbol(q.FromClause).Name);
            var i = model.GetDeclaredSymbol(q.FromClause);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Null(info1.CastInfo.Symbol);
            Assert.Null(info1.OperationInfo.Symbol);
            Assert.Null(model.GetDeclaredSymbol(q.Body.Clauses[0]));

            var order = q.Body.Clauses[0] as OrderByClauseSyntax;
            var oinfo0 = model.GetSemanticInfoSummary(order.Orderings[0]);
            Assert.Equal("OrderByDescending", oinfo0.Symbol.Name);

            var oinfo1 = model.GetSemanticInfoSummary(order.Orderings[1]);
            Assert.Equal("ThenBy", oinfo1.Symbol.Name);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(541774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541774")]
        [Fact]
        public void MultipleFromClauseIdentifierInExprNotInContext()
        {
            string source = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q2 = /*<bind>*/from n1 in nums 
                 from n2 in nums
                 select n1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from n1 in  ... select n1')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'from n2 in nums')
      Children(3):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'nums')
            Children(0)
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid, IsImplicit) (Syntax: 'nums')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, IsImplicit) (Syntax: 'nums')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: 'nums')
                ReturnedValue: 
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'nums')
                    Children(0)
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'n1')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'n1')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'n1')
                ReturnedValue: 
                  IOperation:  (OperationKind.None) (Syntax: 'n1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'nums' does not exist in the current context
                //         var q2 = /*<bind>*/from n1 in nums 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nums").WithArguments("nums").WithLocation(8, 39),
                // CS0103: The name 'nums' does not exist in the current context
                //                  from n2 in nums
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nums").WithArguments("nums").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(541906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541906")]
        [Fact]
        public void NullLiteralFollowingJoinInQuery()
        {
            string source = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var query = /*<bind>*/from int i in new int[] { 1 } join null on true equals true select i/*</bind>*/; //CS1031
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from int i  ... ue select i')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'join null o ... equals true')
      Children(5):
          IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Cast<System.Int32>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from int i  ... int[] { 1 }')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'new int[] { 1 }')
                  IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new int[] { 1 }')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[] { 1 }')
                        Dimension Sizes(1):
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new int[] { 1 }')
                        Initializer: 
                          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1 }')
                            Element Values(1):
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'join null o ... equals true')
            Children(1):
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'null')
                  Children(1):
                      ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'true')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'true')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'true')
                ReturnedValue: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'true')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'true')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'true')
                ReturnedValue: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i')
                ReturnedValue: 
                  IOperation:  (OperationKind.None) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //         var query = /*<bind>*/from int i in new int[] { 1 } join null on true equals true select i/*</bind>*/; //CS1031
                Diagnostic(ErrorCode.ERR_TypeExpected, "null").WithLocation(8, 66),
                // CS1001: Identifier expected
                //         var query = /*<bind>*/from int i in new int[] { 1 } join null on true equals true select i/*</bind>*/; //CS1031
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "null").WithLocation(8, 66),
                // CS1003: Syntax error, 'in' expected
                //         var query = /*<bind>*/from int i in new int[] { 1 } join null on true equals true select i/*</bind>*/; //CS1031
                Diagnostic(ErrorCode.ERR_SyntaxError, "null").WithArguments("in", "null").WithLocation(8, 66)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(541779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541779")]
        [Fact]
        public void MultipleFromClauseQueryExpr()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var nums = new int[] { 3, 4 };

        var q2 = from int n1 in nums 
                 from int n2 in nums
                 select n1;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 3 4 4");
        }

        [WorkItem(541782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541782")]
        [Fact]
        public void FromSelectQueryExprOnArraysWithTypeImplicit()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var nums = new int[] { 3, 4 };

        var q2 = from n1 in nums select n1;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";
            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 4");
        }


        [WorkItem(541788, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541788")]
        [Fact]
        public void JoinClauseTest()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var q2 =
           from a in Enumerable.Range(1, 13)
           join b in Enumerable.Range(1, 13) on 4 * a equals b
           select a;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "1 2 3");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void JoinClause_IOperation()
        {
            string source = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var q2 =
           /*<bind>*/from a in Enumerable.Range(1, 13)
           join b in Enumerable.Range(1, 13) on 4 * a equals b
           select a/*</bind>*/;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from a in E ... select a')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Join<System.Int32, System.Int32, System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> outer, System.Collections.Generic.IEnumerable<System.Int32> inner, System.Func<System.Int32, System.Int32> outerKeySelector, System.Func<System.Int32, System.Int32> innerKeySelector, System.Func<System.Int32, System.Int32, System.Int32> resultSelector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'join b in E ...  a equals b')
      Instance Receiver: 
        null
      Arguments(5):
          IArgument (ArgumentKind.Explicit, Matching Parameter: outer) (OperationKind.Argument, IsImplicit) (Syntax: 'Enumerable.Range(1, 13)')
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'Enumerable.Range(1, 13)')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '1')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: '13')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 13) (Syntax: '13')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, IsImplicit) (Syntax: 'Enumerable.Range(1, 13)')
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'Enumerable.Range(1, 13)')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '1')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: '13')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 13) (Syntax: '13')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: '4 * a')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: '4 * a')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: '4 * a')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: '4 * a')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: '4 * a')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '4 * a')
                          Left: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                          Right: 
                            IOperation:  (OperationKind.None) (Syntax: 'a')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'b')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'b')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'b')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'b')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'b')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'b')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'a')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32, System.Int32>, IsImplicit) (Syntax: 'a')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'a')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'a')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'a')
                      ReturnedValue: 
                        IOperation:  (OperationKind.None) (Syntax: 'a')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(541789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541789")]
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void WhereClauseTest()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                where (x > 2)
                select x;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 4");
        }

        [WorkItem(541942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541942")]
        [Fact]
        public void WhereDefinedInType()
        {
            var csSource = @"
using System;

class Y
{
    public int Where(Func<int, bool> predicate)
    {
        return 45;
    }
}

class P
{
    static void Main()
    {
        var src = new Y();
        var query = from x in src
                where x > 0
                select x;

        Console.Write(query);
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "45");
        }

        [Fact]
        public void GetInfoForSelectExpression01()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            SelectClauseSyntax selectClause = (SelectClauseSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("select", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(selectClause.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            var info2 = semanticModel.GetSemanticInfoSummary(selectClause);
            var m = (MethodSymbol)info2.Symbol;
            Assert.Equal("Select", m.ReducedFrom.Name);
        }

        [Fact]
        public void GetInfoForSelectExpression02()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x into w
                 select w;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            SelectClauseSyntax selectClause = (SelectClauseSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("select w", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(selectClause.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
        }

        [Fact]
        public void GetInfoForSelectExpression03()
        {
            string sourceCode = @"
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x+1 into w
                 select w+1;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            compilation.VerifyDiagnostics();
            var semanticModel = compilation.GetSemanticModel(tree);

            var e = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("x+1", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(e);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            Assert.Equal("x", info.Symbol.Name);

            e = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("w+1", StringComparison.Ordinal)).Parent;
            info = semanticModel.GetSemanticInfoSummary(e);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            Assert.Equal("w", info.Symbol.Name);

            var e2 = e.Parent as ExpressionSyntax; // w+1
            var info2 = semanticModel.GetSemanticInfoSummary(e2);
            Assert.Equal(SpecialType.System_Int32, info2.Type.SpecialType);
            Assert.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)", info2.Symbol.ToTestDisplayString());
        }

        [WorkItem(541806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541806")]
        [Fact]
        public void GetDeclaredSymbolForQueryContinuation()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x into w
                 select w;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var queryContinuation = tree.GetRoot().FindToken(sourceCode.IndexOf("into w", StringComparison.Ordinal)).Parent;
            var symbol = semanticModel.GetDeclaredSymbol(queryContinuation);

            Assert.NotNull(symbol);
            Assert.Equal("w", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
        }

        [WorkItem(541899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541899")]
        [Fact]
        public void ComputeQueryVariableType()
        {
            string sourceCode = @"
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select 5;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var selectExpression = tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf('5'));
            var info = semanticModel.GetSpeculativeTypeInfo(selectExpression.SpanStart, SyntaxFactory.ParseExpression("x"), SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
        }

        [WorkItem(541893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541893")]
        [Fact]
        public void GetDeclaredSymbolForJoinIntoClause()
        {
            string sourceCode = @"
using System;
using System.Linq;

static class Test
{
    static void Main()
    {
        var qie = from x3 in new int[] { 0 }
                      join x7 in (new int[] { 0 }) on 5 equals 5 into x8
                      select x8;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var joinInto = tree.GetRoot().FindToken(sourceCode.IndexOf("into x8", StringComparison.Ordinal)).Parent;
            var symbol = semanticModel.GetDeclaredSymbol(joinInto);

            Assert.NotNull(symbol);
            Assert.Equal("x8", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
            Assert.Equal("? x8", symbol.ToTestDisplayString());
        }

        [WorkItem(541982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541982")]
        [WorkItem(543494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543494")]
        [Fact()]
        public void GetDeclaredSymbolAddAccessorDeclIncompleteQuery()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new[] { 1, 2, 3, 4, 5 };

        var query1 = from  event in expr1 select event;
        var query2 = from int
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var unknownAccessorDecls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>();
            var symbols = unknownAccessorDecls.Select(decl => semanticModel.GetDeclaredSymbol(decl));

            Assert.True(symbols.All(s => ReferenceEquals(s, null)));
        }

        [WorkItem(542235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542235")]
        [Fact]
        public void TwoFromClauseFollowedBySelectClause()
        {
            string sourceCode = @"
using System.Linq;

class Test
{
    public static void Main()
    {

        var q2 = from num1 in new int[] { 4, 5 }
                 from num2 in new int[] { 4, 5 }
                 select num1;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var fromClause1 = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => (n.IsKind(SyntaxKind.FromClause)) && (n.ToString().Contains("num1"))).Single() as FromClauseSyntax;
            var fromClause2 = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => (n.IsKind(SyntaxKind.FromClause)) && (n.ToString().Contains("num2"))).Single() as FromClauseSyntax;

            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);
            var queryInfoForFrom1 = semanticModel.GetQueryClauseInfo(fromClause1);
            var queryInfoForFrom2 = semanticModel.GetQueryClauseInfo(fromClause2);

            Assert.Null(queryInfoForFrom1.CastInfo.Symbol);
            Assert.Null(queryInfoForFrom1.OperationInfo.Symbol);

            Assert.Null(queryInfoForFrom2.CastInfo.Symbol);
            Assert.Equal("SelectMany", queryInfoForFrom2.OperationInfo.Symbol.Name);

            Assert.Null(symbolInfoForSelect.Symbol);
            Assert.Empty(symbolInfoForSelect.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfoForSelect.CandidateReason);
        }

        [WorkItem(528747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528747")]
        [Fact]
        public void SemanticInfoForOrderingClauses()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var q1 =
            from x in new int[] { 4, 5 }
            orderby
                x descending,
                x.ToString() ascending,
                x descending
            select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            int count = 0;
            string[] names = { "OrderByDescending", "ThenBy", "ThenByDescending" };
            foreach (var ordering in tree.GetCompilationUnitRoot().DescendantNodes().OfType<OrderingSyntax>())
            {
                var symbolInfo = model.GetSemanticInfoSummary(ordering);
                Assert.Equal(names[count++], symbolInfo.Symbol.Name);
            }
            Assert.Equal(3, count);
        }

        [WorkItem(542266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542266")]
        [Fact]
        public void FromOrderBySelectQueryTranslation()
        {
            string sourceCode = @"
using System;
using System.Collections;
using System.Collections.Generic;

public interface IOrderedEnumerable<TElement> : IEnumerable<TElement>,
    IEnumerable
{
}

public static class Extensions
{
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
    this IEnumerable<TSource> source,
    Func<TSource, TKey> keySelector)

    {
        return null;
    }

    public static IEnumerable<TResult> Select<TSource, TResult>(
    this IEnumerable<TSource> source,
    Func<TSource, TResult> selector)

    {
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {        

        var q1 = from num in new int[] { 4, 5 }
                 orderby num
                 select num;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);

            Assert.Null(symbolInfoForSelect.Symbol);
        }

        [WorkItem(528756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528756")]
        [Fact]
        public void FromWhereSelectTranslation()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;

public static class Extensions
{

    public static IEnumerable<TSource> Where<TSource>(
    this IEnumerable<TSource> source,
    Func<TSource, bool> predicate)
    {
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {

        var q1 = from num in System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)
                 select num;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            semanticModel.GetDiagnostics().Verify(
                // (21,30): error CS1935: Could not find an implementation of the query pattern for source type 'System.Collections.Generic.IEnumerable<int>'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
                //         var q1 = from num in System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)
                Diagnostic(ErrorCode.ERR_QueryNoProviderStandard, "System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)").WithArguments("System.Collections.Generic.IEnumerable<int>", "Select"));
        }

        [WorkItem(528760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528760")]
        [Fact]
        public void FromJoinSelectTranslation()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q1 = from num in new int[] { 4, 5 }
                 join x1 in new int[] { 4, 5 } on num equals x1
                 select x1 + 5;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);

            Assert.Null(symbolInfoForSelect.Symbol);
        }

        [WorkItem(528761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528761")]
        [WorkItem(544585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544585")]
        [Fact]
        public void OrderingSyntaxWithOverloadResolutionFailure()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int[] numbers = new int[] { 4, 5 };

        var q1 = from num in numbers.Single()
                 orderby (x1) => x1.ToString()
                 select num;
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (10,30): error CS1936: Could not find an implementation of the query pattern for source type 'int'.  'OrderBy' not found.
                //         var q1 = from num in numbers.Single()
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "numbers.Single()").WithArguments("int", "OrderBy")
                );
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var orderingClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.AscendingOrdering)).Single() as OrderingSyntax;
            var symbolInfoForOrdering = semanticModel.GetSemanticInfoSummary(orderingClause);

            Assert.Null(symbolInfoForOrdering.Symbol);
        }

        [WorkItem(542292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542292")]
        [Fact]
        public void EmitIncompleteQueryWithSyntaxErrors()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static int Main()
    {
        int [] goo = new int [] {1};
        var q = from x in goo
                select x + 1 into z
                    select z.T
";
            using (var output = new MemoryStream())
            {
                Assert.False(CreateCompilationWithMscorlibAndSystemCore(sourceCode).Emit(output).Success);
            }
        }

        [WorkItem(542294, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542294")]
        [Fact]
        public void EmitQueryWithBindErrors()
        {
            string sourceCode = @"
using System.Linq;
class Program
{
    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        var query = from num in nums
                    let num = 3 // CS1930
                    select num; 
    }
}";
            using (var output = new MemoryStream())
            {
                Assert.False(CreateCompilationWithMscorlibAndSystemCore(sourceCode).Emit(output).Success);
            }
        }

        [WorkItem(542372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542372")]
        [Fact]
        public void BindToIncompleteSelectManyDecl()
        {
            string sourceCode = @"
class P
{
    static C<X> M2<X>(X x)
    {
        return new C<X>(x);
    }

    static void Main()
    {
        C<int> e1 = new C<int>(1);

        var q = from x1 in M2<int>(x1)
                from x2 in e1
                select x1;
    }
}

class C<T>
{
    public C<V> SelectMany";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var diags = semanticModel.GetDiagnostics();

            Assert.NotEmpty(diags);
        }

        [WorkItem(542419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542419")]
        [Fact]
        public void BindIdentifierInWhereErrorTolerance()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var r = args.Where(b => b < > );
        var q = from a in args
                where a <> 
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var diags = semanticModel.GetDiagnostics();
            Assert.NotEmpty(diags);
        }

        [WorkItem(542460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542460")]
        [Fact]
        public void QueryWithMultipleParseErrorsAndScriptParseOption()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new int[] { 1, 2, 3, 4, 5 };

        var query2 = from int namespace in expr1 select namespace;
        var query25 = from i in expr1 let namespace = expr1 select i;
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Where(x => x.ToFullString() == "from i in expr1 let ").Single();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);

            Assert.Null(symbolInfo.Symbol);
        }

        [WorkItem(542496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542496")]
        [Fact]
        public void QueryExpressionInFieldInitReferencingAnotherFieldWithScriptParseOption()
        {
            string sourceCode = @"
using System.Linq;
using System.Collections;

class P
{
    double one = 1;

    public IEnumerable e = 
               from x in new int[] { 1, 2, 3 }
               select x + one;
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Single();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);

            Assert.Null(symbolInfo.Symbol);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(542559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542559")]
        [Fact]
        public void StaticTypeInFromClause()
        {
            string source = @"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        var q2 = string.Empty.Cast<GC>().Select(x => x);
        var q1 = /*<bind>*/from GC x in string.Empty select x/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from GC x i ... ty select x')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'select x')
      Children(2):
          IInvocationExpression (System.Collections.Generic.IEnumerable<System.GC> System.Linq.Enumerable.Cast<System.GC>(this System.Collections.IEnumerable source)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.GC>, IsInvalid, IsImplicit) (Syntax: 'from GC x i ... tring.Empty')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsInvalid, IsImplicit) (Syntax: 'string.Empty')
                  IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsInvalid, IsImplicit) (Syntax: 'string.Empty')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
                        Instance Receiver: 
                          null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x')
                ReturnedValue: 
                  IOperation:  (OperationKind.None) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0718: 'GC': static types cannot be used as type arguments
                //         var q2 = string.Empty.Cast<GC>().Select(x => x);
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "string.Empty.Cast<GC>").WithArguments("System.GC").WithLocation(9, 18),
                // CS0718: 'GC': static types cannot be used as type arguments
                //         var q1 = /*<bind>*/from GC x in string.Empty select x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "from GC x in string.Empty").WithArguments("System.GC").WithLocation(10, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(542560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542560")]
        [Fact]
        public void MethodGroupInFromClause()
        {
            string source = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var q1 = /*<bind>*/from y in Main select y/*</bind>*/;
        var q2 = Main.Select(y => y);
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from y in Main select y')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'select y')
      Children(2):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'from y in Main')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Main')
                  Children(1):
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsInvalid, IsImplicit) (Syntax: 'Main')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'y')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'y')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'y')
                ReturnedValue: 
                  IOperation:  (OperationKind.None) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0119: 'Program.Main()' is a method, which is not valid in the given context
                //         var q1 = /*<bind>*/from y in Main select y/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Main").WithArguments("Program.Main()", "method").WithLocation(9, 38),
                // CS0119: 'Program.Main()' is a method, which is not valid in the given context
                //         var q2 = Main.Select(y => y);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Main").WithArguments("Program.Main()", "method").WithLocation(10, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(542558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542558")]
        [Fact]
        public void SelectFromType01()
        {
            string sourceCode = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = from x in C select x;
    }

    static IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "C").Single();
            dynamic main = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = main.Body.Statements[0].Declaration.Variables[0].Initializer.Value;
            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal(null, info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            var infoSelect = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.Equal("Select", infoSelect.Symbol.Name);
        }

        [WorkItem(542558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542558")]
        [Fact]
        public void SelectFromType02()
        {
            string sourceCode = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = from x in C select x;
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "C").Single();
            dynamic main = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = main.Body.Statements[0].Declaration.Variables[0].Initializer.Value;
            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal(null, info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            var infoSelect = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.Equal("Select", infoSelect.Symbol.Name);
        }

        [WorkItem(542624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542624")]
        [Fact]
        public void QueryColorColor()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;

class Color
{
    public static IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}

class Flavor
{
    public IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}

class Program
{
    Color Color;
    static Flavor Flavor;
    static void Main()
    {
        var q1 = from x in Color select x;
        var q2 = from x in Flavor select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (17,11): warning CS0169: The field 'Program.Color' is never used
                //     Color Color;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Color").WithArguments("Program.Color"),
                // (18,19): warning CS0649: Field 'Program.Flavor' is never assigned to, and will always have its default value null
                //     static Flavor Flavor;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Flavor").WithArguments("Program.Flavor", "null")
            );
        }

        [WorkItem(542704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542704")]
        [Fact]
        public void QueryOnSourceWithGroupByMethod()
        {
            string source = @"
delegate T Func<A, T>(A a);

class Y<U>
{
    public U u;
    public Y(U u)
    {
        this.u = u;
    }

    public string GroupBy(Func<U, string> keySelector)
    {
        return null;
    }
}

class Test
{
    static int Main()
    {
        Y<int> src = new Y<int>(2);
        string q1 = src.GroupBy(x => x.GetType().Name); // ok
        string q2 = from x in src group x by x.GetType().Name; // Roslyn CS1501
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void RangeTypeAlreadySpecified()
        {
            string source = @"
using System.Linq;
using System.Collections;

static class Test
{
    public static void Main2()
    {
        var list = new CastableToArrayList();
        var q = /*<bind>*/from int x in list
                select x + 1/*</bind>*/;
    }
}

class CastableToArrayList
{
    public ArrayList Cast<T>() { return null; }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from int x  ... elect x + 1')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'select x + 1')
      Children(2):
          IInvocationExpression ( System.Collections.ArrayList CastableToArrayList.Cast<System.Int32>()) (OperationKind.InvocationExpression, Type: System.Collections.ArrayList, IsInvalid, IsImplicit) (Syntax: 'from int x in list')
            Instance Receiver: 
              ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: CastableToArrayList, IsInvalid) (Syntax: 'list')
            Arguments(0)
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x + 1')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x + 1')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + 1')
                ReturnedValue: 
                  IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: ?) (Syntax: 'x + 1')
                    Left: 
                      IOperation:  (OperationKind.None) (Syntax: 'x')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1936: Could not find an implementation of the query pattern for source type 'ArrayList'.  'Select' not found.
                //         var q = /*<bind>*/from int x in list
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "list").WithArguments("System.Collections.ArrayList", "Select").WithLocation(10, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(11414, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InvalidQueryWithAnonTypesAndKeywords()
        {
            string source = @"
public class QueryExpressionTest
{
    public static void Main()
    {
        var query7 = from  i in expr1 join  const in expr2 on i equals const select new { i, const };
        var query8 = from int i in expr1  select new { i, const };
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(543787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543787")]
        [ClrOnlyFact]
        public void GetSymbolInfoOfSelectNodeWhenTypeOfRangeVariableIsErrorType()
        {
            string source = @"
using System.Linq;

class Test
{
    static void V()
    {
    }

    public static int Main()
    {
        var e1 = from i in V() select i;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            var tree = compilation.SyntaxTrees.First();
            var index = source.IndexOf("select i", StringComparison.Ordinal);
            var selectNode = tree.GetCompilationUnitRoot().FindToken(index).Parent as SelectClauseSyntax;
            var model = compilation.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(selectNode);
            Assert.NotNull(symbolInfo);
            Assert.Null(symbolInfo.Symbol); // there is no select method to call because the receiver is bad
            var typeInfo = model.GetTypeInfo(selectNode);
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
        }

        [WorkItem(543790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543790")]
        [Fact]
        public void GetQueryClauseInfoForQueryWithSyntaxErrors()
        {
            string source = @"
using System.Linq;

class Test
{
	public static void Main ()
	{
        var query8 = from int i in expr1 join int delegate in expr2 on i equals delegate select new { i, delegate };
	}
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            var tree = compilation.SyntaxTrees.First();
            var index = source.IndexOf("join int delegate in expr2 on i equals delegate", StringComparison.Ordinal);
            var joinNode = tree.GetCompilationUnitRoot().FindToken(index).Parent as JoinClauseSyntax;
            var model = compilation.GetSemanticModel(tree);
            var queryInfo = model.GetQueryClauseInfo(joinNode);

            Assert.NotNull(queryInfo);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(545797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545797")]
        [Fact]
        public void QueryOnNull()
        {
            string source = @"
using System;
static class C
{
    static void Main()
    {
        var q = /*<bind>*/from x in null select x/*</bind>*/;
    }

    static object Select(this object x, Func<int, int> y)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Object, IsInvalid) (Syntax: 'from x in null select x')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'select x')
      Children(2):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'from x in null')
            Children(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, IsImplicit) (Syntax: 'x')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: 'x')
                ReturnedValue: 
                  IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0186: Use of null is not valid in this context
                //         var q = /*<bind>*/from x in null select x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NullNotValid, "select x").WithLocation(7, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(545797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545797")]
        [Fact]
        public void QueryOnLambda()
        {
            string source = @"
using System;
static class C
{
    static void Main()
    {
        var q = /*<bind>*/from x in y => y select x/*</bind>*/;
    }

    static object Select(this object x, Func<int, int> y)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Object, IsInvalid) (Syntax: 'from x in y ...  y select x')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'select x')
      Children(2):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'from x in y => y')
            Children(1):
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'y => y')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'y')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'y')
                      ReturnedValue: 
                        IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: ?) (Syntax: 'y')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, IsImplicit) (Syntax: 'x')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: 'x')
                ReturnedValue: 
                  IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1936: Could not find an implementation of the query pattern for source type 'anonymous method'.  'Select' not found.
                //         var q = /*<bind>*/from x in y => y select x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "select x").WithArguments("anonymous method", "Select").WithLocation(7, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(545444, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545444")]
        [Fact]
        public void RefOmittedOnComCall()
        {
            string source = @"using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

[ComImport]
[Guid(""A88A175D-2448-447A-B786-64682CBEF156"")]
public interface IRef1
{
    int M(ref int x, int y);
}

public class Ref1Impl : IRef1
{
    public int M(ref int x, int y) { return x + y; }
}

class Test
{
   public static void Main()
   {
       IRef1 ref1 = new Ref1Impl();
       Expression<Func<int, int, int>> F = (x, y) => ref1.M(x, y);
   }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (22,54): error CS2037: An expression tree lambda may not contain a COM call with ref omitted on arguments
                //        Expression<Func<int, int, int>> F = (x, y) => ref1.M(x, y);
                Diagnostic(ErrorCode.ERR_ComRefCallInExpressionTree, "ref1.M(x, y)")
                );
        }

        [Fact, WorkItem(5728, "https://github.com/dotnet/roslyn/issues/5728")]
        public void RefOmittedOnComCallErr()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

[ComImport]
[Guid(""A88A175D-2448-447A-B786-64682CBEF156"")]
public interface IRef1
{
    long M(uint y, ref int x, int z);
    long M(uint y, ref int x, int z, int q);
}

public class Ref1Impl : IRef1
{
    public long M(uint y, ref int x, int z) { return x + y; }
    public long M(uint y, ref int x, int z, int q) { return x + y; }
}

class Test1
{
    static void Test(Expression<Action<IRef1>> e)
    {

    }

    static void Test<U>(Expression<Func<IRef1, U>> e)
    {

    }

    public static void Main()
    {
        Test(ref1 => ref1.M(1, ));
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
    // (34,32): error CS1525: Invalid expression term ')'
    //         Test(ref1 => ref1.M(1, ));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(34, 32)
                );
        }


        [WorkItem(529350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529350")]
        [Fact]
        public void BindLambdaBodyWhenError()
        {
            string source =
@"using System.Linq;

class A
{
    static void Main()
    {
    }
    static void M(System.Reflection.Assembly[] a)
    {
        var q2 = a.SelectMany(assem2 => assem2.UNDEFINED, (assem2, t) => t);

        var q1 = from assem1 in a
                 from t in assem1.UNDEFINED
                 select t;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (10,48): error CS1061: 'System.Reflection.Assembly' does not contain a definition for 'UNDEFINED' and no extension method 'UNDEFINED' accepting a first argument of type 'System.Reflection.Assembly' could be found (are you missing a using directive or an assembly reference?)
                //         var q2 = a.SelectMany(assem2 => assem2.UNDEFINED, (assem2, t) => t);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "UNDEFINED").WithArguments("System.Reflection.Assembly", "UNDEFINED"),
                // (13,35): error CS1061: 'System.Reflection.Assembly' does not contain a definition for 'UNDEFINED' and no extension method 'UNDEFINED' accepting a first argument of type 'System.Reflection.Assembly' could be found (are you missing a using directive or an assembly reference?)
                //                  from t in assem1.UNDEFINED
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "UNDEFINED").WithArguments("System.Reflection.Assembly", "UNDEFINED")
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var assem2 =
                tree.GetCompilationUnitRoot().DescendantNodes(n => n.ToString().Contains("assem2"))
                .Where(e => e.ToString() == "assem2")
                .OfType<ExpressionSyntax>()
                .Single();
            var typeInfo2 = model.GetTypeInfo(assem2);
            Assert.NotEqual(TypeKind.Error, typeInfo2.Type.TypeKind);
            Assert.Equal("Assembly", typeInfo2.Type.Name);

            var assem1 =
                tree.GetCompilationUnitRoot().DescendantNodes(n => n.ToString().Contains("assem1"))
                .Where(e => e.ToString() == "assem1")
                .OfType<ExpressionSyntax>()
                .Single();
            var typeInfo1 = model.GetTypeInfo(assem1);
            Assert.NotEqual(TypeKind.Error, typeInfo1.Type.TypeKind);
            Assert.Equal("Assembly", typeInfo1.Type.Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetQueryClauseInfo()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
    }
}";
            var speculatedSource = @"
        C r1 =
            from int x in c1
            from int y in c2
            select x + y;
";
            var queryStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateStandardCompilation(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[1].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);
            var q = (QueryExpressionSyntax)queryStatement.Declaration.Variables[0].Initializer.Value;

            var info0 = speculativeModel.GetQueryClauseInfo(q.FromClause);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("x", speculativeModel.GetDeclaredSymbol(q.FromClause).Name);

            var info1 = speculativeModel.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal("y", speculativeModel.GetDeclaredSymbol(q.Body.Clauses[0]).Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetSemanticInfoForSelectClause()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
    }
}";
            var speculatedSource = @"
        C r1 =
            from int x in c1
            select x;
";

            var queryStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateStandardCompilation(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[1].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);
            var q = (QueryExpressionSyntax)queryStatement.Declaration.Variables[0].Initializer.Value;

            var x = speculativeModel.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);

            var selectExpression = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression;
            Assert.Equal(x, speculativeModel.GetSemanticInfoSummary(selectExpression).Symbol);

            var selectClauseSymbolInfo = speculativeModel.GetSymbolInfo(q.Body.SelectOrGroup);
            Assert.NotNull(selectClauseSymbolInfo.Symbol);
            Assert.Equal("Select", selectClauseSymbolInfo.Symbol.Name);

            var selectClauseTypeInfo = speculativeModel.GetTypeInfo(q.Body.SelectOrGroup);
            Assert.NotNull(selectClauseTypeInfo.Type);
            Assert.Equal("List1", selectClauseTypeInfo.Type.Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetDeclaredSymbolForJoinIntoClause()
        {
            string sourceCode = @"
public class Test
{
    public static void Main()
    { 
    }
}";

            var speculatedSource = @"
                  var qie = from x3 in new int[] { 0 }
                            join x7 in (new int[] { 1 }) on 5 equals 5 into x8
                            select x8;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Test").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.SpanStart, queryStatement, out speculativeModel);

            var queryExpression = (QueryExpressionSyntax)((LocalDeclarationStatementSyntax)queryStatement).Declaration.Variables[0].Initializer.Value;
            JoinIntoClauseSyntax joinInto = ((JoinClauseSyntax)queryExpression.Body.Clauses[0]).Into;
            var symbol = speculativeModel.GetDeclaredSymbol(joinInto);

            Assert.NotNull(symbol);
            Assert.Equal("x8", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
            Assert.Equal("? x8", symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetDeclaredSymbolForQueryContinuation()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
    }
}";
            var speculatedSource = @"
                var q2 = from x in nums
                         select x into w
                         select w;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Test2").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[0].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);

            var queryExpression = (QueryExpressionSyntax)((LocalDeclarationStatementSyntax)queryStatement).Declaration.Variables[0].Initializer.Value;
            var queryContinuation = queryExpression.Body.Continuation;
            var symbol = speculativeModel.GetDeclaredSymbol(queryContinuation);

            Assert.NotNull(symbol);
            Assert.Equal("w", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetSymbolInfoForOrderingClauses()
        {
            string sourceCode = @"
using System.Linq; // Needed for speculative code.

public class QueryExpressionTest
{
    public static void Main()
    {
    }
}";
            var speculatedSource = @"
        var q1 =
            from x in new int[] { 4, 5 }
            orderby
                x descending,
                x.ToString() ascending,
                x descending
            select x;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Linq; // Needed for speculative code.
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "QueryExpressionTest").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.SpanStart, queryStatement, out speculativeModel);
            Assert.True(success);

            int count = 0;
            string[] names = { "OrderByDescending", "ThenBy", "ThenByDescending" };
            foreach (var ordering in queryStatement.DescendantNodes().OfType<OrderingSyntax>())
            {
                var symbolInfo = speculativeModel.GetSemanticInfoSummary(ordering);
                Assert.Equal(names[count++], symbolInfo.Symbol.Name);
            }
            Assert.Equal(3, count);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void BrokenQueryPattern()
        {
            string source = @"
using System;

class Q<T>
{
    public Q<V> SelectMany<U, V>(Func<T, U> f1, Func<T, U, V> f2) { return null; }
    public Q<U> Select<U>(Func<T, U> f1) { return null; }

    //public Q<T> Where(Func<T, bool> f1) { return null; }
    public X Where(Func<T, bool> f1) { return null; }
}

class X
{
    public X Select<U>(Func<int, U> f1) { return null; }
}

class Program
{
    static void Main(string[] args)
    {
        Q<int> q = null;
        var r =
            /*<bind>*/from x in q
            from y in q
            where x.ToString() == y.ToString()
            select x.ToString()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: X, IsInvalid) (Syntax: 'from x in q ... .ToString()')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: X, IsInvalid, IsImplicit) (Syntax: 'select x.ToString()')
      Children(2):
          IInvocationExpression ( X Q<<anonymous type: System.Int32 x, Q<System.Int32> y>>.Where(System.Func<<anonymous type: System.Int32 x, Q<System.Int32> y>, System.Boolean> f1)) (OperationKind.InvocationExpression, Type: X, IsImplicit) (Syntax: 'where x.ToS ... .ToString()')
            Instance Receiver: 
              IInvocationExpression ( Q<<anonymous type: System.Int32 x, Q<System.Int32> y>> Q<System.Int32>.SelectMany<Q<System.Int32>, <anonymous type: System.Int32 x, Q<System.Int32> y>>(System.Func<System.Int32, Q<System.Int32>> f1, System.Func<System.Int32, Q<System.Int32>, <anonymous type: System.Int32 x, Q<System.Int32> y>> f2)) (OperationKind.InvocationExpression, Type: Q<<anonymous type: System.Int32 x, Q<System.Int32> y>>, IsImplicit) (Syntax: 'from y in q')
                Instance Receiver: 
                  ILocalReferenceExpression: q (OperationKind.LocalReferenceExpression, Type: Q<System.Int32>) (Syntax: 'q')
                Arguments(2):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: f1) (OperationKind.Argument, IsImplicit) (Syntax: 'q')
                      IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, Q<System.Int32>>, IsImplicit) (Syntax: 'q')
                        Target: 
                          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'q')
                            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'q')
                              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'q')
                                ReturnedValue: 
                                  ILocalReferenceExpression: q (OperationKind.LocalReferenceExpression, Type: Q<System.Int32>) (Syntax: 'q')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgument (ArgumentKind.Explicit, Matching Parameter: f2) (OperationKind.Argument, IsImplicit) (Syntax: 'from y in q')
                      IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, Q<System.Int32>, <anonymous type: System.Int32 x, Q<System.Int32> y>>, IsImplicit) (Syntax: 'from y in q')
                        Target: 
                          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'from y in q')
                            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'from y in q')
                              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'from y in q')
                                ReturnedValue: 
                                  IObjectCreationExpression (Constructor: <anonymous type: System.Int32 x, Q<System.Int32> y>..ctor(System.Int32 x, Q<System.Int32> y)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Int32 x, Q<System.Int32> y>, IsImplicit) (Syntax: 'from y in q')
                                    Arguments(2):
                                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsImplicit) (Syntax: 'from y in q')
                                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsImplicit) (Syntax: 'from y in q')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, IsImplicit) (Syntax: 'from y in q')
                                          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: Q<System.Int32>, IsImplicit) (Syntax: 'from y in q')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Initializer: 
                                      null
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: f1) (OperationKind.Argument, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                  IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<<anonymous type: System.Int32 x, Q<System.Int32> y>, System.Boolean>, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                    Target: 
                      IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                          IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                            ReturnedValue: 
                              IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x.ToString( ... .ToString()')
                                Left: 
                                  IInvocationExpression (virtual System.String System.Int32.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'x.ToString()')
                                    Instance Receiver: 
                                      IOperation:  (OperationKind.None) (Syntax: 'x')
                                    Arguments(0)
                                Right: 
                                  IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'y.ToString()')
                                    Instance Receiver: 
                                      IOperation:  (OperationKind.None) (Syntax: 'y')
                                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid, IsImplicit) (Syntax: 'x.ToString()')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, IsImplicit) (Syntax: 'x.ToString()')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: 'x.ToString()')
                ReturnedValue: 
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x.ToString()')
                    Children(1):
                        IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x.ToString')
                          Children(1):
                              IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8016: Transparent identifier member access failed for field 'x' of 'int'.  Does the data being queried implement the query pattern?
                //             select x.ToString()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsupportedTransparentIdentifierAccess, "x").WithArguments("x", "int").WithLocation(27, 20)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void Bug204561_01()
        {
            string sourceCode =
@"
class C
{
    public static void Main()
    {
        var x01 = from a in Test select a + 1;
    }
}

public class Test
{
}

public static class TestExtensions
{
    public static Test Select<T>(this Test x, System.Func<int, T> selector)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
                
            compilation.VerifyDiagnostics(
                // (6,34): error CS1936: Could not find an implementation of the query pattern for source type 'Test'.  'Select' not found.
                //         var x01 = from a in Test select a + 1;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "select a + 1").WithArguments("Test", "Select").WithLocation(6, 34)
                );
        }

        [Fact]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void Bug204561_02()
        {
            string sourceCode =
@"
class C
{
    public static void Main()
    {
        var y02 = from a in Test select a + 1;
        var x02 = from a in Test where a > 0 select a + 1;
    }
}

class Test
{
    public static Test Select<T>(System.Func<int, T> selector)
    {
        return null;
    }
}

static class TestExtensions
{
    public static Test Where(this Test x, System.Func<int, bool> filter)
    {
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);

            compilation.VerifyDiagnostics(
                // (7,34): error CS1936: Could not find an implementation of the query pattern for source type 'Test'.  'Where' not found.
                //         var x02 = from a in Test where a > 0 select a + 1;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "where a > 0").WithArguments("Test", "Where").WithLocation(7, 34),
                // (7,46): error CS0176: Member 'Test.Select<int>(Func<int, int>)' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var x02 = from a in Test where a > 0 select a + 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "select a + 1").WithArguments("Test.Select<int>(System.Func<int, int>)").WithLocation(7, 46)
                );
        }
        
        [Fact]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void Bug204561_03()
        {
            string sourceCode =
@"
class C
{
    public static void Main()
    {
        var y03 = from a in Test select a + 1;
        var x03 = from a in Test where a > 0 select a + 1;
    }
}

class Test
{
}

static class TestExtensions
{
    public static Test Select<T>(this Test x, System.Func<int, T> selector)
    {
        return null;
    }

    public static Test Where(this Test x, System.Func<int, bool> filter)
    {
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);

            compilation.VerifyDiagnostics(
                // (6,34): error CS1936: Could not find an implementation of the query pattern for source type 'Test'.  'Select' not found.
                //         var y03 = from a in Test select a + 1;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "select a + 1").WithArguments("Test", "Select").WithLocation(6, 34),
                // (7,34): error CS1936: Could not find an implementation of the query pattern for source type 'Test'.  'Where' not found.
                //         var x03 = from a in Test where a > 0 select a + 1;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "where a > 0").WithArguments("Test", "Where").WithLocation(7, 34)
                );
        }

        [Fact]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void Bug204561_04()
        {
            string sourceCode =
@"
class C
{
    public static void Main()
    {
        var x04 = from a in Test select a + 1;
    }
}

class Test
{
    public static Test Select<T>(System.Func<int, T> selector)
    {
        System.Console.WriteLine(""Select"");
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: "Select");
        }

        [WorkItem(15910, "https://github.com/dotnet/roslyn/issues/15910")]
        [Fact]
        public void ExpressionVariablesInQueryClause_01()
        {
            var csSource = @"
using System.Linq;

class Program
{
    public static void Main(string[] args)
    {
        var a = new[] { 1, 2, 3, 4 };
        var za = from x in M(a, out var q1) select x; // ok
        var zc = from x in a from y in M(a, out var z) select x; // error 1
        var zd = from x in a from int y in M(a, out var z) select x; // error 2
        var ze = from x in a from y in M(a, out var z) where true select x; // error 3
        var zf = from x in a from int y in M(a, out var z) where true select x; // error 4
        var zg = from x in a let y = M(a, out var z) select x; // error 5
        var zh = from x in a where M(x, out var z) == 1 select x; // error 6
        var zi = from x in a join y in M(a, out var q2) on x equals y select x; // ok
        var zj = from x in a join y in a on M(x, out var z) equals y select x; // error 7
        var zk = from x in a join y in a on x equals M(y, out var z) select x; // error 8
        var zl = from x in a orderby M(x, out var z) select x; // error 9
        var zm = from x in a orderby x, M(x, out var z) select x; // error 10
        var zn = from x in a group M(x, out var z) by x; // error 11
        var zo = from x in a group x by M(x, out var z); // error 12
    }
    public static T M<T>(T x, out T z) => z = x;
}";
            CreateCompilationWithMscorlibAndSystemCore(csSource).VerifyDiagnostics(
                // (10,53): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zc = from x in a from y in M(a, out var z) select x; // error 1
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(10, 53),
                // (11,57): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zd = from x in a from int y in M(a, out var z) select x; // error 2
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(11, 57),
                // (12,53): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var ze = from x in a from y in M(a, out var z) where true select x; // error 3
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(12, 53),
                // (13,57): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zf = from x in a from int y in M(a, out var z) where true select x; // error 4
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(13, 57),
                // (14,51): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zg = from x in a let y = M(a, out var z) select x; // error 5
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(14, 51),
                // (15,49): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zh = from x in a where M(x, out var z) == 1 select x; // error 6
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(15, 49),
                // (17,58): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zj = from x in a join y in a on M(x, out var z) equals y select x; // error 7
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(17, 58),
                // (18,67): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zk = from x in a join y in a on x equals M(y, out var z) select x; // error 8
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(18, 67),
                // (19,51): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zl = from x in a orderby M(x, out var z) select x; // error 9
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(19, 51),
                // (20,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zm = from x in a orderby x, M(x, out var z) select x; // error 10
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(20, 54),
                // (21,49): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zn = from x in a group M(x, out var z) by x; // error 11
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(21, 49),
                // (22,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zo = from x in a group x by M(x, out var z); // error 12
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(22, 54)
                );
        }

        [WorkItem(15910, "https://github.com/dotnet/roslyn/issues/15910")]
        [Fact]
        public void ExpressionVariablesInQueryClause_02()
        {
            var csSource = @"
using System.Linq;

class Program
{
    public static void Main(string[] args)
    {
        var a = new[] { 1, 2, 3, 4 };
        var za = from x in M(a, a is var q1) select x; // ok
        var zc = from x in a from y in M(a, a is var z) select x; // error 1
        var zd = from x in a from int y in M(a, a is var z) select x; // error 2
        var ze = from x in a from y in M(a, a is var z) where true select x; // error 3
        var zf = from x in a from int y in M(a, a is var z) where true select x; // error 4
        var zg = from x in a let y = M(a, a is var z) select x; // error 5
        var zh = from x in a where M(x, x is var z) == 1 select x; // error 6
        var zi = from x in a join y in M(a, a is var q2) on x equals y select x; // ok
        var zj = from x in a join y in a on M(x, x is var z) equals y select x; // error 7
        var zk = from x in a join y in a on x equals M(y, y is var z) select x; // error 8
        var zl = from x in a orderby M(x, x is var z) select x; // error 9
        var zm = from x in a orderby x, M(x, x is var z) select x; // error 10
        var zn = from x in a group M(x, x is var z) by x; // error 11
        var zo = from x in a group x by M(x, x is var z); // error 12
    }
    public static T M<T>(T x, bool b) => x;
}";
            CreateCompilationWithMscorlibAndSystemCore(csSource).VerifyDiagnostics(
                // (10,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zc = from x in a from y in M(a, a is var z) select x; // error 1
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(10, 54),
                // (11,58): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zd = from x in a from int y in M(a, a is var z) select x; // error 2
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(11, 58),
                // (12,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var ze = from x in a from y in M(a, a is var z) where true select x; // error 3
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(12, 54),
                // (13,58): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zf = from x in a from int y in M(a, a is var z) where true select x; // error 4
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(13, 58),
                // (14,52): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zg = from x in a let y = M(a, a is var z) select x; // error 5
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(14, 52),
                // (15,50): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zh = from x in a where M(x, x is var z) == 1 select x; // error 6
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(15, 50),
                // (17,59): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zj = from x in a join y in a on M(x, x is var z) equals y select x; // error 7
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(17, 59),
                // (18,68): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zk = from x in a join y in a on x equals M(y, y is var z) select x; // error 8
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(18, 68),
                // (19,52): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zl = from x in a orderby M(x, x is var z) select x; // error 9
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(19, 52),
                // (20,55): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zm = from x in a orderby x, M(x, x is var z) select x; // error 10
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(20, 55),
                // (21,50): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zn = from x in a group M(x, x is var z) by x; // error 11
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(21, 50),
                // (22,55): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zo = from x in a group x by M(x, x is var z); // error 12
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(22, 55)
                );
        }

        [WorkItem(15910, "https://github.com/dotnet/roslyn/issues/15910")]
        [Fact]
        public void ExpressionVariablesInQueryClause_03()
        {
            var csSource = @"
using System.Linq;

class Program
{
    public static void Main(string[] args)
    {
        var a = new[] { (1, 2), (3, 4) };
        var za = from x in M(a, (int qa, int wa) = a[0]) select x; // scoping ok
        var zc = from x in a from y in M(a, (int z, int w) = x) select x; // error 1
        var zd = from x in a from int y in M(a, (int z, int w) = x) select x; // error 2
        var ze = from x in a from y in M(a, (int z, int w) = x) where true select x; // error 3
        var zf = from x in a from int y in M(a, (int z, int w) = x) where true select x; // error 4
        var zg = from x in a let y = M(x, (int z, int w) = x) select x; // error 5
        var zh = from x in a where M(x, (int z, int w) = x).Item1 == 1 select x; // error 6
        var zi = from x in a join y in M(a, (int qi, int wi) = a[0]) on x equals y select x; // scoping ok
        var zj = from x in a join y in a on M(x, (int z, int w) = x) equals y select x; // error 7
        var zk = from x in a join y in a on x equals M(y, (int z, int w) = y) select x; // error 8
        var zl = from x in a orderby M(x, (int z, int w) = x) select x; // error 9
        var zm = from x in a orderby x, M(x, (int z, int w) = x) select x; // error 10
        var zn = from x in a group M(x, (int z, int w) = x) by x; // error 11
        var zo = from x in a group x by M(x, (int z, int w) = x); // error 12
    }
    public static T M<T>(T x, (int, int) z) => x;
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
";
            CreateCompilationWithMscorlibAndSystemCore(csSource)
                .GetDiagnostics()
                .Where(d => d.Code != (int)ErrorCode.ERR_DeclarationExpressionNotPermitted)
                .Verify(
                // (10,50): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zc = from x in a from y in M(a, (int z, int w) = x) select x; // error 1
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(10, 50),
                // (11,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zd = from x in a from int y in M(a, (int z, int w) = x) select x; // error 2
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(11, 54),
                // (12,50): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var ze = from x in a from y in M(a, (int z, int w) = x) where true select x; // error 3
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(12, 50),
                // (13,54): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zf = from x in a from int y in M(a, (int z, int w) = x) where true select x; // error 4
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(13, 54),
                // (14,48): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zg = from x in a let y = M(x, (int z, int w) = x) select x; // error 5
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(14, 48),
                // (15,46): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zh = from x in a where M(x, (int z, int w) = x).Item1 == 1 select x; // error 6
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(15, 46),
                // (17,55): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zj = from x in a join y in a on M(x, (int z, int w) = x) equals y select x; // error 7
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(17, 55),
                // (18,64): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zk = from x in a join y in a on x equals M(y, (int z, int w) = y) select x; // error 8
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(18, 64),
                // (19,48): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zl = from x in a orderby M(x, (int z, int w) = x) select x; // error 9
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(19, 48),
                // (20,51): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zm = from x in a orderby x, M(x, (int z, int w) = x) select x; // error 10
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(20, 51),
                // (21,46): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zn = from x in a group M(x, (int z, int w) = x) by x; // error 11
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(21, 46),
                // (22,51): error CS8201: Out variable and pattern variable declarations are not allowed within a query clause.
                //         var zo = from x in a group x by M(x, (int z, int w) = x); // error 12
                Diagnostic(ErrorCode.ERR_ExpressionVariableInQueryClause, "z").WithLocation(22, 51)
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(14689, "https://github.com/dotnet/roslyn/issues/14689")]
        public void SelectFromNamespaceShouldGiveAnError()
        {
            string source = @"
using System.Linq;
using NSAlias = ParentNamespace.ConsoleApp;

namespace ParentNamespace
{
    namespace ConsoleApp
    {
        class Program
        {
            static void Main()
            {
                var x = from c in ConsoleApp select 3;
                var y = from c in ParentNamespace.ConsoleApp select 3;
                var z = /*<bind>*/from c in NSAlias select 3/*</bind>*/;
            }
        }
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: ?, IsInvalid) (Syntax: 'from c in N ... as select 3')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsImplicit) (Syntax: 'select 3')
      Children(2):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'from c in NSAlias')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'NSAlias')
          IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: '3')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: '3')
              IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: '3')
                ReturnedValue: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0119: 'ConsoleApp' is a namespace, which is not valid in the given context
                //                 var x = from c in ConsoleApp select 3;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleApp").WithArguments("ConsoleApp", "namespace").WithLocation(13, 35),
                // CS0119: 'ParentNamespace.ConsoleApp' is a namespace, which is not valid in the given context
                //                 var y = from c in ParentNamespace.ConsoleApp select 3;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ParentNamespace.ConsoleApp").WithArguments("ParentNamespace.ConsoleApp", "namespace").WithLocation(14, 35),
                // CS0119: 'NSAlias' is a namespace, which is not valid in the given context
                //                 var z = /*<bind>*/from c in NSAlias select 3/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "NSAlias").WithArguments("NSAlias", "namespace").WithLocation(15, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(12052, "https://github.com/dotnet/roslyn/issues/12052")]
        public void LambdaParameterConflictsWithRangeVariable()
        {
            string source = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var res = /*<bind>*/from a in new[] { 1 }
                  select (Func<int, int>)(a => 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Func<System.Int32, System.Int32>>, IsInvalid) (Syntax: 'from a in n ... t>)(a => 1)')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Func<System.Int32, System.Int32>> System.Linq.Enumerable.Select<System.Int32, System.Func<System.Int32, System.Int32>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Func<System.Int32, System.Int32>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Func<System.Int32, System.Int32>>, IsInvalid, IsImplicit) (Syntax: 'select (Fun ... t>)(a => 1)')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from a in new[] { 1 }')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from a in new[] { 1 }')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { 1 }')
                  Dimension Sizes(1):
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { 1 }')
                  Initializer: 
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1 }')
                      Element Values(1):
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsInvalid, IsImplicit) (Syntax: '(Func<int, int>)(a => 1)')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Func<System.Int32, System.Int32>>, IsInvalid, IsImplicit) (Syntax: '(Func<int, int>)(a => 1)')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid, IsImplicit) (Syntax: '(Func<int, int>)(a => 1)')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, IsImplicit) (Syntax: '(Func<int, int>)(a => 1)')
                    IReturnStatement (OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: '(Func<int, int>)(a => 1)')
                      ReturnedValue: 
                        IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsInvalid) (Syntax: '(Func<int, int>)(a => 1)')
                          Target: 
                            IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'a => 1')
                              IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: '1')
                                IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: '1')
                                  ReturnedValue: 
                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0136: A local or parameter named 'a' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select (Func<int, int>)(a => 1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a").WithArguments("a").WithLocation(10, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void IOperationForQueryClause()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
        var r = /*<bind>*/from i in c select i + 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from i in c select i + 1')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i + 1')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i + 1')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i + 1')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i + 1')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i + 1')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i + 1')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void IOperationForRangeVariableDefinition()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
        var r = /*<bind>*/from i in c select i + 1/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from i in c select i + 1')
  Expression: 
    IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select i + 1')
      Instance Receiver: 
        null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, IsImplicit) (Syntax: 'from i in c')
            IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from i in c')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'c')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'i + 1')
            IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: 'i + 1')
              Target: 
                IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'i + 1')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'i + 1')
                    IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'i + 1')
                      ReturnedValue: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                          Left: 
                            IOperation:  (OperationKind.None) (Syntax: 'i')
                          Right: 
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")]
        public void IOperationForRangeVariableReference()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
        var r = from i in c select /*<bind>*/i/*</bind>*/ + 1;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(21484, "https://github.com/dotnet/roslyn/issues/21484")]
        public void QueryOnTypeExpression()
        {
            var code = @"
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void M<T>() where T : IEnumerable
    {
        var query1 = from object a in IEnumerable select 1;
        var query2 = from b in IEnumerable select 2;

        var query3 = from int c in IEnumerable<int> select 3;
        var query4 = from d in IEnumerable<int> select 4;

        var query5 = from object d in T select 5;
        var query6 = from d in T select 6;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(code);
            comp.VerifyDiagnostics(
                // (10,22): error CS0120: An object reference is required for the non-static field, method, or property 'Enumerable.Cast<object>(IEnumerable)'
                //         var query1 = from object a in IEnumerable select 1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "from object a in IEnumerable").WithArguments("System.Linq.Enumerable.Cast<object>(System.Collections.IEnumerable)").WithLocation(10, 22),
                // (11,32): error CS1934: Could not find an implementation of the query pattern for source type 'IEnumerable'.  'Select' not found.  Consider explicitly specifying the type of the range variable 'b'.
                //         var query2 = from b in IEnumerable select 2;
                Diagnostic(ErrorCode.ERR_QueryNoProviderCastable, "IEnumerable").WithArguments("System.Collections.IEnumerable", "Select", "b").WithLocation(11, 32),
                // (13,22): error CS0120: An object reference is required for the non-static field, method, or property 'Enumerable.Cast<int>(IEnumerable)'
                //         var query3 = from int c in IEnumerable<int> select 3;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "from int c in IEnumerable<int>").WithArguments("System.Linq.Enumerable.Cast<int>(System.Collections.IEnumerable)").WithLocation(13, 22),
                // (14,49): error CS1936: Could not find an implementation of the query pattern for source type 'IEnumerable<int>'.  'Select' not found.
                //         var query4 = from d in IEnumerable<int> select 4;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "select 4").WithArguments("System.Collections.Generic.IEnumerable<int>", "Select").WithLocation(14, 49),
                // (16,22): error CS0120: An object reference is required for the non-static field, method, or property 'Enumerable.Cast<object>(IEnumerable)'
                //         var query5 = from object d in T select 5;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "from object d in T").WithArguments("System.Linq.Enumerable.Cast<object>(System.Collections.IEnumerable)").WithLocation(16, 22),
                // (17,32): error CS1936: Could not find an implementation of the query pattern for source type 'T'.  'Select' not found.
                //         var query6 = from d in T select 6;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "T").WithArguments("T", "Select").WithLocation(17, 32)
                );
        }
    }
}
