// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    using Symbols.Source;

    internal sealed partial class SemanticAnalyzer
    {
        private ExpressionNode BindMemberAccess(QualifiedNameSyntax node, bool invoked)
        {
            var left = BindExpression(node.Left);
            return BindMemberAccess(node, left, node.Right, invoked);
        }

        private ExpressionNode BindMemberAccess(BinaryExpressionSyntax node, bool invoked)
        {
            Debug.Assert(node != null);
            var left = BindExpression(node.Left);
            return BindMemberAccess(node, left, (SimpleNameSyntax)node.Right, invoked);
        }

        private ExpressionNode BindMemberAccess(SyntaxNode node, ExpressionNode left, SimpleNameSyntax right, bool invoked)
        {
            Debug.Assert(node != null);
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(node != null);

            // A member-access consists of a primary-expression, a predefined-type, or a 
            // qualified-alias-member, followed by a "." token, followed by an identifier, 
            // optionally followed by a type-argument-list.

            // A member-access is either of the form E.I or of the form E.I<A1, ..., AK>, 
            // where E is a primary-expression, I is a single identifier and <A1, ..., AK> 
            // is an optional type-argument-list. When no type-argument-list is specified,
            // consider K to be zero. 

            // UNDONE: A member-access with a primary-expression of type dynamic is dynamically bound. 
            // UNDONE: In this case the compiler classifies the member access as a property access of 
            // UNDONE: type dynamic. The rules below to determine the meaning of the member-access are 
            // UNDONE: then applied at run-time, using the run-time type instead of the compile-time 
            // UNDONE: type of the primary-expression. If this run-time classification leads to a method 
            // UNDONE: group, then the member access must be the primary-expression of an invocation-expression.

            // The member-access is evaluated and classified as follows:

            string rightName = right.PlainName;
            int rightArity = right.Arity;
            LookupResult lookupResult = new LookupResult();

            if (left.Kind == NodeKind.NamespaceExpression)
            {
                // If K is zero and E is a namespace and E contains a nested namespace with name I, 
                // then the result is that namespace.

                var ns = ((NamespaceExpression)left).NamespaceSymbol;
                lookupResult = MemberLookupInNamespace(ns, rightName, rightArity);

                // UNDONE: Report errors if more than one, or none.

                if (lookupResult.IsViable) {
                    Symbol sym = lookupResult.Symbols.First();
                    if (sym.Kind == SymbolKind.Namespace)
                        return new NamespaceExpression(node, (NamespaceSymbol)sym);
                    else {
                        Debug.Assert(sym.Kind == SymbolKind.NamedType);
                        return new TypeExpression(node, (NamedTypeSymbol)sym);
                    }
                }
                else {
                    return null;
                }
#if SLOW
                if (node.Right.Arity == 0)
                {
                    var childnamespaces = ns.GetMembers(node.Right.Identifier.ValueText).OfType<NamespaceSymbol>();
                    var childnamespace = childnamespaces.SingleOrDefault();
                    if (childnamespace != null)
                    {
                        return new NamespaceExpression(node, childnamespace);
                    }
                }

                // Otherwise, if E is a namespace and E contains an accessible type having name I and K type
                // parameters, then the result is that type constructed with the given type arguments.

                var childTypes = ns.GetMembers(node.Right.Identifier.Text).OfType<NamedTypeSymbol>().Where(s => s.Arity == node.Right.Arity && IsMemberAccessible(s));
                var childType = childTypes.SingleOrDefault();
                if (childType != null)
                {
                    // UNDONE: Construct the child type if it is generic!
                    return new TypeExpression(node, childType);
                }
#endif
            }

            // If E is a predefined-type or a primary-expression classified as a type, if E is not a 
            // type parameter, and if a member lookup of I in E with K type parameters produces a 
            // match, then E.I is evaluated and classified as follows:
            else if (left.Kind == NodeKind.TypeExpression)
            {
                var type = ((TypeExpression)left).Type;
                if (!(type is TypeParameterSymbol))
                {
                    lookupResult = MemberLookup(type, rightName, rightArity, invoked);
                    if (lookupResult.IsViable)
                    {
                        return BindStaticMemberOfType(node, left, right, lookupResult);
                    }
                }
            }
            // If E is a property access, indexer access, variable, or value, the type of which is T, 
            // and a member lookup of I in T with K type arguments produces a match, then E.I 
            // is evaluated and classified as follows:

            // UNDONE: Classify E as prop access, indexer access, variable or value
            else {
                var type = ((ValueNode)left).Type;
                lookupResult = MemberLookup(type, rightName, rightArity, invoked);
                if (lookupResult.IsViable)
                {
                    return BindInstanceMemberOfType(node, type, left, right, lookupResult);
                }
            }

            // UNDONE: Otherwise, an attempt is made to process E.I as an extension method invocation. 

            // UNDONE: If this fails, E.I is an invalid member reference, and a binding-time error occurs.

            return null;
        }

        private ExpressionNode BindStaticMemberOfType(SyntaxNode node, ExpressionNode left, SimpleNameSyntax right, LookupResult lookupResult)
        {
            Debug.Assert(node != null);
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(lookupResult.IsViable);
            Debug.Assert(lookupResult.Symbols.Any());

            SymbolOrMethodGroup symbolOrMethods = GetSymbolOrMethodGroup(lookupResult);

            if (symbolOrMethods.IsMethodGroup) {
                // If I identifies one or more methods, then the result is a method group with no 
                // associated instance expression. If a type argument list was specified, it is used 
                // in calling a generic method.
                // UNDONE: Construct the type argument list if there is one.
                return new MethodGroup(right, null, left, symbolOrMethods.MethodGroup);
            }
            else {
                Symbol symbol = symbolOrMethods.NonMethod;

                switch (symbol.Kind) {
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        // If I identifies a type, then the result is that type constructed with the given type arguments.
                        // UNDONE: Construct the child type if it is generic!
                        return new TypeExpression(node, (TypeSymbol)symbol);

                    case SymbolKind.Property:
                        // If I identifies a static property, then the result is a property access with no
                        // associated instance expression.
                        // UNDONE: give error if not static.
                        return null;

                    case SymbolKind.Field:
                        // If I identifies a static field:
                        // UNDONE: If the field is readonly and the reference occurs outside the static constructor of 
                        // UNDONE: the class or struct in which the field is declared, then the result is a value, namely
                        // UNDONE: the value of the static field I in E.
                        // UNDONE: Otherwise, the result is a variable, namely the static field I in E.

                        // UNDONE: Need a way to mark an expression node as "I am a variable, not a value".

                        // UNDONE: Give error for non-static.
                        return null;

                    default:
                        Debug.Fail("Unexpected symbol kind");
                        return null;
                }
            }
        }

        private ExpressionNode BindInstanceMemberOfType(SyntaxNode node, TypeSymbol type,
                                                        ExpressionNode left, SimpleNameSyntax right, LookupResult lookupResult)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(node != null);
            Debug.Assert(lookupResult.IsViable);
            Debug.Assert(lookupResult.Symbols.Any());

            // UNDONE: First, if E is a property or indexer access, then the value of the property or indexer access is obtained (§7.1.1) and E is reclassified as a value.

            SymbolOrMethodGroup symbolOrMethods = GetSymbolOrMethodGroup(lookupResult);

            if (symbolOrMethods.IsMethodGroup)
            {
                // If I identifies one or more methods, then the result is a method group with an associated
                // instance expression of E. If a type argument list was specified, it is used in calling 
                // a generic method.

                // UNDONE: Construct the type argument list if there is one.
                return new MethodGroup(right, null, left, symbolOrMethods.MethodGroup);
            }

            // UNDONE: If I identifies an instance property, then the result is a property access with an associated instance expression of E. 
            // UNDONE: If T is a class-type and I identifies an instance field of that class-type:
            // UNDONE:   If the value of E is null, then a System.NullReferenceException is thrown.
            // UNDONE:   Otherwise, if the field is readonly and the reference occurs outside an instance constructor of the class in which the field is declared, then the result is a value, namely the value of the field I in the object referenced by E.
            // UNDONE:   Otherwise, the result is a variable, namely the field I in the object referenced by E.
            // UNDONE: If T is a struct-type and I identifies an instance field of that struct-type:
            // UNDONE:   If E is a value, or if the field is readonly and the reference occurs outside an instance constructor of the struct in which the field is declared, then the result is a value, namely the value of the field I in the struct instance given by E.
            // UNDONE:   Otherwise, the result is a variable, namely the field I in the struct instance given by E.
            // UNDONE: If I identifies an instance event:
            // UNDONE:   If the reference occurs within the class or struct in which the event is declared, and the event was declared without event-accessor-declarations (§10.8), then E.I is processed exactly as if I was an instance field.
            // UNDONE:   Otherwise, the result is an event access with an associated instance expression of E.

            return null;
        }

        // Represents either a single non-method symbol, or a method group.
        private struct SymbolOrMethodGroup
        {
            public readonly bool IsMethodGroup;
            public readonly Symbol NonMethod;
            public readonly List<MethodSymbol> MethodGroup;

            public SymbolOrMethodGroup(Symbol nonMethod)
            {
                this.IsMethodGroup = false;
                this.NonMethod = nonMethod;
                this.MethodGroup = null;
            }

            public SymbolOrMethodGroup(List<MethodSymbol> methodSymbols)
            {
                this.IsMethodGroup = true;
                this.NonMethod = null;
                this.MethodGroup = methodSymbols;
            }
        }

        // Given a viable LookupResult, report any ambiguity errors and return either a single non-method symbols
        // or a method group.
        private SymbolOrMethodGroup GetSymbolOrMethodGroup(LookupResult result)
        {
            Debug.Assert(result.IsViable);

            Symbol nonMethod = null;
            List<MethodSymbol> methodGroup = null;

            if (result.IsSingleton) {
                Symbol sym = result.SingleSymbol;
                if (sym.Kind == SymbolKind.Method) {
                    return new SymbolOrMethodGroup(new List<MethodSymbol> { (MethodSymbol)sym });
                }
                else {
                    return new SymbolOrMethodGroup(sym);
                }
            }
            else {
                foreach (Symbol sym in result.Symbols) {
                    if (sym.Kind == SymbolKind.Method) {
                        if (methodGroup == null) {
                            methodGroup = new List<MethodSymbol>();
                        }
                        methodGroup.Add((MethodSymbol)sym);
                    }
                    else {
                        if (nonMethod == null)
                            nonMethod = sym;
                        else {
                            // UNDONE: report ambiguity error between two non-methods.
                        }
                    }
                }

                if (nonMethod != null) {
                    if (methodGroup != null) {
                        // UNDONE: report ambiguity error between method and non-method.
                    }
                    return new SymbolOrMethodGroup(nonMethod);
                }
                else {
                    return new SymbolOrMethodGroup(methodGroup);
                }
            }
        }

        // Given two lookups done in the same scope, merge the results.
        private LookupResult MergeLookupsInSameScope(LookupResult result1, LookupResult result2)
        {
            // TODO: Make sure that this merging gives the correct semantics.
            return result1.MergeEqual(result2);
        }

        // Given two looksup in two scopes, whereby viable results in resultHiding should hide results
        // in resultHidden, merge the lookups.
        private LookupResult MergeHidingLookups(LookupResult resultHiding, LookupResult resultHidden)
        {
            // Methods hide non-methods, non-methods hide everything. We do not implement hiding by signature
            // here; that can be handled later in overload lookup. Doing this efficiently is a little complex...

            if (resultHiding.IsViable && resultHidden.IsViable) {
                if (resultHiding.IsSingleton) {
                    if (resultHiding.SingleSymbol.Kind != SymbolKind.Method)
                        return resultHiding;
                }
                else {
                    foreach (Symbol sym in resultHiding.Symbols) {
                        if (sym.Kind != SymbolKind.Method)
                            return resultHiding; // any non-method hides everything in the hiding scope.
                    }
                }

                // "resultHiding" only has methods. Hide all non-methods from resultHidden.
                if (resultHidden.IsSingleton) {
                    if (resultHidden.SingleSymbol.Kind == SymbolKind.Method)
                        return resultHiding.MergeEqual(resultHidden);
                    else 
                        return resultHiding;
                }
                else {
                    LookupResult result = resultHiding;
                    foreach (Symbol sym in resultHidden.Symbols) {
                        if (sym.Kind == SymbolKind.Method)
                            result = result.MergeEqual(LookupResult.Good(sym));
                    }
                    return result;
                }
            }
            else {
                return resultHiding.MergePrioritized(resultHidden);
            }
        }

        // Give lookups in two scopes which should be ambiguous toward eachother; merge the results.
        private LookupResult MergeAmbiguousLookups(LookupResult result1, LookupResult result2)
        {
            // TODO: Make sure that this merging works correctly.
            return result1.MergeEqual(result2);
        }

        // Does a member lookup in a single type, without considering inheritance.
        private LookupResult MemberLookupWithoutInheritance(TypeSymbol type, string name, int arity, bool invoked)
        {
            LookupResult result = new LookupResult();

            IEnumerable<Symbol> members = type.GetMembers(name);
            foreach (Symbol member in members) {
                LookupResult resultOfThisMember;
                // Do we need to exclude override members, or is that done later by overload resolution. It seems like
                // not excluding them here can't lead to problems, because we will always find the overridden method as well.

                SymbolKind memberKind = member.Kind;
                DiagnosticInfo diagInfo;
                if (WrongArity(member, arity, out diagInfo))
                    resultOfThisMember = LookupResult.WrongArity(member, diagInfo);
                else if (invoked && !IsInvocable(member))
                    resultOfThisMember = LookupResult.Bad(member, new CSDiagnosticInfo(ErrorCode.ERR_NonInvocableMemberCalled, member.GetFullName()));
                else if (!IsMemberAccessible(member))
                    resultOfThisMember = LookupResult.Inaccessible(member);
                else
                    resultOfThisMember = LookupResult.Good(member);

                result = MergeLookupsInSameScope(result, resultOfThisMember);
            }

            return result;
        }

        // Lookup member in a class, struct, enum, delegate.
        private LookupResult MemberLookupInClass(TypeSymbol type, string name, int arity, bool invoked)
        {
            Debug.Assert(type != null && type.TypeKind != TypeKind.Interface && type.TypeKind != TypeKind.TypeParameter);

            TypeSymbol currentType = type;
            LookupResult result = new LookupResult();

            while (currentType != null) {
                result = MergeHidingLookups(result, MemberLookupWithoutInheritance(currentType, name, arity, invoked));
                currentType = currentType.BaseType;
            }

            return result;
        }

        // Lookup member in interface.
        // TODO: This doesn't implement the hiding rules correctly. Implementing them correctly is
        // hard in case of diamond-style inheritence. First step is probably to get a topologically sorted
        // all interface set from the type (which should be cached on the type). Then, when a member is found, search
        // later parts of the list for types that should be not considered any more and remove them from the list or
        // otherwise mark them.
        private LookupResult MemberLookupInInterface(TypeSymbol type, string name, int arity, bool invoked)
        {
            Debug.Assert(type != null && type.TypeKind == TypeKind.Interface);

            LookupResult current = MemberLookupWithoutInheritance(type, name, arity, invoked);

            LookupResult lookupInBases = LookupResult.Empty;
            bool atLeastOneInterface = false;
            foreach (TypeSymbol baseInterface in type.Interfaces) {
                atLeastOneInterface = true;
                lookupInBases = MergeAmbiguousLookups(lookupInBases, MemberLookupInInterface(baseInterface, name, arity, invoked));
            }

            current = MergeHidingLookups(current, lookupInBases);

            // Lookup on interface includes lookup on object. This was already done if we looked in a base interface.
            if (! atLeastOneInterface) 
                current = MergeHidingLookups(current, MemberLookupInClass(System_Object, name, arity, invoked));

            return current; 
        }

        // Lookup member in type parameter
        private LookupResult MemberLookupInTypeParameter(TypeSymbol type, string name, int arity, bool invoked)
        {
            Debug.Assert(type != null && type.TypeKind == TypeKind.TypeParameter);

            // TODO: determine effective base and effective interfaces of type parameter.
            TypeSymbol effectiveBase = System_Object;
            IEnumerable<TypeSymbol> effectiveInterfaces = Enumerable.Empty<TypeSymbol>();

            LookupResult current = MemberLookupInClass(effectiveBase, name, arity, invoked);

            LookupResult lookupInInterfaces = LookupResult.Empty;
            foreach (TypeSymbol baseInterface in type.Interfaces) {
                lookupInInterfaces = MergeAmbiguousLookups(lookupInInterfaces, MemberLookupInInterface(baseInterface, name, arity, invoked));
            }

            current = MergeHidingLookups(current, lookupInInterfaces);

            return current;
        }

        private LookupResult MemberLookupInNamespace(NamespaceSymbol ns, string name, int arity)
        {
            LookupResult result = new LookupResult();

            IEnumerable<Symbol> members = ns.GetMembers(name);
            foreach (Symbol member in members) {
                LookupResult resultOfThisMember;
                DiagnosticInfo diagInfo;

                if (WrongArity(member, arity, out diagInfo))
                    resultOfThisMember = LookupResult.WrongArity(member, diagInfo);
                else if (!IsMemberAccessible(member))
                    resultOfThisMember = LookupResult.Inaccessible(member);
                else
                    resultOfThisMember = LookupResult.Good(member);

                result = MergeLookupsInSameScope(result, resultOfThisMember);
            }

            return result;
        }

        // Looks up a member of given name and arity in a particular type.
        private LookupResult MemberLookup(TypeSymbol type, string name, int arity, bool invoked)
        {
            LookupResult result;

            switch (type.TypeKind) {
                case TypeKind.RefType:
                    return MemberLookup(((RefTypeSymbol)type).ReferencedType, name, arity, invoked);

                case TypeKind.TypeParameter:
                    result = MemberLookupInTypeParameter(type, name, arity, invoked); break;

                case TypeKind.Interface:
                    result = MemberLookupInInterface(type, name, arity, invoked); break;

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.ArrayType:
                    result = MemberLookupInClass(type, name, arity, invoked); break;

                case TypeKind.Error:
                case TypeKind.PointerType:
                    return LookupResult.Empty;

                case TypeKind.Unknown:
                default:
                    Debug.Fail("Unknown type kind");
                    return LookupResult.Empty;
            }

            // TODO: Diagnose ambiguity problems here, and conflicts between non-method and method? Or is that
            // done in the caller?
            return result;


#if SLOW
            // A member lookup is the process whereby the meaning of a name in the context of
            // a type is determined. A member lookup can occur as part of evaluating a 
            // simple-name or a member-access in an expression. If the 
            // simple-name or member-access occurs as the simple-expression of an 
            // invocation-expression, the member is said to be invoked.

            // If a member is a method or event, or if it is a constant, field or property 
            // of a delegate type, then the member is said to be invocable.

            // Member lookup considers not only the name of a member but also the number of
            // type parameters the member has and whether the member is accessible. For the 
            // purposes of member lookup, generic methods and nested generic types have the 
            // number of type parameters indicated in their respective declarations and all 
            // other members have zero type parameters.

            // A member lookup of a name N with K type parameters in a type T is processed as follows:

            // First, a set of accessible members named N is determined.

            // If T is a type parameter, then the set is the union of the sets of accessible 
            // members named N in each of the types specified as a primary constraint or secondary 
            // constraint for T, along with the set of accessible members named N in object.

            // Otherwise, the set consists of all accessible members named N in T, 
            // including inherited members and the accessible members named N in object. If T is 
            // a constructed type, the set of members is obtained by substituting type arguments 
            // as described in §10.3.2. Members that include an override modifier are excluded 
            // from the set.

            var results = new HashSet<Symbol>();
            var inaccessible = new HashSet<Symbol>();
            var notInvocable = new HashSet<Symbol>();
            var hidden1 = new HashSet<Symbol>();
            var hidden2 = new HashSet<Symbol>();

            var types = new HashSet<TypeSymbol>(type.TypeAndAllBaseTypes());
            types.Add(System_Object);

            foreach (TypeSymbol t in types)
            {
                results.UnionWith(
                    from s in t.GetMembers(name)
                    where !s.IsOverride
                    select s);
            }

            inaccessible.UnionWith(from s in results where !IsMemberAccessible(s) select s);
            results.ExceptWith(inaccessible);

            var badArity = new HashSet<Symbol>();

            // Next, if K is zero, all nested types whose declarations include type parameters are removed. 
            // If K is not zero, all members with a different number of type parameters are removed. 
            // Note that when K is zero, methods having type parameters are not removed, since the 
            // type inference process might be able to infer the type arguments.

            if (arity == 0)
            {
                badArity.UnionWith(from s in results where s.IsNestedType() && ((NamedTypeSymbol)s).Arity != 0 select s);
            }
            else
            {
                badArity.UnionWith(from s in results where s is NamedTypeSymbol && ((NamedTypeSymbol)s).Arity != arity select s);
                badArity.UnionWith(from s in results where s is MethodSymbol && ((MethodSymbol)s).TypeParameters.Count != arity select s);
            }

            results.ExceptWith(badArity);

            // Next, if the member is invoked, all non-invocable members are removed from the set.
            if (invoked)
            {
                notInvocable.UnionWith(from s in results where !IsInvocable(s) select s);
            }

            results.ExceptWith(notInvocable);

            // Next, members that are hidden by other members are removed from the set. 
            // For every member S.M in the set, where S is the type in which the member M is declared, 
            // the following rules are applied:
            foreach (var member in results)
            {
                var declaringType = member.ContainingType;

                // If M is a constant, field, property, event, or enumeration member, 
                // then all members declared in a base type of S are removed from the set.
                if (member is FieldSymbol || member is PropertySymbol /* UNDONE || member is EventSymbol */)
                {
                    foreach (var baseType in declaringType.AllBaseTypeDefinitions())
                    {
                        hidden1.UnionWith(
                            from s in results
                            where s.ContainingType.OriginalDefinition == baseType
                            select s);
                    }
                }
                else if (member is NamedTypeSymbol)
                {
                    // If M is a type declaration, then all non-types declared in a base type of S 
                    // are removed from the set, and all type declarations with the same number of
                    // type parameters as M declared in a base type of S are removed from the set.
                    foreach (var baseType in declaringType.AllBaseTypeDefinitions())
                    {
                        hidden1.UnionWith(
                            from s in results
                            where s.ContainingType.OriginalDefinition == baseType
                            where !(s is NamedTypeSymbol) || ((NamedTypeSymbol)s).Arity == ((NamedTypeSymbol)member).Arity
                            select s);
                    }
                }
                else if (member is MethodSymbol)
                {
                    // If M is a method, then all non-method members declared in a base type of S
                    // are removed from the set.
                    foreach (var baseType in declaringType.AllBaseTypeDefinitions())
                    {
                        hidden1.UnionWith(
                            from m in results
                            where m.ContainingType.OriginalDefinition == baseType && !(m is MethodSymbol)
                            select m);
                    }
                }
            }

            results.ExceptWith(hidden1);

            // Next, interface members that are hidden by class members are removed from the set. 
            // This step only has an effect if T is a type parameter and T has both an effective base 
            // class other than object and a non-empty effective interface set. For every 
            // member S.M in the set, where S is the type in which the member M is declared, the 
            // following rules are applied if S is a class declaration other than object:

            foreach (var member in results)
            {
                var declaringType = member.ContainingType;
                if (!declaringType.IsClassType() || declaringType == System_Object)
                {
                    continue;
                }

                // If M is a constant, field, property, event, enumeration member, or type declaration, 
                // then all members declared in an interface declaration are removed from the set.
                if (member is FieldSymbol || member is PropertySymbol /* UNDONE || member is EventSymbol */ || member is NamedTypeSymbol)
                {
                    hidden2.UnionWith(
                        from m in results
                        where m.ContainingType.IsInterfaceType()
                        select m);
                }
                else if (member is MethodSymbol)
                {
                    // If M is a method, then all non-method members declared in an interface declaration 
                    // are removed from the set, and all methods with the same signature as M declared 
                    // in an interface declaration are removed from the set.
                    hidden2.UnionWith(
                        from m in results
                        where m.ContainingType.IsInterfaceType() && (!(m is MethodSymbol) || HaveSameSignature((MethodSymbol)m, (MethodSymbol)member))
                        select m);
                }
            }

            results.ExceptWith(hidden2);
            hidden1.UnionWith(hidden2);

            // Finally, having removed hidden members, the result of the lookup is determined:

            // If the set consists of a single member that is not a method, then this member is the result of the lookup.
            // Otherwise, if the set contains only methods, then this group of methods is the result of the lookup.

            // Otherwise, the lookup is ambiguous, and a compile-time error occurs.

            // For member lookups in types other than type parameters and interfaces, and member lookups in interfaces 
            // that are strictly single-inheritance (each interface in the inheritance chain has exactly zero or one direct 
            // base interface), the effect of the lookup rules is simply that derived members hide base members with the 
            // same name or signature. Such single-inheritance lookups are never ambiguous. The ambiguities that can possibly 
            // arise from member lookups in multiple-inheritance interfaces are described in §13.2.5.

            // UNDONE: Make this match what the original compiler does.
            if (results.Count == 0 || 
                (results.Count > 1 && !results.All(m => m is MethodSymbol)))
            {
                if (inaccessible.Count != 0)
                {
                    return LookupResult.Inaccessible(inaccessible.First());
                }
                else if (badArity.Count != 0)
                {
                    return LookupResult.WrongArity(badArity.First(), null);
                }
                else
                {
                    return LookupResult.Bad((DiagnosticInfo)null);
                }
            }
            else
            {
                return LookupResult.ForSymbols(results);
            }
#endif
        }

