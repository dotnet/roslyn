// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>Analyzer that looks for empty array allocations and recommends their replacement.</summary>
    public class EmptyArrayAnalyzer : DiagnosticAnalyzer
    {
        private const string SystemCategory = "System";

        /// <summary>The name of the array type.</summary>
        internal const string ArrayTypeName = "System.Array"; // using instead of GetSpecialType to make more testable

        /// <summary>The name of the Empty method on System.Array.</summary>
        internal const string ArrayEmptyMethodName = "Empty";

        private static LocalizableString s_localizableTitle = "Empty Array";
        private static LocalizableString s_localizableMessage = "Empty array creation can be replaced with Array.Empty";

        /// <summary>The diagnostic descriptor used when Array.Empty should be used instead of a new array allocation.</summary>
        public static readonly DiagnosticDescriptor UseArrayEmptyDescriptor = new DiagnosticDescriptor(
            "EmptyArrayRule",
            s_localizableTitle,
            s_localizableMessage,
            SystemCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(UseArrayEmptyDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(ctx =>
            {
                RegisterOperationAction(ctx);
            });
        }

        /// <summary>Reports a diagnostic warning for an array creation that should be replaced.</summary>
        /// <param name="context">The context.</param>
        /// <param name="arrayCreationExpression">The array creation expression to be replaced.</param>
        internal void Report(OperationAnalysisContext context, SyntaxNode arrayCreationExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(UseArrayEmptyDescriptor, arrayCreationExpression.GetLocation()));
        }

        /// <summary>Called once at compilation start to register actions in the compilation context.</summary>
        /// <param name="context">The analysis context.</param>
        internal void RegisterOperationAction(CompilationStartAnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                    {
                        IArrayCreationOperation arrayCreation = (IArrayCreationOperation)operationContext.Operation;

                        // ToDo: Need to suppress analysis of array creation expressions within attribute applications.

                        // Detect array creation expression that have rank 1 and size 0. Such expressions
                        // can be replaced with Array.Empty<T>(), provided that the element type can be a generic type argument.

                        var elementType = (arrayCreation as IArrayTypeSymbol)?.ElementType;
                        if (arrayCreation.DimensionSizes.Length == 1
                            //// Pointer types can't be generic type arguments.
                            && elementType?.TypeKind != TypeKind.Pointer)
                        {
                            Optional<object> arrayLength = arrayCreation.DimensionSizes[0].ConstantValue;
                            if (arrayLength is { HasValue: true, Value: int _ } && (int)arrayLength.Value is 0)
                            {
                                Report(operationContext, arrayCreation.Syntax);
                            }
                        }
                    },
                OperationKind.ArrayCreation);
        }
    }
}
