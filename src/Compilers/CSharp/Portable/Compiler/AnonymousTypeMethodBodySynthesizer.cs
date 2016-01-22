// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        private sealed partial class AnonymousTypeConstructorSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                //  Method body:
                //
                //  {
                //      Object..ctor();
                //      this.backingField_1 = arg1;
                //      ...
                //      this.backingField_N = argN;
                //  }
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                int paramCount = this.ParameterCount;

                // List of statements
                BoundStatement[] statements = new BoundStatement[paramCount + 2];
                int statementIndex = 0;

                //  explicit base constructor call
                BoundExpression call = MethodCompiler.GenerateObjectConstructorInitializer(this, diagnostics);
                if (call == null)
                {
                    // This may happen if Object..ctor is not found or is unaccessible
                    return;
                }
                statements[statementIndex++] = F.ExpressionStatement(call);

                if (paramCount > 0)
                {
                    AnonymousTypeTemplateSymbol anonymousType = (AnonymousTypeTemplateSymbol)this.ContainingType;
                    Debug.Assert(anonymousType.Properties.Length == paramCount);

                    // Assign fields
                    for (int index = 0; index < this.ParameterCount; index++)
                    {
                        // Generate 'field' = 'parameter' statement
                        statements[statementIndex++] =
                            F.Assignment(F.Field(F.This(), anonymousType.Properties[index].BackingField), F.Parameter(_parameters[index]));
                    }
                }

                // Final return statement
                statements[statementIndex++] = F.Return();

                // Create a bound block 
                F.CloseMethod(F.Block(statements));
            }

            internal override bool HasSpecialName
            {
                get { return true; }
            }
        }

        private sealed partial class AnonymousTypePropertyGetAccessorSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                //  Method body:
                //
                //  {
                //      return this.backingField;
                //  }

                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);
                F.CloseMethod(F.Block(F.Return(F.Field(F.This(), _property.BackingField))));
            }

            internal override bool HasSpecialName
            {
                get { return true; }
            }
        }

        private sealed partial class AnonymousTypeEqualsMethodSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingType).Manager;
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                //  Method body:
                //
                //  {
                //      $anonymous$ local = value as $anonymous$;
                //      return local != null 
                //             && System.Collections.Generic.EqualityComparer<T_1>.Default.Equals(this.backingFld_1, local.backingFld_1)
                //             ...
                //             && System.Collections.Generic.EqualityComparer<T_N>.Default.Equals(this.backingFld_N, local.backingFld_N);
                //  }

                // Type and type expression
                AnonymousTypeTemplateSymbol anonymousType = (AnonymousTypeTemplateSymbol)this.ContainingType;

                //  local
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal boundLocal = F.StoreToTemp(F.As(F.Parameter(_parameters[0]), anonymousType), out assignmentToTemp);

                //  Generate: statement <= 'local = value as $anonymous$'
                BoundStatement assignment = F.ExpressionStatement(assignmentToTemp);

                //  Generate expression for return statement
                //      retExpression <= 'local != null'
                BoundExpression retExpression = F.Binary(BinaryOperatorKind.ObjectNotEqual,
                                                         manager.System_Boolean,
                                                         F.Convert(manager.System_Object, boundLocal),
                                                         F.Null(manager.System_Object));

                //  prepare symbols
                MethodSymbol equalityComparer_Equals = manager.System_Collections_Generic_EqualityComparer_T__Equals;
                MethodSymbol equalityComparer_get_Default = manager.System_Collections_Generic_EqualityComparer_T__get_Default;
                NamedTypeSymbol equalityComparerType = equalityComparer_Equals.ContainingType;

                // Compare fields
                for (int index = 0; index < anonymousType.Properties.Length; index++)
                {
                    // Prepare constructed symbols
                    TypeParameterSymbol typeParameter = anonymousType.TypeParameters[index];
                    FieldSymbol fieldSymbol = anonymousType.Properties[index].BackingField;
                    NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(typeParameter);

                    // Generate 'retExpression' = 'retExpression && System.Collections.Generic.EqualityComparer<T_index>.
                    //                                                  Default.Equals(this.backingFld_index, local.backingFld_index)'
                    retExpression = F.LogicalAnd(retExpression,
                                                 F.Call(F.StaticCall(constructedEqualityComparer,
                                                                     equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                                                        equalityComparer_Equals.AsMember(constructedEqualityComparer),
                                                        F.Field(F.This(), fieldSymbol),
                                                        F.Field(boundLocal, fieldSymbol)));
                }

                // Final return statement
                BoundStatement retStatement = F.Return(retExpression);

                // Create a bound block 
                F.CloseMethod(F.Block(ImmutableArray.Create<LocalSymbol>(boundLocal.LocalSymbol), ImmutableArray<LocalFunctionSymbol>.Empty, assignment, retStatement));
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }
        }

        private sealed partial class AnonymousTypeGetHashCodeMethodSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingType).Manager;
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                //  Method body:
                //
                //  HASH_FACTOR = 0xa5555529;
                //  INIT_HASH = (...((0 * HASH_FACTOR) + backingFld_1.Name.GetHashCode()) * HASH_FACTOR
                //                                     + backingFld_2.Name.GetHashCode()) * HASH_FACTOR
                //                                     + ...
                //                                     + backingFld_N.Name.GetHashCode()
                //
                //  {
                //      return (...((INITIAL_HASH * HASH_FACTOR) + EqualityComparer<T_1>.Default.GetHashCode(this.backingFld_1)) * HASH_FACTOR
                //                                               + EqualityComparer<T_2>.Default.GetHashCode(this.backingFld_2)) * HASH_FACTOR
                //                                               ...
                //                                               + EqualityComparer<T_N>.Default.GetHashCode(this.backingFld_N)
                //  }

                const int HASH_FACTOR = -1521134295; // (int)0xa5555529

                // Type expression
                AnonymousTypeTemplateSymbol anonymousType = (AnonymousTypeTemplateSymbol)this.ContainingType;

                //  INIT_HASH
                int initHash = 0;
                foreach (var property in anonymousType.Properties)
                {
                    initHash = unchecked(initHash * HASH_FACTOR + property.BackingField.Name.GetHashCode());
                }

                //  Generate expression for return statement
                //      retExpression <= 'INITIAL_HASH'
                BoundExpression retExpression = F.Literal(initHash);

                //  prepare symbols
                MethodSymbol equalityComparer_GetHashCode = manager.System_Collections_Generic_EqualityComparer_T__GetHashCode;
                MethodSymbol equalityComparer_get_Default = manager.System_Collections_Generic_EqualityComparer_T__get_Default;
                NamedTypeSymbol equalityComparerType = equalityComparer_GetHashCode.ContainingType;

                //  bound HASH_FACTOR
                BoundLiteral boundHashFactor = F.Literal(HASH_FACTOR);

                // Process fields
                for (int index = 0; index < anonymousType.Properties.Length; index++)
                {
                    // Prepare constructed symbols
                    TypeParameterSymbol typeParameter = anonymousType.TypeParameters[index];
                    NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(typeParameter);

                    // Generate 'retExpression' <= 'retExpression * HASH_FACTOR 
                    retExpression = F.Binary(BinaryOperatorKind.IntMultiplication, manager.System_Int32, retExpression, boundHashFactor);

                    // Generate 'retExpression' <= 'retExpression + EqualityComparer<T_index>.Default.GetHashCode(this.backingFld_index)'
                    retExpression = F.Binary(BinaryOperatorKind.IntAddition,
                                             manager.System_Int32,
                                             retExpression,
                                             F.Call(
                                                F.StaticCall(constructedEqualityComparer,
                                                             equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                                                equalityComparer_GetHashCode.AsMember(constructedEqualityComparer),
                                                F.Field(F.This(), anonymousType.Properties[index].BackingField)));
                }

                // Create a bound block 
                F.CloseMethod(F.Block(F.Return(retExpression)));
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }
        }

        private sealed partial class AnonymousTypeToStringMethodSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingType).Manager;
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                //  Method body:
                //
                //  {
                //      return String.Format(
                //          "{ <name1> = {0}", <name2> = {1}", ... <nameN> = {N-1}",
                //          this.backingFld_1, 
                //          this.backingFld_2, 
                //          ...
                //          this.backingFld_N
                //  }

                // Type expression
                AnonymousTypeTemplateSymbol anonymousType = (AnonymousTypeTemplateSymbol)this.ContainingType;

                //  build arguments
                int fieldCount = anonymousType.Properties.Length;
                BoundExpression retExpression = null;

                if (fieldCount > 0)
                {
                    //  we do have fields, so have to use String.Format(...)
                    BoundExpression[] arguments = new BoundExpression[fieldCount];

                    //  process properties
                    PooledStringBuilder formatString = PooledStringBuilder.GetInstance();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        AnonymousTypePropertySymbol property = anonymousType.Properties[i];

                        // build format string
                        formatString.Builder.AppendFormat(i == 0 ? "{{{{ {0} = {{{1}}}" : ", {0} = {{{1}}}", property.Name, i);

                        // build argument
                        arguments[i] = F.Convert(manager.System_Object,
                                                 new BoundLoweredConditionalAccess(F.Syntax,
                                                                            F.Field(F.This(), property.BackingField),
                                                                            null,
                                                                            F.Call(new BoundConditionalReceiver(
                                                                                F.Syntax,
                                                                                id: i,
                                                                                type: property.BackingField.Type.TypeSymbol), manager.System_Object__ToString),
                                                                            null,
                                                                            id: i,
                                                                            type: manager.System_String),
                                                 ConversionKind.ImplicitReference);
                    }
                    formatString.Builder.Append(" }}");

                    //  add format string argument
                    BoundExpression format = F.Literal(formatString.ToStringAndFree());

                    //  Generate expression for return statement
                    //      retExpression <= System.String.Format(args)
                    var formatMethod = manager.System_String__Format_IFormatProvider;
                    retExpression = F.StaticCall(manager.System_String, formatMethod, F.Null(formatMethod.Parameters[0].Type.TypeSymbol), format, F.ArrayOrEmpty(manager.System_Object, arguments));
                }
                else
                {
                    //  this is an empty anonymous type, just return "{ }"
                    retExpression = F.Literal("{ }");
                }

                F.CloseMethod(F.Block(F.Return(retExpression)));
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }
        }
    }
}
