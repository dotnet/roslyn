// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class MethodSymbolExtensions
    {
        public static bool IsParams(this MethodSymbol method)
        {
            return method.ParameterCount != 0 && method.Parameters[method.ParameterCount - 1].IsParams;
        }

        internal static bool IsSynthesizedLambda(this MethodSymbol method)
        {
            Debug.Assert((object)method != null);
            return method is
            {
                IsImplicitlyDeclared: true,
                MethodKind: MethodKind.AnonymousFunction
            };
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
        public static MethodSymbol ConstructIfGeneric(this MethodSymbol method, ImmutableArray<TypeWithAnnotations> typeArguments)
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
                && method.ReturnType.IsNonGenericTaskType(compilation);
        }

        /// <summary>
        /// Returns whether this method is async and returns a generic task.
        /// </summary>
        public static bool IsGenericTaskReturningAsync(this MethodSymbol method, CSharpCompilation compilation)
        {
            return method.IsAsync
                && method.ReturnType.IsGenericTaskType(compilation);
        }

        /// <summary>
        /// Returns whether this method is async and returns an IAsyncEnumerable`1.
        /// </summary>
        public static bool IsIAsyncEnumerableReturningAsync(this MethodSymbol method, CSharpCompilation compilation)
        {
            return method.IsAsync
                && method.ReturnType.IsIAsyncEnumerableType(compilation);
        }

        /// <summary>
        /// Returns whether this method is async and returns an IAsyncEnumerator`1.
        /// </summary>
        public static bool IsIAsyncEnumeratorReturningAsync(this MethodSymbol method, CSharpCompilation compilation)
        {
            return method.IsAsync
                && method.ReturnType.IsIAsyncEnumeratorType(compilation);
        }

        internal static CSharpSyntaxNode ExtractReturnTypeSyntax(this MethodSymbol method)
        {
            method = method.PartialDefinitionPart ?? method;
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is MethodDeclarationSyntax methodDeclaration)
                {
                    return methodDeclaration.ReturnType;
                }
            }

            return (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot();
        }
    }
}
