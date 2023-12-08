// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IObjectCreationExpression : SemanticModelTestBase
    {
        private static readonly CSharpParseOptions ImplicitObjectCreationOptions = TestOptions.Regular9;

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithArguments()
        {
            string source = @"
class C
{
    public C(byte i, long j)
    {
    }
    void M()
    /*<bind>*/{
        C x1 = new(3, 4);
        C x2 = new(j: 3, i: 4);
        C x3 = new(3, j: 4);
        C x4 = new(i: 3, 4);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (4 statements, 4 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: C x1
    Local_2: C x2
    Local_3: C x3
    Local_4: C x4
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'C x1 = new(3, 4);')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C x1 = new(3, 4)')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C x1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x1 = new(3, 4)')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new(3, 4)')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new(3, 4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: C..ctor(System.Byte i, System.Int64 j)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new(3, 4)')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null) (Syntax: '4')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'C x2 = new(j: 3, i: 4);')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C x2 = new(j: 3, i: 4)')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C x2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x2 = new(j: 3, i: 4)')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new(j: 3, i: 4)')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new(j: 3, i: 4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: C..ctor(System.Byte i, System.Int64 j)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new(j: 3, i: 4)')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null) (Syntax: 'j: 3')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 3, IsImplicit) (Syntax: '3')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i: 4')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 4, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'C x3 = new(3, j: 4);')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C x3 = new(3, j: 4)')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C x3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x3 = new(3, j: 4)')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new(3, j: 4)')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new(3, j: 4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: C..ctor(System.Byte i, System.Int64 j)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new(3, j: 4)')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null) (Syntax: 'j: 4')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'C x4 = new(i: 3, 4);')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C x4 = new(i: 3, 4)')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C x4) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x4 = new(i: 3, 4)')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new(i: 3, 4)')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new(i: 3, 4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: C..ctor(System.Byte i, System.Int64 j)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new(i: 3, 4)')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i: 3')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null) (Syntax: '4')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
      Initializer:
        null
