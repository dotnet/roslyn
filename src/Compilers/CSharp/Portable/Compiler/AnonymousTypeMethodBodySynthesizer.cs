// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        private sealed partial class AnonymousTypeConstructorSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
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
                Debug.Assert(ContainingType.BaseTypeNoUseSiteDiagnostics.SpecialType == SpecialType.System_Object);
                BoundExpression call = Binder.GenerateBaseParameterlessConstructorInitializer(this, diagnostics);
                if (call == null)
                {
                    // This may happen if Object..ctor is not found or is inaccessible
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

            protected override bool HasSetsRequiredMembersImpl => false;
        }

        private sealed partial class AnonymousTypePropertyGetAccessorSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
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
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingType).Manager;
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                //  Method body:
                //
                //  {
                //      $anonymous$ local = value as $anonymous$;
                //      return (object)local == this || (local != null 
                //             && System.Collections.Generic.EqualityComparer<T_1>.Default.Equals(this.backingFld_1, local.backingFld_1)
                //             ...
                //             && System.Collections.Generic.EqualityComparer<T_N>.Default.Equals(this.backingFld_N, local.backingFld_N));
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

                // Compare fields
                if (anonymousType.Properties.Length > 0)
                {
                    var fields = ArrayBuilder<FieldSymbol>.GetInstance(anonymousType.Properties.Length);
                    foreach (var prop in anonymousType.Properties)
                    {
                        fields.Add(prop.BackingField);
                    }
                    retExpression = MethodBodySynthesizer.GenerateFieldEquals(retExpression, boundLocal, fields, F);
                    fields.Free();
                }

                // Compare references
                retExpression = F.LogicalOr(F.ObjectEqual(F.This(), boundLocal), retExpression);

                // Final return statement
                BoundStatement retStatement = F.Return(retExpression);

                // Create a bound block 
                F.CloseMethod(F.Block(ImmutableArray.Create<LocalSymbol>(boundLocal.LocalSymbol), assignment, retStatement));
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }
        }

        private sealed partial class AnonymousTypeGetHashCodeMethodSymbol : SynthesizedMethodBase
        {
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingType).Manager;
                SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

                //  Method body:
                //
                //  HASH_FACTOR = 0xa5555529;
                //  INIT_HASH = (...((0 * HASH_FACTOR) + GetFNVHashCode(backingFld_1.Name)) * HASH_FACTOR
                //                                     + GetFNVHashCode(backingFld_2.Name)) * HASH_FACTOR
                //                                     + ...
                //                                     + GetFNVHashCode(backingFld_N.Name)
                //
                //  {
                //      return (...((INITIAL_HASH * HASH_FACTOR) + EqualityComparer<T_1>.Default.GetHashCode(this.backingFld_1)) * HASH_FACTOR
                //                                               + EqualityComparer<T_2>.Default.GetHashCode(this.backingFld_2)) * HASH_FACTOR
                //                                               ...
                //                                               + EqualityComparer<T_N>.Default.GetHashCode(this.backingFld_N)
                //  }
                //
                // Where GetFNVHashCode is the FNV-1a hash code.

                // Type expression
                AnonymousTypeTemplateSymbol anonymousType = (AnonymousTypeTemplateSymbol)this.ContainingType;

                //  INIT_HASH
                int initHash = 0;
                foreach (var property in anonymousType.Properties)
                {
                    initHash = unchecked(initHash * MethodBodySynthesizer.HASH_FACTOR + Hash.GetFNVHashCode(property.BackingField.Name));
                }

                //  Generate expression for return statement
                //      retExpression <= 'INITIAL_HASH'
                BoundExpression retExpression = F.Literal(initHash);

                //  prepare symbols
                MethodSymbol equalityComparer_GetHashCode = manager.System_Collections_Generic_EqualityComparer_T__GetHashCode;
                MethodSymbol equalityComparer_get_Default = manager.System_Collections_Generic_EqualityComparer_T__get_Default;

                //  bound HASH_FACTOR
                BoundLiteral boundHashFactor = null;

                // Process fields
                for (int index = 0; index < anonymousType.Properties.Length; index++)
                {
                    retExpression = MethodBodySynthesizer.GenerateHashCombine(retExpression, equalityComparer_GetHashCode, equalityComparer_get_Default, ref boundHashFactor,
                                                                              F.Field(F.This(), anonymousType.Properties[index].BackingField),
                                                                              F);
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
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
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
                                                                                type: property.BackingField.Type), manager.System_Object__ToString),
                                                                            null,
                                                                            id: i,
                                                                            forceCopyOfNullableValueType: true,
                                                                            type: manager.System_String),
                                                 Conversion.ImplicitReference);
                    }
                    formatString.Builder.Append(" }}");

                    //  add format string argument
                    BoundExpression format = F.Literal(formatString.ToStringAndFree());

                    //  Generate expression for return statement
                    //      retExpression <= System.String.Format(args)
                    var formatMethod = manager.System_String__Format_IFormatProvider;
                    retExpression = F.StaticCall(manager.System_String, formatMethod, F.Null(formatMethod.Parameters[0].Type), format, F.ArrayOrEmpty(manager.System_Object, arguments));
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
