// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        /// <summary>
        /// Checks if the given method overrides Object.Equals.
        /// </summary>
        public static bool IsEqualsOverride(this IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.Name == WellKnownMemberNames.ObjectEquals &&
                   method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                   method.Parameters.Length == 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                   IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method overrides Object.GetHashCode.
        /// </summary>
        public static bool IsGetHashCodeOverride(this IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.Name == WellKnownMemberNames.ObjectGetHashCode &&
                   method.ReturnType.SpecialType == SpecialType.System_Int32 &&
                   method.Parameters.Length == 0 &&
                   IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method overrides Object.ToString.
        /// </summary>
        public static bool IsToStringOverride(this IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_String &&
                   method.Name == WellKnownMemberNames.ObjectToString &&
                   method.Parameters.Length == 0 &&
                   IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method overrides a method from System.Object
        /// </summary>
        private static bool IsObjectMethodOverride(IMethodSymbol method)
        {
            IMethodSymbol overriddenMethod = method.OverriddenMethod;
            while (overriddenMethod != null)
            {
                if (overriddenMethod.ContainingType.SpecialType == SpecialType.System_Object)
                {
                    return true;
                }

                overriddenMethod = overriddenMethod.OverriddenMethod;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given method is a Finalizer implementation.
        /// </summary>
        public static bool IsFinalizer(this IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Destructor)
            {
                return true; // for C#
            }

            if (method.Name != WellKnownMemberNames.DestructorName || method.Parameters.Length != 0 || !method.ReturnsVoid)
            {
                return false;
            }

            IMethodSymbol overridden = method.OverriddenMethod;

            if (method.ContainingType.SpecialType == SpecialType.System_Object)
            {
                // This is object.Finalize
                return true;
            }

            if (overridden == null)
            {
                return false;
            }

            for (IMethodSymbol o = overridden.OverriddenMethod; o != null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }

        /// <summary>
        /// Checks if the given method is an implementation of the given interface method 
        /// Substituted with the given typeargument.
        /// </summary>
        public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol typeArgument, INamedTypeSymbol interfaceType, string interfaceMethodName)
        {
            INamedTypeSymbol constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;
            var interfaceMethod = constructedInterface?.GetMembers(interfaceMethodName).Single() as IMethodSymbol;

            return interfaceMethod != null && method.Equals(method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// Checks if the given method implements IDisposable.Dispose()
        /// </summary>
        public static bool IsDisposeImplementation(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol iDisposable = WellKnownTypes.IDisposable(compilation);
            return method.IsDisposeImplementation(iDisposable);
        }

        /// <summary>
        /// Checks if the given method implements <see cref="IDisposable.Dispose"/> or overrides an implementation of <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public static bool IsDisposeImplementation(this IMethodSymbol method, INamedTypeSymbol iDisposable)
        {
            if (method == null)
            {
                return false;
            }

            if (method.IsOverride)
            {
                return method.OverriddenMethod.IsDisposeImplementation(iDisposable);
            }

            // Identify the implementor of IDisposable.Dispose in the given method's containing type and check
            // if it is the given method.
            return method.ReturnsVoid &&
                method.Parameters.Length == 0 &&
                method.IsImplementationOfInterfaceMethod(null, iDisposable, "Dispose");
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose()".
        /// </summary>
        private static bool HasDisposeMethodSignature(this IMethodSymbol method)
        {
            return method.Name == "Dispose" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose(bool)".
        /// </summary>
        public static bool HasDisposeBoolMethodSignature(this IMethodSymbol method)
        {
            if (method.Name == "Dispose" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.Length == 1)
            {
                IParameterSymbol parameter = method.Parameters[0];
                return parameter.Type != null &&
                    parameter.Type.SpecialType == SpecialType.System_Boolean &&
                    parameter.RefKind == RefKind.None;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given method has the signature "void Close()".
        /// </summary>
        private static bool HasDisposeCloseMethodSignature(this IMethodSymbol method)
        {
            return method.Name == "Close" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Gets the <see cref="DisposeMethodKind"/> for the given method.
        /// </summary>
        public static DisposeMethodKind GetDisposeMethodKind(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol iDisposable = WellKnownTypes.IDisposable(compilation);
            return method.GetDisposeMethodKind(iDisposable);
        }

        /// <summary>
        /// Gets the <see cref="DisposeMethodKind"/> for the given method.
        /// </summary>
        public static DisposeMethodKind GetDisposeMethodKind(this IMethodSymbol method, INamedTypeSymbol iDisposable)
        {
            if (method.ContainingType.IsDisposable(iDisposable))
            {
                if (IsDisposeImplementation(method, iDisposable) ||
                    (method.ContainingType == iDisposable &&
                     method.HasDisposeMethodSignature()))
                {
                    return DisposeMethodKind.Dispose;
                }
                else if (method.HasDisposeBoolMethodSignature())
                {
                    return DisposeMethodKind.DisposeBool;
                }
                else if (method.HasDisposeCloseMethodSignature())
                {
                    return DisposeMethodKind.Close;
                }
            }

            return DisposeMethodKind.None;
        }

        /// <summary>
        /// Checks if the method is a property getter.
        /// </summary>
        public static bool IsPropertyGetter(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.PropertyGet &&
                   method.AssociatedSymbol?.GetParameters().Length == 0;
        }

        /// <summary>
        /// Checks if the method is the getter for an indexer.
        /// </summary>
        public static bool IsIndexerGetter(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.PropertyGet &&
                   method.AssociatedSymbol.IsIndexer();
        }

        /// <summary>
        /// Checks if the method is an accessor for a property.
        /// </summary>
        public static bool IsPropertyAccessor(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.PropertyGet ||
                   method.MethodKind == MethodKind.PropertySet;
        }

        /// <summary>
        /// Checks if the method is an accessor for an event.
        /// </summary>
        public static bool IsEventAccessor(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.EventAdd ||
                   method.MethodKind == MethodKind.EventRaise ||
                   method.MethodKind == MethodKind.EventRemove;
        }

        public static bool IsOperator(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.BuiltinOperator;
        }

        public static bool HasOptionalParameters(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.Parameters.Any(p => p.IsOptional);
        }

        public static IEnumerable<IMethodSymbol> GetOverloads(this IMethodSymbol method)
        {
            foreach (var member in method?.ContainingType?.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (!member.Equals(method))
                {
                    yield return member;
                }
            }
        }

        /// <summary>
        /// Determine if the specific method is an Add method that adds to a collection.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <returns>'true' if <paramref name="method"/> is believed to be the add method of a collection.</returns>
        /// <remarks>
        /// The current heuristic is that we consider a method to be an add method if its name begins with "Add" and its
        /// enclosing type derives from ICollection or any instantiation of ICollection&lt;T&gt;.
        /// </remarks>
        public static bool IsCollectionAddMethod(this IMethodSymbol method, ImmutableHashSet<INamedTypeSymbol> iCollectionTypes)
            => !iCollectionTypes.IsEmpty &&
               method.Name.StartsWith("Add", StringComparison.Ordinal) &&
               method.ContainingType.AllInterfaces.Any(i => iCollectionTypes.Contains(i.OriginalDefinition));

        /// <summary>
        /// Returns the topmost <see cref="IBlockOperation"/> for given <paramref name="method"/>.
        /// </summary>
        public static IBlockOperation GetTopmostOperationBlock(this IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var decl in method.DeclaringSyntaxReferences)
            {
                var syntax = decl.GetSyntax(cancellationToken);

                // VB Workaround: declaration.GetSyntax returns StatementSyntax nodes instead of BlockSyntax nodes
                //                GetOperation returns null for StatementSyntax, and the method's operation block for BlockSyntax.
                if (compilation.Language == LanguageNames.VisualBasic)
                {
                    syntax = syntax.Parent;
                }

                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                foreach (var descendant in syntax.DescendantNodesAndSelf())
                {
                    var operation = semanticModel.GetOperation(descendant, cancellationToken);
                    if (operation is IBlockOperation blockOperation)
                    {
                        return blockOperation;
                    }
                }
            }

            return null;
        }

        public static bool IsLambdaOrLocalFunctionOrDelegate(this IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.LambdaMethod:
                case MethodKind.LocalFunction:
                case MethodKind.DelegateInvoke:
                    return true;

                default:
                    return false;
            }
        }
    }
}
