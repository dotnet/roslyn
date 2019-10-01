// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    using static Helpers;

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

            if (!OverridesSystemObject(_objectGetHashCodeMethod, method))
            {
                return default;
            }

            // Unwind through nested blocks. This also handles if we're in an 'unchecked' block in C#
            while (blockOperation.Operations.Length == 1 &&
                   blockOperation.Operations[0] is IBlockOperation childBlock)
            {
                blockOperation = childBlock;
            }

            return MatchAccumulatorPattern(method, blockOperation) ??
                   MatchTuplePattern(method, blockOperation) ??
                   default;
        }

        private (bool accessesBase, ImmutableArray<ISymbol> members)? MatchTuplePattern(IMethodSymbol method, IBlockOperation blockOperation)
        {
            // look for code of the form `return (a, b, c).GetHashCode()`.
            if (blockOperation.Operations.Length != 1)
            {
                return default;
            }

            if (!(blockOperation.Operations[0] is IReturnOperation returnOperation))
            {
                return default;
            }

            using var analyzer = new ValueAnalyzer(
                _objectGetHashCodeMethod, _equalityComparerTypeOpt, method, hashCodeVariableOpt: null);
            if (!analyzer.TryAddHashedSymbol(returnOperation.ReturnedValue, seenHash: false))
            {
                return default;
            }

            return analyzer.GetResult();
        }

        private (bool accessesBase, ImmutableArray<ISymbol> members)? MatchAccumulatorPattern(
            IMethodSymbol method, IBlockOperation blockOperation)
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

            using var valueAnalyzer = new ValueAnalyzer(
                _objectGetHashCodeMethod, _equalityComparerTypeOpt, method, hashCodeVariable);

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
                !valueAnalyzer.TryAddHashedSymbol(initializerValue, seenHash: true))
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
                    !valueAnalyzer.TryAddHashedSymbol(simpleAssignment.Value, seenHash: false))
                {
                    return default;
                }
            }

            return valueAnalyzer.GetResult();
        }
    }
}