#if SLOW
        private static bool HaveSameSignature(MethodSymbol m1, MethodSymbol m2)
        {
            // UNDONE:
            return false;
        }
#endif

        // Check if the given symbol can be accessed with the given arity. If OK, return false.
        // If not OK, return true and return a diagnosticinfo. Note that methods with type arguments
        // can be accesses with arity zero due to type inference (but non types).
        private bool WrongArity(Symbol symbol, int arity, out DiagnosticInfo diagInfo)
        {
            switch (symbol.Kind) {
                case SymbolKind.NamedType:
                    NamedTypeSymbol namedType = (NamedTypeSymbol)symbol;
                    if (namedType.Arity != arity) {
                        diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadArity, namedType.GetFullName(), namedType.Arity);
                        return true;
                    }
                    break;

                case SymbolKind.Method:
                    if (arity != 0) {
                        MethodSymbol method = (MethodSymbol)symbol;
                        if (method.Arity != arity) {
                            diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_HasNoTypeVars, method.GetFullName());
                            return true;
                        }
                    }
                    break;

                default:
                    if (arity != 0) {
                        diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_TypeArgsNotAllowed, symbol.GetFullName());
                        return true;
                    }
                    break;
            }

            diagInfo = null;
            return false;
        }

        // Is the given member accessible from the current method?
        private bool IsMemberAccessible(Symbol symbol)
        {
            // UNDONE: This code must exist in the context looker-upper as well; rationalize that to prevent duplicated code.
            if (symbol == null)
            {
                return true;
            }

            switch (symbol.DeclaredAccessibility)
            {
                default:
                case Accessibility.NotApplicable:
                    Debug.Fail("Unexpected accessibility");
                    return false;
                case Accessibility.Internal:
                    // UNDONE: friend assemblies
                    if (symbol.ContainingAssembly != this.containingMethod.ContainingAssembly)
                    {
                        return false;
                    }

                    return IsMemberAccessible(symbol.ContainingType);
                case Accessibility.Private:
                    return this.containingMethod.OuterTypeDefinitions().Contains(symbol.ContainingType);
                case Accessibility.Protected:
                    return this.containingMethod.ContainingType.TypeAndAllOuterTypeAndBaseTypeDefinitions().Contains(symbol.ContainingType.OriginalDefinition);
                case Accessibility.ProtectedAndInternal:
                    if (!this.containingMethod.ContainingType.TypeAndAllOuterTypeAndBaseTypeDefinitions().Contains(symbol.ContainingType.OriginalDefinition))
                    {
                        return false;
                    }

                    goto case Accessibility.Internal;
                case Accessibility.ProtectedInternal:
                    if (this.containingMethod.ContainingType.TypeAndAllOuterTypeAndBaseTypeDefinitions().Contains(symbol.ContainingType.OriginalDefinition))
                    {
                        return true;
                    }

                    goto case Accessibility.Internal;

                case Accessibility.Public:
                    return IsMemberAccessible(symbol.ContainingType);
            }
        }
    }
}
