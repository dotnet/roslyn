// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    /// <summary>
    /// Helper code to support both <see cref="UseSystemHashCodeCodeFixProvider"/> and
    /// <see cref="UseSystemHashCodeDiagnosticAnalyzer"/>.
    /// </summary>
    internal partial struct Analyzer
    {
        private readonly Compilation _compilation;
        private readonly IMethodSymbol _objectGetHashCodeMethod;
        private readonly INamedTypeSymbol? _equalityComparerType;

        public readonly INamedTypeSymbol SystemHashCodeType;

        private Analyzer(
            Compilation compilation, IMethodSymbol objectGetHashCodeMethod,
            INamedTypeSymbol? equalityComparerType, INamedTypeSymbol systemHashCodeType)
        {
            _compilation = compilation;
            _objectGetHashCodeMethod = objectGetHashCodeMethod;
            _equalityComparerType = equalityComparerType;
            SystemHashCodeType = systemHashCodeType;
        }

        public static bool TryGetAnalyzer(Compilation compilation, [NotNullWhen(true)]out Analyzer analyzer)
        {
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            // This may not find anything.  However, CanAnalyze checks for this. So
            // we represent the value as non-nullable for all future code.
            var objectGetHashCodeMethod = objectType?.GetMembers(nameof(GetHashCode)).FirstOrDefault() as IMethodSymbol;
            var equalityComparerType = compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName);
            var systemHashCodeType = compilation.GetTypeByMetadataName("System.HashCode");

            if (systemHashCodeType == null || objectGetHashCodeMethod == null)
            {
                analyzer = default;
                return false;
            }

            analyzer = new Analyzer(compilation, objectGetHashCodeMethod, equalityComparerType, systemHashCodeType);
            return true;
        }

        /// <summary>
        /// Analyzes the containing <c>GetHashCode</c> method to determine which fields and
        /// properties were combined to form a hash code for this type.
        /// </summary>
        public (bool accessesBase, ImmutableArray<ISymbol> members) GetHashedMembers(ISymbol owningSymbol, IOperation operation)
        {
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

            var statements = blockOperation.Operations.WhereAsArray(o => !o.IsImplicit);
            return MatchAccumulatorPattern(method, statements) ??
                   MatchTuplePattern(method, statements) ??
                   default;
        }

        private (bool accessesBase, ImmutableArray<ISymbol> members)? MatchTuplePattern(
            IMethodSymbol method, ImmutableArray<IOperation> statements)
        {
            // look for code of the form `return (a, b, c).GetHashCode()`.
            if (statements.Length != 1)
            {
                return null;
            }

            if (!(statements[0] is IReturnOperation returnOperation))
            {
                return null;
            }

            using var analyzer = new OperationDeconstructor(this, method, hashCodeVariable: null);
            if (!analyzer.TryAddHashedSymbol(returnOperation.ReturnedValue, seenHash: false))
            {
                return null;
            }

            return analyzer.GetResult();
        }

        private (bool accessesBase, ImmutableArray<ISymbol> members)? MatchAccumulatorPattern(
            IMethodSymbol method, ImmutableArray<IOperation> statements)
        {
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
            if (statements.Length < 3)
            {
                return null;
            }

            // First statement has to be the declaration of the accumulator.
            // Last statement has to be the return of it.
            if (!(statements.First() is IVariableDeclarationGroupOperation varDeclStatement) ||
                !(statements.Last() is IReturnOperation returnStatement))
            {
                return null;
            }

            var variables = varDeclStatement.GetDeclaredVariables();
            if (variables.Length != 1 ||
                varDeclStatement.Declarations.Length != 1)
            {
                return null;
            }

            var declaration = varDeclStatement.Declarations[0];
            if (declaration.Declarators.Length != 1)
            {
                return null;
            }

            var declarator = declaration.Declarators[0];
            var initializerValue = declaration.Initializer?.Value ?? declarator.Initializer?.Value;
            if (initializerValue == null)
            {
                return null;
            }

            var hashCodeVariable = declarator.Symbol;
            if (!(IsLocalReference(returnStatement.ReturnedValue, hashCodeVariable)))
            {
                return null;
            }

            using var valueAnalyzer = new OperationDeconstructor(this, method, hashCodeVariable);

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
            if (!IsLiteralNumber(initializerValue) &&
                !valueAnalyzer.TryAddHashedSymbol(initializerValue, seenHash: true))
            {
                return null;
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
                    !valueAnalyzer.TryAddHashedSymbol(simpleAssignment.Value, seenHash: false))
                {
                    return null;
                }
            }

            return valueAnalyzer.GetResult();
        }

        private bool OverridesSystemObject(IMethodSymbol? method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (Equals(_objectGetHashCodeMethod, current))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLocalReference(IOperation value, ILocalSymbol accumulatorVariable)
            => Unwrap(value) is ILocalReferenceOperation localReference && accumulatorVariable.Equals(localReference.Local);

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
    }
}
