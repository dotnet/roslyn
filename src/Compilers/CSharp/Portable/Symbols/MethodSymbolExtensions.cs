// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class MethodSymbolExtensions
    {
        public static bool IsParams(this MethodSymbol method)
        {
            return method.ParameterCount != 0 && method.Parameters[method.ParameterCount - 1].IsParams;
        }

        /// <summary>
        /// If the extension method is applicable based on the "this" argument type, return
        /// the method constructed with the inferred type arguments. If the method is not an
        /// unconstructed generic method, type inference is skipped. If the method is not
        /// applicable, or if constraints when inferring type parameters from the "this" type
        /// are not satisfied, the return value is null.
        /// </summary>
        public static MethodSymbol InferExtensionMethodTypeArguments(this MethodSymbol method, TypeSymbol thisType, Compilation compilation, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(method.IsExtensionMethod);
            Debug.Assert((object)thisType != null);

            if (!method.IsGenericMethod || method != method.ConstructedFrom)
            {
                return method;
            }

            // We never resolve extension methods on a dynamic receiver.
            if (thisType.IsDynamic())
            {
                return null;
            }

            var containingAssembly = method.ContainingAssembly;
            var errorNamespace = containingAssembly.GlobalNamespace;
            var conversions = new TypeConversions(containingAssembly.CorLibrary);

            // There is absolutely no plausible syntax/tree that we could use for these
            // synthesized literals.  We could be speculatively binding a call to a PE method.
            var syntaxTree = CSharpSyntaxTree.Dummy;
            var syntax = (CSharpSyntaxNode)syntaxTree.GetRoot();

            // Create an argument value for the "this" argument of specific type,
            // and pass the same bad argument value for all other arguments.
            var thisArgumentValue = new BoundLiteral(syntax, ConstantValue.Bad, thisType) { WasCompilerGenerated = true };
            var otherArgumentType = new ExtendedErrorTypeSymbol(errorNamespace, name: string.Empty, arity: 0, errorInfo: null, unreported: false);
            var otherArgumentValue = new BoundLiteral(syntax, ConstantValue.Bad, otherArgumentType) { WasCompilerGenerated = true };

            var paramCount = method.ParameterCount;
            var arguments = new BoundExpression[paramCount];
            var argumentTypes = new TypeSymbol[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                var argument = (i == 0) ? thisArgumentValue : otherArgumentValue;
                arguments[i] = argument;
                argumentTypes[i] = argument.Type;
            }

            var typeArgs = MethodTypeInferrer.InferTypeArgumentsFromFirstArgument(
                conversions,
                method,
                argumentTypes.AsImmutableOrNull(),
                arguments.AsImmutableOrNull(),
                ref useSiteDiagnostics);

            if (typeArgs.IsDefault)
            {
                return null;
            }

            int firstNullInTypeArgs = -1;

            // For the purpose of constraint checks we use error type symbol in place of type arguments that we couldn't infer from the first argument.
            // This prevents constraint checking from failing for corresponding type parameters. 
            var typeArgsForConstraintsCheck = typeArgs.SelectAsArray(a => (object)a == null ? null : TypeSymbolWithAnnotations.Create(a));
            for (int i = 0; i < typeArgsForConstraintsCheck.Length; i++)
            {
                if ((object)typeArgsForConstraintsCheck[i] == null)
                {
                    firstNullInTypeArgs = i;
                    var builder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
                    builder.AddRange(typeArgsForConstraintsCheck, firstNullInTypeArgs);

                    for (; i < typeArgsForConstraintsCheck.Length; i++)
                    {
                        builder.Add(typeArgsForConstraintsCheck[i] ?? TypeSymbolWithAnnotations.Create(ErrorTypeSymbol.UnknownResultType));
                    }

                    typeArgsForConstraintsCheck = builder.ToImmutableAndFree();
                    break;
                }
            }

            // Check constraints.
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var typeParams = method.TypeParameters;
            var substitution = new TypeMap(typeParams, typeArgsForConstraintsCheck);
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var success = method.CheckConstraints(conversions, substitution, typeParams, typeArgsForConstraintsCheck, compilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder);
            diagnosticsBuilder.Free();

            if (useSiteDiagnosticsBuilder != null && useSiteDiagnosticsBuilder.Count > 0)
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }

                foreach (var diag in useSiteDiagnosticsBuilder)
                {
                    useSiteDiagnostics.Add(diag.DiagnosticInfo);
                }
            }

            if (!success)
            {
                return null;
            }

            // For the purpose of construction we use original type parameters in place of type arguments that we couldn't infer from the first argument.
            var typeArgsForConstruct = typeArgs;
            if (firstNullInTypeArgs != -1)
            {
                var builder = ArrayBuilder<TypeSymbol>.GetInstance();
                builder.AddRange(typeArgs, firstNullInTypeArgs);

                for (int i = firstNullInTypeArgs; i < typeArgsForConstruct.Length; i++)
                {
                    builder.Add(typeArgsForConstruct[i] ?? typeParams[i]);
                }

                typeArgsForConstruct = builder.ToImmutableAndFree();
            }

            return method.Construct(typeArgsForConstruct);
        }

        internal static bool IsSynthesizedLambda(this MethodSymbol method)
        {
            Debug.Assert((object)method != null);
            return method.IsImplicitlyDeclared && method.MethodKind == MethodKind.AnonymousFunction;
        }

        /// <summary>
        /// The runtime considers a method to be a finalizer (i.e. a method that should be invoked
        /// by the garbage collector) if it (directly or indirectly) overrides System.Object.Finalize.
        /// </summary>
        /// <remarks>
        /// As an optimization, return true immediately for metadata methods with MethodKind
        /// Destructor - they are guaranteed to be finalizers.
        /// </remarks>
        /// <param name="method">Method to inspect.</param>
        /// <param name="skipFirstMethodKindCheck">This method is used to determine the method kind of
        /// a PEMethodSymbol, so we may need to avoid using MethodKind until we move on to a different
        /// MethodSymbol.</param>
        public static bool IsRuntimeFinalizer(this MethodSymbol method, bool skipFirstMethodKindCheck = false)
        {
            // Note: Flipping the metadata-virtual bit on a method can't change it from not-a-runtime-finalize
            // to runtime-finalizer (since it will also be marked newslot), so it is safe to use
            // IsMetadataVirtualIgnoringInterfaceImplementationChanges.  This also has the advantage of making
            // this method safe to call before declaration diagnostics have been computed.
            if ((object)method == null || method.Name != WellKnownMemberNames.DestructorName ||
                method.ParameterCount != 0 || method.Arity != 0 || !method.IsMetadataVirtual(ignoreInterfaceImplementationChanges: true))
            {
                return false;
            }

            while ((object)method != null)
            {
                if (!skipFirstMethodKindCheck && method.MethodKind == MethodKind.Destructor)
                {
                    // For metadata symbols (and symbols that wrap them), having MethodKind
                    // Destructor guarantees that the method is a runtime finalizer.
                    // This is also true for source symbols, since we make them explicitly
                    // override System.Object.Finalize.
                    return true;
                }
                else if (method.ContainingType.SpecialType == SpecialType.System_Object)
                {
                    return true;
                }
                else if (method.IsMetadataNewSlot(ignoreInterfaceImplementationChanges: true))
                {
                    // If the method isn't a runtime override, then it can't be a finalizer.
                    return false;
                }

                // At this point, we know method originated with a DestructorDeclarationSyntax in source,
                // so it can't have the "new" modifier.
                // First is fine, since there should only be one, since there are no parameters.
                method = method.GetFirstRuntimeOverriddenMethodIgnoringNewSlot(ignoreInterfaceImplementationChanges: true);
                skipFirstMethodKindCheck = false;
            }

            return false;
        }

        /// <summary>
        /// Returns a constructed method symbol if 'method' is generic, otherwise just returns 'method'
        /// </summary>
        public static MethodSymbol ConstructIfGeneric(this MethodSymbol method, ImmutableArray<TypeSymbolWithAnnotations> typeArguments)
        {
            Debug.Assert(method.IsGenericMethod == (typeArguments.Length > 0));
            return method.IsGenericMethod ? method.Construct(typeArguments) : method;
        }

        /// <summary>
        /// Some kinds of methods are not considered to be hideable by certain kinds of members.
        /// Specifically, methods, properties, and types cannot hide constructors, destructors,
        /// operators, conversions, or accessors.
        /// </summary>
        public static bool CanBeHiddenByMemberKind(this MethodSymbol hiddenMethod, SymbolKind hidingMemberKind)
        {
            Debug.Assert((object)hiddenMethod != null);

            // Nothing can hide a destructor (see SymbolPreparer::ReportHiding)
            if (hiddenMethod.MethodKind == MethodKind.Destructor)
            {
                return false;
            }

            switch (hidingMemberKind)
            {
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    return CanBeHiddenByMethodPropertyOrType(hiddenMethod);
                case SymbolKind.Field:
                case SymbolKind.Event: // Events are not covered by CSemanticChecker::FindSymHiddenByMethPropAgg.
                    return true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(hidingMemberKind);
            }
        }

        /// <summary>
        /// Some kinds of methods are never considered hidden by methods, properties, or types
        /// (constructors, destructors, operators, conversions, and accessors).
        /// </summary>
        private static bool CanBeHiddenByMethodPropertyOrType(MethodSymbol method)
        {
            switch (method.MethodKind)
            {
                // See CSemanticChecker::FindSymHiddenByMethPropAgg.
                case MethodKind.Destructor:
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                case MethodKind.UserDefinedOperator:
                case MethodKind.Conversion:
                    return false;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    return method.IsIndexedPropertyAccessor();
                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns whether this method is async and returns void.
        /// </summary>
        public static bool IsVoidReturningAsync(this MethodSymbol method)
        {
            return method.IsAsync && method.ReturnsVoid;
        }

        /// <summary>
        /// Returns whether this method is async and returns a task.
        /// </summary>
        public static bool IsTaskReturningAsync(this MethodSymbol method, CSharpCompilation compilation)
        {
            return method.IsAsync
                && method.ReturnType.TypeSymbol == compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
        }

        /// <summary>
        /// Returns whether this method is async and returns a generic task.
        /// </summary>
        public static bool IsGenericTaskReturningAsync(this MethodSymbol method, CSharpCompilation compilation)
        {
            return method.IsAsync
                && (object)method.ReturnType != null
                && method.ReturnType.Kind == SymbolKind.NamedType
                && ((NamedTypeSymbol)method.ReturnType.TypeSymbol).ConstructedFrom == compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
        }
    }
}