";
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithMemberInitializers()
        {
            var comp = CreateCompilation(@"
struct B
{
    public bool Field;
}
class F
{
    public int Field;
    public string Property1 { set; get; }
    public B Property2 { set; get; }
}
class C
{
    public void M1()
    /*<bind>*/{
        F x1 = new();
        F x2 = new() { Field = 2 };
        F x3 = new() { Property1 = """" };
        F x4 = new() { Property1 = """", Field = 2 };
        F x5 = new() { Property2 = new B { Field = true } };
        F e1 = new() { Property2 = 1 };
        F e2 = new() { """" };
    }/*</bind>*/
}
");
            string expectedOperationTree = @"
IBlockOperation (7 statements, 7 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: F x1
    Local_2: F x2
    Local_3: F x3
    Local_4: F x4
    Local_5: F x5
    Local_6: F e1
    Local_7: F e2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'F x1 = new();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F x1 = new()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x1 = new()')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new()')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new()')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new()')
                      Arguments(0)
                      Initializer:
                        null
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'F x2 = new( ... ield = 2 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F x2 = new( ... Field = 2 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x2 = new() { Field = 2 }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new() { Field = 2 }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Field = 2 }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Field = 2 }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Field = 2 }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                                Left:
                                  IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Field')
                                Right:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'F x3 = new( ... ty1 = """" };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F x3 = new( ... rty1 = """" }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x3 = new()  ... rty1 = """" }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new() { P ... rty1 = """" }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Property1 = """" }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Property1 = """" }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property1 = """" }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                                Left:
                                  IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property1')
                                Right:
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'F x4 = new( ... ield = 2 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F x4 = new( ... Field = 2 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x4) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x4 = new()  ... Field = 2 }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new() { P ... Field = 2 }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Pro ... Field = 2 }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property1 ... Field = 2 }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                                Left:
                                  IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property1')
                                Right:
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                                Left:
                                  IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Field')
                                Right:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'F x5 = new( ... = true } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F x5 = new( ...  = true } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x5) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x5 = new()  ...  = true } }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new() { P ...  = true } }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Pro ...  = true } }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Pro ...  = true } }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property2 ...  = true } }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B) (Syntax: 'Property2 = ... ld = true }')
                                Left:
                                  IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property2')
                                Right:
                                  IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B { Field = true }')
                                    Arguments(0)
                                    Initializer:
                                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B) (Syntax: '{ Field = true }')
                                        Initializers(1):
                                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'Field = true')
                                              Left:
                                                IFieldReferenceOperation: System.Boolean B.Field (OperationKind.FieldReference, Type: System.Boolean) (Syntax: 'Field')
                                                  Instance Receiver:
                                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Field')
                                              Right:
                                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'F e1 = new( ... rty2 = 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'F e1 = new( ... erty2 = 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F e1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e1 = new()  ... erty2 = 1 }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new() { P ... erty2 = 1 }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { Property2 = 1 }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new() { Property2 = 1 }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: '{ Property2 = 1 }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
                                Left:
                                  IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property2')
                                Right:
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Operand:
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer:
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'F e2 = new() { """" };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'F e2 = new() { """" }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F e2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e2 = new() { """" }')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new() { """" }')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { """" }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new() { """" }')
                      Arguments(0)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: '{ """" }')
                          Initializers(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
                                Children(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
      Initializer:
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(21,36): error CS0029: Cannot implicitly convert type 'int' to 'B'
                //         F e1 = new() { Property2 = 1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "B").WithLocation(21, 36),
                // file.cs(22,22): error CS1922: Cannot initialize type 'F' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         F e2 = new() { "" };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ """" }").WithArguments("F").WithLocation(22, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, expectedDiagnostics);
            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [F x1] [F x2] [F x3] [F x4] [F x5] [F e1] [F e2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsImplicit) (Syntax: 'x1 = new()')
              Left:
                ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsImplicit) (Syntax: 'x1 = new()')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new()')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ObjectCreation)
                  Operand:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new()')
                      Arguments(0)
                      Initializer:
                        null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { Field = 2 }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Field = 2 }')
                      Arguments(0)
                      Initializer:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                  Left:
                    IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Field = 2 }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsImplicit) (Syntax: 'x2 = new() { Field = 2 }')
                  Left:
                    ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsImplicit) (Syntax: 'x2 = new() { Field = 2 }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Field = 2 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Field = 2 }')
            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [1]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { Property1 = """" }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Property1 = """" }')
                      Arguments(0)
                      Initializer:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                  Left:
                    IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Property1 = """" }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsImplicit) (Syntax: 'x3 = new()  ... rty1 = """" }')
                  Left:
                    ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsImplicit) (Syntax: 'x3 = new()  ... rty1 = """" }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Property1 = """" }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Property1 = """" }')
            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [2]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (4)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Pro ... Field = 2 }')
                      Arguments(0)
                      Initializer:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                  Left:
                    IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                  Left:
                    IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsImplicit) (Syntax: 'x4 = new()  ... Field = 2 }')
                  Left:
                    ILocalReferenceOperation: x4 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsImplicit) (Syntax: 'x4 = new()  ... Field = 2 }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Pro ... Field = 2 }')
            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { Pro ...  = true } }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new() { Pro ...  = true } }')
                      Arguments(0)
                      Initializer:
                        null
            Next (Regular) Block[B6]
                Entering: {R6}
        .locals {R6}
        {
            CaptureIds: [4]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (3)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new B { Field = true }')
                      Value:
                        IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B { Field = true }')
                          Arguments(0)
                          Initializer:
                            null
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'Field = true')
                      Left:
                        IFieldReferenceOperation: System.Boolean B.Field (OperationKind.FieldReference, Type: System.Boolean) (Syntax: 'Field')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B { Field = true }')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B) (Syntax: 'Property2 = ... ld = true }')
                      Left:
                        IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Pro ...  = true } }')
                      Right:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B { Field = true }')
                Next (Regular) Block[B7]
                    Leaving: {R6}
        }
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsImplicit) (Syntax: 'x5 = new()  ...  = true } }')
                  Left:
                    ILocalReferenceOperation: x5 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsImplicit) (Syntax: 'x5 = new()  ...  = true } }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsImplicit) (Syntax: 'new() { Pro ...  = true } }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: F, IsImplicit) (Syntax: 'new() { Pro ...  = true } }')
            Next (Regular) Block[B8]
                Leaving: {R5}
                Entering: {R7}
    }
    .locals {R7}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (3)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new() { Property2 = 1 }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new() { Property2 = 1 }')
                      Arguments(0)
                      Initializer:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
                  Left:
                    IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { Property2 = 1 }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsInvalid, IsImplicit) (Syntax: 'e1 = new()  ... erty2 = 1 }')
                  Left:
                    ILocalReferenceOperation: e1 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'e1 = new()  ... erty2 = 1 }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { Property2 = 1 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { Property2 = 1 }')
            Next (Regular) Block[B9]
                Leaving: {R7}
                Entering: {R8}
    }
    .locals {R8}
    {
        CaptureIds: [6]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (3)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new() { """" }')
                  Value:
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new() { """" }')
                      Arguments(0)
                      Initializer:
                        null
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
                  Children(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: F, IsInvalid, IsImplicit) (Syntax: 'e2 = new() { """" }')
                  Left:
                    ILocalReferenceOperation: e2 (IsDeclaration: True) (OperationKind.LocalReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'e2 = new() { """" }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { """" }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'new() { """" }')
            Next (Regular) Block[B10]
                Leaving: {R8} {R1}
    }
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    private readonly int field;
    public void M1(int x)
    {
        int y = 0;
        List<int> x1 = /*<bind>*/new() { x, y, field }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new() { x, y, field }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, field }')
      Initializers(3):
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { x, y, field }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { x, y, field }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { x, y, field }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                  IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(5,23): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                //     private readonly int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0").WithLocation(5, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithNestedCollectionInitializer()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
using System.Linq;
class C
{
    private readonly int field = 0;
    public void M1(int x)
    {
        int y = 0;
        List<List<int>> x1 = /*<bind>*/new() {
            new[] { x, y }.ToList(),
            new() { field }
        }/*</bind>*/;
    }
}
");
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: 'new() { ... }')
  Arguments(0)
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '{ ... }')
      Initializers(2):
          IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
                  IInvocationOperation (System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.Invocation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
                    Instance Receiver:
                      null
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }')
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'new[] { x, y }')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { x, y }')
                                Initializer:
                                  IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ x, y }')
                                    Element Values(2):
                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new() { field }')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new() { field }')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { field }')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new() { field }')
                        Arguments(0)
                        Initializer:
                          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ field }')
                            Initializers(1):
                                IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { field }')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                                        IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                                          Instance Receiver:
                                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var m1 = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            VerifyFlowGraph(comp, m1, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 y] [System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> x1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y = 0')
              Left:
                ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'y = 0')
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { ... }')
                  Value:
                    IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: 'new() { ... }')
                      Arguments(0)
                      Initializer:
                        null
                IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
                        IInvocationOperation (System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.Invocation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'new[] { x, y }')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand:
                                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                                      Dimension Sizes(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { x, y }')
                                      Initializer:
                                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ x, y }')
                                          Element Values(2):
                                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (3)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { field }')
                      Value:
                        IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new() { field }')
                          Arguments(0)
                          Initializer:
                            null
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { field }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                            IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new() { field }')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new() { field }')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { field }')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (ObjectCreation)
                              Operand:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new() { field }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x1 = /*<bin ... }')
                  Left:
                    ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x1 = /*<bin ... }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'new() { ... }')
            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
    }
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithMemberAndCollectionInitializers()
        {
            string source = @"
using System.Collections.Generic;
internal class Class
{
    public int X { get; set; }
    public List<int> Y { get; set; }
    public Dictionary<int, int> Z { get; set; }
    public Class C { get; set; }
    private readonly int field = 0;
    public void M(int x)
    {
        int y = 0;
        Class c = /*<bind>*/new() {
            X = x,
            Y = { x, y, 3 },
            Z = { { x, y } },
            C = { X = field }
        }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new() { ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ ... }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = x')
            Left: 
              IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Y')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Z')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    IInvocationOperation ( void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ x, y }')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit) (Syntax: 'Z')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: Class) (Syntax: 'C = { X = field }')
            InitializedMember: 
              IPropertyReferenceOperation: Class Class.C { get; set; } (OperationKind.PropertyReference, Type: Class) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'C')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ X = field }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = field')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
                      Right: 
                        IFieldReferenceOperation: System.Int32 Class.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithArrayInitializer()
        {
            var comp = CreateCompilation(@"
class C
{
    int[] a;
    static void Main()
    {
        C a = /*<bind>*/new() { a = { [0] = 1, [1] = 2 } }/*</bind>*/;
    }
}
");
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new() { a = ... [1] = 2 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ a = { [0] ... [1] = 2 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[]) (Syntax: 'a = { [0] = 1, [1] = 2 }')
            InitializedMember: 
              IFieldReferenceOperation: System.Int32[] C.a (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'a')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'a')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[]) (Syntax: '{ [0] = 1, [1] = 2 }')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[0] = 1')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[0]')
                          Array reference: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[1] = 2')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[1]')
                          Array reference: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a").WithLocation(4, 11)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var main = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            VerifyFlowGraph(comp, main, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C a]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() { a = ... [1] = 2 } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new() { a = ... [1] = 2 } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[0] = 1')
                  Left:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[0]')
                      Array reference:
                        IFieldReferenceOperation: System.Int32[] C.a (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'a')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new() { a = ... [1] = 2 } }')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[1] = 2')
                  Left:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[1]')
                      Array reference:
                        IFieldReferenceOperation: System.Int32[] C.a (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'a')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new() { a = ... [1] = 2 } }')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'a = /*<bind ... [1] = 2 } }')
              Left:
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'a = /*<bind ... [1] = 2 } }')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new() { a = ... [1] = 2 } }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ObjectCreation)
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new() { a = ... [1] = 2 } }')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithInvalidInitializer()
        {
            string source = @"
class C
{
    public void M1()
    {
        C x1 = /*<bind>*/new() { MissingMember = 1 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new() { Mis ... ember = 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingMember = 1 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'MissingMember = 1')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingMember')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingMember')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new() { Mis ... ember = 1 }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,34): error CS0117: 'C' does not contain a definition for 'MissingMember'
                //         C x1 = /*<bind>*/new() { MissingMember = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingMember").WithArguments("C", "MissingMember").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithInvalidMemberInitializer()
        {
            string source = @"
class C
{
    public void M1()
    {
        C x1 = /*<bind>*/new(){ MissingField = { x = 1 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new(){ Miss ... { x = 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingFi ... { x = 1 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: ?, IsInvalid) (Syntax: 'MissingField = { x = 1 }')
            InitializedMember: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... { x = 1 } }')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ?) (Syntax: '{ x = 1 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'x = 1')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'x')
                          Children(1):
                              IOperation:  (OperationKind.None, Type: null) (Syntax: 'x')
                                Children(1):
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,33): error CS0117: 'C' does not contain a definition for 'MissingField'
                //         C x1 = /*<bind>*/new(){ MissingField = { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingField").WithArguments("C", "MissingField").WithLocation(6, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithInvalidCollectionInitializer()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    public void M1()
    {
        C x1 = /*<bind>*/new(){ MissingField = new List<int>() { 1 }}/*</bind>*/;
    }
}
");
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new(){ Miss ... t>() { 1 }}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingFi ... t>() { 1 }}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'MissingFiel ... nt>() { 1 }')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... t>() { 1 }}')
            Right: 
              IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int>() { 1 }')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ 1 }')
                    Initializers(1):
                        IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,33): error CS0117: 'C' does not contain a definition for 'MissingField'
                //         C x1 = /*<bind>*/new(){ MissingField = new List<int>() { 1 }}/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingField").WithArguments("C", "MissingField").WithLocation(7, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var m1 = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            VerifyFlowGraph(comp, m1, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C x1]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... t>() { 1 }}')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new(){ Miss ... t>() { 1 }}')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                  Value:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingField')
                            Children(1):
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... t>() { 1 }}')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new List<int>() { 1 }')
                  Value:
                    IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int>() { 1 }')
                      Arguments(0)
                      Initializer:
                        null
                IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new List<int>() { 1 }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'MissingFiel ... nt>() { 1 }')
                  Left:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                  Right:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'new List<int>() { 1 }')
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid, IsImplicit) (Syntax: 'x1 = /*<bin ... t>() { 1 }}')
              Left:
                ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'x1 = /*<bin ... t>() { 1 }}')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... t>() { 1 }}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ObjectCreation)
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new(){ Miss ... t>() { 1 }}')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(1198816, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1198816/")]
        public void ImplicitObjectCreationUnconverted_ArrayIndex()
        {
            var comp = CreateCompilation("_ = new int[/*<bind>*/new(bad)/*</bind>*/];");

            var expectedDiagnostics = new DiagnosticDescription[] { 
                // (1,27): error CS0103: The name 'bad' does not exist in the current context
                // _ = new int[/*<bind>*/new(bad)/*</bind>*/];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "bad").WithArguments("bad").WithLocation(1, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new(bad)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'bad')
        Children(0)
            ", expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(1198816, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1198816/")]
        public void ImplicitObjectCreationUnconverted_IfCondition()
        {
            var comp = CreateCompilation(@"
if (/*<bind>*/new(bad)/*</bind>*/) {}
", options: TestOptions.UnsafeReleaseExe);

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (2,19): error CS0103: The name 'bad' does not exist in the current context
                // if (/*<bind>*/new(bad)/*</bind>*/) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "bad").WithArguments("bad").WithLocation(2, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new(bad)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'bad')
        Children(0)
            ", expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(1198816, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1198816/")]
        public void ImplicitObjectCreationUnconverted_ConditionalOperator()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = /*<bind>*/new(bad)/*</bind>*/ ? null : new object();
    }
}";
            var comp = CreateCompilation(source);

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (5,27): error CS0103: The name 'bad' does not exist in the current context
                //         _ = /*<bind>*/new(bad)/*</bind>*/ ? null : new object();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "bad").WithArguments("bad").WithLocation(5, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new(bad)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'bad')
        Children(0)
            ", expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(1198816, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1198816/")]
        public void ImplicitObjectCreationUnconverted_ConditionalOperator_Nested1()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = (/*<bind>*/new(bad)/*</bind>*/, null) ? null : new object();
    }
}";
            var comp = CreateCompilation(source);

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (5,28): error CS0103: The name 'bad' does not exist in the current context
                //         _ = (/*<bind>*/new(bad)/*</bind>*/, null) ? null : new object();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "bad").WithArguments("bad").WithLocation(5, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new(bad)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'bad')
        Children(0)
            ", expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(1198816, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1198816/")]
        public void ImplicitObjectCreationUnconverted_ConditionalOperator_Nested2()
        {
            var source =
@"class Program
{
    static void Main(int i)
    {
        _ = i switch { 1 => /*<bind>*/new(bad)/*</bind>*/, _ => null } ? null : new object();
    }
}";
            var comp = CreateCompilation(source);

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (5,43): error CS0103: The name 'bad' does not exist in the current context
                //         _ = i switch { 1 => /*<bind>*/new(bad)/*</bind>*/, _ => null } ? null : new object();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "bad").WithArguments("bad").WithLocation(5, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(comp, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new(bad)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'bad')
        Children(0)
            ", expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithDynamicMemberInitializer_01()
        {
            var comp = CreateCompilation(@"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }
    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }
    int X, Y, Z;
    static void Main()
    {
        dynamic x = 1;
        A _ = new() {/*<bind>*/[y: x, x: x] = { X = 1, Y = 1, Z = 1 }/*</bind>*/ };
    }
}
");
            string expectedOperationTree = @"
IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: '[y: x, x: x ...  1, Z = 1 }')
  InitializedMember: 
    IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
      Expression: 
        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
      Arguments(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ArgumentNames(2):
        ""y""
        ""x""
      ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ X = 1, Y = 1, Z = 1 }')
      Initializers(3):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""X"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'X')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Y = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""Y"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Y')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'Y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Z = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""Z"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Z')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'Z')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var main = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            VerifyFlowGraph(comp, main, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [dynamic x] [A _]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'x = 1')
              Left:
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = 1')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
                  Value:
                    IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new() {/*<b ... </bind>*/ }')
                      Arguments(0)
                      Initializer:
                        null
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1] [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (5)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 1')
                      Left:
                        IDynamicMemberReferenceOperation (Member Name: ""X"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'X')
                          Type Arguments(0)
                          Instance Receiver:
                            IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
                              Expression:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
                              Arguments(2):
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              ArgumentNames(2):
                                ""y""
                                ""x""
                              ArgumentRefKinds(0)
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Y = 1')
                      Left:
                        IDynamicMemberReferenceOperation (Member Name: ""Y"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Y')
                          Type Arguments(0)
                          Instance Receiver:
                            IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
                              Expression:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
                              Arguments(2):
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              ArgumentNames(2):
                                ""y""
                                ""x""
                              ArgumentRefKinds(0)
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Z = 1')
                      Left:
                        IDynamicMemberReferenceOperation (Member Name: ""Z"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Z')
                          Type Arguments(0)
                          Instance Receiver:
                            IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
                              Expression:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
                              Arguments(2):
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              ArgumentNames(2):
                                ""y""
                                ""x""
                              ArgumentRefKinds(0)
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: A, IsImplicit) (Syntax: '_ = new() { ... </bind>*/ }')
                  Left:
                    ILocalReferenceOperation: _ (IsDeclaration: True) (OperationKind.LocalReference, Type: A, IsImplicit) (Syntax: '_ = new() { ... </bind>*/ }')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: A, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new() {/*<b ... </bind>*/ }')
            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
    }
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithDynamicMemberInitializer_02()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }
    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }
    int X, Y, Z;
    static void Main()
    {
        dynamic x = 1;
        A _ = new() {/*<bind>*/[y: x, x: x] = { }/*</bind>*/ };
    }
}
";
            string expectedOperationTree = @"
IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: '[y: x, x: x] = { }')
  InitializedMember: 
    IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
      Expression: 
        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
      Arguments(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ArgumentNames(2):
        ""y""
        ""x""
      ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationWithDynamicMemberInitializer_03()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }
    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }
    int X, Y, Z;
    static void Main()
    {
        dynamic x = 1;
        A _ = new() {/*<bind>*/[y: x, x: x]/*</bind>*/ = { } };
    }
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
  Expression: 
    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
  Arguments(2):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
  ArgumentNames(2):
    ""y""
    ""x""
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationDynamicCollectionInitializer()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
class C1 : IEnumerable<int>
{
    public static void M(C1 c, dynamic d1)
    {
        c = /*<bind>*/new() { d1 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new() { d1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ d1 }')
      Initializers(1):
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd1')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'new() { d1 }')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'new() { d1 }')
            Arguments(1):
                IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
            ArgumentNames(0)
            ArgumentRefKinds(0)
";
            VerifyOperationTreeAndDiagnosticsForTest<ImplicitObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerWithRefAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;
class C : IEnumerable<int>
{
    public static void M()
    {
        C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
    public void Add(ref int i)
    {
    }
}
";
            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '3')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(9,33): error CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("C.Add(ref int)").WithLocation(9, 33),
                // file.cs(9,36): error CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "2").WithArguments("C.Add(ref int)").WithLocation(9, 36),
                // file.cs(9,39): error CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "3").WithArguments("C.Add(ref int)").WithLocation(9, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerWithDefaultParameterAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;
class C : IEnumerable<int>
{
    public static void M()
    {
        C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
    public void Add(int i, object o = null)
    {
    }
}
";
            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerWithParamsAddMethod()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
class C : IEnumerable<int>
{
    void M(C c)
    {
        c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public void Add(params int[] ints)
    {
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '1')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '1')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '2')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '2')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '3')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '3')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '3')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerExtensionAddMethod()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
class C : IEnumerable<int>
{
    void M(C c)
    {
        c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
static class CExtensions
{
    public static void Add(this C c, int i)
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerStaticAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;
class C : IEnumerable<int>
{
    public static void M()
    {
        C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
    public static void Add(int i)
    {
    }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(9,33): error CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "1").WithArguments("C.Add(int)").WithLocation(9, 33),
                // file.cs(9,36): error CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "2").WithArguments("C.Add(int)").WithLocation(9, 36),
                // file.cs(9,39): error CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         C c = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "3").WithArguments("C.Add(int)").WithLocation(9, 39)
            };

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '3')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: ImplicitObjectCreationOptions);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationCollectionInitializerAddMethodOnInterface()
        {
            string source = @"
using System.Collections.Generic;
using System.Runtime.InteropServices;
[ComImport()]
[CoClass(typeof(CoClassImplementation))]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
public interface IInterface : IEnumerable<int>
{
    void Add(int i);
}
public class CoClassImplementation
{
}
class C
{
    void M()
    {
        IInterface iinterface = new() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: IInterface) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'new() /*<bi ... { 1, 2, 3 }')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationNestedObjectInitializerNoSet()
        {
            var comp = CreateCompilation(@"
class C1
{
    void M(C1 c1)
    {
        c1 = new() /*<bind>*/{ [1] = { A = 1 } }/*</bind>*/;
    }
    C2 this[int i] { get => null; }
}
class C2
{
    public int A { get; set; }
}
");
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ [1] = { A = 1 } }')
  Initializers(1):
      IMemberInitializerOperation (OperationKind.MemberInitializer, Type: C2) (Syntax: '[1] = { A = 1 }')
        InitializedMember: 
          IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: '[1]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Initializer: 
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: '{ A = 1 }')
            Initializers(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'A')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var m = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            VerifyFlowGraph(comp, m, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value:
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() /*<bi ... { A = 1 } }')
              Value:
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new() /*<bi ... { A = 1 } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C2.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                      Instance Receiver:
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... { A = 1 } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1 = new()  ... *</bind>*/;')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c1 = new()  ... { A = 1 } }')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... { A = 1 } }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... { A = 1 } }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitObjectCreationNestedCollectionInitializerNoSet()
        {
            var comp = CreateCompilation(@"
using System.Collections;
using System.Collections.Generic;
class C1
{
    void M(C1 c1)
    {
        c1 = new() /*<bind>*/{ [1] = { 1 } }/*</bind>*/;
    }
    C2 this[int i] { get => null; }
}
class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw new System.NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
    public void Add(int i) { }
}
");
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ [1] = { 1 } }')
  Initializers(1):
      IMemberInitializerOperation (OperationKind.MemberInitializer, Type: C2) (Syntax: '[1] = { 1 }')
        InitializedMember: 
          IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: '[1]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Initializer: 
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: '{ 1 }')
            Initializers(1):
                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: '[1]')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var m = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M");
            VerifyFlowGraph(comp, m, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value:
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new() /*<bi ... ] = { 1 } }')
              Value:
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new() /*<bi ... ] = { 1 } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                  Instance Receiver:
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... ] = { 1 } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1 = new()  ... *</bind>*/;')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c1 = new()  ... ] = { 1 } }')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... ] = { 1 } }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ObjectCreation)
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new() /*<bi ... ] = { 1 } }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithMemberInitializers()
        {
            string source = @"
struct B
{
    public bool Field;
}

class F
{
    public int Field;
    public string Property1 { set; get; }
    public B Property2 { set; get; }
}

class C
{
    public void M1()
    /*<bind>*/{
        var x1 = new F();
        var x2 = new F() { Field = 2 };
        var x3 = new F() { Property1 = """" };
        var x4 = new F() { Property1 = """", Field = 2 };
        var x5 = new F() { Property2 = new B { Field = true } };

        var e1 = new F() { Property2 = 1 };
        var e2 = new F() { """" };
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (7 statements, 7 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: F x1
    Local_2: F x2
    Local_3: F x3
    Local_4: F x4
    Local_5: F x5
    Local_6: F e1
    Local_7: F e2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x1 = new F();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x1 = new F()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x1 = new F()')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F()')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F()')
                  Arguments(0)
                  Initializer: 
                    null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x2 = ne ... ield = 2 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x2 = ne ... Field = 2 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x2 = new F( ... Field = 2 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { Field = 2 }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F() { Field = 2 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Field = 2 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                            Left: 
                              IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Field')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x3 = ne ... ty1 = """" };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x3 = ne ... rty1 = """" }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x3 = new F( ... rty1 = """" }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ... rty1 = """" }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F() { P ... rty1 = """" }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property1 = """" }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                            Left: 
                              IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property1')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x4 = ne ... ield = 2 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x4 = ne ... Field = 2 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x4) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x4 = new F( ... Field = 2 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ... Field = 2 }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F() { P ... Field = 2 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property1 ... Field = 2 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Property1 = """"')
                            Left: 
                              IPropertyReferenceOperation: System.String F.Property1 { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property1')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Field = 2')
                            Left: 
                              IFieldReferenceOperation: System.Int32 F.Field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Field')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x5 = ne ... = true } };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x5 = ne ...  = true } }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F x5) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x5 = new F( ...  = true } }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F() { ...  = true } }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F() { P ...  = true } }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ Property2 ...  = true } }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B) (Syntax: 'Property2 = ... ld = true }')
                            Left: 
                              IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property2')
                            Right: 
                              IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B { Field = true }')
                                Arguments(0)
                                Initializer: 
                                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B) (Syntax: '{ Field = true }')
                                    Initializers(1):
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'Field = true')
                                          Left: 
                                            IFieldReferenceOperation: System.Boolean B.Field (OperationKind.FieldReference, Type: System.Boolean) (Syntax: 'Field')
                                              Instance Receiver: 
                                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Field')
                                          Right: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var e1 = ne ... erty2 = 1 }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F e1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e1 = new F( ... erty2 = 1 }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new F() { ... erty2 = 1 }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new F() { P ... erty2 = 1 }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: '{ Property2 = 1 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B, IsInvalid) (Syntax: 'Property2 = 1')
                            Left: 
                              IPropertyReferenceOperation: B F.Property2 { get; set; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'Property2')
                            Right: 
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Operand: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var e2 = new F() { """" };')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var e2 = new F() { """" }')
      Declarators:
          IVariableDeclaratorOperation (Symbol: F e2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e2 = new F() { """" }')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new F() { """" }')
                IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'new F() { """" }')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: '{ """" }')
                      Initializers(1):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""""')
                            Children(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'B'
                //         var e1 = new F() { Property2 = 1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "B").WithLocation(24, 40),
                // CS1922: Cannot initialize type 'F' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var e2 = new F() { "" };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ """" }").WithArguments("F").WithLocation(25, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    private readonly int field;
    public void M1(int x)
    {
        int y = 0;
        var x1 = /*<bind>*/new List<int> { x, y, field }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<in ...  y, field }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, field }')
      Initializers(3):
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                  IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                //     private readonly int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithNestedCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    private readonly int field = 0;
    public void M1(int x)
    {
        int y = 0;
        var x1 = /*<bind>*/new List<List<int>> {
            new[] { x, y }.ToList(),
            new List<int> { field }
        }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: 'new List<Li ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '{ ... }')
      Initializers(2):
          IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'List<List<int>>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }.ToList()')
                  IInvocationOperation (System.Collections.Generic.List<System.Int32> System.Linq.Enumerable.ToList<System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source)) (OperationKind.Invocation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new[] { x, y }.ToList()')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new[] { x, y }')
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'new[] { x, y }')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { x, y }')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { x, y }')
                                Initializer: 
                                  IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ x, y }')
                                    Element Values(2):
                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new List<int> { field }')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'List<List<int>>')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new List<int> { field }')
                  IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int> { field }')
                    Arguments(0)
                    Initializer: 
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ field }')
                        Initializers(1):
                            IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                                    IFieldReferenceOperation: System.Int32 C.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                                      Instance Receiver: 
                                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithMemberAndCollectionInitializers()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public int X { get; set; }
    public List<int> Y { get; set; }
    public Dictionary<int, int> Z { get; set; }
    public Class C { get; set; }

    private readonly int field = 0;

    public void M(int x)
    {
        int y = 0;
        var c = /*<bind>*/new Class() {
            X = x,
            Y = { x, y, 3 },
            Z = { { x, y } },
            C = { X = field }
        }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class() ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ ... }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = x')
            Left: 
              IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Y')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Z')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    IInvocationOperation ( void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ x, y }')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit) (Syntax: 'Z')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: Class) (Syntax: 'C = { X = field }')
            InitializedMember: 
              IPropertyReferenceOperation: Class Class.C { get; set; } (OperationKind.PropertyReference, Type: Class) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'C')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ X = field }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = field')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
                      Right: 
                        IFieldReferenceOperation: System.Int32 Class.field (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")]
        public void ObjectCreationWithArrayInitializer()
        {
            string source = @"
class C
{
    int[] a;

    static void Main()
    {
        var a = /*<bind>*/new C { a = { [0] = 1, [1] = 2 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C { a = ... [1] = 2 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ a = { [0] ... [1] = 2 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[]) (Syntax: 'a = { [0] = 1, [1] = 2 }')
            InitializedMember: 
              IFieldReferenceOperation: System.Int32[] C.a (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'a')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'a')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[]) (Syntax: '{ [0] = 1, [1] = 2 }')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[0] = 1')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[0]')
                          Array reference: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[1] = 2')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[1]')
                          Array reference: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a").WithLocation(4, 11)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")]
        public void ObjectCreationWithInvalidInitializer()
        {
            string source = @"
class C
{
    public void M1()
    {
        var x1 = /*<bind>*/new C() { MissingMember = 1 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C() { M ... ember = 1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingMember = 1 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'MissingMember = 1')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingMember')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingMember')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,38): error CS0117: 'C' does not contain a definition for 'MissingMember'
                //         var x1 = /*<bind>*/new C() { MissingMember = 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingMember").WithArguments("C", "MissingMember").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")]
        public void ObjectCreationWithInvalidMemberInitializer()
        {
            string source = @"
class C
{
    public void M1()
    {
        var x1 = /*<bind>*/new C(){ MissingField = { x = 1 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(){ Mi ... { x = 1 } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingFi ... { x = 1 } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: ?, IsInvalid) (Syntax: 'MissingField = { x = 1 }')
            InitializedMember: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ?) (Syntax: '{ x = 1 }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'x = 1')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'x')
                          Children(1):
                              IOperation:  (OperationKind.None, Type: null) (Syntax: 'x')
                                Children(1):
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,37): error CS0117: 'C' does not contain a definition for 'MissingField'
                //         var x1 = /*<bind>*/new C(){ MissingField = { x = 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingField").WithArguments("C", "MissingField").WithLocation(6, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")]
        public void ObjectCreationWithInvalidCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    public void M1()
    {
        var x1 = /*<bind>*/new C(){ MissingField = new List<int>() { 1 }}/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(){ Mi ... t>() { 1 }}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ MissingFi ... t>() { 1 }}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'MissingFiel ... nt>() { 1 }')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'MissingField')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
            Right: 
              IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'new List<int>() { 1 }')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ 1 }')
                    Initializers(1):
                        IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'List<int>')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,37): error CS0117: 'C' does not contain a definition for 'MissingField'
                //         var x1 = /*<bind>*/new C(){ MissingField = new List<int>() { 1 }}/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "MissingField").WithArguments("C", "MissingField").WithLocation(8, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(23154, "https://github.com/dotnet/roslyn/issues/23154")]
        public void ObjectCreationWithDynamicMemberInitializer_01()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }

    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }

    int X, Y, Z;

    static void Main()
    {
        dynamic x = 1;
        new A {/*<bind>*/[y: x, x: x] = { X = 1, Y = 1, Z = 1 }/*</bind>*/ };
    }
}
";
            string expectedOperationTree = @"
IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: '[y: x, x: x ...  1, Z = 1 }')
  InitializedMember: 
    IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
      Expression: 
        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
      Arguments(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ArgumentNames(2):
        ""y""
        ""x""
      ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ X = 1, Y = 1, Z = 1 }')
      Initializers(3):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""X"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'X')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Y = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""Y"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Y')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'Y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Z = 1')
            Left: 
              IDynamicMemberReferenceOperation (Member Name: ""Z"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Z')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'Z')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(23154, "https://github.com/dotnet/roslyn/issues/23154")]
        public void ObjectCreationWithDynamicMemberInitializer_02()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }

    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }

    int X, Y, Z;

    static void Main()
    {
        dynamic x = 1;
        new A {/*<bind>*/[y: x, x: x] = { }/*</bind>*/ };
    }
}
";
            string expectedOperationTree = @"
IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: '[y: x, x: x] = { }')
  InitializedMember: 
    IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
      Expression: 
        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
      Arguments(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ArgumentNames(2):
        ""y""
        ""x""
      ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ }')
      Initializers(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(23154, "https://github.com/dotnet/roslyn/issues/23154")]
        public void ObjectCreationWithDynamicMemberInitializer_03()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }

    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }

    int X, Y, Z;

    static void Main()
    {
        dynamic x = 1;
        new A {/*<bind>*/[y: x, x: x]/*</bind>*/ = { } };
    }
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[y: x, x: x]')
  Expression: 
    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: '[y: x, x: x]')
  Arguments(2):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
  ArgumentNames(2):
    ""y""
    ""x""
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationDynamicCollectionInitializer()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<int>
{
    public static void M(C1 c, dynamic d1)
    {
        c = /*<bind>*/new C1 { d1 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { d1 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ d1 }')
      Initializers(1):
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd1')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'C1')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'C1')
            Arguments(1):
                IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
            ArgumentNames(0)
            ArgumentRefKinds(0)
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerWithRefAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    public static void M()
    {
        var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void Add(ref int i)
    {
    }
}
";
            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '3')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "1").WithArguments("C.Add(ref int)").WithLocation(10, 35),
                // CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "2").WithArguments("C.Add(ref int)").WithLocation(10, 38),
                // CS1954: The best overloaded method match 'C.Add(ref int)' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasParamModifiers, "3").WithArguments("C.Add(ref int)").WithLocation(10, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerWithDefaultParameterAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    public static void M()
    {
        var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void Add(int i, object o = null)
    {
    }
}
";
            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(System.Int32 i, [System.Object o = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerWithParamsAddMethod()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    void M(C c)
    {
        c = new C() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }

    public void Add(params int[] ints)
    {
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '1')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '1')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '2')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '2')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( void C.Add(params System.Int32[] ints)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '3')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '3')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '3')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerExtensionAddMethod()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    void M(C c)
    {
        c = new C() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

static class CExtensions
{
    public static void Add(this C c, int i)
    {
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'C')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'C')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (void CExtensions.Add(this C c, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          null
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'C')
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerStaticAddMethod()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    public static void M()
    {
        var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public static void Add(int i)
    {
    }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "1").WithArguments("C.Add(int)").WithLocation(10, 35),
                // CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "2").WithArguments("C.Add(int)").WithLocation(10, 38),
                // CS1921: The best overloaded method match for 'C.Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
                //         var c = new C /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "3").WithArguments("C.Add(int)").WithLocation(10, 41)
            };

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '3')
        Children(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerAddMethodOnInterface()
        {
            string source = @"
using System.Collections.Generic;
using System.Runtime.InteropServices;

[ComImport()]
[CoClass(typeof(CoClassImplementation))]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
public interface IInterface : IEnumerable<int>
{
    void Add(int i);
}

public class CoClassImplementation
{
}

class C
{
    void M()
    {
        var iinterface = new IInterface() /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: IInterface) (Syntax: '{ 1, 2, 3 }')
  Initializers(3):
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'IInterface')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'IInterface')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual void IInterface.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'IInterface')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationCollectionInitializerBoxingConversion()
        {
            string source = @"
using System.Collections;
struct S : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => null;
}
static class Program
{
    static void Add(this object x, int y) { }
    static void Main()
    {
        _ = /*<bind>*/new S() { 1, 2 }/*</bind>*/;
    }
}";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedOperationTree = @"
IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S() { 1, 2 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: S) (Syntax: '{ 1, 2 }')
      Initializers(2):
          IInvocationOperation (void Program.Add(this System.Object x, System.Int32 y)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              null
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'S')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'S')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: 'S')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation (void Program.Add(this System.Object x, System.Int32 y)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
            Instance Receiver: 
              null
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'S')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'S')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: 'S')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationNestedObjectInitializerNoSet()
        {
            string source = @"
class C1
{
    void M(C1 c1)
    {
        c1 = new C1() /*<bind>*/{ [1] = { A = 1 } }/*</bind>*/;
    }
    C2 this[int i] { get => null; }
}
class C2
{
    public int A { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ [1] = { A = 1 } }')
  Initializers(1):
      IMemberInitializerOperation (OperationKind.MemberInitializer, Type: C2) (Syntax: '[1] = { A = 1 }')
        InitializedMember: 
          IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: '[1]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Initializer: 
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: '{ A = 1 }')
            Initializers(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'A')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ObjectCreationNestedCollectionInitializerNoSet()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    void M(C1 c1)
    {
        c1 = new C1() /*<bind>*/{ [1] = { 1 } }/*</bind>*/;
    }
    C2 this[int i] { get => null; }
}
class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw new System.NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
    public void Add(int i) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: '{ [1] = { 1 } }')
  Initializers(1):
      IMemberInitializerOperation (OperationKind.MemberInitializer, Type: C2) (Syntax: '[1] = { 1 }')
        InitializedMember: 
          IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[1]')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: '[1]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Initializer: 
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: '{ 1 }')
            Initializers(1):
                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: '[1]')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_01()
        {
            string source = @"
public class MyClass
{
    public MyClass(int i1, int i2, int i3) { }
    static void M(bool b)
    /*<bind>*/{
        var c = new MyClass(1, 2, b ? 3 : 4);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass c]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 3 : 4)')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 3 : 4)')
              Right: 
                IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 i1, System.Int32 i2, System.Int32 i3)) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ...  b ? 3 : 4)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: '2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i3) (OperationKind.Argument, Type: null) (Syntax: 'b ? 3 : 4')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 3 : 4')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_02()
        {
            string source = @"
public class MyClass
{
    public MyClass(int i1, int i2, int i3) { }
    static void M(bool b)
    /*<bind>*/{
        var c = new MyClass(i1: 1, i2: 2, i3: b ? 3 : 4);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass c]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 3 : 4)')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 3 : 4)')
              Right: 
                IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 i1, System.Int32 i2, System.Int32 i3)) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ...  b ? 3 : 4)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i1: 1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i2: 2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i3) (OperationKind.Argument, Type: null) (Syntax: 'i3: b ? 3 : 4')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 3 : 4')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_03()
        {
            string source = @"
public class MyClass
{
    public MyClass(int i1, int i2, int i3) { }
    static void M(bool b)
    /*<bind>*/{
        var c = new MyClass(i3: 3, i2: 2, i1: b ? 1 : 4);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass c]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 1 : 4)')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 1 : 4)')
              Right: 
                IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 i1, System.Int32 i2, System.Int32 i3)) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ...  b ? 1 : 4)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i3) (OperationKind.Argument, Type: null) (Syntax: 'i3: 3')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i2: 2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i1: b ? 1 : 4')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 4')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_04()
        {
            string source = @"
public class MyClass
{
    public MyClass(int i1, int i2, int i3 = 0) { }
    static void M(bool b)
    /*<bind>*/{
        var c = new MyClass(i1: 1, i2: b ? 2 : 3);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass c]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 2 : 3)')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ...  b ? 2 : 3)')
              Right: 
                IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 i1, System.Int32 i2, [System.Int32 i3 = 0])) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ...  b ? 2 : 3)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i1: 1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i2: b ? 2 : 3')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: i3) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new MyClass ...  b ? 2 : 3)')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'new MyClass ...  b ? 2 : 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_05()
        {
            string source = @"
public class MyClass
{
    public int A { get; set; }
    static void M()
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass a]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 MyClass.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_06()
        {
            string source = @"
public class MyClass
{
    public int A { get; set; }
    public int B { get; set; }
    static void M(bool b)
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1,
            B = b ? 2 : 3
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass a]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 MyClass.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = b ? 2 : 3')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 MyClass.B { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_07()
        {
            string source = @"
public class MyClass
{
    public object A { get; set; }
    static void M(bool b)
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1
        }?.A;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Object a]
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
                  Value: 
                    IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'A = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Object MyClass.A { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new MyClass ... }')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.A')
                  Value: 
                    IPropertyReferenceOperation: System.Object MyClass.A { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'a = new MyC ... }?.A')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'a = new MyC ... }?.A')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'new MyClass ... }?.A')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_08()
        {
            string source = @"
public class MyClass
{
    public MyClass(int a, int b) { }
    public object A { get; set; }
    public object B { get; set; }
    static void M(bool b)
    /*<bind>*/{
        var a = new MyClass(1, b ? 2 : 3)
        {
            A = 1,
            B = b ? 2 : 3
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [MyClass a]
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Jump if False (Regular) to Block[B3]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
                  Value: 
                    IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 a, System.Int32 b)) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'b ? 2 : 3')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'A = 1')
              Left: 
                IPropertyReferenceOperation: System.Object MyClass.A { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B6]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B9]
        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'B = b ? 2 : 3')
                  Left: 
                    IPropertyReferenceOperation: System.Object MyClass.B { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? 2 : 3')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B10]
                Leaving: {R3}
    }

    Block[B10] - Block
        Predecessors: [B9]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B11]
            Leaving: {R1}
}

