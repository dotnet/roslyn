// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class SyntaxBinder
    {
        // A simple-name is either of the form I or of the form I<A1, ..., AK>, where I is a single 
        // identifier and <A1, ..., AK> is an optional type-argument-list. When no type-argument-list 
        // is specified, consider K to be zero. The simple-name is evaluated and classified as follows:

        private BoundExpression BindIdentifier(IdentifierNameSyntax node)
        {
            Debug.Assert(node != null);

            // If K is zero and the simple-name appears within a block and if the block’s 
            // (or an enclosing block’s) local variable declaration space contains a local variable, parameter
            // or constant with name I, then the simple-name refers to that local variable, parameter
            // or constant and is classified as a variable or value.

            // If K is zero and the simple-name appears within the body of a generic method declaration 
            // and if that declaration includes a type parameter with name I, then the simple-name refers 
            // to that type parameter.


            // UNDONE: I think we need to use a form of Lookup that takes an arity, and explicity pass 0. Otherwise
            // UNDONE: we will find generic 
            var result = context.Lookup(node.PlainName, null, null);
            if (result.IsViable) {
                SymbolOrMethodGroup symbolOrMethods = GetSymbolOrMethodGroup(result);

                if (symbolOrMethods.IsMethodGroup) {
                    // If T is the instance type of the immediately enclosing class or struct type and the lookup identifies
                    // one or more methods, the result is a method group with an associated instance expression of this. 

                    // The semantics of lookup mean that we'll only find members associated with one containing
                    // class or struct, or bases thereof. 

                    // UNDONE: Construct the type argument list if there is one.
                    if (IsMemberOfType(symbolOrMethods.MethodGroup[0], this.containingMethod.ContainingType))
                        return new BoundMethodGroup(node, null, new BoundThisReference(null, this.containingMethod.ContainingType), symbolOrMethods.MethodGroup);
                    else {
                        // UNDONE: diagnose error, it was a method in an enclosing type that wasn't immediately enclosing.
                        return null;
                    }
                }
                else {
                    Symbol symbol = symbolOrMethods.NonMethod;

                    switch (symbol.Kind) {
                        case SymbolKind.Local:
                            // UNDONE: Better ctor for local that doesn't take a type.
                            return new BoundLocal(node, (LocalSymbol)symbol, ((LocalSymbol)symbol).Type);

                        case SymbolKind.Parameter:
                            // UNDONE: Formal parameter
                            Debug.Fail("Undone: formal parameters");
                            return null;

                        case SymbolKind.NamedType:
                        case SymbolKind.ErrorType:
                            // If I identifies a type, then the result is that type constructed with the given type arguments.
                            // UNDONE: Construct the child type if it is generic!
                            return new BoundTypeExpression(node, (TypeSymbol)symbol);

                        case SymbolKind.Property:
                        case SymbolKind.Field:
                            // UNDONE: Otherwise, if T is the instance type of the immediately enclosing class or struct type, 
                            // UNDONE: if the lookup identifies an instance member, and if the reference occurs within the 
                            // UNDONE: block of an instance constructor, an instance method, or an instance accessor, the 
                            // UNDONE: result is the same as a member access of the form this.I. This can only happen when K is zero.

                            // UNDONE: Otherwise, the result is the same as a member access of the form T.I or T.I<A1, ..., AK>. In this case, it is a 
                            // UNDONE: compile-time error for the simple-name to refer to an instance member.

                            bool inImmediateEnclosing = IsMemberOfType(symbol, this.containingMethod.ContainingType);
                            bool isStatic = symbol.IsStatic;

                            return null;

                        case SymbolKind.Namespace:
                            return new BoundNamespaceExpression(node, (NamespaceSymbol)symbol);

                        default:
                            Debug.Fail("Unexpected symbol kind");
                            return null;
                    }
                }
            }

#if SLOW        // A lookup in the containing types is already done by context.Lookup.
                // So no need to do it again.


            // Otherwise, for each instance type T, starting with the instance type of the immediately enclosing
            // type declaration and continuing with the instance type of each enclosing class or struct declaration (if any):

            foreach (NamedTypeSymbol t in this.containingMethod.ContainingType.TypeAndOuterTypes())
            {
                // If K is zero and the declaration of T includes a type parameter with name I, 
                // then the simple-name refers to that type parameter.
                var typeParameter = t.TypeParameters.FirstOrDefault(p => p.Name == node.PlainName);
                if (typeParameter != null)
                {
                    Debug.Fail("Undone: type parameters");
                    return null;
                    // UNDONE: Type parameter
                }

                // Otherwise, if a member lookup of I in T with K type arguments produces a match:

                var lookupResult = MemberLookup(t, node.PlainName, 0, false);
                if (lookupResult.IsViable)
                {
                    Debug.Assert(lookupResult.Symbols.Any());
                    // If T is the instance type of the immediately enclosing class or struct type and the lookup identifies
                    // one or more methods, the result is a method group with an associated instance expression of this. 

                    if (t == this.containingMethod.ContainingType)
                    {
                        if (lookupResult.Symbols.OfType<MethodSymbol>().Any())
                        {
                            return new BoundMethodGroup(node, null, 
                                new BoundThisReference(null, t), 
                                lookupResult.Symbols.OfType<MethodSymbol>().ToList());
                        }

                        // UNDONE: Otherwise, if T is the instance type of the immediately enclosing class or struct type, 
                        // UNDONE: if the lookup identifies an instance member, and if the reference occurs within the 
                        // UNDONE: block of an instance constructor, an instance method, or an instance accessor, the 
                        // UNDONE: result is the same as a member access of the form this.I. This can only happen when K is zero.

                        // UNDONE: Field, event, property...
                    }

                    // UNDONE: Otherwise, the result is the same as a member access of the form T.I or T.I<A1, ..., AK>. In this case, it is a 
                    // UNDONE: compile-time error for the simple-name to refer to an instance member.
                }
            }
#endif
#if false
            // Otherwise, for each namespace N, starting with the namespace in which the simple-name occurs, 
            // continuing with each enclosing namespace (if any), and ending with the global namespace, 
            // the following steps are evaluated until an entity is located: 

            foreach(var ns in containingMethod.ContainingNamespaces())
            {

                // If K is zero and I is the name of a namespace in N, then:
                var childNamespace = ns.GetMembers(node.PlainName).OfType<NamespaceSymbol>().FirstOrDefault();
                if (childNamespace != null)
                {
                    // UNDONE: If the location where the simple-name occurs is enclosed by a 
                    // UNDONE: namespace declaration for N and the namespace declaration contains 
                    // UNDONE: an extern-alias-directive or using-alias-directive that associates the 
                    // UNDONE: name I with a namespace or type, then the simple-name is ambiguous and 
                    // UNDONE: a compile-time error occurs.

                    // Otherwise, the simple-name refers to the namespace named I in N.
                    return new NamespaceExpression(node, childNamespace);
                }
                // Otherwise, if N contains an accessible type having name I and K type parameters, then:
                var childType = ns.GetMembers(node.PlainName).OfType<TypeSymbol>().FirstOrDefault();
                // UNDONE: Check accessibility
                if (childType != null)
                {
                    // UNDONE: If K is zero and the location where the simple-name occurs 
                    // UNDONE: is enclosed by a namespace declaration for N and the namespace 
                    // UNDONE: declaration contains an extern-alias-directive or using-alias-directive 
                    // UNDONE: that associates the name I with a namespace or type, then the simple-name 
                    // UNDONE: is ambiguous and a compile-time error occurs.

                    // Otherwise, the namespace-or-type-name refers to the type constructed with the given type arguments.
                    return new TypeExpression(node, childType);
                }
                // UNDONE: Otherwise, if the location where the simple-name occurs is enclosed by a namespace declaration for N:

                // UNDONE: If K is zero and the namespace declaration contains an extern-alias-directive or using-alias-directive 
                // UNDONE: that associates the name I with an imported namespace or type, then the simple-name refers to that namespace or type.
                // UNDONE: Otherwise, if the namespaces imported by the using-namespace-directives of the namespace declaration 
                // UNDONE: contain exactly one type having name I and K type parameters, then the simple-name refers to that type 
                // UNDONE: constructed with the given type arguments.
                // UNDONE: Otherwise, if the namespaces imported by the using-namespace-directives of the namespace declaration contain more than one type having name I and K type parameters, then the simple-name is ambiguous and an error occurs.
            }
#endif
            // UNDONE: Otherwise, the simple-name is undefined and a compile-time error occurs.
            return null;
        }

        private BoundExpression BindGenericName(GenericNameSyntax node)
        {
            // TODO: I think this method can share code with BindIdentifier. (petergo)

            Debug.Assert(node != null);

            // For each instance type T, starting with the instance type of the immediately enclosing
            // type declaration and continuing with the instance type of each enclosing class or struct declaration (if any):

            var typeArguments = node.Arguments.Select(x => context.BindType(x)).ToArray();

            foreach (NamedTypeSymbol t in this.containingMethod.ContainingType.TypeAndOuterTypes())
            {
                // If a member lookup of I in T with K type arguments produces a match:

                var lookupResult = MemberLookup(t, node.PlainName, node.Arity, false);
                if (lookupResult.IsViable)
                {
                    Debug.Assert(lookupResult.Symbols.Any());
                    // If T is the instance type of the immediately enclosing class or struct type and the lookup identifies
                    // one or more methods, the result is a method group with an associated instance expression of this. 

                    if (t == this.containingMethod.ContainingType)
                    {
                        if (lookupResult.Symbols.OfType<MethodSymbol>().Any())
                        {
                            return new BoundMethodGroup(node, typeArguments, 
                                new BoundThisReference(null, t), 
                                lookupResult.Symbols.OfType<MethodSymbol>().ToList());
                        }
                    }
                    // UNDONE: Otherwise, the result is the same as a member access of the form T.I<A1, ..., AK>. In this case, it is a 
                    // UNDONE: compile-time error for the simple-name to refer to an instance member.
                }
            }

#if false
            // Otherwise, for each namespace N, starting with the namespace in which the simple-name occurs, 
            // continuing with each enclosing namespace (if any), and ending with the global namespace, 
            // the following steps are evaluated until an entity is located: 

            foreach(var ns in containingMethod.ContainingNamespaces())
            {
                // If N contains an accessible type having name I and K type parameters, then:
                var childType = ns.GetMembers(node.PlainName).OfType<TypeSymbol>().FirstOrDefault();

                // UNDONE: Check accessibility
                // UNDONE: Check arity

                if (childType != null)
                {
                    // The namespace-or-type-name refers to the type constructed with the given type arguments.
                    return new TypeExpression(node, childType);
                }

                // UNDONE: Otherwise, if the location where the simple-name occurs is enclosed by a namespace declaration for N:

                // UNDONE: Otherwise, if the namespaces imported by the using-namespace-directives of the namespace declaration 
                // UNDONE: contain more than one type having name I and K type parameters, then the simple-name is ambiguous and 
                // UNDONE: an error occurs.
            }
#endif
            // UNDONE: Until we get the lookup and error reporting logic above correct we can simply use the context lookup:
            var result = context.LookupType(node.PlainName, node.Arity, null, ConsList<Symbol>.Empty);
            if (result.IsViable)
            {
                var type = result.Symbols.OfType<NamedTypeSymbol>().FirstOrDefault();
                if (type != null)
                {
                    return new BoundTypeExpression(node, type.Construct(typeArguments));
                }
            }

            // UNDONE: Otherwise, the simple-name is undefined and a compile-time error occurs.
            return null;
        }

        // Determine if the symbol "member" is a member of the type "type" or one of its
        // base types.
        private bool IsMemberOfType(Symbol member, NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            Debug.Assert(member != null);

            NamedTypeSymbol container = member.ContainingType;
            NamedTypeSymbol currentType = type;

            while (currentType != null) {
                if (container.Equals(currentType))
                    return true;
                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}
