// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class AnonymousTypesSemanticsTests : CompilingTestBase
    {
        [Fact()]
        public void AnonymousTypeSymbols_Simple()
        {
            var source = @"
public class ClassA
{
    public struct SSS
    {
    }

    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = 1,
            [# BB  #] = """",
            [# CCC #] = new SSS()
        } #];

        object v2 = [# new
        {
            [# aa  #] = new SSS(),
            [# BB  #] = 123.456,
            [# CCC #] = [# new
            {
                (new ClassA()).[# aa  #],
                ClassA.[# BB  #],
                ClassA.[# CCC #]
            } #]
        } #];

        object v3 = [# new {} #];
        var v4 = [# new {} #];
    }

    public int aa
    {
        get { return 123; }
    }

    public const string BB = ""-=-=-"";

    public static SSS CCC = new SSS();
}";
            var data = Compile(source, 14);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6, 7);

            var info2 = GetAnonymousTypeInfoSummary(data, 8,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 5).Span,
                            9, 10, 11);

            Assert.Equal("AnonymousTypePublicSymbol", info0.Type.GetType().Name);
            Assert.False(((INamedTypeSymbol)info0.Type).IsSerializable);

            Assert.Equal(info0.Type, info2.Type);
            Assert.NotEqual(info0.Type, info1.Type);

            var info3 = GetAnonymousTypeInfoSummary(data, 12,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 7).Span);
            var info4 = GetAnonymousTypeInfoSummary(data, 13,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 8).Span);
            Assert.Equal(info3.Type, info4.Type);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_Simple_OperationTree()
        {
            string source = @"
class ClassA
{
    public struct SSS
    {
    }

    public static void Test1(int x)
    /*<bind>*/{
        object v1 = new
        {
            aa = 1,
            BB = """",
            CCC = new SSS()
        };

        object v2 = new
        {
            aa = new SSS(),
            BB = 123.456,
            CCC = new
            {
                (new ClassA()).aa,
                ClassA.BB,
                ClassA.CCC
            }
        };

        object v3 = new { };
        var v4 = new { };
    }/*</bind>*/

    public int aa
    {
        get { return 123; }
    }

    public const string BB = ""-=-= -"";

    public static SSS CCC = new SSS();
}
";
            string expectedOperationTree = @"
IBlockOperation (4 statements, 4 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Object v1
    Local_2: System.Object v2
    Local_3: System.Object v3
    Local_4: <empty anonymous type> v4
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object v1 = ... };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object v1 = ... }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object v1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1 = new ... }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new ... }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new ... }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>) (Syntax: 'new ... }')
                      Initializers(3):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: 'aa = 1')
                            Left: 
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>.aa { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'aa')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: """") (Syntax: 'BB = """"')
                            Left: 
                              IPropertyReferenceOperation: System.String <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>.BB { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'BB')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ClassA.SSS) (Syntax: 'CCC = new SSS()')
                            Left: 
                              IPropertyReferenceOperation: ClassA.SSS <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>.CCC { get; } (OperationKind.PropertyReference, Type: ClassA.SSS) (Syntax: 'CCC')
                                Instance Receiver: 
                                  null
                            Right: 
                              IObjectCreationOperation (Constructor: ClassA.SSS..ctor()) (OperationKind.ObjectCreation, Type: ClassA.SSS) (Syntax: 'new SSS()')
                                Arguments(0)
                                Initializer: 
                                  null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object v2 = ... };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object v2 = ... }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object v2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v2 = new ... }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new ... }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new ... }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: ClassA.SSS aa, System.Double BB, <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC> CCC>) (Syntax: 'new ... }')
                      Initializers(3):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ClassA.SSS) (Syntax: 'aa = new SSS()')
                            Left: 
                              IPropertyReferenceOperation: ClassA.SSS <anonymous type: ClassA.SSS aa, System.Double BB, <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC> CCC>.aa { get; } (OperationKind.PropertyReference, Type: ClassA.SSS) (Syntax: 'aa')
                                Instance Receiver: 
                                  null
                            Right: 
                              IObjectCreationOperation (Constructor: ClassA.SSS..ctor()) (OperationKind.ObjectCreation, Type: ClassA.SSS) (Syntax: 'new SSS()')
                                Arguments(0)
                                Initializer: 
                                  null
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Double, Constant: 123.456) (Syntax: 'BB = 123.456')
                            Left: 
                              IPropertyReferenceOperation: System.Double <anonymous type: ClassA.SSS aa, System.Double BB, <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC> CCC>.BB { get; } (OperationKind.PropertyReference, Type: System.Double) (Syntax: 'BB')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 123.456) (Syntax: '123.456')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>) (Syntax: 'CCC = new ... }')
                            Left: 
                              IPropertyReferenceOperation: <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC> <anonymous type: ClassA.SSS aa, System.Double BB, <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC> CCC>.CCC { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>) (Syntax: 'CCC')
                                Instance Receiver: 
                                  null
                            Right: 
                              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 aa, System.String BB, ClassA.SSS CCC>) (Syntax: 'new ... }')
                                Initializers(3):
                                    IPropertyReferenceOperation: System.Int32 ClassA.aa { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '(new ClassA()).aa')
                                      Instance Receiver: 
                                        IObjectCreationOperation (Constructor: ClassA..ctor()) (OperationKind.ObjectCreation, Type: ClassA) (Syntax: 'new ClassA()')
                                          Arguments(0)
                                          Initializer: 
                                            null
                                    IFieldReferenceOperation: System.String ClassA.BB (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""-=-= -"") (Syntax: 'ClassA.BB')
                                      Instance Receiver: 
                                        null
                                    IFieldReferenceOperation: ClassA.SSS ClassA.CCC (Static) (OperationKind.FieldReference, Type: ClassA.SSS) (Syntax: 'ClassA.CCC')
                                      Instance Receiver: 
                                        null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object v3 = new { };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object v3 = new { }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object v3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v3 = new { }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new { }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
                      Initializers(0)
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var v4 = new { };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var v4 = new { }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: <empty anonymous type> v4) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v4 = new { }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new { }')
                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
                  Initializers(0)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_ContextualKeywordsInFields()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        object v1 = [# new
        {
            [# var #] = ""var"",
            [# get #] = new {},
            [# partial #] = [# new
                            {
                                (new ClassA()).[# select #],
                                [# global  #]
                            } #]
        } #];
    }

    public int select
    {
        get { return 123; }
    }

    public const string global = ""-=-=-"";
}";
            var data = Compile(source, 7);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6);

            Assert.Equal(
                "<anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>",
                info0.Type.ToTestDisplayString());

            Assert.Equal(
                "<anonymous type: System.Int32 select, System.String global>..ctor(System.Int32 select, System.String global)",
                info1.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_ContextualKeywordsInFields_OperationTree()
        {
            string source = @"
class ClassA
{
    static void Test1(int x)
    {
        object v1 = /*<bind>*/new
        {
            var = ""var"",
            get = new { },
            partial = new
            {
                (new ClassA()).select,
                global
            }
        }/*</bind>*/;
    }

    public int select
    {
        get { return 123; }
    }

    public const string global = "" -=-= -"";
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>) (Syntax: 'new ... }')
  Initializers(3):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: ""var"") (Syntax: 'var = ""var""')
        Left: 
          IPropertyReferenceOperation: System.String <anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>.var { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'var')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""var"") (Syntax: '""var""')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <empty anonymous type>) (Syntax: 'get = new { }')
        Left: 
          IPropertyReferenceOperation: <empty anonymous type> <anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>.get { get; } (OperationKind.PropertyReference, Type: <empty anonymous type>) (Syntax: 'get')
            Instance Receiver: 
              null
        Right: 
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
            Initializers(0)
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 select, System.String global>) (Syntax: 'partial = n ... }')
        Left: 
          IPropertyReferenceOperation: <anonymous type: System.Int32 select, System.String global> <anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>.partial { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 select, System.String global>) (Syntax: 'partial')
            Instance Receiver: 
              null
        Right: 
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 select, System.String global>) (Syntax: 'new ... }')
            Initializers(2):
                IPropertyReferenceOperation: System.Int32 ClassA.select { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '(new ClassA()).select')
                  Instance Receiver: 
                    IObjectCreationOperation (Constructor: ClassA..ctor()) (OperationKind.ObjectCreation, Type: ClassA) (Syntax: 'new ClassA()')
                      Arguments(0)
                      Initializer: 
                        null
                IFieldReferenceOperation: System.String ClassA.global (Static) (OperationKind.FieldReference, Type: System.String, Constant: "" -=-= -"") (Syntax: 'global')
                  Instance Receiver: 
                    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_DelegateMembers()
        {
            var source = @"
delegate bool D1();
class ClassA
{
    void Main()
    {
        var at1 = [# new { [# module #] = (D1)(() => false)} #].module();
    }
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            Assert.Equal("<anonymous type: D1 module>", info0.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: D1 module>..ctor(D1 module)", info0.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_DelegateMembers_OperationTree()
        {
            string source = @"
delegate bool D1();
class ClassA
{
    void Main()
    {
        var at1 = /*<bind>*/new { module = (D1)(() => false) }/*</bind>*/.module();
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: D1 module>) (Syntax: 'new { modul ... => false) }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: D1) (Syntax: 'module = (D ... ) => false)')
        Left: 
          IPropertyReferenceOperation: D1 <anonymous type: D1 module>.module { get; } (OperationKind.PropertyReference, Type: D1) (Syntax: 'module')
            Instance Receiver: 
              null
        Right: 
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: D1) (Syntax: '(D1)(() => false)')
            Target: 
              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => false')
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'false')
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'false')
                    ReturnedValue: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_BaseAccessInMembers()
        {
            var source = @"
delegate bool D1();
class ClassB
{
    protected System.Func<int, int> F = x => x;
}
class ClassA: ClassB
{
    void Main()
    {
        var at1 = [# [# new { base.[# F #] } #].F(1) #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 1,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            2);

            Assert.Equal("<anonymous type: System.Func<System.Int32, System.Int32> F>", info0.Type.ToTestDisplayString());

            var info1 = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            Assert.Equal("System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)", info1.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_BaseAccessInMembers_OperationTree()
        {
            string source = @"
delegate bool D1();
class ClassB
{
    protected System.Func<int, int> F = x => x;
}
class ClassA : ClassB
{
    void Main()
    {
        var at1 = /*<bind>*/new { base.F }/*</bind>*/.F(1);
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Func<System.Int32, System.Int32> F>) (Syntax: 'new { base.F }')
  Initializers(1):
      IFieldReferenceOperation: System.Func<System.Int32, System.Int32> ClassB.F (OperationKind.FieldReference, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'base.F')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: ClassB) (Syntax: 'base')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_InFieldInitializer()
        {
            var source = @"
class ClassA
{
    private static object F = [# new { [# F123 #] = typeof(ClassA) } #];
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            Assert.Equal("<anonymous type: System.Type F123>", info0.Type.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_InFieldInitializer_OperationTree()
        {
            string source = @"
class ClassA
{
    private static object F = /*<bind>*/new { F123 = typeof(ClassA) }/*</bind>*/;
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Type F123>) (Syntax: 'new { F123  ... f(ClassA) }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Type) (Syntax: 'F123 = typeof(ClassA)')
        Left: 
          IPropertyReferenceOperation: System.Type <anonymous type: System.Type F123>.F123 { get; } (OperationKind.PropertyReference, Type: System.Type) (Syntax: 'F123')
            Instance Receiver: 
              null
        Right: 
          ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'typeof(ClassA)')
            TypeOperand: ClassA
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_Equals()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        bool result = [# new { f1 = 1, f2 = """" }.Equals(new { }) #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.Equals(object)", method.ToDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_Equals_OperationTree()
        {
            string source = @"
class ClassA
{
    static void Test1(int x)
    {
        bool result = /*<bind>*/new { f1 = 1, f2 = """" }.Equals(new { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationOperation (virtual System.Boolean System.Object.Equals(System.Object obj)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'new { f1 =  ... ls(new { })')
  Instance Receiver: 
    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 f1, System.String f2>) (Syntax: 'new { f1 = 1, f2 = """" }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: 'f1 = 1')
            Left: 
              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 f1, System.String f2>.f1 { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'f1')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: """") (Syntax: 'f2 = """"')
            Left: 
              IPropertyReferenceOperation: System.String <anonymous type: System.Int32 f1, System.String f2>.f2 { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'f2')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null) (Syntax: 'new { }')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { }')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
              Initializers(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_ToString()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        string result = [# new { f1 = 1, f2 = """" }.ToString() #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.ToString()", method.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_GetHashCode()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        int result = [# new { f1 = 1, f2 = """" }.GetHashCode() #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.GetHashCode()", method.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_Ctor()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        var result = [# new { f1 = 1, f2 = """" } #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("<anonymous type: int f1, string f2>..ctor(int, string)", method.ToDisplayString());
            Assert.Equal("<anonymous type: System.Int32 f1, System.String f2>..ctor(System.Int32 f1, System.String f2)", method.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeTemplateCannotConstruct()
        {
            var source = @"
class ClassA
{
    object F = [# new { [# F123 #] = typeof(ClassA) } #];
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            var type = info0.Type;
            Assert.Equal("<anonymous type: System.Type F123>", type.ToTestDisplayString());
            Assert.True(type.IsDefinition);
            AssertCannotConstruct(type);
        }

        [Fact()]
        public void AnonymousTypeTemplateCannotConstruct_Empty()
        {
            var source = @"
class ClassA
{
    object F = [# new { } #];
}";
            var data = Compile(source, 1);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span);

            var type = info0.Type;
            Assert.Equal("<empty anonymous type>", type.ToTestDisplayString());
            Assert.True(type.IsDefinition);
            AssertCannotConstruct(type);
        }

        [Fact()]
        public void AnonymousTypeFieldDeclarationIdentifier()
        {
            var source = @"
class ClassA
{
    object F = new { [# F123 #] = typeof(ClassA) };
}";
            var data = Compile(source, 1);
            var info = data.Model.GetSymbolInfo((ExpressionSyntax)data.Nodes[0]);
            Assert.NotNull(info.Symbol);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("System.Type <anonymous type: System.Type F123>.F123 { get; }", info.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery()
        {
            var source = LINQ + @"
class ClassA
{
    void m()
    {
        var o = from x in new List1<int>(1, 2, 3) select [# new { [# x #], [# y #] = x } #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, NumberOfNewKeywords(LINQ) + 2).Span,
                1, 2);

            var info1 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[1]).Expression);
            Assert.NotNull(info1.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info1.Symbol.Kind);
            Assert.Equal("x", info1.Symbol.ToDisplayString());

            var info2 = data.Model.GetSymbolInfo((ExpressionSyntax)data.Nodes[2]);
            Assert.NotNull(info2.Symbol);
            Assert.Equal(SymbolKind.Property, info2.Symbol.Kind);
            Assert.Equal("System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.y { get; }", info2.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery_OperationTree()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class ClassA
{
    void m()
    {
        var o = from x in new List<int>() { 1, 2, 3 } select /*<bind>*/new { x, y = x }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, System.Int32 y>) (Syntax: 'new { x, y = x }')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.y { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: 
              null
        Right: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery2()
        {
            var source = LINQ + @"
class ClassA
{
    void m()
    {
        var o = from x in new List1<int>(1, 2, 3) let y = """" select [# new { [# x #], [# y #] } #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, NumberOfNewKeywords(LINQ) + 2).Span,
                1, 2);

            Assert.Equal("<anonymous type: System.Int32 x, System.String y>", info0.Type.ToTestDisplayString());

            var info1 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[1]).Expression);
            Assert.NotNull(info1.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info1.Symbol.Kind);
            Assert.Equal("x", info1.Symbol.ToDisplayString());

            var info2 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[2]).Expression);
            Assert.NotNull(info2.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info2.Symbol.Kind);
            Assert.Equal("y", info2.Symbol.ToDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery2_OperationTree()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class ClassA
{
    void m()
    {
        var o = from x in new List<int>() { 1, 2, 3 } let y = """" select /*<bind>*/new { x, y }/*</bind>*/;
    }
}
";
            // OperationKind.None is for Range variables, IOperation support for it is NYI.
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, System.String y>) (Syntax: 'new { x, y }')
  Initializers(2):
      IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.String y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
        Instance Receiver: 
          IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'x')
      IPropertyReferenceOperation: System.String <anonymous type: System.Int32 x, System.String y>.y { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'y')
        Instance Receiver: 
          IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda()
        {
            var source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)(() => ( [# new { [# x #] = 1, [# y #] = [# new { } #] } #]).ToString());;
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                1, 2);

            var info1 = GetAnonymousTypeInfoSummary(data, 3,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<anonymous type: System.Int32 x, <empty anonymous type> y>..ctor(System.Int32 x, <empty anonymous type> y)", info0.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda_OperationTree()
        {
            string source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)(() => (/*<bind>*/new { x = 1, y = new { } }/*</bind>*/).ToString()); ;
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, <empty anonymous type> y>) (Syntax: 'new { x = 1 ... = new { } }')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: 'x = 1')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, <empty anonymous type> y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <empty anonymous type>) (Syntax: 'y = new { }')
        Left: 
          IPropertyReferenceOperation: <empty anonymous type> <anonymous type: System.Int32 x, <empty anonymous type> y>.y { get; } (OperationKind.PropertyReference, Type: <empty anonymous type>) (Syntax: 'y')
            Instance Receiver: 
              null
        Right: 
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
            Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda2()
        {
            var source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)
                    (() =>
                        ((Func<string>) (() => ( [# new { [# x #] = 1, [# y #] = [# new { } #] } #]).ToString())
                    ).Invoke());
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                1, 2);

            var info1 = GetAnonymousTypeInfoSummary(data, 3,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<anonymous type: System.Int32 x, <empty anonymous type> y>..ctor(System.Int32 x, <empty anonymous type> y)", info0.Symbol.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda2_OperationTree()
        {
            string source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)
                    (() =>
                        ((Func<string>)(() => (/*<bind>*/new { x = 1, y = new { } }/*</bind>*/).ToString())
                    ).Invoke());
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, <empty anonymous type> y>) (Syntax: 'new { x = 1 ... = new { } }')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: 'x = 1')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, <empty anonymous type> y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <empty anonymous type>) (Syntax: 'y = new { }')
        Left: 
          IPropertyReferenceOperation: <empty anonymous type> <anonymous type: System.Int32 x, <empty anonymous type> y>.y { get; } (OperationKind.PropertyReference, Type: <empty anonymous type>) (Syntax: 'y')
            Instance Receiver: 
              null
        Right: 
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
            Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [ClrOnlyFact]
        public void AnonymousTypeSymbols_DontCrashIfNameIsQueriedBeforeEmit()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new { [# aa  #] = 1, [# BB  #] = 2 } #];
        object v2 = [# new { } #];
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2);

            CheckAnonymousType(info0.Type, "", "");

            info0 = GetAnonymousTypeInfoSummary(data, 3,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            CheckAnonymousType(info0.Type, "", "");

            //  perform emit
            CompileAndVerify(
                data.Compilation,
                symbolValidator: module => CheckAnonymousTypes(module)
            );
        }

        #region "AnonymousTypeSymbols_DontCrashIfNameIsQueriedBeforeEmit"

        private void CheckAnonymousType(ITypeSymbol type, string name, string metadataName)
        {
            Assert.NotNull(type);
            Assert.Equal(name, type.Name);
            Assert.Equal(metadataName, type.MetadataName);
        }

        private void CheckAnonymousTypes(ModuleSymbol module)
        {
            var ns = module.GlobalNamespace;
            Assert.NotNull(ns);

            CheckAnonymousType(ns.GetMember<NamedTypeSymbol>("<>f__AnonymousType0"), "<>f__AnonymousType0", "<>f__AnonymousType0`2");
            CheckAnonymousType(ns.GetMember<NamedTypeSymbol>("<>f__AnonymousType1"), "<>f__AnonymousType1", "<>f__AnonymousType1");
        }

        #endregion

        [Fact()]
        public void AnonymousTypeSymbols_Error_Simple()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = xyz,
            [# BB  #] = """",
            [# CCC #] = new SSS()
        } #];

        object v2 = [# new
        {
            [# aa  #] = new SSS(),
            [# BB  #] = 123.456,
            [# CCC #] = [# new
            {
                (new ClassA()).[# aa  #],
                ClassA.[# BB  #],
                ClassA.[# CCC #]
            } #]
        } #];
    }
}";
            var data = Compile(source, 12,
                // (8,25): error CS0103: The name 'xyz' does not exist in the current context
                //                aa     = xyz,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "xyz").WithArguments("xyz"),
                // (10,29): error CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //                CCC    = new SSS()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS"),
                // (15,29): error CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //                aa     = new SSS(),
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS"),
                // (19,35): error CS1061: 'ClassA' does not contain a definition for 'aa' and no extension method 'aa' accepting a first argument of type 'ClassA' could be found (are you missing a using directive or an assembly reference?)
                //                 (new ClassA()).   aa    ,
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "aa").WithArguments("ClassA", "aa"),
                // (20,27): error CS0117: 'ClassA' does not contain a definition for 'BB'
                //                 ClassA.   BB    ,
                Diagnostic(ErrorCode.ERR_NoSuchMember, "BB").WithArguments("ClassA", "BB"),
                // (21,27): error CS0117: 'ClassA' does not contain a definition for 'CCC'
                //                 ClassA.   CCC   
                Diagnostic(ErrorCode.ERR_NoSuchMember, "CCC").WithArguments("ClassA", "CCC")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6, 7);

            var info2 = GetAnonymousTypeInfoSummary(data, 8,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 5).Span,
                            9, 10, 11);

            Assert.Equal("<anonymous type: ? aa, System.String BB, SSS CCC>", info0.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>", info1.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: ? aa, ? BB, ? CCC>", info2.Type.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_Error_Simple_OperationTree()
        {
            string source = @"
class ClassA
{
    public static void Test1(int x)
    /*<bind>*/{
        object v1 = new
        {
            aa = xyz,
            BB = """",
            CCC = new SSS()
        };

        object v2 = new
        {
            aa = new SSS(),
            BB = 123.456,
            CCC = new
            {
                (new ClassA()).aa,
                ClassA.BB,
                ClassA.CCC
            }
        };
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (2 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: System.Object v1
    Local_2: System.Object v2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'object v1 = ... };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'object v1 = ... }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object v1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'v1 = new ... }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new ... }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new ... }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: ? aa, System.String BB, SSS CCC>, IsInvalid) (Syntax: 'new ... }')
                      Initializers(3):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'aa = xyz')
                            Left: 
                              IPropertyReferenceOperation: ? <anonymous type: ? aa, System.String BB, SSS CCC>.aa { get; } (OperationKind.PropertyReference, Type: ?) (Syntax: 'aa')
                                Instance Receiver: 
                                  null
                            Right: 
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'xyz')
                                Children(0)
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: """") (Syntax: 'BB = """"')
                            Left: 
                              IPropertyReferenceOperation: System.String <anonymous type: ? aa, System.String BB, SSS CCC>.BB { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'BB')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: SSS, IsInvalid) (Syntax: 'CCC = new SSS()')
                            Left: 
                              IPropertyReferenceOperation: SSS <anonymous type: ? aa, System.String BB, SSS CCC>.CCC { get; } (OperationKind.PropertyReference, Type: SSS) (Syntax: 'CCC')
                                Instance Receiver: 
                                  null
                            Right: 
                              IInvalidOperation (OperationKind.Invalid, Type: SSS, IsInvalid) (Syntax: 'new SSS()')
                                Children(0)
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'object v2 = ... };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'object v2 = ... }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object v2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'v2 = new ... }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new ... }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new ... }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>, IsInvalid) (Syntax: 'new ... }')
                      Initializers(3):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: SSS, IsInvalid) (Syntax: 'aa = new SSS()')
                            Left: 
                              IPropertyReferenceOperation: SSS <anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>.aa { get; } (OperationKind.PropertyReference, Type: SSS) (Syntax: 'aa')
                                Instance Receiver: 
                                  null
                            Right: 
                              IInvalidOperation (OperationKind.Invalid, Type: SSS, IsInvalid) (Syntax: 'new SSS()')
                                Children(0)
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Double, Constant: 123.456) (Syntax: 'BB = 123.456')
                            Left: 
                              IPropertyReferenceOperation: System.Double <anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>.BB { get; } (OperationKind.PropertyReference, Type: System.Double) (Syntax: 'BB')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 123.456) (Syntax: '123.456')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: ? aa, ? BB, ? CCC>, IsInvalid) (Syntax: 'CCC = new ... }')
                            Left: 
                              IPropertyReferenceOperation: <anonymous type: ? aa, ? BB, ? CCC> <anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>.CCC { get; } (OperationKind.PropertyReference, Type: <anonymous type: ? aa, ? BB, ? CCC>) (Syntax: 'CCC')
                                Instance Receiver: 
                                  null
                            Right: 
                              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: ? aa, ? BB, ? CCC>, IsInvalid) (Syntax: 'new ... }')
                                Initializers(3):
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '(new ClassA()).aa')
                                      Children(1):
                                          IObjectCreationOperation (Constructor: ClassA..ctor()) (OperationKind.ObjectCreation, Type: ClassA) (Syntax: 'new ClassA()')
                                            Arguments(0)
                                            Initializer: 
                                              null
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ClassA.BB')
                                      Children(1):
                                          IOperation:  (OperationKind.None, Type: null) (Syntax: 'ClassA')
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ClassA.CCC')
                                      Children(1):
                                          IOperation:  (OperationKind.None, Type: null) (Syntax: 'ClassA')
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'xyz' does not exist in the current context
                //             aa = xyz,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "xyz").WithArguments("xyz").WithLocation(8, 18),
                // CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //             CCC = new SSS()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS").WithLocation(10, 23),
                // CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //             aa = new SSS(),
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS").WithLocation(15, 22),
                // CS1061: 'ClassA' does not contain a definition for 'aa' and no extension method 'aa' accepting a first argument of type 'ClassA' could be found (are you missing a using directive or an assembly reference?)
                //                 (new ClassA()).aa,
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "aa").WithArguments("ClassA", "aa").WithLocation(19, 32),
                // CS0117: 'ClassA' does not contain a definition for 'BB'
                //                 ClassA.BB,
                Diagnostic(ErrorCode.ERR_NoSuchMember, "BB").WithArguments("ClassA", "BB").WithLocation(20, 24),
                // CS0117: 'ClassA' does not contain a definition for 'CCC'
                //                 ClassA.CCC
                Diagnostic(ErrorCode.ERR_NoSuchMember, "CCC").WithArguments("ClassA", "CCC").WithLocation(21, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_Error_InUsingStatement()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        using (var v1 = [# new { } #])
        {
        }
    }
}";
            var data = Compile(source, 1,
                // (6,16): error CS1674: '<empty anonymous type>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v1 =    new { }   )
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v1 =    new { }").WithArguments("<empty anonymous type>")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span);

            Assert.Equal("<empty anonymous type>", info0.Type.ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void AnonymousTypeSymbols_Error_InUsingStatement_OperationTree()
        {
            string source = @"
class ClassA
{
    public static void Test1(int x)
    {
        using (/*<bind>*/var v1 = new { }/*</bind>*/)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var v1 = new { }')
  Declarators:
      IVariableDeclaratorOperation (Symbol: <empty anonymous type> v1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'v1 = new { }')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new { }')
            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>, IsInvalid) (Syntax: 'new { }')
              Initializers(0)
  Initializer: 
    null";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1674: '<empty anonymous type>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (/*<bind>*/var v1 = new { }/*</bind>*/)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v1 = new { }").WithArguments("<empty anonymous type>").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_Error_DuplicateName()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = 1,
            ClassA.[# aa #],
            [# bb #] = 1.2
        } #];
    }

    public static string aa = ""-field-aa-"";
}";
            var data = Compile(source, 4,
                // (9,13): error CS0833: An anonymous type cannot have multiple properties with the same name
                //             ClassA.   aa   ,
                Diagnostic(ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, "ClassA.   aa")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, /*2,*/ 3);

            Assert.Equal("<anonymous type: System.Int32 aa, System.String $1, System.Double bb>", info0.Type.ToTestDisplayString());

            var properties = (from m in info0.Type.GetMembers() where m.Kind == SymbolKind.Property select m).ToArray();
            Assert.Equal(3, properties.Length);

            Assert.Equal("System.Int32 <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.aa { get; }", properties[0].ToTestDisplayString());
            Assert.Equal("System.String <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.$1 { get; }", properties[1].ToTestDisplayString());
            Assert.Equal("System.Double <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.bb { get; }", properties[2].ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20338")]
        public void AnonymousTypeSymbols_Error_DuplicateName_OperationTree()
        {
            string source = @"
class ClassA
{
    public static void Test1(int x)
    {
        object v1 = /*<bind>*/new
        {
            aa = 1,
            ClassA.aa,
            bb = 1.2
        }/*</bind>*/;
    }

    public static string aa = ""-field-aa-"";
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: System.Int32 aa, System.String $1, System.Double bb>, IsInvalid) (Syntax: 'new ... }')
  Initializers(3): ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: 'aa = 1')
      Left: IPropertyReferenceExpression: System.Int32 <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.aa { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'aa')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IFieldReferenceExpression: System.String ClassA.aa (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'ClassA.aa')
    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Double) (Syntax: 'ClassA.aa')
      Left: IPropertyReferenceExpression: System.Double <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.bb { get; } (OperationKind.PropertyReferenceExpression, Type: System.Double) (Syntax: 'bb')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1.2) (Syntax: '1.2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0833: An anonymous type cannot have multiple properties with the same name
                //             ClassA.aa,
                Diagnostic(ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, "ClassA.aa").WithLocation(9, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact()]
        public void AnonymousTypeSymbols_LookupSymbols()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = """",
            [# abc #] = 123.456
        } #];
        object v2 = [# new{ } #];
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2);

            Assert.Equal("<anonymous type: System.String aa, System.Double abc>", info0.Type.ToTestDisplayString());

            var pos = data.Nodes[0].Span.End;
            var syms = data.Model.LookupSymbols(pos, container: info0.Type).Select(x => x.ToTestDisplayString()).OrderBy(x => x).ToArray();
            Assert.Equal(8, syms.Length);

            int index = 0;
            Assert.Equal("System.Boolean System.Object.Equals(System.Object obj)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.Equals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Double <anonymous type: System.String aa, System.Double abc>.abc { get; }", syms[index++]);
            Assert.Equal("System.Int32 System.Object.GetHashCode()", syms[index++]);
            Assert.Equal("System.String <anonymous type: System.String aa, System.Double abc>.aa { get; }", syms[index++]);
            Assert.Equal("System.String System.Object.ToString()", syms[index++]);
            Assert.Equal("System.Type System.Object.GetType()", syms[index++]);

            info0 = GetAnonymousTypeInfoSummary(data, 3,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<empty anonymous type>", info0.Type.ToTestDisplayString());

            pos = data.Nodes[3].Span.End;
            syms = data.Model.LookupSymbols(pos, container: info0.Type).Select(x => x.ToTestDisplayString()).OrderBy(x => x).ToArray();
            Assert.Equal(6, syms.Length);

            index = 0;
            Assert.Equal("System.Boolean System.Object.Equals(System.Object obj)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.Equals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Int32 System.Object.GetHashCode()", syms[index++]);
            Assert.Equal("System.String System.Object.ToString()", syms[index++]);
            Assert.Equal("System.Type System.Object.GetType()", syms[index++]);
        }

        [WorkItem(543189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543189")]
        [Fact()]
        public void CheckAnonymousTypeAsConstValue()
        {
            var source = @"
public class A
{
    const int i = /*<bind>*/(new {a = 2}).a/*</bind>*/;
}";

            var comp = CreateCompilation(source);
            var tuple = GetBindingNodeAndModel<ExpressionSyntax>(comp);
            var info = tuple.Item2.GetSymbolInfo(tuple.Item1);
            Assert.NotNull(info.Symbol);
            Assert.Equal("<anonymous type: int a>.a", info.Symbol.ToDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact()]
        public void CheckAnonymousTypeAsConstValue_OperationTree()
        {
            string source = @"
class A
{
    const int i = (/*<bind>*/new { a = 2 }/*</bind>*/).a;
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a>, IsInvalid) (Syntax: 'new { a = 2 }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 2) (Syntax: 'a = 2')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0836: Cannot use anonymous type in a constant expression
                //     const int i = (/*<bind>*/new { a = 2 }/*</bind>*/).a;
                Diagnostic(ErrorCode.ERR_AnonymousTypeNotAvailable, "new").WithLocation(4, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Queryable()
        {
            CompileAndVerify(
 @"using System.Linq;

public class Product
{
    public int ProductID;
    public string ProductName;
    public int SupplierID;
}
public class DB
{
    public IQueryable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.ProductName, p.SupplierID } : new { p.ProductID, p.ProductName, p.SupplierID } }).ToList();
    }
}").VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Queryable_OperationTree()
        {
            string source = @"
using System.Linq;

class Product
{
    public int ProductID;
    public string ProductName;
    public int SupplierID;
}
class DB
{
    public IQueryable<Product> Products;
}

class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => /*<bind>*/new { Conditional = false ? new { p.ProductID, p.ProductName, p.SupplierID } : new { p.ProductID, p.ProductName, p.SupplierID } }/*</bind>*/).ToList();
    }
}
";
            string expectedOperationTree = @"IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID> Conditional>) (Syntax: 'new { Condi ... plierID } }')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID>) (Syntax: 'Conditional ... upplierID }')
        Left: 
          IPropertyReferenceOperation: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID> <anonymous type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID> Conditional>.Conditional { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID>) (Syntax: 'Conditional')
            Instance Receiver: 
              null
        Right: 
          IConditionalOperation (OperationKind.Conditional, Type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID>) (Syntax: 'false ? new ... upplierID }')
            Condition: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            WhenTrue: 
              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID>) (Syntax: 'new { p.Pro ... upplierID }')
                Initializers(3):
                    IFieldReferenceOperation: System.Int32 Product.ProductID (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'p.ProductID')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
                    IFieldReferenceOperation: System.String Product.ProductName (OperationKind.FieldReference, Type: System.String) (Syntax: 'p.ProductName')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
                    IFieldReferenceOperation: System.Int32 Product.SupplierID (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'p.SupplierID')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
            WhenFalse: 
              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 ProductID, System.String ProductName, System.Int32 SupplierID>) (Syntax: 'new { p.Pro ... upplierID }')
                Initializers(3):
                    IFieldReferenceOperation: System.Int32 Product.ProductID (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'p.ProductID')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
                    IFieldReferenceOperation: System.String Product.ProductName (OperationKind.FieldReference, Type: System.String) (Syntax: 'p.ProductName')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
                    IFieldReferenceOperation: System.Int32 Product.SupplierID (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'p.SupplierID')
                      Instance Receiver: 
                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: Product) (Syntax: 'p')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'Product.ProductName' is never assigned to, and will always have its default value null
                //     public string ProductName;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ProductName").WithArguments("Product.ProductName", "null").WithLocation(7, 19),
                // CS0649: Field 'Product.SupplierID' is never assigned to, and will always have its default value 0
                //     public int SupplierID;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "SupplierID").WithArguments("Product.SupplierID", "0").WithLocation(8, 16),
                // CS0649: Field 'Product.ProductID' is never assigned to, and will always have its default value 0
                //     public int ProductID;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ProductID").WithArguments("Product.ProductID", "0").WithLocation(6, 16),
                // CS0649: Field 'DB.Products' is never assigned to, and will always have its default value null
                //     public IQueryable<Product> Products;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Products").WithArguments("DB.Products", "null").WithLocation(12, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Enumerable()
        {
            CompileAndVerify(
 @"using System.Linq;
using System.Collections.Generic;

public class Product
{
    public int ProductID;
    public string ProductName;
    public int SupplierID;
}
public class DB
{
    public IEnumerable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.ProductName, p.SupplierID } : new { p.ProductID, p.ProductName, p.SupplierID } }).ToList();
    }
}").VerifyDiagnostics();
        }

        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Enumerable2()
        {
            CompileAndVerify(
 @"using System.Linq;
using System.Collections.Generic;

public class Product
{
    public int ProductID;
    public int SupplierID;
}
public class DB
{
    public IEnumerable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.SupplierID } : new { p.ProductID, p.SupplierID } }).ToList();
        var q1 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.SupplierID } : new { p.ProductID, p.SupplierID } }).ToList();
    }
}").VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22588, "https://github.com/dotnet/roslyn/issues/22588")]
        public void AnonymousTypeSymbols_ErrorCases()
        {
            string source = @"
class ClassA
{
    static void Test1(int x)
    {
        var obj = /*<bind>*/new { f1 = null, f2 = M, f3 = default }/*</bind>*/;
    }

    static void M()
    {
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: error f1, error f2, error f3>, IsInvalid) (Syntax: 'new { f1 =  ... = default }')
  Initializers(3):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: error, Constant: null, IsInvalid) (Syntax: 'f1 = null')
        Left: 
          IPropertyReferenceOperation: error <anonymous type: error f1, error f2, error f3>.f1 { get; } (OperationKind.PropertyReference, Type: error, IsInvalid) (Syntax: 'f1')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: error, IsInvalid) (Syntax: 'f2 = M')
        Left: 
          IPropertyReferenceOperation: error <anonymous type: error f1, error f2, error f3>.f2 { get; } (OperationKind.PropertyReference, Type: error, IsInvalid) (Syntax: 'f2')
            Instance Receiver: 
              null
        Right: 
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: ClassA, IsInvalid, IsImplicit) (Syntax: 'M')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: error, IsInvalid) (Syntax: 'f3 = default')
        Left: 
          IPropertyReferenceOperation: error <anonymous type: error f1, error f2, error f3>.f3 { get; } (OperationKind.PropertyReference, Type: error, IsInvalid) (Syntax: 'f3')
            Instance Receiver: 
              null
        Right: 
          IDefaultValueOperation (OperationKind.DefaultValue, Type: null, IsInvalid) (Syntax: 'default')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,35): error CS0828: Cannot assign '<null>' to anonymous type property
                //         var obj = /*<bind>*/new { f1 = null, f2 = M, f3 = default }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "f1 = null").WithArguments("<null>").WithLocation(6, 35),
                // file.cs(6,46): error CS0828: Cannot assign 'method group' to anonymous type property
                //         var obj = /*<bind>*/new { f1 = null, f2 = M, f3 = default }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "f2 = M").WithArguments("method group").WithLocation(6, 46),
                // file.cs(6,54): error CS0828: Cannot assign 'default' to anonymous type property
                //         var obj = /*<bind>*/new { f1 = null, f2 = M, f3 = default }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "f3 = default").WithArguments("default").WithLocation(6, 54)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/27338")]
        public void AnonymousTypeCreation_MixedInitializers()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class ClassA
{
    void M(int a)
    {
        var o = /*<bind>*/new { a, b = 1 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a, b = 1 }')
  Initializers(2):
      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = 1')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
           ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #region "Utility methods"

        private void AssertCannotConstruct(ISymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            Assert.NotNull(namedType);

            var objType = namedType.BaseType();
            Assert.NotNull(objType);
            Assert.Equal("System.Object", objType.ToTestDisplayString());

            TypeSymbol[] args = new TypeSymbol[namedType.Arity];
            for (int i = 0; i < namedType.Arity; i++)
            {
                args[i] = objType;
            }

            Assert.Throws<InvalidOperationException>(() => namedType.Construct(args));
        }

        private CompilationUtils.SemanticInfoSummary GetAnonymousTypeInfoSummary(TestData data, int node, TextSpan typeSpan, params int[] fields)
        {
            var info = data.Model.GetSemanticInfoSummary(data.Nodes[node]);
            var type = info.Type;

            Assert.True(type.IsAnonymousType);
            Assert.False(type.CanBeReferencedByName);

            Assert.Equal("System.Object", type.BaseType.ToTestDisplayString());
            Assert.Equal(0, type.Interfaces.Length);

            Assert.Equal(1, type.Locations.Length);
            Assert.Equal(typeSpan, type.Locations[0].SourceSpan);

            foreach (int field in fields)
            {
                CheckFieldNameAndLocation(data, type, data.Nodes[field]);
            }

            return info;
        }

        private void CheckFieldNameAndLocation(TestData data, ITypeSymbol type, SyntaxNode identifier)
        {
            var anonymousType = (NamedTypeSymbol)type;

            var current = identifier;
            while (current.Span == identifier.Span && !current.IsKind(SyntaxKind.IdentifierName))
            {
                current = current.ChildNodes().Single();
            }
            var node = (IdentifierNameSyntax)current;
            Assert.NotNull(node);

            var span = node.Span;
            var fieldName = node.ToString();

            var property = anonymousType.GetMember<PropertySymbol>(fieldName);
            Assert.NotNull(property);
            Assert.Equal(fieldName, property.Name);
            Assert.Equal(1, property.Locations.Length);
            Assert.Equal(span, property.Locations[0].SourceSpan);

            MethodSymbol getter = property.GetMethod;
            Assert.NotNull(getter);
            Assert.Equal("get_" + fieldName, getter.Name);
        }

        private struct TestData
        {
            public CSharpCompilation Compilation;
            public SyntaxTree Tree;
            public List<SyntaxNode> Nodes;
            public SemanticModel Model;
        }

        private TestData Compile(string source, int expectedIntervals, params DiagnosticDescription[] diagnostics)
        {
            var intervals = ExtractTextIntervals(ref source);
            Assert.Equal(expectedIntervals, intervals.Count);

            var compilation = Compile(source);

            compilation.VerifyDiagnostics(diagnostics);

            var tree = compilation.SyntaxTrees[0];
            var nodes = new List<SyntaxNode>();

            foreach (var span in intervals)
            {
                var stack = new Stack<SyntaxNode>();
                stack.Push(tree.GetCompilationUnitRoot());

                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (span.Contains(node.Span))
                    {
                        nodes.Add(node);
                        break;
                    }

                    foreach (var child in node.ChildNodes())
                    {
                        stack.Push(child);
                    }
                }
            }
            Assert.Equal(expectedIntervals, nodes.Count);

            return new TestData()
            {
                Compilation = compilation,
                Tree = tree,
                Model = compilation.GetSemanticModel(tree),
                Nodes = nodes
            };
        }

        private CSharpCompilation Compile(string source) =>
            CreateCompilationWithMscorlib40(
                source,
                options: TestOptions.ReleaseDll,
                parseOptions: TestOptions.Regular);

        private static List<TextSpan> ExtractTextIntervals(ref string source)
        {
            const string startTag = "[#";
            const string endTag = "#]";

            List<TextSpan> intervals = new List<TextSpan>();

            var all = (from s in FindAll(source, startTag)
                       select new { start = true, offset = s }).Union(
                                from s in FindAll(source, endTag)
                                select new { start = false, offset = s }
                      ).OrderBy(value => value.offset).ToList();

            while (all.Count > 0)
            {
                int i = 1;
                bool added = false;
                while (i < all.Count)
                {
                    if (all[i - 1].start && !all[i].start)
                    {
                        intervals.Add(TextSpan.FromBounds(all[i - 1].offset, all[i].offset));
                        all.RemoveAt(i);
                        all.RemoveAt(i - 1);
                        added = true;
                    }
                    else
                    {
                        i++;
                    }
                }
                Assert.True(added);
            }

            source = source.Replace(startTag, "  ").Replace(endTag, "  ");

            intervals.Sort((x, y) => x.Start.CompareTo(y.Start));
            return intervals;
        }

        private static IEnumerable<int> FindAll(string source, string what)
        {
            int index = source.IndexOf(what, StringComparison.Ordinal);
            while (index >= 0)
            {
                yield return index;
                index = source.IndexOf(what, index + 1, StringComparison.Ordinal);
            }
        }

        private int NumberOfNewKeywords(string source)
        {
            int cnt = 0;
            foreach (var line in source.Split(new String[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (!line.Trim().StartsWith("//", StringComparison.Ordinal))
                    {
                        for (int index = line.IndexOf("new ", StringComparison.Ordinal); index >= 0;)
                        {
                            cnt++;
                            index = line.IndexOf("new ", index + 1, StringComparison.Ordinal);
                        }
                    }
                }
            }
            return cnt;
        }

        #endregion
    }
}