Block[B11] - Exit
    Predecessors: [B10]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_09()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();

    public void M()
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_10()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = { P1 = 3, P2 = b ? 4 : 5 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 3')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C32')
                  Value: 
                    IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = b ? 4 : 5')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C32')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 4 : 5')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 4 : 5 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 4 : 5 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_11()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = b ? 1 : 5, P2 = 2 }, C32 = { P1 = 3, P2 = 4 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C31')
                  Value: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = b ? 1 : 5')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C31')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 5')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (4)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 3')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 4')
              Left: 
                IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 4 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 4 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_12()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
    }/*</bind>*/ };
    }
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,70): error CS1525: Invalid expression term '{'
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(8, 70),
                // (8,70): error CS1026: ) expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(8, 70),
                // (8,70): error CS1003: Syntax error, ':' expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":").WithLocation(8, 70),
                // (8,70): error CS1525: Invalid expression term '{'
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(8, 70),
                // (8,70): error CS1003: Syntax error, ',' expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(8, 70),
                // (8,88): error CS1003: Syntax error, ',' expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(8, 88),
                // (8,90): error CS1513: } expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(8, 90),
                // (8,90): error CS1513: } expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(8, 90),
                // (8,90): error CS1002: ; expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(8, 90),
                // (8,90): error CS1513: } expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(8, 90),
                // (8,93): error CS1525: Invalid expression term '{'
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(8, 93),
                // (8,93): error CS1026: ) expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(8, 93),
                // (8,93): error CS1002: ; expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(8, 93),
                // (8,101): error CS1002: ; expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 101),
                // (8,101): error CS1513: } expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 101),
                // (8,110): error CS1002: ; expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(8, 110),
                // (8,111): error CS1513: } expected
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 111),
                // (10,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(10, 5),
                // (11,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(11, 1),
                // (8,72): error CS0103: The name 'P1' does not exist in the current context
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P1").WithArguments("P1").WithLocation(8, 72),
                // (8,80): error CS0103: The name 'P2' does not exist in the current context
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P2").WithArguments("P2").WithLocation(8, 80),
                // (8,70): error CS0747: Invalid initializer member declarator
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "{ P1 = 3, P2 = 4 }").WithLocation(8, 70),
                // (8,95): error CS0103: The name 'P1' does not exist in the current context
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P1").WithArguments("P1").WithLocation(8, 95),
                // (8,103): error CS0103: The name 'P2' does not exist in the current context
                //         var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = b ? ({ P1 = 3, P2 = 4 }) : ({ P1 = 3, P2 = 4 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P2").WithArguments("P2").WithLocation(8, 103)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [C1 x]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
                  Value:
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
                      Arguments(0)
                      Initializer:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C3.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                      Instance Receiver:
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver:
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C3.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                      Instance Receiver:
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver:
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B2]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1] [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C2')
                      Value:
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
                Jump if False (Regular) to Block[B4]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                      Value:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                          Children(0)
                Next (Regular) Block[B5]
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                      Value:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                          Children(0)
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C3, IsInvalid) (Syntax: 'C32 = b ? (')
                      Left:
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'C2')
                      Right:
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C3, IsInvalid, IsImplicit) (Syntax: 'b ? (')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (NoConversion)
                          Operand:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'b ? (')
                Next (Regular) Block[B6]
                    Leaving: {R3}
        }
        Block[B6] - Block
            Predecessors: [B5]
            Statements (2)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{ P1 = 3, P2 = 4 }')
                  Children(2):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'P1 = 3')
                        Left:
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'P1')
                            Children(0)
                        Right:
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'P2 = 4')
                        Left:
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'P2')
                            Children(0)
                        Right:
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4, IsInvalid) (Syntax: '4')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... , P2 = 4 })')
                  Left:
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... , P2 = 4 })')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... , P2 = 4 })')
            Next (Regular) Block[B7]
                Leaving: {R2}
    }
    Block[B7] - Block
        Predecessors: [B6]
        Statements (3)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(')
              Expression:
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'P1 = 3')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'P1 = 3')
                  Left:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'P1')
                      Children(0)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'P2 = 4 ')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'P2 = 4')
                  Left:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'P2')
                      Children(0)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        Next (Regular) Block[B8]
            Leaving: {R1}
}
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_13()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [1] = 2, [3] = 4 }, C32 = { [5] = 6, [7] = 8 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (2)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[7] = 8')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[7]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '7')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '7')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '8')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

            Next (Regular) Block[B6]
                Leaving: {R5}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 8 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 8 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_14()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [1] = 2, [3] = 4 }, C32 = { [5] = 6, [b ? 7 : 8] = 9 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '8')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[b ? 7 : 8] = 9')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[b ? 7 : 8]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'b ? 7 : 8')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 7 : 8')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '9')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

            Next (Regular) Block[B9]
                Leaving: {R5}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 9 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 9 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_15()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [1] = 2, [3] = 4 }, C32 = { [5] = 6, [7] = b ? 8 : 9 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [4] [5] [6]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (2)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C32')
                  Value: 
                    IPropertyReferenceOperation: C3 C2.C32 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '8')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '9')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[7] = b ? 8 : 9')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[7]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C32')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '7')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '7')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? 8 : 9')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 8 : 9')

            Next (Regular) Block[B9]
                Leaving: {R5}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_16()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; } = new C2();


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { [""C3""] = b ? new C3 { [1] = 2, [3] = 4 } : new C3 { [5] = 6, [7] = b ? 8 : 9 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public object this[string i]
    {
        get => null;
        set { }
    }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '""C3""')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""C3"") (Syntax: '""C3""')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C2')
                  Value: 
                    IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Entering: {R6}

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [4]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                      Value: 
                        IObjectCreationOperation (Constructor: C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                          Arguments(0)
                          Initializer: 
                            null

                Next (Regular) Block[B4]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [5]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (2)
                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                    Next (Regular) Block[B5]
                        Leaving: {R4}
                        Entering: {R5}
            }
            .locals {R5}
            {
                CaptureIds: [6]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (2)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                    Next (Regular) Block[B6]
                        Leaving: {R5}
            }

            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                      Value: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')

                Next (Regular) Block[B14]
                    Leaving: {R3}
        }
        .locals {R6}
        {
            CaptureIds: [7]
            Block[B7] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                      Value: 
                        IObjectCreationOperation (Constructor: C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                          Arguments(0)
                          Initializer: 
                            null

                Next (Regular) Block[B8]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (2)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                                    IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

                    Next (Regular) Block[B9]
                        Leaving: {R7}
                        Entering: {R8}
            }
            .locals {R8}
            {
                CaptureIds: [9] [10]
                Block[B9] - Block
                    Predecessors: [B8]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

                    Jump if False (Regular) to Block[B11]
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                    Next (Regular) Block[B10]
                Block[B10] - Block
                    Predecessors: [B9]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '8')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

                    Next (Regular) Block[B12]
                Block[B11] - Block
                    Predecessors: [B9]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '9')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B10] [B11]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[7] = b ? 8 : 9')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[7]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '7')
                                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '7')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? 8 : 9')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 8 : 9')

                    Next (Regular) Block[B13]
                        Leaving: {R8}
            }

            Block[B13] - Block
                Predecessors: [B12]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                      Value: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')

                Next (Regular) Block[B14]
                    Leaving: {R6}
        }

        Block[B14] - Block
            Predecessors: [B6] [B13]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[""C3""] = b  ... b ? 8 : 9 }')
                  Left: 
                    IPropertyReferenceOperation: System.Object C2.this[System.String i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[""C3""]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'C2')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '""C3""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""C3"", IsImplicit) (Syntax: '""C3""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? new C3  ... b ? 8 : 9 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'b ? new C3  ... b ? 8 : 9 }')

            Next (Regular) Block[B15]
                Leaving: {R2}
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B15]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_17()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; }

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [GetInt(1)] = b ? new C1() : null } } };
    }/*</bind>*/

    int GetInt(int x) => x;
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ...  null } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(1)')
                  Value: 
                    IInvocationOperation ( System.Int32 C1.GetInt(System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(1)')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'GetInt')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C31')
                  Value: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1()')
                  Value: 
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[GetInt(1)] ... C1() : null')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[GetInt(1)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C31')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(1)')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(1)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? new C1() : null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'b ? new C1() : null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_18()
        {
            string source = @"
class C1
{
    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { [1, b ? 2 : 3] = null };
    }/*</bind>*/

    private C1 this[int i, int j]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [1 ... 3] = null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [1 ... 3] = null }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: '[1, b ? 2 : 3] = null')
                  Left: 
                    IPropertyReferenceOperation: C1 C1.this[System.Int32 i, System.Int32 j] { get; set; } (OperationKind.PropertyReference, Type: C1) (Syntax: '[1, b ? 2 : 3]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [1 ... 3] = null }')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null) (Syntax: 'b ? 2 : 3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 3] = null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 3] = null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [1 ... 3] = null }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_19()
        {
            string source = @"
class C1
{
    public C2 C2 { get; set; }

    public void M(bool b, int? i)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [i ?? GetInt(1)] = b ? new C1() : null } } };
    }/*</bind>*/

    int GetInt(int x) => x;
}

class C2
{
    public C2()
    {
    }

    public C3 C31 { get; set; }
    public C3 C32 { get; set; }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ...  null } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2] [3] [4]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                      Value: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(1)')
                  Value: 
                    IInvocationOperation ( System.Int32 C1.GetInt(System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(1)')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'GetInt')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C31')
                  Value: 
                    IPropertyReferenceOperation: C3 C2.C31 { get; set; } (OperationKind.PropertyReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1()')
                  Value: 
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[i ?? GetIn ... C1() : null')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[i ?? GetInt(1)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C31')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i ?? GetInt(1)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i ?? GetInt(1)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? new C1() : null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'b ? new C1() : null')

            Next (Regular) Block[B9]
                Leaving: {R2}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_20()
        {
            string source = @"
class C1
{
    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { O[b ? 1 : 2] = null };
    }/*</bind>*/

    public object[] O { get; set; }
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'C1' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var x = new C1 { O[b ? 1 : 2] = null };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ O[b ? 1 : 2] = null }").WithArguments("C1").WithLocation(6, 24),
                // CS0747: Invalid initializer member declarator
                //         var x = new C1 { O[b ? 1 : 2] = null };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "O[b ? 1 : 2] = null").WithLocation(6, 26)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { O[ ... 2] = null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { O[ ... 2] = null }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'O')
                  Value: 
                    IPropertyReferenceOperation: System.Object[] C1.O { get; set; } (OperationKind.PropertyReference, Type: System.Object[], IsInvalid) (Syntax: 'O')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'O')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'O[b ? 1 : 2] = null')
                  Children(1):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'O[b ? 1 : 2] = null')
                        Left: 
                          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Object, IsInvalid) (Syntax: 'O[b ? 1 : 2]')
                            Array reference: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object[], IsInvalid, IsImplicit) (Syntax: 'O')
                            Indices(1):
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b ? 1 : 2')
                        Right: 
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              (ImplicitReference)
                            Operand: 
                              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2] = null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2] = null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { O[ ... 2] = null }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_21()
        {
            string source = @"
public class MyClass
{
    public int A;
    static void M()
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass a]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 MyClass.A (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_22()
        {
            string source = @"
public class MyClass
{
    public int A;
    public int B;
    static void M(bool b)
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1,
            B = b ? 2 : 3
        };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass a]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 MyClass.A (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = b ? 2 : 3')
                  Left: 
                    IFieldReferenceOperation: System.Int32 MyClass.B (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'a = new MyC ... }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_23()
        {
            string source = @"
public class MyClass
{
    public object A;
    static void M(bool b)
    /*<bind>*/{
        var a = new MyClass
        {
            A = 1
        }?.A;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Object a]
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
                  Value: 
                    IObjectCreationOperation (Constructor: MyClass..ctor()) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... }')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'A = 1')
                  Left: 
                    IFieldReferenceOperation: System.Object MyClass.A (OperationKind.FieldReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new MyClass ... }')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.A')
                  Value: 
                    IFieldReferenceOperation: System.Object MyClass.A (OperationKind.FieldReference, Type: System.Object) (Syntax: '.A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'new MyClass ... }')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new MyClass ... }')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'new MyClass ... }')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'a = new MyC ... }?.A')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'a = new MyC ... }?.A')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'new MyClass ... }?.A')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_24()
        {
            string source = @"
class C1
{
    public C2 C2;

    public void M()
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public int P1;
    public int P2;
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C2.C32' is never assigned to, and will always have its default value null
                //     public C3 C32;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "C32").WithArguments("C2.C32", "null").WithLocation(19, 15)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 2 } } }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_25()
        {
            string source = @"
class C1
{
    public C2 C2;

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = 1, P2 = 2 }, C32 = { P1 = 3, P2 = b ? 4 : 5 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public int P1;
    public int P2;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 3')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C32')
                  Value: 
                    IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = b ? 4 : 5')
                  Left: 
                    IFieldReferenceOperation: System.Int32 C3.P2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C32')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 4 : 5')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 4 : 5 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 4 : 5 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 4 : 5 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_26()
        {
            string source = @"
class C1
{
    public C2 C2;

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { P1 = b ? 1 : 5, P2 = 2 }, C32 = { P1 = 3, P2 = 4 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public int P1;
    public int P2;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C31')
                  Value: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = b ? 1 : 5')
                  Left: 
                    IFieldReferenceOperation: System.Int32 C3.P1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C31')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 5')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (4)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 2')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P1 = 3')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P2 = 4')
              Left: 
                IFieldReferenceOperation: System.Int32 C3.P2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 4 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 2 = 4 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 2 = 4 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_27()
        {
            string source = @"
class C1
{
    public C2 C2;


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [1] = 2, [3] = 4 }, C32 = { [5] = 6, [7] = 8 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (2)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[7] = 8')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[7]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '7')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '7')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '8')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

            Next (Regular) Block[B6]
                Leaving: {R5}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 8 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 8 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 8 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_28()
        {
            string source = @"
class C1
{
    public C2 C2;


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [1] = 2, [3] = 4 }, C32 = { [5] = 6, [b ? 7 : 8] = 9 } } };
    }/*</bind>*/
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

            Next (Regular) Block[B5]
                Leaving: {R4}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '8')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[b ? 7 : 8] = 9')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[b ? 7 : 8]')
                      Instance Receiver: 
                        IFieldReferenceOperation: C3 C2.C32 (OperationKind.FieldReference, Type: C3) (Syntax: 'C32')
                          Instance Receiver: 
                            IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'b ? 7 : 8')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 7 : 8')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '9')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

            Next (Regular) Block[B9]
                Leaving: {R5}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 9 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... ] = 9 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... ] = 9 } } }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_29()
        {
            string source = @"
class C1
{
    public C2 C2;


    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { C2 = { [""C3""] = b ? new C3 { [1] = 2, [3] = 4 } : new C3 { [5] = 6, [7] = b ? 8 : 9 } } };
    }/*</bind>*/
}

