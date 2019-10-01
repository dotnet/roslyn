// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    /// <summary>
    /// Helper code to support both <see cref="UseSystemHashCodeCodeFixProvider"/> and
    /// <see cref="UseSystemHashCodeDiagnosticAnalyzer"/>.
    /// </summary>
    internal struct Analyzer
    {
        private readonly Compilation _compilation;
        private readonly IMethodSymbol _objectGetHashCodeMethod;
        private readonly INamedTypeSymbol _equalityComparerTypeOpt;

        public readonly INamedTypeSymbol SystemHashCodeType;

        public Analyzer(Compilation compilation)
        {
            _compilation = compilation;

            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            _objectGetHashCodeMethod = objectType?.GetMembers(nameof(GetHashCode)).FirstOrDefault() as IMethodSymbol;
            _equalityComparerTypeOpt = compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName);
            SystemHashCodeType = compilation.GetTypeByMetadataName("System.HashCode");
        }

        public bool CanAnalyze()
            => SystemHashCodeType != null && _objectGetHashCodeMethod != null;

        /// <summary>
        /// Analyzes the containing <c>GetHashCode</c> method to determine which fields and
        /// properties were combined to form a hash code for this type.
        /// </summary>
        public (bool accessesBase, ImmutableArray<ISymbol> members) GetHashedMembers(ISymbol owningSymbol, IOperation operation)
        {
            Debug.Assert(CanAnalyze());

            // Capture a copy of `this` as we're in a struct and need to access it from local
            // functions in this method.
            var _this = this;

            if (!(operation is IBlockOperation blockOperation))
            {
                return default;
            }

            // Owning symbol has to be an override of Object.GetHashCode.
            if (!(owningSymbol is IMethodSymbol { Name: nameof(GetHashCode) } method))
            {
                return default;
            }

            if (method.Locations.Length != 1 ||
                method.DeclaringSyntaxReferences.Length != 1)
            {
                return default;
            }

            if (!method.Locations[0].IsInSource)
            {
                return default;
            }

            if (!OverridesSystemObject(method))
            {
                return default;
            }

            // Unwind through nested blocks. This also handles if we're in an 'unchecked' block in C#
            while (blockOperation.Operations.Length == 1 &&
                   blockOperation.Operations[0] is IBlockOperation childBlock)
            {
                blockOperation = childBlock;
            }

            // Needs to be of the form:
            //
            //      // accumulator
            //      var hashCode = <initializer_or_hash>
            //
            //      // 1-N member hashes mixed into the accumulator.
            //      hashCode = (hashCode op constant) op Hash(member)
            //
            //      // return of the value.
            //      return hashCode;

            var statements = blockOperation.Operations;
            if (statements.Length < 3)
            {
                return default;
            }

            // First statement has to be the declaration of the accumulator.
            // Last statement has to be the return of it.
            if (!(statements.First() is IVariableDeclarationGroupOperation varDeclStatement) ||
                !(statements.Last() is IReturnOperation returnStatement))
            {
                return default;
            }

            var variables = varDeclStatement.GetDeclaredVariables();
            if (variables.Length != 1 ||
                varDeclStatement.Declarations.Length != 1)
            {
                return default;
            }

            var declaration = varDeclStatement.Declarations[0];
            if (declaration.Declarators.Length != 1)
            {
                return default;
            }

            var declarator = declaration.Declarators[0];
            if (declarator.Initializer?.Value == null)
            {
                return default;
            }

            var hashCodeVariable = declarator.Symbol;
            if (!(IsLocalReference(returnStatement.ReturnedValue, hashCodeVariable)))
            {
                return default;
            }

            var hashedSymbols = ArrayBuilder<ISymbol>.GetInstance();
            var accessesBase = false;

            // Local declaration can be of the form:
            //
            //      // VS code gen
            //      var hashCode = number;
            //
            // or
            //
            //      // ReSharper code gen
            //      var hashCode = Hash(firstSymbol);

            // Note: we pass in `seenHash: true` here because ReSharper may just initialize things
            // like `var hashCode = intField`.  In this case, there won't be any specific hashing
            // operations in the value that we have to look for.
            var initializerValue = declarator.Initializer.Value;
            if (!IsLiteralNumber(initializerValue) &&
                !TryAddHashedSymbol(initializerValue, seenHash: true))
            {
                return default;
            }

            // Now check all the intermediary statements.  They all have to be of the form:
            //
            //      hashCode = (hashCode op constant) op Hash(member)
            //
            // Or recursively built out of that.  For example, in VB we sometimes generate:
            //
            //      hashCode = Hash((hashCode op constant) op Hash(member))
            //
            // So, after confirming we're assigning to our accumulator, we recursively break down
            // the expression, looking for valid forms that only end up hashing a single field in
            // some way.
            for (var i = 1; i < statements.Length - 1; i++)
            {
                var statement = statements[i];
                if (!(statement is IExpressionStatementOperation expressionStatement) ||
                    !(expressionStatement.Operation is ISimpleAssignmentOperation simpleAssignment) ||
                    !IsLocalReference(simpleAssignment.Target, hashCodeVariable) ||
                    !TryAddHashedSymbol(simpleAssignment.Value, seenHash: false))
                {
                    return default;
                }
            }

            return (accessesBase, hashedSymbols.ToImmutableAndFree());

            // Recursive function that decomposes <paramref name="value"/>, looking for particular
            // forms that VS or ReSharper generate to hash fields in the containing type.
            bool TryAddHashedSymbol(IOperation value, bool seenHash)
            {
                value = Unwrap(value);
                if (value is IInvocationOperation invocation)
                {
                    var targetMethod = invocation.TargetMethod;
                    if (_this.OverridesSystemObject(targetMethod))
                    {
                        // Either:
                        //
                        //      a.GetHashCode()
                        //
                        // or
                        //
                        //      (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
                        //
                        // recurse on the value we're calling GetHashCode on.
                        return TryAddHashedSymbol(invocation.Instance, seenHash: true);
                    }

                    if (targetMethod.Name == nameof(GetHashCode) &&
                        Equals(_this._equalityComparerTypeOpt, targetMethod.ContainingType.OriginalDefinition) &&
                        invocation.Arguments.Length == 1)
                    {
                        // EqualityComparer<T>.Default.GetHashCode(i)
                        //
                        // VS codegen only.
                        return TryAddHashedSymbol(invocation.Arguments[0].Value, seenHash: true);
                    }
                }

                // (hashCode op1 constant) op1 hashed_value
                //
                // This is generated by both VS and ReSharper.  Though each use different mathematical
                // ops to combine the values.
                if (value is IBinaryOperation topBinary)
                {
                    return topBinary.LeftOperand is IBinaryOperation leftBinary &&
                           IsLocalReference(leftBinary.LeftOperand, hashCodeVariable) &&
                           IsLiteralNumber(leftBinary.RightOperand) &&
                           TryAddHashedSymbol(topBinary.RightOperand, seenHash: true);
                }

                // (StringProperty != null ? StringProperty.GetHashCode() : 0)
                //
                // ReSharper codegen only.
                if (value is IConditionalOperation conditional &&
                    conditional.Condition is IBinaryOperation binary)
                {
                    if (binary.RightOperand.IsNullLiteral() &&
                        TryGetFieldOrProperty(binary.LeftOperand, out _))
                    {
                        if (binary.OperatorKind == BinaryOperatorKind.Equals)
                        {
                            // (StringProperty == null ? 0 : StringProperty.GetHashCode())
                            return TryAddHashedSymbol(conditional.WhenFalse, seenHash: true);
                        }
                        else if (binary.OperatorKind == BinaryOperatorKind.NotEquals)
                        {
                            // (StringProperty != null ? StringProperty.GetHashCode() : 0)
                            return TryAddHashedSymbol(conditional.WhenTrue, seenHash: true);
                        }
                    }
                }

                // Look to see if we're referencing some field/prop/base.  However, we only accept
                // this reference if we've at least been through something that indicates that we've
                // hashed the value.
                if (seenHash)
                {
                    if (value is IInstanceReferenceOperation instanceReference &&
                        instanceReference.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance &&
                        Equals(method.ContainingType.BaseType, instanceReference.Type))
                    {
                        if (accessesBase)
                        {
                            // already had a reference to base.GetHashCode();
                            return false;
                        }

                        // reference to base.
                        //
                        // Happens with code like: `var hashCode = base.GetHashCode();`
                        accessesBase = true;
                        return true;
                    }

                    // After decomposing all of the above patterns, we must end up with an operation that is
                    // a reference to an instance-field (or prop) in our type.  If so, and this is the only
                    // time we've seen that field/prop, then we're good.
                    //
                    // We only do this if we actually did something that counts as hashing along the way.  This
                    // way
                    if (TryGetFieldOrProperty(value, out var fieldOrProp) &&
                        Equals(fieldOrProp.ContainingType.OriginalDefinition, method.ContainingType))
                    {
                        return Add(hashedSymbols, fieldOrProp);
                    }
                }

                // Anything else is not recognized.
                return false;
            }
        }

        private static bool TryGetFieldOrProperty(IOperation operation, out ISymbol symbol)
        {
            if (operation is IFieldReferenceOperation fieldReference)
            {
                symbol = fieldReference.Member;
                return !symbol.IsStatic;
            }

            if (operation is IPropertyReferenceOperation propertyReference)
            {
                symbol = propertyReference.Member;
                return !symbol.IsStatic;
            }

            symbol = null;
            return false;
        }

        private static bool Add(ArrayBuilder<ISymbol> hashedSymbols, ISymbol member)
        {
            // Not a legal GetHashCode to convert if we refer to members multiple times.
            if (hashedSymbols.Contains(member))
            {
                return false;
            }

            hashedSymbols.Add(member);
            return true;
        }

        /// <summary>
        /// Matches positive and negative numeric literals.
        /// </summary>
        private static bool IsLiteralNumber(IOperation value)
        {
            value = Unwrap(value);
            return value is IUnaryOperation unary
                ? unary.OperatorKind == UnaryOperatorKind.Minus && IsLiteralNumber(unary.Operand)
                : value.IsNumericLiteral();
        }

        private static bool IsLocalReference(IOperation value, ILocalSymbol accumulatorVariable)
            => Unwrap(value) is ILocalReferenceOperation localReference && accumulatorVariable.Equals(localReference.Local);

        private static IOperation Unwrap(IOperation value)
        {
            // ReSharper and VS generate different patterns for parentheses (which also depends on
            // the particular parentheses settings the user has enabled).  So just descend through
            // any parentheses we see to create a uniform view of the code.
            //
            // Also, lots of operations in a GetHashCode impl will involve conversions all over the
            // place (for example, some computations happen in 64bit, but convert to/from 32bit
            // along the way).  So we descend through conversions as well to create a uniform view
            // of things.
            while (true)
            {
                if (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }
                else if (value is IParenthesizedOperation parenthesized)
                {
                    value = parenthesized.Operand;
                }
                else
                {
                    return value;
                }
            }
        }

        public bool OverridesSystemObject(IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (_objectGetHashCodeMethod.Equals(current))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