class C2
{
    public object this[string i]
    {
        get => null;
        set { }
    }
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '""C3""')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""C3"") (Syntax: '""C3""')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C2')
                  Value: 
                    IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                Entering: {R6}

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [4]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                      Value: 
                        IObjectCreationOperation (Constructor: C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                          Arguments(0)
                          Initializer: 
                            null

                Next (Regular) Block[B4]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [5]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (2)
                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[1] = 2')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[1]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                    Next (Regular) Block[B5]
                        Leaving: {R4}
                        Entering: {R5}
            }
            .locals {R5}
            {
                CaptureIds: [6]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (2)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[3] = 4')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[3]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '4')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                    Next (Regular) Block[B6]
                        Leaving: {R5}
            }

            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')
                      Value: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [1 ... , [3] = 4 }')

                Next (Regular) Block[B14]
                    Leaving: {R3}
        }
        .locals {R6}
        {
            CaptureIds: [7]
            Block[B7] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                      Value: 
                        IObjectCreationOperation (Constructor: C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                          Arguments(0)
                          Initializer: 
                            null

                Next (Regular) Block[B8]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (2)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[5] = 6')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[5]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '5')
                                    IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '5')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '6')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

                    Next (Regular) Block[B9]
                        Leaving: {R7}
                        Entering: {R8}
            }
            .locals {R8}
            {
                CaptureIds: [9] [10]
                Block[B9] - Block
                    Predecessors: [B8]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '7')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

                    Jump if False (Regular) to Block[B11]
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                    Next (Regular) Block[B10]
                Block[B10] - Block
                    Predecessors: [B9]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '8')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

                    Next (Regular) Block[B12]
                Block[B11] - Block
                    Predecessors: [B9]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '9')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B10] [B11]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[7] = b ? 8 : 9')
                          Left: 
                            IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[7]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '7')
                                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '7')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? 8 : 9')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 8 : 9')

                    Next (Regular) Block[B13]
                        Leaving: {R8}
            }

            Block[B13] - Block
                Predecessors: [B12]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')
                      Value: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3 { [5 ... b ? 8 : 9 }')

                Next (Regular) Block[B14]
                    Leaving: {R6}
        }

        Block[B14] - Block
            Predecessors: [B6] [B13]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[""C3""] = b  ... b ? 8 : 9 }')
                  Left: 
                    IPropertyReferenceOperation: System.Object C2.this[System.String i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[""C3""]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'C2')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '""C3""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""C3"", IsImplicit) (Syntax: '""C3""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? new C3  ... b ? 8 : 9 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'b ? new C3  ... b ? 8 : 9 }')

            Next (Regular) Block[B15]
                Leaving: {R2}
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... 8 : 9 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ... 8 : 9 } } }')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B15]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_30()
        {
            string source = @"
class C1
{
    public C2 C2;

    public void M(bool b, int? i)
    /*<bind>*/{
        var x = new C1 { C2 = { C31 = { [i ?? GetInt(1)] = b ? new C1() : null } } };
    }/*</bind>*/

    int GetInt(int x) => x;
}

class C2
{
    public C2()
    {
    }

    public C3 C31;
    public C3 C32;
}

class C3
{
    public object this[int i]
    {
        get => null;
        set { }
    }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C2.C32' is never assigned to, and will always have its default value null
                //     public C3 C32;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "C32").WithArguments("C2.C32", "null").WithLocation(21, 15)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { C2 ...  null } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2] [3] [4]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                      Value: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(1)')
                  Value: 
                    IInvocationOperation ( System.Int32 C1.GetInt(System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(1)')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'GetInt')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C31')
                  Value: 
                    IFieldReferenceOperation: C3 C2.C31 (OperationKind.FieldReference, Type: C3) (Syntax: 'C31')
                      Instance Receiver: 
                        IFieldReferenceOperation: C2 C1.C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'C2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1()')
                  Value: 
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '[i ?? GetIn ... C1() : null')
                  Left: 
                    IPropertyReferenceOperation: System.Object C3.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: '[i ?? GetInt(1)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'C31')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i ?? GetInt(1)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i ?? GetInt(1)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b ? new C1() : null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'b ? new C1() : null')

            Next (Regular) Block[B9]
                Leaving: {R2}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ...  null } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { C2 ...  null } } }')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_31()
        {
            string source = @"
using System;

class C1
{
    public event EventHandler<object> ev;

    public void M(bool b)
    /*<bind>*/{
        var x = new C1 { ev = null };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0067: The event 'C1.ev' is never used
                //     public event EventHandler<object> ev;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "ev").WithArguments("C1.ev").WithLocation(6, 39)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { ev = null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { ev = null }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler<System.Object>) (Syntax: 'ev = null')
              Left: 
                IEventReferenceOperation: event System.EventHandler<System.Object> C1.ev (OperationKind.EventReference, Type: System.EventHandler<System.Object>) (Syntax: 'ev')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ev = null }')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler<System.Object>, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1 { ev = null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1 { ev = null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ev = null }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_32()
        {
            string source = @"
using System;

class C1
{
    public event EventHandler<object> ev;

    public void M(bool b, object o)
    /*<bind>*/{
        var x = new C1 { ev = b ? (EventHandler<object>)o : null };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0067: The event 'C1.ev' is never used
                //     public event EventHandler<object> ev;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "ev").WithArguments("C1.ev").WithLocation(6, 39)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { ev ... )o : null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { ev ... )o : null }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(EventHandler<object>)o')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler<System.Object>) (Syntax: '(EventHandler<object>)o')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler<System.Object>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler<System.Object>) (Syntax: 'ev = b ? (E ... t>)o : null')
                  Left: 
                    IEventReferenceOperation: event System.EventHandler<System.Object> C1.ev (OperationKind.EventReference, Type: System.EventHandler<System.Object>) (Syntax: 'ev')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ev ... )o : null }')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler<System.Object>, IsImplicit) (Syntax: 'b ? (EventH ... t>)o : null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... )o : null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'x = new C1  ... )o : null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ev ... )o : null }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_33()
        {
            string source = @"
using System;

class C1
{
    public C2 C2 { get; set; }

    public void M(bool b, object o)
    /*<bind>*/{
        var x = new C1 { C2 = { ev = b ? (EventHandler<object>)o : null } };
    }/*</bind>*/
}

class C2
{
    public event EventHandler<object> ev;
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0070: The event 'C2.ev' can only appear on the left hand side of += or -= (except when used from within the type 'C2')
                //         var x = new C1 { C2 = { ev = b ? (EventHandler<object>)o : null } };
                Diagnostic(ErrorCode.ERR_BadEventUsage, "ev").WithArguments("C2.ev", "C2").WithLocation(10, 33),
                // CS0067: The event 'C2.ev' is never used
                //     public event EventHandler<object> ev;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "ev").WithArguments("C2.ev").WithLocation(16, 39)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ...  : null } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { C2 ...  : null } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C2')
                  Value: 
                    IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ...  : null } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(EventHandler<object>)o')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler<System.Object>) (Syntax: '(EventHandler<object>)o')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler<System.Object>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler<System.Object>, IsInvalid) (Syntax: 'ev = b ? (E ... t>)o : null')
                  Left: 
                    IEventReferenceOperation: event System.EventHandler<System.Object> C2.ev (OperationKind.EventReference, Type: System.EventHandler<System.Object>, IsInvalid) (Syntax: 'ev')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'C2')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler<System.Object>, IsImplicit) (Syntax: 'b ? (EventH ... t>)o : null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ...  : null } }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ...  : null } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ...  : null } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_34()
        {
            string source = @"
using System;

class C1
{
    public object P1 { get; set; }
    public object P2 { get; set; }

    public void M(bool b, object o)
    /*<bind>*/{
        var x = new C1 { P1 = null, (P1 ?? P2) = null };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         var x = new C1 { P1 = null, (P1 ?? P2) = null };
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "P1 ?? P2").WithLocation(11, 38),
                // CS0747: Invalid initializer member declarator
                //         var x = new C1 { P1 = null, (P1 ?? P2) = null };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "(P1 ?? P2) = null").WithLocation(11, 37)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { P1 ... 2) = null }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'P1 = null')
              Left: 
                IPropertyReferenceOperation: System.Object C1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'P1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Value: 
                        IPropertyReferenceOperation: System.Object C1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Object, IsInvalid) (Syntax: 'P1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'P1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P2')
                  Value: 
                    IPropertyReferenceOperation: System.Object C1.P2 { get; set; } (OperationKind.PropertyReference, Type: System.Object, IsInvalid) (Syntax: 'P2')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'P2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: '(P1 ?? P2) = null')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1 ?? P2')
                      Children(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1 ?? P2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2) = null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2) = null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_35()
        {
            string source = @"
using System;

class C1
{
    public object P1 { get; set; }
    public object P2;

    public void M(bool b, object o)
    /*<bind>*/{
        var x = new C1 { P1 = null, (P1 ?? P2) = null };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         var x = new C1 { P1 = null, (P1 ?? P2) = null };
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "P1 ?? P2").WithLocation(11, 38),
                // CS0747: Invalid initializer member declarator
                //         var x = new C1 { P1 = null, (P1 ?? P2) = null };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "(P1 ?? P2) = null").WithLocation(11, 37),
                // CS0649: Field 'C1.P2' is never assigned to, and will always have its default value null
                //     public object P2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "P2").WithArguments("C1.P2", "null").WithLocation(7, 19)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C1 x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { P1 ... 2) = null }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'P1 = null')
              Left: 
                IPropertyReferenceOperation: System.Object C1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'P1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Value: 
                        IPropertyReferenceOperation: System.Object C1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Object, IsInvalid) (Syntax: 'P1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'P1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P1')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'P2')
                  Value: 
                    IFieldReferenceOperation: System.Object C1.P2 (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'P2')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'P2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: '(P1 ?? P2) = null')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1 ?? P2')
                      Children(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'P1 ?? P2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2) = null }')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'x = new C1  ... 2) = null }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { P1 ... 2) = null }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_36()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, object o)
    /*<bind>*/{
        Class c = new Class { [GetInt()] = { C = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2 this[int i]
    {
        get => null;
        set { }
    }

    int GetInt() => 1;
}

class C2
{
    public C2 C { get; set; }
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                  Value: 
                    IInvocationOperation ( System.Int32 Class.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                      Arguments(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C2.C { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 Class.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt()]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt()')
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C2.C { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 Class.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt()]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt()')
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_37()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, object o)
    /*<bind>*/{
        Class c = new Class { (C21 ?? C22).I1 = 1 };
    }/*</bind>*/

    public C2 C21 { get; set; }
    public C2 C22 { get; set; }
}

class C2
{
    public int I1 { get; set; }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1922: Cannot initialize type 'Class' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         Class c = new Class { (C21 ?? C22).I1 = 1 };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ (C21 ?? C22).I1 = 1 }").WithArguments("Class").WithLocation(8, 29),
                // CS0747: Invalid initializer member declarator
                //         Class c = new Class { (C21 ?? C22).I1 = 1 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "(C21 ?? C22).I1 = 1").WithLocation(8, 31)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Class { ... 2).I1 = 1 }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class, IsInvalid) (Syntax: 'new Class { ... 2).I1 = 1 }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'C21')
                      Value: 
                        IPropertyReferenceOperation: C2 Class.C21 { get; set; } (OperationKind.PropertyReference, Type: C2, IsInvalid) (Syntax: 'C21')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'C21')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'C21')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'C21')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'C21')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'C21')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'C22')
                  Value: 
                    IPropertyReferenceOperation: C2 Class.C22 { get; set; } (OperationKind.PropertyReference, Type: C2, IsInvalid) (Syntax: 'C22')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'C22')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '(C21 ?? C22).I1 = 1')
                  Children(1):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '(C21 ?? C22).I1 = 1')
                        Left: 
                          IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '(C21 ?? C22).I1')
                            Instance Receiver: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'C21 ?? C22')
                        Right: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ... 2).I1 = 1 }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ... 2).I1 = 1 }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ... 2).I1 = 1 }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_38()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, object o)
    /*<bind>*/{
        Class c = new Class { [GetInt() ?? 3] = { C = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2 this[int i]
    {
        get => null;
        set { }
    }

    int? GetInt() => 1;
}

class C2
{
    public C2 C { get; set; }
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                          Arguments(0)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetInt()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C2.C { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 Class.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt() ?? 3]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt() ?? 3')
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C2.C { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C')
                          Instance Receiver: 
                            IPropertyReferenceOperation: C2 Class.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt() ?? 3]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt() ?? 3')
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_39()
        {
            string source = @"
internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C21 = { I1 = 1, I2 = 2 } };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'Class' does not contain a definition for 'C21'
                //         Class c = new Class { C21 = { I1 = 1, I2 = 2 } };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "C21").WithArguments("Class", "C21").WithLocation(6, 31)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class, IsInvalid) (Syntax: 'new Class { ...  I2 = 2 } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I1 = 1')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I1')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'I1')
                        Children(1):
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'C21')
                              Children(1):
                                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C21')
                                    Children(1):
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I2 = 2')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I2')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'I2')
                        Children(1):
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'C21')
                              Children(1):
                                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C21')
                                    Children(1):
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 2 } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 2 } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_40()
        {
            string source = @"
internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { [GetInt()] = { I1 = 1, I2 = 2 } };
    }/*</bind>*/

    int GetInt() => 1;
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0021: Cannot apply indexing with [] to an expression of type 'Class'
                //         Class c = new Class { [GetInt()] = { I1 = 1, I2 = 2 } };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[GetInt()]").WithArguments("Class").WithLocation(6, 31)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class, IsInvalid) (Syntax: 'new Class { ...  I2 = 2 } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I1 = 1')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I1')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'I1')
                        Children(1):
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[GetInt()]')
                              Children(2):
                                  IInvocationOperation ( System.Int32 Class.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'GetInt')
                                    Arguments(0)
                                  IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I2 = 2')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I2')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'I2')
                        Children(1):
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[GetInt()]')
                              Children(2):
                                  IInvocationOperation ( System.Int32 Class.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'GetInt')
                                    Arguments(0)
                                  IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 2 } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 2 } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 2 } }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_41()
        {
            string source = @"
internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { [GetInt() ?? 1] = { I1 = 2, I2 = 3 } };
    }/*</bind>*/

    int? GetInt() => 1;
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0021: Cannot apply indexing with [] to an expression of type 'Class'
                //         Class c = new Class { [GetInt() ?? 1] = { I1 = 1, I2 = 2 } };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[GetInt() ?? 1]").WithArguments("Class").WithLocation(6, 31)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class, IsInvalid) (Syntax: 'new Class { ...  I2 = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?, IsInvalid) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'GetInt')
                          Arguments(0)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'GetInt()')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I1 = 2')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I1')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null) (Syntax: 'I1')
                            Children(1):
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[GetInt() ?? 1]')
                                  Children(2):
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'GetInt() ?? 1')
                                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 3 } }')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: 'I2 = 3')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'I2')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null) (Syntax: 'I2')
                            Children(1):
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[GetInt() ?? 1]')
                                  Children(2):
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'GetInt() ?? 1')
                                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 3 } }')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 3 } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'c = new Cla ...  I2 = 3 } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  I2 = 3 } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_42()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, object o)
    /*<bind>*/{
        Class c = new Class { C2s = { [GetInt()] = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2[] C2s { get; set; }
    public int GetInt() => 0;
}

class C2
{
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                  Value: 
                    IInvocationOperation ( System.Int32 Class.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                      Arguments(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt()]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt()]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_43()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, object o)
    /*<bind>*/{
        Class c = new Class { C2s = { [GetInt() ?? 0] = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2[] C2s { get; set; }
    public int? GetInt() => 0;
}

class C2
{
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                          Arguments(0)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetInt()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_44()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C2s = { [0] = { I1 = b ? 1 : 2, I2 = b ? 3 : 4 } } };
    }/*</bind>*/

    public C2[] C2s { get; set; }
    public int? GetInt() => 0;
}

class C2
{
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 3 : 4 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 3 : 4 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [2] [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[0]')
                      Value: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[0]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 3 : 4 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B6]
            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = b ? 1 : 2')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[0]')
                      Right: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 2')

                Next (Regular) Block[B7]
                    Leaving: {R3}
                    Entering: {R4}
        }
        .locals {R4}
        {
            CaptureIds: [4] [5]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[0]')
                      Value: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[0]')
                          Array reference: 
                            IPropertyReferenceOperation: C2[] Class.C2s { get; set; } (OperationKind.PropertyReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 3 : 4 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

                Jump if False (Regular) to Block[B9]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B10]
            Block[B9] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B10]
            Block[B10] - Block
                Predecessors: [B8] [B9]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = b ? 3 : 4')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[0]')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 3 : 4')

                Next (Regular) Block[B11]
                    Leaving: {R4} {R2}
        }
    }

    Block[B11] - Block
        Predecessors: [B10]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 3 : 4 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 3 : 4 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 3 : 4 } } }')

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_45()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C2s = { [GetInt()] = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2[] C2s;
    public int GetInt() => 0;
}

class C2
{
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                  Value: 
                    IInvocationOperation ( System.Int32 Class.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                      Arguments(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt()]')
                          Array reference: 
                            IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt()]')
                          Array reference: 
                            IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_46()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C2s = { [GetInt() ?? 0] = { I1 = 1, I2 = 2 } } };
    }/*</bind>*/

    public C2[] C2s;
    public int? GetInt() => 0;
}

class C2
{
    public int I1 { get; set; }
    public int I2 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... 2 = 2 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                          Arguments(0)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetInt()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                          Array reference: 
                            IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = 2')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C2.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                          Array reference: 
                            IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... 2 = 2 } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... 2 = 2 } } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_47()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C2s = { [GetInt() ?? 0] = { I1 = { [1] = 2, [3] = 4 } } } };
    }/*</bind>*/

    public C2[] C2s;
    public int? GetInt() => 0;
}

class C2
{
    public int[] I1 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... = 4 } } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... = 4 } } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                          Arguments(0)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetInt()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
                Entering: {R4}

        .locals {R4}
        {
            CaptureIds: [3]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (2)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[1] = 2')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[1]')
                          Array reference: 
                            IPropertyReferenceOperation: System.Int32[] C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[]) (Syntax: 'I1')
                              Instance Receiver: 
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                                  Array reference: 
                                    IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 4 } } } }')
                                  Indices(1):
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B6]
                    Leaving: {R4}
                    Entering: {R5}
        }
        .locals {R5}
        {
            CaptureIds: [4]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (2)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[3] = 4')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[3]')
                          Array reference: 
                            IPropertyReferenceOperation: System.Int32[] C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[]) (Syntax: 'I1')
                              Instance Receiver: 
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[GetInt() ?? 0]')
                                  Array reference: 
                                    IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 4 } } } }')
                                  Indices(1):
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 0')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B7]
                    Leaving: {R5} {R2}
        }
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... = 4 } } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... = 4 } } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 4 } } } }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_48()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool b)
    /*<bind>*/{
        Class c = new Class { C2s = { [0] = { I1 = { [GetInt() ?? 1] = 2, [b ? 3 : 4] = 5 } } } };
    }/*</bind>*/

    public C2[] C2s;
    public int? GetInt() => 0;
}

class C2
{
    public int[] I1 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [Class c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ... = 5 } } } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ... = 5 } } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B3]
                Entering: {R3} {R4}

        .locals {R3}
        {
            CaptureIds: [3]
            .locals {R4}
            {
                CaptureIds: [2]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                          Value: 
                            IInvocationOperation ( System.Int32? Class.GetInt()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'GetInt()')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'GetInt')
                              Arguments(0)

                    Jump if True (Regular) to Block[B5]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetInt()')
                          Operand: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt()')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'GetInt()')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'GetInt()')
                              Arguments(0)

                    Next (Regular) Block[B6]
                        Leaving: {R4}
            }

            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[GetInt() ?? 1] = 2')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[GetInt() ?? 1]')
                          Array reference: 
                            IPropertyReferenceOperation: System.Int32[] C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[]) (Syntax: 'I1')
                              Instance Receiver: 
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[0]')
                                  Array reference: 
                                    IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 5 } } } }')
                                  Indices(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt() ?? 1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B7]
                    Leaving: {R3}
                    Entering: {R5}
        }
        .locals {R5}
        {
            CaptureIds: [4]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (0)
                Jump if False (Regular) to Block[B9]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B10]
            Block[B9] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B10]
            Block[B10] - Block
                Predecessors: [B8] [B9]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[b ? 3 : 4] = 5')
                      Left: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[b ? 3 : 4]')
                          Array reference: 
                            IPropertyReferenceOperation: System.Int32[] C2.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[]) (Syntax: 'I1')
                              Instance Receiver: 
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C2) (Syntax: '[0]')
                                  Array reference: 
                                    IFieldReferenceOperation: C2[] Class.C2s (OperationKind.FieldReference, Type: C2[]) (Syntax: 'C2s')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 5 } } } }')
                                  Indices(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')
                          Indices(1):
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 3 : 4')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                Next (Regular) Block[B11]
                    Leaving: {R5} {R2}
        }
    }

    Block[B11] - Block
        Predecessors: [B10]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... = 5 } } } }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: Class, IsImplicit) (Syntax: 'c = new Cla ... = 5 } } } }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ... = 5 } } } }')

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_49()
        {
            string source = @"
internal class Class
{
    public void M(Class c, int i, bool b)
    /*<bind>*/{
        c = new Class { P1 = { [(i = 3), i] = 3 } };
    }/*</bind>*/

    public int[,] P1 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: Class) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ...  i] = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i = 3')
                  Value: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 3')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[(i = 3), i] = 3')
                  Left: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[(i = 3), i]')
                      Array reference: 
                        IPropertyReferenceOperation: System.Int32[,] Class.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[,]) (Syntax: 'P1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 3')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new Cla ... i] = 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class) (Syntax: 'c = new Cla ...  i] = 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_50()
        {
            string source = @"
internal class Class
{
    public void M(Class c, int i, bool b)
    /*<bind>*/{
        c = new Class { P1 = { [b ? 1 : 2, i] = 3 } };
    }/*</bind>*/

    public int[,] P1 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: Class) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ...  i] = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[b ? 1 : 2, i] = 3')
                  Left: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[b ? 1 : 2, i]')
                      Array reference: 
                        IPropertyReferenceOperation: System.Int32[,] Class.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[,]) (Syntax: 'P1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 2')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new Cla ... i] = 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class) (Syntax: 'c = new Cla ...  i] = 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  i] = 3 } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_51()
        {
            string source = @"
internal class Class
{
    public void M(Class c, int i, bool b)
    /*<bind>*/{
        c = new Class { P1 = { [i, b ? 1 : 2] = 3 } };
    }/*</bind>*/

    public int[,] P1 { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: Class) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Class { ...  2] = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class { ...  2] = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[i, b ? 1 : 2] = 3')
                  Left: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[i, b ? 1 : 2]')
                      Array reference: 
                        IPropertyReferenceOperation: System.Int32[,] Class.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[,]) (Syntax: 'P1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  2] = 3 } }')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new Cla ... 2] = 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class) (Syntax: 'c = new Cla ...  2] = 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'new Class { ...  2] = 3 } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_52()
        {
            string source = @"
internal class Class
{
    public void M(Class c, int i, bool b)
    /*<bind>*/{
        c = new Class { P1 = { [] = 3 } };
    }/*</bind>*/

    public int[,] P1 { get; set; }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0443: Syntax error; value expected
                //         c = new Class { P1 = { [] = 3 } };
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 33)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: Class) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  [] = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class, IsInvalid) (Syntax: 'new Class { ...  [] = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                      Children(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '[] = 3')
                  Left: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32, IsInvalid) (Syntax: '[]')
                      Array reference: 
                        IPropertyReferenceOperation: System.Int32[,] Class.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32[,]) (Syntax: 'P1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  [] = 3 } }')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = new Cla ... [] = 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Class, IsInvalid) (Syntax: 'c = new Cla ...  [] = 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Class, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Class, IsInvalid, IsImplicit) (Syntax: 'new Class { ...  [] = 3 } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_53()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    /*<bind>*/public static void M(C c)
    {
        c = new C { 1, 2, 3 };
    }/*</bind>*/

    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int i) {}
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (6)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C { 1, 2, 3 }')
              Value: 
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C { 1, 2, 3 }')
                  Arguments(0)
                  Initializer: 
                    null

            IInvocationOperation ( void C.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C { 1, 2, 3 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( void C.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C { 1, 2, 3 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( void C.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C { 1, 2, 3 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C { 1, 2, 3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = new C { 1, 2, 3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C { 1, 2, 3 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_54()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c)
    {
        c = new C1 { [GetInt(0)] = { GetInt(1), GetInt(2), GetInt(3) } };
    }/*</bind>*/

    C2 this[int i] { get => null; set { } }
    static int GetInt(int i) => i;
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int i) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [G ... tInt(3) } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [G ... tInt(3) } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(0)')
                  Value: 
                    IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(0)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(1)')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... tInt(3) } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(1)')
                        IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(1)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(2)')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... tInt(3) } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(2)')
                        IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(2)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(3)')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... tInt(3) } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(3)')
                        IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(3)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... Int(3) } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... tInt(3) } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... tInt(3) } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_55()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int? i1, int? i2, int? i3)
    {
        c = new C1 { [GetInt(i1 ?? 0)] = { GetInt(i2 ?? 1), 2 } };
    }/*</bind>*/

    C2 this[int i] { get => null; set { } }
    static int GetInt(int i) => i;
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int i) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [G ... ? 1), 2 } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [G ... ? 1), 2 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4}

    .locals {R2}
    {
        CaptureIds: [4]
        .locals {R3}
        {
            CaptureIds: [3]
            .locals {R4}
            {
                CaptureIds: [2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                          Value: 
                            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

                    Jump if True (Regular) to Block[B4]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                        Leaving: {R4}

                    Next (Regular) Block[B3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                              Arguments(0)

                    Next (Regular) Block[B5]
                        Leaving: {R4}
            }

            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(i1 ?? 0)')
                      Value: 
                        IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i1 ?? 0)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B6]
                    Leaving: {R3}
                    Entering: {R5}
        }
        .locals {R5}
        {
            CaptureIds: [5] [7]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[GetInt(i1 ?? 0)]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(i1 ?? 0)]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... ? 1), 2 } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(i1 ?? 0)')
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(i1 ?? 0)')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B7]
                    Entering: {R6}

            .locals {R6}
            {
                CaptureIds: [6]
                Block[B7] - Block
                    Predecessors: [B6]
                    Statements (1)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

                    Jump if True (Regular) to Block[B9]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                        Leaving: {R6}

                    Next (Regular) Block[B8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                              Arguments(0)

                    Next (Regular) Block[B10]
                        Leaving: {R6}
            }

            Block[B9] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B10]
            Block[B10] - Block
                Predecessors: [B8] [B9]
                Statements (1)
                    IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[GetInt(i1 ?? 0)]')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i2 ?? 1)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2 ?? 1')
                                    IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? 1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B11]
                    Leaving: {R5}
        }

        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(i1 ?? 0)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... ? 1), 2 } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(i1 ?? 0)')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(i1 ?? 0)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B12]
                Leaving: {R2}
    }

    Block[B12] - Block
        Predecessors: [B11]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ...  1), 2 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... ? 1), 2 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... ? 1), 2 } }')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B12]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_56()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int? i1, int? i2, int? i3)
    {
        c = new C1 { [i1 ?? 0] = { GetInt(i2 ?? 1), { 3, GetInt(i3 ?? 4) } } };
    }/*</bind>*/
    C2 this[int i] { get => null; set { } }
    static int GetInt(int i) => i;
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

static class C2Extensions
{
    public static void Add(this C2 c2, int i) { }
    public static void Add(this C2 c2, int i, int j) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
                Entering: {R4}

        .locals {R4}
        {
            CaptureIds: [4] [6]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B6]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [5]
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

                    Jump if True (Regular) to Block[B8]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                        Leaving: {R5}

                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B6]
                    Statements (1)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                              Arguments(0)

                    Next (Regular) Block[B9]
                        Leaving: {R5}
            }

            Block[B8] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B7] [B8]
                Statements (1)
                    IInvocationOperation (void C2Extensions.Add(this C2 c2, System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                      Instance Receiver: 
                        null
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i2 ?? 1)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2 ?? 1')
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? 1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B10]
                    Leaving: {R4}
                    Entering: {R6}
        }
        .locals {R6}
        {
            CaptureIds: [7] [8] [10]
            Block[B10] - Block
                Predecessors: [B9]
                Statements (2)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B11]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [9]
                Block[B11] - Block
                    Predecessors: [B10]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i3')

                    Jump if True (Regular) to Block[B13]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i3')
                          Operand: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                        Leaving: {R7}

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B11]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                              Arguments(0)

                    Next (Regular) Block[B14]
                        Leaving: {R7}
            }

            Block[B13] - Block
                Predecessors: [B11]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B14]
            Block[B14] - Block
                Predecessors: [B12] [B13]
                Statements (1)
                    IInvocationOperation (void C2Extensions.Add(this C2 c2, System.Int32 i, System.Int32 j)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                      Instance Receiver: 
                        null
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i3 ?? 4)')
                            IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i3 ?? 4)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i3 ?? 4')
                                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3 ?? 4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B15]
                    Leaving: {R6} {R2}
        }
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... ? 4) } } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... ?? 4) } } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B15]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_57()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int? i1, int? i2, int? i3)
    {
        c = new C1 { [i1 ?? 0] = { GetInt(i2 ?? 1), { 3, GetInt(i3 ?? 4) } } };
    }/*</bind>*/
    C2 this[int i] { get => null; set { } }
    static int GetInt(int i) => i;
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int i, int j = -1) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
                Entering: {R4}

        .locals {R4}
        {
            CaptureIds: [4] [6]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B6]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [5]
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

                    Jump if True (Regular) to Block[B8]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                        Leaving: {R5}

                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B6]
                    Statements (1)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                              Arguments(0)

                    Next (Regular) Block[B9]
                        Leaving: {R5}
            }

            Block[B8] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B7] [B8]
                Statements (1)
                    IInvocationOperation ( void C2.Add(System.Int32 i, [System.Int32 j = -1])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i2 ?? 1)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2 ?? 1')
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? 1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: j) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: -1, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B10]
                    Leaving: {R4}
                    Entering: {R6}
        }
        .locals {R6}
        {
            CaptureIds: [7] [8] [10]
            Block[B10] - Block
                Predecessors: [B9]
                Statements (2)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B11]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [9]
                Block[B11] - Block
                    Predecessors: [B10]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i3')

                    Jump if True (Regular) to Block[B13]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i3')
                          Operand: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                        Leaving: {R7}

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B11]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                              Arguments(0)

                    Next (Regular) Block[B14]
                        Leaving: {R7}
            }

            Block[B13] - Block
                Predecessors: [B11]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B14]
            Block[B14] - Block
                Predecessors: [B12] [B13]
                Statements (1)
                    IInvocationOperation ( void C2.Add(System.Int32 i, [System.Int32 j = -1])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i3 ?? 4)')
                            IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i3 ?? 4)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i3 ?? 4')
                                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3 ?? 4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B15]
                    Leaving: {R6} {R2}
        }
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... ? 4) } } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... ?? 4) } } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B15]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_58()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int? i1, int? i2, int? i3)
    {
        c = new C1 { [i1 ?? 0] = { GetInt(i2 ?? 1), { 3, GetInt(i3 ?? 4) } } };
    }/*</bind>*/
    C2 this[int i] { get => null; set { } }
    static int GetInt(int i) => i;
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(params int[] @is) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B5]
                Entering: {R4}

        .locals {R4}
        {
            CaptureIds: [4] [5] [7]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (2)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')

                Next (Regular) Block[B6]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [6]
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

                    Jump if True (Regular) to Block[B8]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                        Leaving: {R5}

                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B6]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                              Arguments(0)

                    Next (Regular) Block[B9]
                        Leaving: {R5}
            }

            Block[B8] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B7] [B8]
                Statements (1)
                    IInvocationOperation ( void C2.Add(params System.Int32[] @is)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: is) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                              Dimension Sizes(1):
                                  IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                              Initializer: 
                                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'GetInt(i2 ?? 1)')
                                  Element Values(1):
                                      IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i2 ?? 1)')
                                        Instance Receiver: 
                                          null
                                        Arguments(1):
                                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2 ?? 1')
                                              IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? 1')
                                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B10]
                    Leaving: {R4}
                    Entering: {R6}
        }
        .locals {R6}
        {
            CaptureIds: [8] [9] [10] [12]
            Block[B10] - Block
                Predecessors: [B9]
                Statements (3)
                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[i1 ?? 0]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1 ?? 0')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 0')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')

                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B11]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [11]
                Block[B11] - Block
                    Predecessors: [B10]
                    Statements (1)
                        IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i3')

                    Jump if True (Regular) to Block[B13]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i3')
                          Operand: 
                            IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                        Leaving: {R7}

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B11]
                    Statements (1)
                        IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                              Arguments(0)

                    Next (Regular) Block[B14]
                        Leaving: {R7}
            }

            Block[B13] - Block
                Predecessors: [B11]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B14]
            Block[B14] - Block
                Predecessors: [B12] [B13]
                Statements (1)
                    IInvocationOperation ( void C2.Add(params System.Int32[] @is)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[i1 ?? 0]')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: is) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                              Dimension Sizes(1):
                                  IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                              Initializer: 
                                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{ 3, GetInt(i3 ?? 4) }')
                                  Element Values(2):
                                      IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                                      IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(i3 ?? 4)')
                                        Instance Receiver: 
                                          null
                                        Arguments(1):
                                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i3 ?? 4')
                                              IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3 ?? 4')
                                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B15]
                    Leaving: {R6} {R2}
        }
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... ? 4) } } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... ?? 4) } } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [i ... ?? 4) } } }')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B15]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_59()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<C2>
{
    /*<bind>*/public static void M(C1 c, int? i1, int? i2)
    {
        c = new C1 { new C2() { i1 ?? 1, 2 }, new C2() { 3, i2 ?? 4 } };
    }/*</bind>*/
    public IEnumerator<C2> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(C2 c2) { }
}

class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int i) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { ne ... i2 ?? 4 } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { ne ... i2 ?? 4 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                  Value: 
                    IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B3]
                Entering: {R3} {R4}

        .locals {R3}
        {
            CaptureIds: [4]
            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                          Value: 
                            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

                    Jump if True (Regular) to Block[B5]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                              Arguments(0)

                    Next (Regular) Block[B6]
                        Leaving: {R4}
            }

            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i1 ?? 1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i1 ?? 1')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? 1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B7]
                    Leaving: {R3}
        }

        Block[B7] - Block
            Predecessors: [B6]
            Statements (2)
                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IInvocationOperation ( void C1.Add(C2 c2)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ne ... i2 ?? 4 } }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { i1 ?? 1, 2 }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B8]
                Leaving: {R2}
                Entering: {R5}
    }
    .locals {R5}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (2)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                  Value: 
                    IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                      Arguments(0)
                      Initializer: 
                        null

                IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B9]
                Entering: {R6} {R7}

        .locals {R6}
        {
            CaptureIds: [7]
            .locals {R7}
            {
                CaptureIds: [6]
                Block[B9] - Block
                    Predecessors: [B8]
                    Statements (1)
                        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

                    Jump if True (Regular) to Block[B11]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                        Leaving: {R7}

                    Next (Regular) Block[B10]
                Block[B10] - Block
                    Predecessors: [B9]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                          Value: 
                            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                              Arguments(0)

                    Next (Regular) Block[B12]
                        Leaving: {R7}
            }

            Block[B11] - Block
                Predecessors: [B9]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                Next (Regular) Block[B12]
            Block[B12] - Block
                Predecessors: [B10] [B11]
                Statements (1)
                    IInvocationOperation ( void C2.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i2 ?? 4')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i2 ?? 4')
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? 4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B13]
                    Leaving: {R6}
        }

        Block[B13] - Block
            Predecessors: [B12]
            Statements (1)
                IInvocationOperation ( void C1.Add(C2 c2)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ne ... i2 ?? 4 } }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() { 3, i2 ?? 4 }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B14]
                Leaving: {R5}
    }

    Block[B14] - Block
        Predecessors: [B13]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... 2 ?? 4 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... i2 ?? 4 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { ne ... i2 ?? 4 } }')

        Next (Regular) Block[B15]
            Leaving: {R1}
}

Block[B15] - Exit
    Predecessors: [B14]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_60()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<int>
{
    /*<bind>*/public static void M(C1 c, dynamic d1, dynamic d2)
    {
        c = new C1 { d1, d2 };
    }/*</bind>*/
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { d1, d2 }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { d1, d2 }')
                  Arguments(0)
                  Initializer: 
                    null

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd1')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'C1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1, d2 }')
              Arguments(1):
                  IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
              ArgumentNames(0)
              ArgumentRefKinds(0)

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd2')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'C1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1, d2 }')
              Arguments(1):
                  IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')
              ArgumentNames(0)
              ArgumentRefKinds(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1 { d1, d2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1 { d1, d2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1, d2 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_61()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<int>
{
    /*<bind>*/public static void M(C1 c, bool b, dynamic d1, dynamic d2, dynamic d3)
    {
        c = new C1 { d1, b ? d2 : d3 };
    }/*</bind>*/
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { d1 ... ? d2 : d3 }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { d1 ... ? d2 : d3 }')
                  Arguments(0)
                  Initializer: 
                    null

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd1')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'C1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1 ... ? d2 : d3 }')
              Arguments(1):
                  IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
              ArgumentNames(0)
              ArgumentRefKinds(0)

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'b ? d2 : d3')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'C1')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1 ... ? d2 : d3 }')
                  Arguments(1):
                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'b ? d2 : d3')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ...  d2 : d3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... ? d2 : d3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { d1 ... ? d2 : d3 }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_62()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, bool b, dynamic d1, dynamic d2, dynamic d3)
    {
        c = new C1 { [GetInt(0)] = { d1, b ? d2 : d3 } };
    }/*</bind>*/
    static int GetInt(int i) => i;
    public C2 this[int i] { get => null; }
}
class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [G ... d2 : d3 } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [G ... d2 : d3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(0)')
                  Value: 
                    IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(0)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'd1')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: '[GetInt(0)]')
                      Type Arguments(0)
                      Instance Receiver: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... d2 : d3 } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3] [4]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[GetInt(0)]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... d2 : d3 } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                      Value: 
                        IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')

                Next (Regular) Block[B6]
            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                      Value: 
                        IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd3')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: 'b ? d2 : d3')
                      Expression: 
                        IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: '[GetInt(0)]')
                          Type Arguments(0)
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[GetInt(0)]')
                      Arguments(1):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'b ? d2 : d3')
                      ArgumentNames(0)
                      ArgumentRefKinds(0)

                Next (Regular) Block[B7]
                    Leaving: {R3} {R2}
        }
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... 2 : d3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ... d2 : d3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ... d2 : d3 } }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics, useLatestFrameworkReferences: true);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_63()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, bool b, dynamic d1, dynamic d2, dynamic d3)
    {
        c = new C1 { [GetInt(0)] = { { 1, 2 }, { d1, b ? d2 : d3 } } };
    }/*</bind>*/
    static int GetInt(int i) => i;
    public C2 this[int i] { get => null; }
}
class C2 : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c1, int c2) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1 { [G ...  : d3 } } }')
              Value: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1 { [G ...  : d3 } } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetInt(0)')
                  Value: 
                    IInvocationOperation (System.Int32 C1.GetInt(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt(0)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IInvocationOperation ( void C2.Add(System.Int32 c1, System.Int32 c2)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ 1, 2 }')
                  Instance Receiver: 
                    IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ...  : d3 } } }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3] [4] [5]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[GetInt(0)]')
                      Value: 
                        IPropertyReferenceOperation: C2 C1.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: C2) (Syntax: '[GetInt(0)]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ...  : d3 } } }')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'GetInt(0)')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetInt(0)')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')

                Jump if False (Regular) to Block[B5]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                      Value: 
                        IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')

                Next (Regular) Block[B6]
            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                      Value: 
                        IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd3')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: '{ d1, b ? d2 : d3 }')
                      Expression: 
                        IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: '[GetInt(0)]')
                          Type Arguments(0)
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '[GetInt(0)]')
                      Arguments(2):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1')
                          IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'b ? d2 : d3')
                      ArgumentNames(0)
                      ArgumentRefKinds(0)

                Next (Regular) Block[B7]
                    Leaving: {R3} {R2}
        }
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = new C1  ... : d3 } } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c = new C1  ...  : d3 } } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1 { [G ...  : d3 } } }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics, useLatestFrameworkReferences: true);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_64()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<int>
{
    C1(int x){}

    /*<bind>*/public static void M(C1 c, int i1)
    {
        c = new C1 { i1 };
    }/*</bind>*/
    public IEnumerator<int> GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(int c2) { }
}
";
            var expectedDiagnostics = new[] {
                // file.cs(11,17): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'C1.C1(int)'
                //         c = new C1 { i1 };
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C1").WithArguments("x", "C1.C1(int)").WithLocation(11, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { i1 }')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C1, IsInvalid) (Syntax: 'new C1 { i1 }')
                  Children(0)

            IInvocationOperation ( void C1.Add(System.Int32 c2)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { i1 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i1')
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = new C1 { i1 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid) (Syntax: 'c = new C1 { i1 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { i1 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_65()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int i1, int i2)
    {
        c = new C1(i1) { F = i2 };
    }/*</bind>*/

    public int F;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(9,17): error CS1729: 'C1' does not contain a constructor that takes 1 arguments
                //         c = new C1(i1) { F = i2 };
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C1").WithArguments("C1", "1").WithLocation(9, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1(i1) { F = i2 }')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C1, IsInvalid) (Syntax: 'new C1(i1) { F = i2 }')
                  Children(1):
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F = i2')
              Left: 
                IFieldReferenceOperation: System.Int32 C1.F (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1) { F = i2 }')
              Right: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = new C1( ... { F = i2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid) (Syntax: 'c = new C1( ...  { F = i2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1) { F = i2 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_66()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int i1, int i2, C1 c1, C1 c2)
    {
        c = new C1(i1, c1 ?? c2) { F = i2 };
    }/*</bind>*/

    public int F;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(9,17): error CS1729: 'C1' does not contain a constructor that takes 2 arguments
                //         c = new C1(i1, c1 ?? c2) { F = i2 };
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C1").WithArguments("C1", "2").WithLocation(9, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1(i1,  ...  { F = i2 }')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: C1, IsInvalid) (Syntax: 'new C1(i1,  ...  { F = i2 }')
                      Children(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1 ?? c2')

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F = i2')
              Left: 
                IFieldReferenceOperation: System.Int32 C1.F (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1,  ...  { F = i2 }')
              Right: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = new C1( ... { F = i2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid) (Syntax: 'c = new C1( ...  { F = i2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1,  ...  { F = i2 }')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_67()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/public static void M(C1 c, int i1, C1 c1, C1 c2)
    {
        c = new C1(i1) { F = c1 ?? c2 };
    }/*</bind>*/

    public C1 F;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(9,17): error CS1729: 'C1' does not contain a constructor that takes 1 arguments
                //         c = new C1(i1) { F = c1 ?? c2 };
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C1").WithArguments("C1", "1").WithLocation(9, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1(i1)  ...  c1 ?? c2 }')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C1, IsInvalid) (Syntax: 'new C1(i1)  ...  c1 ?? c2 }')
                  Children(1):
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'F = c1 ?? c2')
                  Left: 
                    IFieldReferenceOperation: C1 C1.F (OperationKind.FieldReference, Type: C1) (Syntax: 'F')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1)  ...  c1 ?? c2 }')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1 ?? c2')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = new C1( ... c1 ?? c2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid) (Syntax: 'c = new C1( ...  c1 ?? c2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1(i1)  ...  c1 ?? c2 }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_68()
        {
            string source = @"
public class MemberInitializerTest
{
    public int x, y;
    /*<bind>*/public static void Main()
    {
        var i = new MemberInitializerTest { x = 0, y++ };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(7,52): error CS0120: An object reference is required for the non-static field, method, or property 'MemberInitializerTest.y'
                //         var i = new MemberInitializerTest { x = 0, y++ };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("MemberInitializerTest.y").WithLocation(7, 52),
                // file.cs(7,52): error CS0747: Invalid initializer member declarator
                //         var i = new MemberInitializerTest { x = 0, y++ };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "y++").WithLocation(7, 52)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MemberInitializerTest i]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new MemberI ...  = 0, y++ }')
              Value: 
                IObjectCreationOperation (Constructor: MemberInitializerTest..ctor()) (OperationKind.ObjectCreation, Type: MemberInitializerTest, IsInvalid) (Syntax: 'new MemberI ...  = 0, y++ }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 0')
              Left: 
                IFieldReferenceOperation: System.Int32 MemberInitializerTest.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'new MemberI ...  = 0, y++ }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: ?, IsInvalid) (Syntax: 'y++')
              Target: 
                IFieldReferenceOperation: System.Int32 MemberInitializerTest.y (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'y')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'i = new Mem ...  = 0, y++ }')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'i = new Mem ...  = 0, y++ }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MemberInitializerTest, IsInvalid, IsImplicit) (Syntax: 'new MemberI ...  = 0, y++ }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_69()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    A this[int x, int y]
    {
        get
        {
            return new A();
        }
    }

    int X, Y, Z;

    /*<bind>*/static void Main(A a, dynamic x, dynamic y)
    {
        a = new A {[x, y] = { X = 1, Y = 2, Z = 3 } };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new A {[x,  ... , Z = 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A {[x,  ... , Z = 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (5)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'x')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'y')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 1')
                  Left: 
                    IDynamicMemberReferenceOperation (Member Name: ""X"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'X')
                      Type Arguments(0)
                      Instance Receiver: 
                        IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[x, y]')
                          Expression: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A {[x,  ... , Z = 3 } }')
                          Arguments(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'y')
                          ArgumentNames(0)
                          ArgumentRefKinds(0)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Y = 2')
                  Left: 
                    IDynamicMemberReferenceOperation (Member Name: ""Y"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Y')
                      Type Arguments(0)
                      Instance Receiver: 
                        IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[x, y]')
                          Expression: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A {[x,  ... , Z = 3 } }')
                          Arguments(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'y')
                          ArgumentNames(0)
                          ArgumentRefKinds(0)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Z = 3')
                  Left: 
                    IDynamicMemberReferenceOperation (Member Name: ""Z"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Z')
                      Type Arguments(0)
                      Instance Receiver: 
                        IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[x, y]')
                          Expression: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A {[x,  ... , Z = 3 } }')
                          Arguments(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'x')
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'y')
                          ArgumentNames(0)
                          ArgumentRefKinds(0)
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = new A { ...  Z = 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: A) (Syntax: 'a = new A { ... , Z = 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'a')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A {[x,  ... , Z = 3 } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_70()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic D { get; set; }

    /*<bind>*/static void Main(A a, bool b)
    {
        a = new A { D = { X = 1, Y = b ? 2 : 3 } };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new A { D = ... ? 2 : 3 } }')
              Value: 
                IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A { D = ... ? 2 : 3 } }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'X = 1')
              Left: 
                IDynamicMemberReferenceOperation (Member Name: ""X"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'X')
                  Type Arguments(0)
                  Instance Receiver: 
                    IPropertyReferenceOperation: dynamic A.D { get; set; } (OperationKind.PropertyReference, Type: dynamic) (Syntax: 'D')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A { D = ... ? 2 : 3 } }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'D')
                  Value: 
                    IPropertyReferenceOperation: dynamic A.D { get; set; } (OperationKind.PropertyReference, Type: dynamic) (Syntax: 'D')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A { D = ... ? 2 : 3 } }')

            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'Y = b ? 2 : 3')
                  Left: 
                    IDynamicMemberReferenceOperation (Member Name: ""Y"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'Y')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'D')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = new A { ...  2 : 3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: A) (Syntax: 'a = new A { ... ? 2 : 3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'a')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A { D = ... ? 2 : 3 } }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_71()
        {
            string source = @"
#pragma warning disable 0169
class A
{
    dynamic this[int x, int y]
    {
        get
        {
            return new A();
        }
    }

    dynamic this[string x, string y]
    {
        get
        {
            throw null;
        }
    }

    int X, Y, Z;

    /*<bind>*/static void Main()
    {
        dynamic x = 1;
        new A {[y: x, x: x] = { } };
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [dynamic x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'x = 1')
              Left:
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = 1')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new A {[y:  ...  x] = { } }')
                  Value:
                    IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A {[y:  ...  x] = { } }')
                      Arguments(0)
                      Initializer:
                        null
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'new A {[y:  ... x] = { } };')
                  Expression:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A {[y:  ...  x] = { } }')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_72()
        {
            string source = @"
public class MyClass
{
    public MyClass(int i1, int i2, int i3) { }
    static void M(bool b)
    /*<bind>*/{
        var c = new MyClass(1, 2, b ? 3 : 4) {};
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyClass c]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ... ? 3 : 4) {}')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'c = new MyC ... ? 3 : 4) {}')
              Right: 
                IObjectCreationOperation (Constructor: MyClass..ctor(System.Int32 i1, System.Int32 i2, System.Int32 i3)) (OperationKind.ObjectCreation, Type: MyClass) (Syntax: 'new MyClass ... ? 3 : 4) {}')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: '2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i3) (OperationKind.Argument, Type: null) (Syntax: 'b ? 3 : 4')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 3 : 4')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_73()
        {
            string source = @"
class C
{
    static int i1;
    public static void Main()
    /*<bind>*/{
        var c = new C { i1 = 1 };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(7,25): error CS1914: Static field or property 'C.i1' cannot be assigned in an object initializer
                //         var c = new C { i1 = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "i1").WithArguments("C.i1").WithLocation(7, 25),
                // file.cs(4,16): warning CS0414: The field 'C.i1' is assigned but its value is never used
                //     static int i1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(4, 16)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [C c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C { i1 = 1 }')
              Value: 
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C { i1 = 1 }')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i1 = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 C.i1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
                  Instance Receiver: 
                    null
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid, IsImplicit) (Syntax: 'c = new C { i1 = 1 }')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c = new C { i1 = 1 }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new C { i1 = 1 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ObjectCreationFlow_74()
        {
            string source = @"
struct S1
{
    public int x;
}

struct S
{
    public S1 x;
 
    static void M(bool result)
    /*<bind>*/{
        result = new S { x = new S1{x=0} }.x.Equals(new S { x = new S1{x=1} });
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [3] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new S { x = ... w S1{x=0} }')
                  Value: 
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S { x = ... w S1{x=0} }')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (3)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new S1{x=0}')
                      Value: 
                        IObjectCreationOperation (Constructor: S1..ctor()) (OperationKind.ObjectCreation, Type: S1) (Syntax: 'new S1{x=0}')
                          Arguments(0)
                          Initializer: 
                            null

                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x=0')
                      Left: 
                        IFieldReferenceOperation: System.Int32 S1.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new S1{x=0}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S1) (Syntax: 'x = new S1{x=0}')
                      Left: 
                        IFieldReferenceOperation: S1 S.x (OperationKind.FieldReference, Type: S1) (Syntax: 'x')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'new S { x = ... w S1{x=0} }')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new S1{x=0}')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new S { x = ... S1{x=0} }.x')
                  Value: 
                    IFieldReferenceOperation: S1 S.x (OperationKind.FieldReference, Type: S1) (Syntax: 'new S { x = ... S1{x=0} }.x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'new S { x = ... w S1{x=0} }')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new S { x = ... w S1{x=1} }')
              Value: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S { x = ... w S1{x=1} }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B6]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [5]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (3)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new S1{x=1}')
                  Value: 
                    IObjectCreationOperation (Constructor: S1..ctor()) (OperationKind.ObjectCreation, Type: S1) (Syntax: 'new S1{x=1}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x=1')
                  Left: 
                    IFieldReferenceOperation: System.Int32 S1.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new S1{x=1}')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S1) (Syntax: 'x = new S1{x=1}')
                  Left: 
                    IFieldReferenceOperation: S1 S.x (OperationKind.FieldReference, Type: S1) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'new S { x = ... w S1{x=1} }')
                  Right: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new S1{x=1}')

            Next (Regular) Block[B7]
                Leaving: {R4}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = ne ... S1{x=1} });')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = ne ...  S1{x=1} })')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'result')
                  Right: 
                    IInvocationOperation (virtual System.Boolean System.ValueType.Equals(System.Object obj)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'new S { x = ...  S1{x=1} })')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'new S { x = ... S1{x=0} }.x')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null) (Syntax: 'new S { x = ... w S1{x=1} }')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new S { x = ... w S1{x=1} }')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Boxing)
                              Operand: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'new S { x = ... w S1{x=1} }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void ObjectCreationExpression_NoNewConstraint()
        {
            var source = @"
class C
{
    static void F2<T2>()
    /*<bind>*/{
        object x2;
        x2 = new T2 { };
    }/*</bind>*/
}";
            var comp = CreateCompilation(source);

            var diagnostics = new DiagnosticDescription[] {
                // (7,14): error CS0304: Cannot create an instance of the variable type 'T2' because it does not have the new() constraint
                //         x2 = new T2 { };
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T2 { }").WithArguments("T2").WithLocation(7, 14)
            };

            var expectedOperationTree = @"
IBlockOperation (2 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: System.Object x2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object x2;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object x2')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object x2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x2')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x2 = new T2 { };')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'x2 = new T2 { }')
        Left: 
          ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x2')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new T2 { }')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvalidOperation (OperationKind.Invalid, Type: T2, IsInvalid) (Syntax: 'new T2 { }')
                Children(1):
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T2, IsInvalid) (Syntax: '{ }')
                      Initializers(0)";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Object x2]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x2 = new T2 { };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'x2 = new T2 { }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'x2')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new T2 { }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IInvalidOperation (OperationKind.Invalid, Type: T2, IsInvalid) (Syntax: 'new T2 { }')
                          Children(0)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)");
        }

        [Fact]
        public void ObjectCreationFlow_ParenthesizedReferenceOffConstructedObject()
        {
            var source =
@"
class A
{
    internal object F1;
}
class B
{
    internal A G;
}
class Program
{
    static void F(A a)
    /*<bind>*/{
        a = (new B() { G = new A() { F1 = new object() } }).G;
    }/*</bind>*/
}";

            var comp = CreateCompilation(source);
            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new B() { G ... bject() } }')
              Value: 
                IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B() { G ... bject() } }')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new A() { F ...  object() }')
                  Value: 
                    IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A() { F ...  object() }')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'F1 = new object()')
                  Left: 
                    IFieldReferenceOperation: System.Object A.F1 (OperationKind.FieldReference, Type: System.Object) (Syntax: 'F1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A() { F ...  object() }')
                  Right: 
                    IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'new object()')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: A) (Syntax: 'G = new A() ...  object() }')
                  Left: 
                    IFieldReferenceOperation: A B.G (OperationKind.FieldReference, Type: A) (Syntax: 'G')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B() { G ... bject() } }')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'new A() { F ...  object() }')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = (new B( ... t() } }).G;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: A) (Syntax: 'a = (new B( ... ct() } }).G')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: A, IsImplicit) (Syntax: 'a')
                  Right: 
                    IFieldReferenceOperation: A B.G (OperationKind.FieldReference, Type: A) (Syntax: '(new B() {  ... ct() } }).G')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B() { G ... bject() } }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)");
        }

        [Fact]
        public void IndexedPropertyWithDefaultArgumentInVB()
        {
            var source1 =
@"Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Object
    ReadOnly Property P2(Optional index As Integer = 2) As IA
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Object Implements IA.P1
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    ReadOnly Property P2(Optional index As Integer = 2) As IA Implements IA.P2
        Get
            Return New A()
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B
{
    static void M(IA a)
    /*<bind>*/{
        a = new IA() { P2 = { P1 = 5 } };
    }/*</bind>*/
}";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source2, expectedDiagnostics: DiagnosticDescription.None, references: new[] { reference1 },
                expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: IA) (Syntax: 'a')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new IA() {  ...  P1 = 5 } }')
              Value: 
                IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: IA) (Syntax: 'new IA() {  ...  P1 = 5 } }')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'P2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'P2')
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'P1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'P1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'P1 = 5')
                      Left: 
                        IPropertyReferenceOperation: System.Object IA.P1[[System.Int32 index = 1]] { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'P1')
                          Instance Receiver: 
                            IPropertyReferenceOperation: IA IA.P2[[System.Int32 index = 2]] { get; } (OperationKind.PropertyReference, Type: IA) (Syntax: 'P2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: IA, IsImplicit) (Syntax: 'new IA() {  ...  P1 = 5 } }')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: index) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'P2')
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'P2')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: index) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'P1')
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'P1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '5')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Boxing)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                Next (Regular) Block[B4]
                    Leaving: {R3} {R2}
        }
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = new IA( ... P1 = 5 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: IA) (Syntax: 'a = new IA( ...  P1 = 5 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: IA, IsImplicit) (Syntax: 'a')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: IA, IsImplicit) (Syntax: 'new IA() {  ...  P1 = 5 } }')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1806208")]
        public void InvalidTrailingUnconvertedExpressions()
        {
            var source = """
                var c = /*<bind>*/new C() { F1 = 1, $"{asdf}", true switch { _ => false }, new() }/*</bind>*/;

                class C
                {
                    public int F1;
                }
                """;

            var expectedDiagnostics = new[]
            {
                // (1,37): error CS0747: Invalid initializer member declarator
                // var c = /*<bind>*/new C() { F1 = 1, $"{asdf}", true switch { _ => false }, new() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"$""{asdf}""").WithLocation(1, 37),
                // (1,40): error CS0103: The name 'asdf' does not exist in the current context
                // var c = /*<bind>*/new C() { F1 = 1, $"{asdf}", true switch { _ => false }, new() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "asdf").WithArguments("asdf").WithLocation(1, 40),
                // (1,48): error CS0747: Invalid initializer member declarator
                // var c = /*<bind>*/new C() { F1 = 1, $"{asdf}", true switch { _ => false }, new() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "true switch { _ => false }").WithLocation(1, 48),
                // (1,76): error CS0747: Invalid initializer member declarator
                // var c = /*<bind>*/new C() { F1 = 1, $"{asdf}", true switch { _ => false }, new() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "new()").WithLocation(1, 76)
            };

            var comp = CreateCompilation(source);

            string expectedOperationTree = """
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C() { F ...  }, new() }')
                  Arguments(0)
                  Initializer:
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ F1 = 1, $ ...  }, new() }')
                      Initializers(4):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F1 = 1')
                            Left:
                              IFieldReferenceOperation: System.Int32 C.F1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F1')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F1')
                            Right:
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{asdf}"')
                            Parts(1):
                                IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{asdf}')
                                  Expression:
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'asdf')
                                      Children(0)
                                  Alignment:
                                    null
                                  FormatString:
                                    null
                          IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'true switch ...  => false }')
                            Children(1):
                                ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Boolean, IsInvalid) (Syntax: 'true switch ...  => false }')
                                  Value:
                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsInvalid) (Syntax: 'true')
                                  Arms(1):
                                      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => false')
                                        Pattern:
                                          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Boolean, NarrowedType: System.Boolean)
                                        Value:
                                          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsInvalid) (Syntax: 'false')
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'new()')
                            Children(1):
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new()')
                                  Children(0)
                """;
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            string expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [C c]
                    CaptureIds: [0]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (3)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C() { F ...  }, new() }')
                              Value:
                                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C() { F ...  }, new() }')
                                  Arguments(0)
                                  Initializer:
                                    null
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'F1 = 1')
                              Left:
                                IFieldReferenceOperation: System.Int32 C.F1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F1')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new C() { F ...  }, new() }')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{asdf}"')
                              Parts(1):
                                  IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{asdf}')
                                    Expression:
                                      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'asdf')
                                        Children(0)
                                    Alignment:
                                      null
                                    FormatString:
                                      null
                        Next (Regular) Block[B2]
                            Entering: {R2} {R3}
                    .locals {R2}
                    {
                        CaptureIds: [1]
                        .locals {R3}
                        {
                            CaptureIds: [2]
                            Block[B2] - Block
                                Predecessors: [B1]
                                Statements (1)
                                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'true')
                                      Value:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsInvalid) (Syntax: 'true')
                                Jump if False (Regular) to Block[B4]
                                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: '_ => false')
                                      Value:
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsInvalid, IsImplicit) (Syntax: 'true')
                                      Pattern:
                                        IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Boolean, NarrowedType: System.Boolean)
                                    Leaving: {R3}
                                Next (Regular) Block[B3]
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (1)
                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'false')
                                      Value:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsInvalid) (Syntax: 'false')
                                Next (Regular) Block[B5]
                                    Leaving: {R3}
                        }
                        Block[B4] - Block
                            Predecessors: [B2]
                            Statements (0)
                            Next (Throw) Block[null]
                                IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: 'true switch ...  => false }')
                                  Arguments(0)
                                  Initializer:
                                    null
                        Block[B5] - Block
                            Predecessors: [B3]
                            Statements (1)
                                IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'true switch ...  => false }')
                                  Children(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'true switch ...  => false }')
                            Next (Regular) Block[B6]
                                Leaving: {R2}
                    }
                    Block[B6] - Block
                        Predecessors: [B5]
                        Statements (2)
                            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'new()')
                              Children(1):
                                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new()')
                                    Children(0)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid, IsImplicit) (Syntax: 'c = /*<bind ...  }, new() }')
                              Left:
                                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c = /*<bind ...  }, new() }')
                              Right:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'new C() { F ...  }, new() }')
                        Next (Regular) Block[B7]
                            Leaving: {R1}
                }
                Block[B7] - Exit
                    Predecessors: [B6]
                    Statements (0)
                """;

            VerifyFlowGraph(comp, comp.SyntaxTrees[0].GetRoot(), expectedFlowGraph);
        }
    }
}
