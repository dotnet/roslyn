// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// A language agnostic factory for creating syntax nodes.
    /// 
    /// This API can be used to create language specific syntax nodes that are semantically similar between languages.
    /// </summary>
    public abstract class SyntaxGenerator : ILanguageService
    {
        public static SyntaxRemoveOptions DefaultRemoveOptions = SyntaxRemoveOptions.KeepUnbalancedDirectives | SyntaxRemoveOptions.AddElasticMarker;

        /// <summary>
        /// Gets the <see cref="SyntaxGenerator"/> for the specified language.
        /// </summary>
        public static SyntaxGenerator GetGenerator(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language).GetService<SyntaxGenerator>();
        }

        /// <summary>
        /// Gets the <see cref="SyntaxGenerator"/> for the language corresponding to the document.
        /// </summary>
        public static SyntaxGenerator GetGenerator(Document document)
        {
            return GetGenerator(document.Project);
        }

        /// <summary>
        /// Gets the <see cref="SyntaxGenerator"/> for the language corresponding to the project.
        /// </summary>
        public static SyntaxGenerator GetGenerator(Project project)
        {
            return project.LanguageServices.GetService<SyntaxGenerator>();
        }

        #region Declarations

        /// <summary>
        /// Returns the node if it is a declaration, the immediate enclosing declaration if one exists, or null.
        /// </summary>
        public SyntaxNode GetDeclaration(SyntaxNode node)
        {
            while (node != null)
            {
                if (GetDeclarationKind(node) != DeclarationKind.None)
                {
                    return node;
                }
                else
                {
                    node = node.Parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the enclosing declaration of the specified kind or null.
        /// </summary>
        public SyntaxNode GetDeclaration(SyntaxNode node, DeclarationKind kind)
        {
            while (node != null)
            {
                if (GetDeclarationKind(node) == kind)
                {
                    return node;
                }
                else
                {
                    node = node.Parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a field declaration.
        /// </summary>
        public abstract SyntaxNode FieldDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            SyntaxNode initializer = null);

        /// <summary>
        /// Creates a field declaration matching an existing field symbol.
        /// </summary>
        public SyntaxNode FieldDeclaration(IFieldSymbol field)
        {
            var initializer = field.HasConstantValue ? this.LiteralExpression(field.ConstantValue) : null;
            return FieldDeclaration(field, initializer);
        }

        /// <summary>
        /// Creates a field declaration matching an existing field symbol.
        /// </summary>
        public SyntaxNode FieldDeclaration(IFieldSymbol field, SyntaxNode initializer)
        {
            return FieldDeclaration(
                field.Name,
                TypeExpression(field.Type),
                field.DeclaredAccessibility,
                DeclarationModifiers.From(field),
                initializer);
        }

        /// <summary>
        /// Creates a method declaration.
        /// </summary>
        public abstract SyntaxNode MethodDeclaration(
            string name,
            IEnumerable<SyntaxNode> parameters = null,
            IEnumerable<string> typeParameters = null,
            SyntaxNode returnType = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> statements = null);

        /// <summary>
        /// Creates a method declaration matching an existing method symbol.
        /// </summary>
        public SyntaxNode MethodDeclaration(IMethodSymbol method, IEnumerable<SyntaxNode> statements = null)
        {
            var decl = MethodDeclaration(
                method.Name,
                parameters: method.Parameters.Select(p => ParameterDeclaration(p)),
                returnType: method.ReturnType.IsSystemVoid() ? null : TypeExpression(method.ReturnType),
                accessibility: method.DeclaredAccessibility,
                modifiers: DeclarationModifiers.From(method),
                statements: statements);

            if (method.TypeParameters.Length > 0)
            {
                decl = this.WithTypeParametersAndConstraints(decl, method.TypeParameters);
            }

            return decl;
        }

        /// <summary>
        /// Creates a method declaration.
        /// </summary>
        public virtual SyntaxNode OperatorDeclaration(
            OperatorKind kind,
            IEnumerable<SyntaxNode> parameters = null,
            SyntaxNode returnType = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> statements = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a method declaration matching an existing method symbol.
        /// </summary>
        public SyntaxNode OperatorDeclaration(IMethodSymbol method, IEnumerable<SyntaxNode> statements = null)
        {
            if (method.MethodKind != MethodKind.UserDefinedOperator)
            {
                throw new ArgumentException("Method is not an operator.");
            }

            var decl = OperatorDeclaration(
                GetOperatorKind(method),
                parameters: method.Parameters.Select(p => ParameterDeclaration(p)),
                returnType: method.ReturnType.IsSystemVoid() ? null : TypeExpression(method.ReturnType),
                accessibility: method.DeclaredAccessibility,
                modifiers: DeclarationModifiers.From(method),
                statements: statements);

            return decl;
        }

        private OperatorKind GetOperatorKind(IMethodSymbol method)
        {
            switch (method.Name)
            {
                case WellKnownMemberNames.ImplicitConversionName: return OperatorKind.ImplicitConversion;
                case WellKnownMemberNames.ExplicitConversionName: return OperatorKind.ExplicitConversion;
                case WellKnownMemberNames.AdditionOperatorName: return OperatorKind.Addition;
                case WellKnownMemberNames.BitwiseAndOperatorName: return OperatorKind.BitwiseAnd;
                case WellKnownMemberNames.BitwiseOrOperatorName: return OperatorKind.BitwiseOr;
                case WellKnownMemberNames.DecrementOperatorName: return OperatorKind.Decrement;
                case WellKnownMemberNames.DivisionOperatorName: return OperatorKind.Division;
                case WellKnownMemberNames.EqualityOperatorName: return OperatorKind.Equality;
                case WellKnownMemberNames.ExclusiveOrOperatorName: return OperatorKind.ExclusiveOr;
                case WellKnownMemberNames.FalseOperatorName: return OperatorKind.False;
                case WellKnownMemberNames.GreaterThanOperatorName: return OperatorKind.GreaterThan;
                case WellKnownMemberNames.GreaterThanOrEqualOperatorName: return OperatorKind.GreaterThanOrEqual;
                case WellKnownMemberNames.IncrementOperatorName: return OperatorKind.Increment;
                case WellKnownMemberNames.InequalityOperatorName: return OperatorKind.Inequality;
                case WellKnownMemberNames.LeftShiftOperatorName: return OperatorKind.LeftShift;
                case WellKnownMemberNames.LessThanOperatorName: return OperatorKind.LessThan;
                case WellKnownMemberNames.LessThanOrEqualOperatorName: return OperatorKind.LessThanOrEqual;
                case WellKnownMemberNames.LogicalNotOperatorName: return OperatorKind.LogicalNot;
                case WellKnownMemberNames.ModulusOperatorName: return OperatorKind.Modulus;
                case WellKnownMemberNames.MultiplyOperatorName: return OperatorKind.Multiply;
                case WellKnownMemberNames.OnesComplementOperatorName: return OperatorKind.OnesComplement;
                case WellKnownMemberNames.RightShiftOperatorName: return OperatorKind.RightShift;
                case WellKnownMemberNames.SubtractionOperatorName: return OperatorKind.Subtraction;
                case WellKnownMemberNames.TrueOperatorName: return OperatorKind.True;
                case WellKnownMemberNames.UnaryNegationOperatorName: return OperatorKind.UnaryNegation;
                case WellKnownMemberNames.UnaryPlusOperatorName: return OperatorKind.UnaryPlus;
                default:
                    throw new ArgumentException("Unknown operator kind.");
            }
        }

        /// <summary>
        /// Creates a parameter declaration.
        /// </summary>
        public abstract SyntaxNode ParameterDeclaration(
            string name,
            SyntaxNode type = null,
            SyntaxNode initializer = null,
            RefKind refKind = RefKind.None);

        /// <summary>
        /// Creates a parameter declaration matching an existing parameter symbol.
        /// </summary>
        public SyntaxNode ParameterDeclaration(IParameterSymbol symbol, SyntaxNode initializer = null)
        {
            return ParameterDeclaration(
                symbol.Name,
                TypeExpression(symbol.Type),
                initializer,
                symbol.RefKind);
        }

        /// <summary>
        /// Creates a property declaration.
        /// </summary>
        public abstract SyntaxNode PropertyDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> getAccessorStatements = null,
            IEnumerable<SyntaxNode> setAccessorStatements = null);

        /// <summary>
        /// Creates a property declaration using an existing property symbol as a signature.
        /// </summary>
        public SyntaxNode PropertyDeclaration(
            IPropertySymbol property,
            IEnumerable<SyntaxNode> getAccessorStatements = null,
            IEnumerable<SyntaxNode> setAccessorStatements = null)
        {
            return PropertyDeclaration(
                    property.Name,
                    TypeExpression(property.Type),
                    property.DeclaredAccessibility,
                    DeclarationModifiers.From(property),
                    getAccessorStatements,
                    setAccessorStatements);
        }

        /// <summary>
        /// Creates an indexer declaration.
        /// </summary>
        public abstract SyntaxNode IndexerDeclaration(
            IEnumerable<SyntaxNode> parameters,
            SyntaxNode type,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> getAccessorStatements = null,
            IEnumerable<SyntaxNode> setAccessorStatements = null);

        /// <summary>
        /// Creates an indexer declaration matching an existing indexer symbol.
        /// </summary>
        public SyntaxNode IndexerDeclaration(
            IPropertySymbol indexer,
            IEnumerable<SyntaxNode> getAccessorStatements = null,
            IEnumerable<SyntaxNode> setAccessorStatements = null)
        {
            return IndexerDeclaration(
                indexer.Parameters.Select(p => this.ParameterDeclaration(p)),
                TypeExpression(indexer.Type),
                indexer.DeclaredAccessibility,
                DeclarationModifiers.From(indexer),
                getAccessorStatements,
                setAccessorStatements);
        }

        /// <summary>
        /// Creates an event declaration.
        /// </summary>
        public abstract SyntaxNode EventDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers));

        /// <summary>
        /// Creates an event declaration from an existing event symbol
        /// </summary>
        public SyntaxNode EventDeclaration(IEventSymbol symbol)
        {
            return EventDeclaration(
                symbol.Name,
                TypeExpression(symbol.Type),
                symbol.DeclaredAccessibility,
                DeclarationModifiers.From(symbol));
        }

        /// <summary>
        /// Creates a custom event declaration.
        /// </summary>
        public abstract SyntaxNode CustomEventDeclaration(
            string name,
            SyntaxNode type,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> parameters = null,
            IEnumerable<SyntaxNode> addAccessorStatements = null,
            IEnumerable<SyntaxNode> removeAccessorStatements = null);

        /// <summary>
        /// Creates a custom event declaration from an existing event symbol.
        /// </summary>
        public SyntaxNode CustomEventDeclaration(
            IEventSymbol symbol,
            IEnumerable<SyntaxNode> addAccessorStatements = null,
            IEnumerable<SyntaxNode> removeAccessorStatements = null)
        {
            var invoke = symbol.Type.GetMembers("Invoke").FirstOrDefault(m => m.Kind == SymbolKind.Method) as IMethodSymbol;
            var parameters = invoke != null ? invoke.Parameters.Select(p => this.ParameterDeclaration(p)) : null;

            return CustomEventDeclaration(
                symbol.Name,
                TypeExpression(symbol.Type),
                symbol.DeclaredAccessibility,
                DeclarationModifiers.From(symbol),
                parameters: parameters,
                addAccessorStatements: addAccessorStatements,
                removeAccessorStatements: removeAccessorStatements);
        }

        /// <summary>
        /// Creates a constructor declaration.
        /// </summary>
        public abstract SyntaxNode ConstructorDeclaration(
            string containingTypeName = null,
            IEnumerable<SyntaxNode> parameters = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> baseConstructorArguments = null,
            IEnumerable<SyntaxNode> statements = null);

        /// <summary>
        /// Create a constructor declaration using 
        /// </summary>
        public SyntaxNode ConstructorDeclaration(
            IMethodSymbol constructorMethod,
            IEnumerable<SyntaxNode> baseConstructorArguments = null,
            IEnumerable<SyntaxNode> statements = null)
        {
            return ConstructorDeclaration(
                constructorMethod.ContainingType != null ? constructorMethod.ContainingType.Name : "New",
                constructorMethod.Parameters.Select(p => ParameterDeclaration(p)),
                constructorMethod.DeclaredAccessibility,
                DeclarationModifiers.From(constructorMethod),
                baseConstructorArguments,
                statements);
        }

        /// <summary>
        /// Converts method, property and indexer declarations into public interface implementations.
        /// This is equivalent to an implicit C# interface implementation (you can access it via the interface or directly via the named member.)
        /// </summary>
        public SyntaxNode AsPublicInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceType)
        {
            return this.AsPublicInterfaceImplementation(declaration, interfaceType, null);
        }

        /// <summary>
        /// Converts method, property and indexer declarations into public interface implementations.
        /// This is equivalent to an implicit C# interface implementation (you can access it via the interface or directly via the named member.)
        /// </summary>
        public abstract SyntaxNode AsPublicInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceType, string interfaceMemberName);

        /// <summary>
        /// Converts method, property and indexer declarations into private interface implementations.
        /// This is equivalent to a C# explicit interface implementation (you can declare it for access via the interface, but cannot call it directly).
        /// </summary>
        public SyntaxNode AsPrivateInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceType)
        {
            return this.AsPrivateInterfaceImplementation(declaration, interfaceType, null);
        }

        /// <summary>
        /// Converts method, property and indexer declarations into private interface implementations.
        /// This is equivalent to a C# explicit interface implementation (you can declare it for access via the interface, but cannot call it directly).
        /// </summary>
        public abstract SyntaxNode AsPrivateInterfaceImplementation(SyntaxNode declaration, SyntaxNode interfaceType, string interfaceMemberName);

        /// <summary>
        /// Creates a class declaration.
        /// </summary>
        public abstract SyntaxNode ClassDeclaration(
            string name,
            IEnumerable<string> typeParameters = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            SyntaxNode baseType = null,
            IEnumerable<SyntaxNode> interfaceTypes = null,
            IEnumerable<SyntaxNode> members = null);

        /// <summary>
        /// Creates a struct declaration.
        /// </summary>
        public abstract SyntaxNode StructDeclaration(
            string name,
            IEnumerable<string> typeParameters = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> interfaceTypes = null,
            IEnumerable<SyntaxNode> members = null);

        /// <summary>
        /// Creates a interface declaration.
        /// </summary>
        public abstract SyntaxNode InterfaceDeclaration(
            string name,
            IEnumerable<string> typeParameters = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            IEnumerable<SyntaxNode> interfaceTypes = null,
            IEnumerable<SyntaxNode> members = null);

        /// <summary>
        /// Creates an enum declaration.
        /// </summary>
        public abstract SyntaxNode EnumDeclaration(
            string name,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers),
            IEnumerable<SyntaxNode> members = null);

        /// <summary>
        /// Creates an enum member
        /// </summary>
        public abstract SyntaxNode EnumMember(string name, SyntaxNode expression = null);

        /// <summary>
        /// Creates a delegate declaration.
        /// </summary>
        public abstract SyntaxNode DelegateDeclaration(
            string name,
            IEnumerable<SyntaxNode> parameters = null,
            IEnumerable<string> typeParameters = null,
            SyntaxNode returnType = null,
            Accessibility accessibility = Accessibility.NotApplicable,
            DeclarationModifiers modifiers = default(DeclarationModifiers));

        /// <summary>
        /// Creates a declaration matching an existing symbol.
        /// </summary>
        public SyntaxNode Declaration(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    return FieldDeclaration((IFieldSymbol)symbol);

                case SymbolKind.Property:
                    var property = (IPropertySymbol)symbol;
                    if (property.IsIndexer)
                    {
                        return IndexerDeclaration(property);
                    }
                    else
                    {
                        return PropertyDeclaration(property);
                    }

                case SymbolKind.Event:
                    var ev = (IEventSymbol)symbol;
                    return EventDeclaration(ev);

                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.SharedConstructor:
                            return ConstructorDeclaration(method);

                        case MethodKind.Ordinary:
                            return MethodDeclaration(method);

                        case MethodKind.UserDefinedOperator:
                            return OperatorDeclaration(method);
                    }
                    break;

                case SymbolKind.Parameter:
                    return ParameterDeclaration((IParameterSymbol)symbol);

                case SymbolKind.NamedType:
                    var type = (INamedTypeSymbol)symbol;
                    SyntaxNode declaration = null;

                    switch (type.TypeKind)
                    {
                        case TypeKind.Class:
                            declaration = ClassDeclaration(
                                type.Name,
                                accessibility: type.DeclaredAccessibility,
                                modifiers: DeclarationModifiers.From(type),
                                baseType: TypeExpression(type.BaseType),
                                interfaceTypes: type.Interfaces != null ? type.Interfaces.Select(i => TypeExpression(i)) : null,
                                members: type.GetMembers().Where(CanBeDeclared).Select(m => Declaration(m)));
                            break;
                        case TypeKind.Struct:
                            declaration = StructDeclaration(
                                type.Name,
                                accessibility: type.DeclaredAccessibility,
                                modifiers: DeclarationModifiers.From(type),
                                interfaceTypes: type.Interfaces != null ? type.Interfaces.Select(i => TypeExpression(i)) : null,
                                members: type.GetMembers().Where(CanBeDeclared).Select(m => Declaration(m)));
                            break;
                        case TypeKind.Interface:
                            declaration = InterfaceDeclaration(
                                type.Name,
                                accessibility: type.DeclaredAccessibility,
                                interfaceTypes: type.Interfaces != null ? type.Interfaces.Select(i => TypeExpression(i)) : null,
                                members: type.GetMembers().Where(CanBeDeclared).Select(m => Declaration(m)));
                            break;
                        case TypeKind.Enum:
                            declaration = EnumDeclaration(
                                type.Name,
                                accessibility: type.DeclaredAccessibility,
                                members: type.GetMembers().Where(CanBeDeclared).Select(m => Declaration(m)));
                            break;
                        case TypeKind.Delegate:
                            var invoke = type.GetMembers("Invoke").First() as IMethodSymbol;

                            declaration = DelegateDeclaration(
                                type.Name,
                                parameters: invoke.Parameters.Select(p => ParameterDeclaration(p)),
                                returnType: TypeExpression(invoke.ReturnType),
                                accessibility: type.DeclaredAccessibility,
                                modifiers: DeclarationModifiers.From(type));
                            break;
                    }

                    if (declaration != null)
                    {
                        return WithTypeParametersAndConstraints(declaration, type.TypeParameters);
                    }

                    break;
            }

            throw new ArgumentException("Symbol cannot be converted to a declaration");
        }

        private static bool CanBeDeclared(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Parameter:
                    return true;

                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.SharedConstructor:
                        case MethodKind.Ordinary:
                            return true;
                    }
                    break;

                case SymbolKind.NamedType:
                    var type = (INamedTypeSymbol)symbol;
                    switch (type.TypeKind)
                    {
                        case TypeKind.Class:
                        case TypeKind.Struct:
                        case TypeKind.Interface:
                        case TypeKind.Enum:
                        case TypeKind.Delegate:
                            return true;
                    }
                    break;
            }

            return false;
        }

        private SyntaxNode WithTypeParametersAndConstraints(SyntaxNode declaration, ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            if (typeParameters.Length > 0)
            {
                declaration = WithTypeParameters(declaration, typeParameters.Select(tp => tp.Name));

                foreach (var tp in typeParameters)
                {
                    if (tp.HasConstructorConstraint || tp.HasReferenceTypeConstraint || tp.HasValueTypeConstraint || tp.ConstraintTypes.Length > 0)
                    {
                        declaration = this.WithTypeConstraint(declaration, tp.Name,
                            kinds: (tp.HasConstructorConstraint ? SpecialTypeConstraintKind.Constructor : SpecialTypeConstraintKind.None)
                                   | (tp.HasReferenceTypeConstraint ? SpecialTypeConstraintKind.ReferenceType : SpecialTypeConstraintKind.None)
                                   | (tp.HasValueTypeConstraint ? SpecialTypeConstraintKind.ValueType : SpecialTypeConstraintKind.None),
                            types: tp.ConstraintTypes.Select(t => TypeExpression(t)));
                    }
                }
            }

            return declaration;
        }

        /// <summary>
        /// Converts a declaration (method, class, etc) into a declaration with type parameters.
        /// </summary>
        public abstract SyntaxNode WithTypeParameters(SyntaxNode declaration, IEnumerable<string> typeParameters);

        /// <summary>
        /// Converts a declaration (method, class, etc) into a declaration with type parameters.
        /// </summary>
        public SyntaxNode WithTypeParameters(SyntaxNode declaration, params string[] typeParameters)
        {
            return WithTypeParameters(declaration, (IEnumerable<string>)typeParameters);
        }

        /// <summary>
        /// Adds a type constraint to a type parameter of a declaration.
        /// </summary>
        public abstract SyntaxNode WithTypeConstraint(SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kinds, IEnumerable<SyntaxNode> types = null);

        /// <summary>
        /// Adds a type constraint to a type parameter of a declaration.
        /// </summary>
        public SyntaxNode WithTypeConstraint(SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kinds, params SyntaxNode[] types)
        {
            return WithTypeConstraint(declaration, typeParameterName, kinds, (IEnumerable<SyntaxNode>)types);
        }

        /// <summary>
        /// Adds a type constraint to a type parameter of a declaration.
        /// </summary>
        public SyntaxNode WithTypeConstraint(SyntaxNode declaration, string typeParameterName, params SyntaxNode[] types)
        {
            return WithTypeConstraint(declaration, typeParameterName, SpecialTypeConstraintKind.None, (IEnumerable<SyntaxNode>)types);
        }

        /// <summary>
        /// Creates a namespace declaration.
        /// </summary>
        /// <param name="name">The name of the namespace.</param>
        /// <param name="declarations">Zero or more namespace or type declarations.</param>
        public abstract SyntaxNode NamespaceDeclaration(SyntaxNode name, IEnumerable<SyntaxNode> declarations);

        /// <summary>
        /// Creates a namespace declaration.
        /// </summary>
        /// <param name="name">The name of the namespace.</param>
        /// <param name="declarations">Zero or more namespace or type declarations.</param>
        public SyntaxNode NamespaceDeclaration(SyntaxNode name, params SyntaxNode[] declarations)
        {
            return NamespaceDeclaration(name, (IEnumerable<SyntaxNode>)declarations);
        }

        /// <summary>
        /// Creates a namespace declaration.
        /// </summary>
        /// <param name="name">The name of the namespace.</param>
        /// <param name="declarations">Zero or more namespace or type declarations.</param>
        public SyntaxNode NamespaceDeclaration(string name, IEnumerable<SyntaxNode> declarations)
        {
            return NamespaceDeclaration(DottedName(name), declarations);
        }

        /// <summary>
        /// Creates a namespace declaration.
        /// </summary>
        /// <param name="name">The name of the namespace.</param>
        /// <param name="declarations">Zero or more namespace or type declarations.</param>
        public SyntaxNode NamespaceDeclaration(string name, params SyntaxNode[] declarations)
        {
            return NamespaceDeclaration(DottedName(name), (IEnumerable<SyntaxNode>)declarations);
        }

        /// <summary>
        /// Creates a compilation unit declaration
        /// </summary>
        /// <param name="declarations">Zero or more namespace import, namespace or type declarations.</param>
        public abstract SyntaxNode CompilationUnit(IEnumerable<SyntaxNode> declarations);

        /// <summary>
        /// Creates a compilation unit declaration
        /// </summary>
        /// <param name="declarations">Zero or more namespace import, namespace or type declarations.</param>
        public SyntaxNode CompilationUnit(params SyntaxNode[] declarations)
        {
            return CompilationUnit((IEnumerable<SyntaxNode>)declarations);
        }

        /// <summary>
        /// Creates a namespace import declaration.
        /// </summary>
        /// <param name="name">The name of the namespace being imported.</param>
        public abstract SyntaxNode NamespaceImportDeclaration(SyntaxNode name);

        /// <summary>
        /// Creates a namespace import declaration.
        /// </summary>
        /// <param name="name">The name of the namespace being imported.</param>
        public SyntaxNode NamespaceImportDeclaration(string name)
        {
            return NamespaceImportDeclaration(DottedName(name));
        }

        /// <summary>
        /// Creates an attribute.
        /// </summary>
        public abstract SyntaxNode Attribute(SyntaxNode name, IEnumerable<SyntaxNode> attributeArguments = null);

        /// <summary>
        /// Creates an attribute.
        /// </summary>
        public SyntaxNode Attribute(string name, IEnumerable<SyntaxNode> attributeArguments = null)
        {
            return Attribute(DottedName(name), attributeArguments);
        }

        /// <summary>
        /// Creates an attribute.
        /// </summary>
        public SyntaxNode Attribute(string name, params SyntaxNode[] attributeArguments)
        {
            return Attribute(name, (IEnumerable<SyntaxNode>)attributeArguments);
        }

        /// <summary>
        /// Creates an attribute matching existing attribute data.
        /// </summary>
        public SyntaxNode Attribute(AttributeData attribute)
        {
            var args = attribute.ConstructorArguments.Select(a => this.AttributeArgument(this.TypedConstantExpression(a)))
                    .Concat(attribute.NamedArguments.Select(n => this.AttributeArgument(n.Key, this.TypedConstantExpression(n.Value))))
                    .ToImmutableReadOnlyListOrEmpty();

            return Attribute(
                name: this.TypeExpression(attribute.AttributeClass),
                attributeArguments: args.Count > 0 ? args : null);
        }

        private IEnumerable<SyntaxNode> GetSymbolAttributes(ISymbol symbol)
        {
            return symbol.GetAttributes().Select(a => Attribute(a));
        }

        /// <summary>
        /// Creates an attribute argument.
        /// </summary>
        public abstract SyntaxNode AttributeArgument(string name, SyntaxNode expression);

        /// <summary>
        /// Creates an attribute argument.
        /// </summary>
        public SyntaxNode AttributeArgument(SyntaxNode expression)
        {
            return AttributeArgument(null, expression);
        }

        /// <summary>
        /// Removes all attributes from the declaration, including return attributes.
        /// </summary>
        public SyntaxNode RemoveAllAttributes(SyntaxNode declaration)
        {
            return this.RemoveNodes(declaration, this.GetAttributes(declaration).Concat(this.GetReturnAttributes(declaration)));
        }

        /// <summary>
        /// Gets the attributes of a declaration, not including the return attributes.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetAttributes(SyntaxNode declaration);

        /// <summary>
        /// Creates a new instance of the declaration with the attributes inserted.
        /// </summary>
        public abstract SyntaxNode InsertAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes);

        /// <summary>
        /// Creates a new instance of the declaration with the attributes inserted.
        /// </summary>
        public SyntaxNode InsertAttributes(SyntaxNode declaration, int index, params SyntaxNode[] attributes)
        {
            return this.InsertAttributes(declaration, index, (IEnumerable<SyntaxNode>)attributes);
        }

        /// <summary>
        /// Creates a new instance of a declaration with the specified attributes added.
        /// </summary>
        public SyntaxNode AddAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes)
        {
            return this.InsertAttributes(declaration, this.GetAttributes(declaration).Count, attributes);
        }

        /// <summary>
        /// Creates a new instance of a declaration with the specified attributes added.
        /// </summary>
        public SyntaxNode AddAttributes(SyntaxNode declaration, params SyntaxNode[] attributes)
        {
            return AddAttributes(declaration, (IEnumerable<SyntaxNode>)attributes);
        }

        /// <summary>
        /// Gets the return attributes from the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetReturnAttributes(SyntaxNode declaration);

        /// <summary>
        /// Creates a new instance of a method declaration with return attributes inserted.
        /// </summary>
        public abstract SyntaxNode InsertReturnAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes);

        /// <summary>
        /// Creates a new instance of a method declaration with return attributes inserted.
        /// </summary>
        public SyntaxNode InsertReturnAttributes(SyntaxNode declaration, int index, params SyntaxNode[] attributes)
        {
            return this.InsertReturnAttributes(declaration, index, (IEnumerable<SyntaxNode>)attributes);
        }

        /// <summary>
        /// Creates a new instance of a method declaration with return attributes added.
        /// </summary>
        public SyntaxNode AddReturnAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes)
        {
            return this.InsertReturnAttributes(declaration, this.GetReturnAttributes(declaration).Count, attributes);
        }

        /// <summary>
        /// Creates a new instance of a method declaration node with return attributes added.
        /// </summary>
        public SyntaxNode AddReturnAttributes(SyntaxNode declaration, params SyntaxNode[] attributes)
        {
            return AddReturnAttributes(declaration, (IEnumerable<SyntaxNode>)attributes);
        }

        /// <summary>
        /// Gets the attribute arguments for the attribute declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetAttributeArguments(SyntaxNode attributeDeclaration);

        /// <summary>
        /// Creates a new instance of the attribute with the arguments inserted.
        /// </summary>
        public abstract SyntaxNode InsertAttributeArguments(SyntaxNode attributeDeclaration, int index, IEnumerable<SyntaxNode> attributeArguments);

        /// <summary>
        /// Creates a new instance of the attribute with the arguments added.
        /// </summary>
        public SyntaxNode AddAttributeArguments(SyntaxNode attributeDeclaration, IEnumerable<SyntaxNode> attributeArguments)
        {
            return this.InsertAttributeArguments(attributeDeclaration, this.GetAttributeArguments(attributeDeclaration).Count, attributeArguments);
        }

        /// <summary>
        /// Gets the namespace imports that are part of the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetNamespaceImports(SyntaxNode declaration);

        /// <summary>
        /// Creates a new instance of the declaration with the namespace imports inserted.
        /// </summary>
        public abstract SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports);

        /// <summary>
        /// Creates a new instance of the declaration with the namespace imports inserted.
        /// </summary>
        public SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, params SyntaxNode[] imports)
        {
            return this.InsertNamespaceImports(declaration, index, (IEnumerable<SyntaxNode>)imports);
        }

        /// <summary>
        /// Creates a new instance of the declaration with the namespace imports added.
        /// </summary>
        public SyntaxNode AddNamespaceImports(SyntaxNode declaration, IEnumerable<SyntaxNode> imports)
        {
            return this.InsertNamespaceImports(declaration, this.GetNamespaceImports(declaration).Count, imports);
        }

        /// <summary>
        /// Creates a new instance of the declaration with the namespace imports added.
        /// </summary>
        public SyntaxNode AddNamespaceImports(SyntaxNode declaration, params SyntaxNode[] imports)
        {
            return this.AddNamespaceImports(declaration, (IEnumerable<SyntaxNode>)imports);
        }

        /// <summary>
        /// Gets the current members of the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetMembers(SyntaxNode declaration);

        /// <summary>
        /// Creates a new instance of the declaration with the members inserted.
        /// </summary>
        public abstract SyntaxNode InsertMembers(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members);

        /// <summary>
        /// Creates a new instance of the declaration with the members inserted.
        /// </summary>
        public SyntaxNode InsertMembers(SyntaxNode declaration, int index, params SyntaxNode[] members)
        {
            return this.InsertMembers(declaration, index, (IEnumerable<SyntaxNode>)members);
        }

        /// <summary>
        /// Creates a new instance of the declaration with the members added to the end.
        /// </summary>
        public SyntaxNode AddMembers(SyntaxNode declaration, IEnumerable<SyntaxNode> members)
        {
            return this.InsertMembers(declaration, this.GetMembers(declaration).Count, members);
        }

        /// <summary>
        /// Creates a new instance of the declaration with the members added to the end.
        /// </summary>
        public SyntaxNode AddMembers(SyntaxNode declaration, params SyntaxNode[] members)
        {
            return this.AddMembers(declaration, (IEnumerable<SyntaxNode>)members);
        }

        /// <summary>
        /// Gets the accessibility of the declaration.
        /// </summary>
        public abstract Accessibility GetAccessibility(SyntaxNode declaration);

        /// <summary>
        /// Changes the accessibility of the declaration.
        /// </summary>
        public abstract SyntaxNode WithAccessibility(SyntaxNode declaration, Accessibility accessibility);

        /// <summary>
        /// Gets the <see cref="DeclarationModifiers"/> for the declaration.
        /// </summary>
        public abstract DeclarationModifiers GetModifiers(SyntaxNode declaration);

        /// <summary>
        /// Changes the <see cref="DeclarationModifiers"/> for the declaration.
        /// </summary>
        public abstract SyntaxNode WithModifiers(SyntaxNode declaration, DeclarationModifiers modifiers);

        /// <summary>
        /// Gets the <see cref="DeclarationKind"/> for the declaration.
        /// </summary>
        public abstract DeclarationKind GetDeclarationKind(SyntaxNode declaration);

        /// <summary>
        /// Gets the name of the declaration.
        /// </summary>
        public abstract string GetName(SyntaxNode declaration);

        /// <summary>
        /// Changes the name of the declaration.
        /// </summary>
        public abstract SyntaxNode WithName(SyntaxNode declaration, string name);

        /// <summary>
        /// Gets the type of the declaration.
        /// </summary>
        public abstract SyntaxNode GetType(SyntaxNode declaration);

        /// <summary>
        /// Changes the type of the declaration.
        /// </summary>
        public abstract SyntaxNode WithType(SyntaxNode declaration, SyntaxNode type);

        /// <summary>
        /// Gets the list of parameters for the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetParameters(SyntaxNode declaration);

        /// <summary>
        /// Inserts the parameters at the specified index into the declaration.
        /// </summary>
        public abstract SyntaxNode InsertParameters(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters);

        /// <summary>
        /// Adds the parameters to the declaration.
        /// </summary>
        public SyntaxNode AddParameters(SyntaxNode declaration, IEnumerable<SyntaxNode> parameters)
        {
            return this.InsertParameters(declaration, this.GetParameters(declaration).Count, parameters);
        }

        /// <summary>
        /// Gets the expression associated with the declaration.
        /// </summary>
        public abstract SyntaxNode GetExpression(SyntaxNode declaration);

        /// <summary>
        /// Changes the expression associated with the declaration.
        /// </summary>
        public abstract SyntaxNode WithExpression(SyntaxNode declaration, SyntaxNode expression);

        /// <summary>
        /// Gets the statements for the body of the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetStatements(SyntaxNode declaration);

        /// <summary>
        /// Changes the statements for the body of the declaration.
        /// </summary>
        public abstract SyntaxNode WithStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Gets the accessors for the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetAccessors(SyntaxNode declaration);

        /// <summary>
        /// Gets the accessor of the specified kind for the declaration.
        /// </summary>
        public SyntaxNode GetAccessor(SyntaxNode declaration, DeclarationKind kind)
        {
            return this.GetAccessors(declaration).FirstOrDefault(a => GetDeclarationKind(a) == kind);
        }

        /// <summary>
        /// Creates a new instance of the declaration with the accessors inserted.
        /// </summary>
        public abstract SyntaxNode InsertAccessors(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> accessors);

        /// <summary>
        /// Creates a new instance of the declaration with the accessors added.
        /// </summary>
        public SyntaxNode AddAccessors(SyntaxNode declaration, IEnumerable<SyntaxNode> accessors)
        {
            return this.InsertAccessors(declaration, this.GetAccessors(declaration).Count, accessors);
        }

        /// <summary>
        /// Gets the statements for the body of the get-accessor of the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetGetAccessorStatements(SyntaxNode declaration);

        /// <summary>
        /// Changes the statements for the body of the get-accessor of the declaration.
        /// </summary>
        public abstract SyntaxNode WithGetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Gets the statements for the body of the set-accessor of the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetSetAccessorStatements(SyntaxNode declaration);

        /// <summary>
        /// Changes the statements for the body of the set-accessor of the declaration.
        /// </summary>
        public abstract SyntaxNode WithSetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Gets a list of the base and interface types for the declaration.
        /// </summary>
        public abstract IReadOnlyList<SyntaxNode> GetBaseAndInterfaceTypes(SyntaxNode declaration);

        /// <summary>
        /// Adds a base type to the declaration
        /// </summary>
        public abstract SyntaxNode AddBaseType(SyntaxNode declaration, SyntaxNode baseType);

        /// <summary>
        /// Adds an interface type to the declaration
        /// </summary>
        public abstract SyntaxNode AddInterfaceType(SyntaxNode declaration, SyntaxNode interfaceType);

        #endregion

        #region Remove, Replace, Insert
        /// <summary>
        /// Replaces the node in the root's tree with the new node.
        /// </summary>
        public virtual SyntaxNode ReplaceNode(SyntaxNode root, SyntaxNode node, SyntaxNode newDeclaration)
        {
            if (newDeclaration != null)
            {
                return root.ReplaceNode(node, newDeclaration);
            }
            else
            {
                return this.RemoveNode(root, node);
            }
        }

        /// <summary>
        /// Inserts the new node before the specified declaration.
        /// </summary>
        public virtual SyntaxNode InsertNodesBefore(SyntaxNode root, SyntaxNode node, IEnumerable<SyntaxNode> newDeclarations)
        {
            return root.InsertNodesBefore(node, newDeclarations);
        }

        /// <summary>
        /// Inserts the new node before the specified declaration.
        /// </summary>
        public virtual SyntaxNode InsertNodesAfter(SyntaxNode root, SyntaxNode node, IEnumerable<SyntaxNode> newDeclarations)
        {
            return root.InsertNodesAfter(node, newDeclarations);
        }

        /// <summary>
        /// Removes the node from the sub tree starting at the root.
        /// </summary>
        public virtual SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node)
        {
            return RemoveNode(root, node, DefaultRemoveOptions);
        }

        /// <summary>
        /// Removes the node from the sub tree starting at the root.
        /// </summary>
        public virtual SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node, SyntaxRemoveOptions options)
        {
            return root.RemoveNode(node, options);
        }

        /// <summary>
        /// Removes all the declarations from the sub tree starting at the root.
        /// </summary>
        public SyntaxNode RemoveNodes(SyntaxNode root, IEnumerable<SyntaxNode> declarations)
        {
            var newRoot = root.TrackNodes(declarations);

            foreach (var decl in declarations)
            {
                newRoot = this.RemoveNode(newRoot, newRoot.GetCurrentNode(decl));
            }

            return newRoot;
        }
        #endregion

        #region Utility

        protected static SyntaxNode PreserveTrivia<TNode>(TNode node, Func<TNode, SyntaxNode> nodeChanger) where TNode : SyntaxNode
        {
            var nodeWithoutTrivia = node.WithoutLeadingTrivia().WithoutTrailingTrivia();

            var changedNode = nodeChanger(nodeWithoutTrivia);

            if (changedNode == nodeWithoutTrivia)
            {
                return node;
            }
            else
            {
                return changedNode
                    .WithLeadingTrivia(node.GetLeadingTrivia().Concat(changedNode.GetLeadingTrivia()))
                    .WithTrailingTrivia(changedNode.GetTrailingTrivia().Concat(node.GetTrailingTrivia()));
            }
        }

        protected static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxNode original, SyntaxNode replacement)
        {
            var combinedTriviaReplacement =
                replacement.WithLeadingTrivia(original.GetLeadingTrivia().AddRange(replacement.GetLeadingTrivia()))
                           .WithTrailingTrivia(replacement.GetTrailingTrivia().AddRange(original.GetTrailingTrivia()));

            return root.ReplaceNode(original, combinedTriviaReplacement);
        }

        protected static SyntaxNode ReplaceWithTrivia<TNode>(SyntaxNode root, TNode original, Func<TNode, SyntaxNode> replacer)
            where TNode : SyntaxNode
        {
            return ReplaceWithTrivia(root, original, replacer(original));
        }

        protected static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxToken original, SyntaxToken replacement)
        {
            var combinedTriviaReplacement =
                replacement.WithLeadingTrivia(original.LeadingTrivia.AddRange(replacement.LeadingTrivia))
                           .WithTrailingTrivia(replacement.TrailingTrivia.AddRange(original.TrailingTrivia));

            return root.ReplaceToken(original, combinedTriviaReplacement);
        }

        /// <summary>
        /// Creates a new instance of the node with the leading and trailing trivia removed and replaced with elastic markers.
        /// </summary>
        public abstract TNode ClearTrivia<TNode>(TNode node) where TNode : SyntaxNode;

        protected int IndexOf<T>(IReadOnlyList<T> list, T element)
        {
            for (int i = 0, count = list.Count; i < count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], element))
                {
                    return i;
                }
            }

            return -1;
        }

        protected static SyntaxNode ReplaceRange(SyntaxNode root, SyntaxNode node, IEnumerable<SyntaxNode> replacements)
        {
            var first = replacements.First();
            var trackedFirst = first.TrackNodes(first);
            var newRoot = root.ReplaceNode(node, trackedFirst);
            var currentFirst = newRoot.GetCurrentNode(first);
            return newRoot.InsertNodesAfter(currentFirst, replacements.Skip(1));
        }

        protected static SeparatedSyntaxList<TNode> RemoveRange<TNode>(SeparatedSyntaxList<TNode> list, int offset, int count)
            where TNode : SyntaxNode
        {
            for (; count > 0 && offset < list.Count; count--)
            {
                list = list.RemoveAt(offset);
            }

            return list;
        }

        protected static SyntaxList<TNode> RemoveRange<TNode>(SyntaxList<TNode> list, int offset, int count)
            where TNode : SyntaxNode
        {
            for (; count > 0 && offset < list.Count; count--)
            {
                list = list.RemoveAt(offset);
            }

            return list;
        }

        #endregion

        #region Statements
        /// <summary>
        /// Creates statement that allows an expression to execute in a statement context.
        /// This is typically an invocation or assignment expression.
        /// </summary>
        /// <param name="expression">The expression that is to be executed. This is usually a method invocation expression.</param>
        public abstract SyntaxNode ExpressionStatement(SyntaxNode expression);

        /// <summary>
        /// Creates a statement that can be used to return a value from a method body.
        /// </summary>
        /// <param name="expression">An optional expression that can be returned.</param>
        public abstract SyntaxNode ReturnStatement(SyntaxNode expression = null);

        /// <summary>
        /// Creates a statement that can be used to throw an exception.
        /// </summary>
        /// <param name="expression">An optional expression that can be thrown.</param>
        public abstract SyntaxNode ThrowStatement(SyntaxNode expression = null);

        /// <summary>
        /// Creates a statement that declares a single local variable.
        /// </summary>
        public abstract SyntaxNode LocalDeclarationStatement(SyntaxNode type, string identifier, SyntaxNode initializer = null, bool isConst = false);

        /// <summary>
        /// Creates a statement that declares a single local variable.
        /// </summary>
        public SyntaxNode LocalDeclarationStatement(ITypeSymbol type, string name, SyntaxNode initializer = null, bool isConst = false)
        {
            return LocalDeclarationStatement(TypeExpression(type), name, initializer, isConst);
        }

        /// <summary>
        /// Creates a statement that declares a single local variable.
        /// </summary>
        public SyntaxNode LocalDeclarationStatement(string name, SyntaxNode initializer)
        {
            return LocalDeclarationStatement((SyntaxNode)null, name, initializer);
        }

        /// <summary>
        /// Creates an if-statement
        /// </summary>
        /// <param name="condition">A condition expression.</param>
        /// <param name="trueStatements">The statements that are executed if the condition is true.</param>
        /// <param name="falseStatements">The statements that are executed if the condition is false.</param>
        public abstract SyntaxNode IfStatement(SyntaxNode condition, IEnumerable<SyntaxNode> trueStatements, IEnumerable<SyntaxNode> falseStatements = null);

        /// <summary>
        /// Creates an if statement
        /// </summary>
        /// <param name="condition">A condition expression.</param>
        /// <param name="trueStatements">The statements that are executed if the condition is true.</param>
        /// <param name="falseStatement">A single statement that is executed if the condition is false.</param>
        public SyntaxNode IfStatement(SyntaxNode condition, IEnumerable<SyntaxNode> trueStatements, SyntaxNode falseStatement)
        {
            return IfStatement(condition, trueStatements, new[] { falseStatement });
        }

        /// <summary>
        /// Creates a switch statement that branches to individual sections based on the value of the specified expression.
        /// </summary>
        public abstract SyntaxNode SwitchStatement(SyntaxNode expression, IEnumerable<SyntaxNode> sections);

        /// <summary>
        /// Creates a switch statement that branches to individual sections based on the value of the specified expression.
        /// </summary>
        public SyntaxNode SwitchStatement(SyntaxNode expression, params SyntaxNode[] sections)
        {
            return SwitchStatement(expression, (IEnumerable<SyntaxNode>)sections);
        }

        /// <summary>
        /// Creates a section for a switch statement.
        /// </summary>
        public abstract SyntaxNode SwitchSection(IEnumerable<SyntaxNode> caseExpressions, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates a single-case section a switch statement.
        /// </summary>
        public SyntaxNode SwitchSection(SyntaxNode caseExpression, IEnumerable<SyntaxNode> statements)
        {
            return SwitchSection(new[] { caseExpression }, statements);
        }

        /// <summary>
        /// Creates a default section for a switch statement.
        /// </summary>
        public abstract SyntaxNode DefaultSwitchSection(IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Create a statement that exits a switch statement and continues after it.
        /// </summary>
        public abstract SyntaxNode ExitSwitchStatement();

        /// <summary>
        /// Creates a statement that represents a using-block pattern.
        /// </summary>
        public abstract SyntaxNode UsingStatement(SyntaxNode type, string name, SyntaxNode expression, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates a statement that represents a using-block pattern.
        /// </summary>
        public SyntaxNode UsingStatement(string name, SyntaxNode expression, IEnumerable<SyntaxNode> statements)
        {
            return UsingStatement(null, name, expression, statements);
        }

        /// <summary>
        /// Creates a statement that represents a using-block pattern.
        /// </summary>
        public abstract SyntaxNode UsingStatement(SyntaxNode expression, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates a try-catch or try-catch-finally statement.
        /// </summary>
        public abstract SyntaxNode TryCatchStatement(IEnumerable<SyntaxNode> tryStatements, IEnumerable<SyntaxNode> catchClauses, IEnumerable<SyntaxNode> finallyStatements = null);

        /// <summary>
        /// Creates a try-catch or try-catch-finally statement.
        /// </summary>
        public SyntaxNode TryCatchStatement(IEnumerable<SyntaxNode> tryStatements, params SyntaxNode[] catchClauses)
        {
            return TryCatchStatement(tryStatements, (IEnumerable<SyntaxNode>)catchClauses);
        }

        /// <summary>
        /// Creates a try-finally statement.
        /// </summary>
        public SyntaxNode TryFinallyStatement(IEnumerable<SyntaxNode> tryStatements, IEnumerable<SyntaxNode> finallyStatements)
        {
            return TryCatchStatement(tryStatements, catchClauses: null, finallyStatements: finallyStatements);
        }

        /// <summary>
        /// Creates a catch-clause.
        /// </summary>
        public abstract SyntaxNode CatchClause(SyntaxNode type, string identifier, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates a catch-clause.
        /// </summary>
        public SyntaxNode CatchClause(ITypeSymbol type, string identifier, IEnumerable<SyntaxNode> statements)
        {
            return CatchClause(TypeExpression(type), identifier, statements);
        }

        /// <summary>
        /// Creates a while-loop statement
        /// </summary>
        public abstract SyntaxNode WhileStatement(SyntaxNode condition, IEnumerable<SyntaxNode> statements);

        #endregion

        #region Expressions
        /// <summary>
        /// An expression that represents the default value of a type.
        /// This is typically a null value for reference types or a zero-filled value for value types.
        /// </summary>
        public abstract SyntaxNode DefaultExpression(SyntaxNode type);
        public abstract SyntaxNode DefaultExpression(ITypeSymbol type);

        /// <summary>
        /// Creates an expression that denotes the containing method's this-parameter.
        /// </summary>
        public abstract SyntaxNode ThisExpression();

        /// <summary>
        /// Creates an expression that denotes the containing method's base-parameter.
        /// </summary>
        public abstract SyntaxNode BaseExpression();

        /// <summary>
        /// Creates a literal expression. This is typically numeric primitives, strings or chars.
        /// </summary>
        public abstract SyntaxNode LiteralExpression(object value);

        /// <summary>
        /// Creates an expression for a typed constant.
        /// </summary>
        public abstract SyntaxNode TypedConstantExpression(TypedConstant value);

        /// <summary>
        /// Creates an expression that denotes the boolean false literal.
        /// </summary>
        public SyntaxNode FalseLiteralExpression()
        {
            return LiteralExpression(false);
        }

        /// <summary>
        /// Creates an expression that denotes the boolean true literal.
        /// </summary>
        public SyntaxNode TrueLiteralExpression()
        {
            return LiteralExpression(true);
        }

        /// <summary>
        /// Creates an expression that denotes the null literal.
        /// </summary>
        public SyntaxNode NullLiteralExpression()
        {
            return LiteralExpression(null);
        }

        /// <summary>
        /// Creates an expression that denotes a simple identifier name.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public abstract SyntaxNode IdentifierName(string identifier);

        /// <summary>
        /// Creates an expression that denotes a generic identifier name.
        /// </summary>
        public abstract SyntaxNode GenericName(string identifier, IEnumerable<SyntaxNode> typeArguments);

        /// <summary>
        /// Creates an expression that denotes a generic identifier name.
        /// </summary>
        public SyntaxNode GenericName(string identifier, IEnumerable<ITypeSymbol> typeArguments)
        {
            return GenericName(identifier, typeArguments.Select(ta => TypeExpression(ta)));
        }

        /// <summary>
        /// Creates an expression that denotes a generic identifier name.
        /// </summary>
        public SyntaxNode GenericName(string identifier, params SyntaxNode[] typeArguments)
        {
            return GenericName(identifier, (IEnumerable<SyntaxNode>)typeArguments);
        }

        /// <summary>
        /// Creates an expression that denotes a generic identifier name.
        /// </summary>
        public SyntaxNode GenericName(string identifier, params ITypeSymbol[] typeArguments)
        {
            return GenericName(identifier, (IEnumerable<ITypeSymbol>)typeArguments);
        }

        /// <summary>
        /// Converts an expression that ends in a name into an expression that ends in a generic name.
        /// If the expression already ends in a generic name, the new type arguments are used instead.
        /// </summary>
        public abstract SyntaxNode WithTypeArguments(SyntaxNode expression, IEnumerable<SyntaxNode> typeArguments);

        /// <summary>
        /// Converts an expression that ends in a name into an expression that ends in a generic name.
        /// If the expression already ends in a generic name, the new type arguments are used instead.
        /// </summary>
        public SyntaxNode WithTypeArguments(SyntaxNode expression, params SyntaxNode[] typeArguments)
        {
            return WithTypeArguments(expression, (IEnumerable<SyntaxNode>)typeArguments);
        }

        /// <summary>
        /// Creates a name expression that denotes a qualified name. 
        /// The left operand can be any name expression.
        /// The right operand can be either and identifier or generic name.
        /// </summary>
        public abstract SyntaxNode QualifiedName(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates a name expression from a dotted name string.
        /// </summary>
        public SyntaxNode DottedName(string dottedName)
        {
            if (dottedName == null)
            {
                throw new ArgumentNullException(nameof(dottedName));
            }

            var parts = dottedName.Split(s_dotSeparator);

            SyntaxNode name = null;
            foreach (var part in parts)
            {
                if (name == null)
                {
                    name = IdentifierName(part);
                }
                else
                {
                    name = QualifiedName(name, IdentifierName(part)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation);
                }
            }

            return name;
        }

        private static readonly char[] s_dotSeparator = new char[] { '.' };

        /// <summary>
        /// Creates an expression that denotes a type.
        /// </summary>
        public abstract SyntaxNode TypeExpression(ITypeSymbol typeSymbol);

        /// <summary>
        /// Creates an expression that denotes a special type name.
        /// </summary>
        public abstract SyntaxNode TypeExpression(SpecialType specialType);

        /// <summary>
        /// Creates an expression that denotes an array type.
        /// </summary>
        public abstract SyntaxNode ArrayTypeExpression(SyntaxNode type);

        /// <summary>
        /// Creates an expression that denotes a nullable type.
        /// </summary>
        public abstract SyntaxNode NullableTypeExpression(SyntaxNode type);

        /// <summary>
        /// Creates an expression that denotes an assignment from the right argument to left argument.
        /// </summary>
        public abstract SyntaxNode AssignmentStatement(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a value-type equality test operation.
        /// </summary>
        public abstract SyntaxNode ValueEqualsExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a reference-type equality test operation.
        /// </summary>
        public abstract SyntaxNode ReferenceEqualsExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a value-type inequality test operation.
        /// </summary>
        public abstract SyntaxNode ValueNotEqualsExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a reference-type inequality test operation.
        /// </summary>
        public abstract SyntaxNode ReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a less-than test operation.
        /// </summary>
        public abstract SyntaxNode LessThanExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a less-than-or-equal test operation.
        /// </summary>
        public abstract SyntaxNode LessThanOrEqualExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a greater-than test operation.
        /// </summary>
        public abstract SyntaxNode GreaterThanExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a greater-than-or-equal test operation.
        /// </summary>
        public abstract SyntaxNode GreaterThanOrEqualExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a unary negation operation.
        /// </summary>
        public abstract SyntaxNode NegateExpression(SyntaxNode expression);

        /// <summary>
        /// Creates an expression that denotes an addition operation.
        /// </summary>
        public abstract SyntaxNode AddExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes an subtraction operation.
        /// </summary>
        public abstract SyntaxNode SubtractExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a multiplication operation.
        /// </summary>
        public abstract SyntaxNode MultiplyExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a division operation.
        /// </summary>
        public abstract SyntaxNode DivideExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a modulo operation.
        /// </summary>
        public abstract SyntaxNode ModuloExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a bitwise-and operation.
        /// </summary>
        public abstract SyntaxNode BitwiseAndExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a bitwise-or operation.
        /// </summary>
        public abstract SyntaxNode BitwiseOrExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a bitwise-not operation
        /// </summary>
        public abstract SyntaxNode BitwiseNotExpression(SyntaxNode operand);

        /// <summary>
        /// Creates an expression that denotes a logical-and operation.
        /// </summary>
        public abstract SyntaxNode LogicalAndExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a logical-or operation.
        /// </summary>
        public abstract SyntaxNode LogicalOrExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates an expression that denotes a logical not operation.
        /// </summary>
        public abstract SyntaxNode LogicalNotExpression(SyntaxNode expression);

        /// <summary>
        /// Creates an expression that denotes a conditional evaluation operation.
        /// </summary>
        public abstract SyntaxNode ConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse);

        /// <summary>
        /// Creates an expression that denotes a coalesce operation. 
        /// </summary>
        public abstract SyntaxNode CoalesceExpression(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Creates a member access expression.
        /// </summary>
        public abstract SyntaxNode MemberAccessExpression(SyntaxNode expression, SyntaxNode memberName);

        /// <summary>
        /// Creates a member access expression.
        /// </summary>
        public SyntaxNode MemberAccessExpression(SyntaxNode expression, string memberName)
        {
            return MemberAccessExpression(expression, IdentifierName(memberName));
        }

        /// <summary>
        /// Creates an array creation expression for a single dimensional array of specified size.
        /// </summary>
        public abstract SyntaxNode ArrayCreationExpression(SyntaxNode elementType, SyntaxNode size);

        /// <summary>
        /// Creates an array creation expression for a single dimensional array with specified initial element values.
        /// </summary>
        public abstract SyntaxNode ArrayCreationExpression(SyntaxNode elementType, IEnumerable<SyntaxNode> elements);

        /// <summary>
        /// Creates an object creation expression.
        /// </summary>
        public abstract SyntaxNode ObjectCreationExpression(SyntaxNode namedType, IEnumerable<SyntaxNode> arguments);

        /// <summary>
        /// Creates an object creation expression.
        /// </summary>
        public SyntaxNode ObjectCreationExpression(ITypeSymbol type, IEnumerable<SyntaxNode> arguments)
        {
            return ObjectCreationExpression(TypeExpression(type), arguments);
        }

        /// <summary>
        /// Creates an object creation expression.
        /// </summary>
        public SyntaxNode ObjectCreationExpression(SyntaxNode type, params SyntaxNode[] arguments)
        {
            return ObjectCreationExpression(type, (IEnumerable<SyntaxNode>)arguments);
        }

        /// <summary>
        /// Creates an object creation expression.
        /// </summary>
        public SyntaxNode ObjectCreationExpression(ITypeSymbol type, params SyntaxNode[] arguments)
        {
            return ObjectCreationExpression(type, (IEnumerable<SyntaxNode>)arguments);
        }

        /// <summary>
        /// Creates a invocation expression.
        /// </summary>
        public abstract SyntaxNode InvocationExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments);

        /// <summary>
        /// Creates a invocation expression
        /// </summary>
        public SyntaxNode InvocationExpression(SyntaxNode expression, params SyntaxNode[] arguments)
        {
            return InvocationExpression(expression, (IEnumerable<SyntaxNode>)arguments);
        }

        /// <summary>
        /// Creates a node that is an argument to an invocation.
        /// </summary>
        public abstract SyntaxNode Argument(string name, RefKind refKind, SyntaxNode expression);

        /// <summary>
        /// Creates a node that is an argument to an invocation.
        /// </summary>
        public SyntaxNode Argument(RefKind refKind, SyntaxNode expression)
        {
            return Argument(null, refKind, expression);
        }

        /// <summary>
        /// Creates a node that is an argument to an invocation.
        /// </summary>
        public SyntaxNode Argument(SyntaxNode expression)
        {
            return Argument(null, RefKind.None, expression);
        }

        /// <summary>
        /// Creates an expression that access an element of an array or indexer.
        /// </summary>
        public abstract SyntaxNode ElementAccessExpression(SyntaxNode expression, IEnumerable<SyntaxNode> arguments);

        /// <summary>
        /// Creates an expression that access an element of an array or indexer.
        /// </summary>
        public SyntaxNode ElementAccessExpression(SyntaxNode expression, params SyntaxNode[] arguments)
        {
            return ElementAccessExpression(expression, (IEnumerable<SyntaxNode>)arguments);
        }

        /// <summary>
        /// Creates an expression that evaluates to the type at runtime.
        /// </summary>
        public abstract SyntaxNode TypeOfExpression(SyntaxNode type);

        /// <summary>
        /// Creates an expression that denotes an is-type-check operation.
        /// </summary>
        public abstract SyntaxNode IsTypeExpression(SyntaxNode expression, SyntaxNode type);

        /// <summary>
        /// Creates an expression that denotes an is-type-check operation.
        /// </summary>
        public SyntaxNode IsTypeExpression(SyntaxNode expression, ITypeSymbol type)
        {
            return IsTypeExpression(expression, TypeExpression(type));
        }

        /// <summary>
        /// Creates an expression that denotes an try-cast operation.
        /// </summary>
        public abstract SyntaxNode TryCastExpression(SyntaxNode expression, SyntaxNode type);

        /// <summary>
        /// Creates an expression that denotes an try-cast operation.
        /// </summary>
        public SyntaxNode TryCastExpression(SyntaxNode expression, ITypeSymbol type)
        {
            return TryCastExpression(expression, TypeExpression(type));
        }

        /// <summary>
        /// Creates an expression that denotes a type cast operation.
        /// </summary>
        public abstract SyntaxNode CastExpression(SyntaxNode type, SyntaxNode expression);

        /// <summary>
        /// Creates an expression that denotes a type cast operation.
        /// </summary>
        public SyntaxNode CastExpression(ITypeSymbol type, SyntaxNode expression)
        {
            return CastExpression(TypeExpression(type), expression);
        }

        /// <summary>
        /// Creates an expression that denotes a type conversion operation.
        /// </summary>
        public abstract SyntaxNode ConvertExpression(SyntaxNode type, SyntaxNode expression);

        /// <summary>
        /// Creates an expression that denotes a type conversion operation.
        /// </summary>
        public SyntaxNode ConvertExpression(ITypeSymbol type, SyntaxNode expression)
        {
            return ConvertExpression(TypeExpression(type), expression);
        }

        /// <summary>
        /// Creates an expression that declares a value returning lambda expression.
        /// </summary>
        public abstract SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, SyntaxNode expression);

        /// <summary>
        /// Creates an expression that declares a void returning lambda expression
        /// </summary>
        public abstract SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, SyntaxNode expression);

        /// <summary>
        /// Creates an expression that declares a value returning lambda expression.
        /// </summary>
        public abstract SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates an expression that declares a void returning lambda expression.
        /// </summary>
        public abstract SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> lambdaParameters, IEnumerable<SyntaxNode> statements);

        /// <summary>
        /// Creates an expression that declares a single parameter value returning lambda expression.
        /// </summary>
        public SyntaxNode ValueReturningLambdaExpression(string parameterName, SyntaxNode expression)
        {
            return ValueReturningLambdaExpression(new[] { LambdaParameter(parameterName) }, expression);
        }

        /// <summary>
        /// Creates an expression that declares a single parameter void returning lambda expression.
        /// </summary>
        public SyntaxNode VoidReturningLambdaExpression(string parameterName, SyntaxNode expression)
        {
            return VoidReturningLambdaExpression(new[] { LambdaParameter(parameterName) }, expression);
        }

        /// <summary>
        /// Creates an expression that declares a single parameter value returning lambda expression.
        /// </summary>
        public SyntaxNode ValueReturningLambdaExpression(string parameterName, IEnumerable<SyntaxNode> statements)
        {
            return ValueReturningLambdaExpression(new[] { LambdaParameter(parameterName) }, statements);
        }

        /// <summary>
        /// Creates an expression that declares a single parameter void returning lambda expression.
        /// </summary>
        public SyntaxNode VoidReturningLambdaExpression(string parameterName, IEnumerable<SyntaxNode> statements)
        {
            return VoidReturningLambdaExpression(new[] { LambdaParameter(parameterName) }, statements);
        }

        /// <summary>
        /// Creates an expression that declares a zero parameter value returning lambda expression.
        /// </summary>
        public SyntaxNode ValueReturningLambdaExpression(SyntaxNode expression)
        {
            return ValueReturningLambdaExpression((IEnumerable<SyntaxNode>)null, expression);
        }

        /// <summary>
        /// Creates an expression that declares a zero parameter void returning lambda expression.
        /// </summary>
        public SyntaxNode VoidReturningLambdaExpression(SyntaxNode expression)
        {
            return VoidReturningLambdaExpression((IEnumerable<SyntaxNode>)null, expression);
        }

        /// <summary>
        /// Creates an expression that declares a zero parameter value returning lambda expression.
        /// </summary>
        public SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> statements)
        {
            return ValueReturningLambdaExpression((IEnumerable<SyntaxNode>)null, statements);
        }

        /// <summary>
        /// Creates an expression that declares a zero parameter void returning lambda expression.
        /// </summary>
        public SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> statements)
        {
            return VoidReturningLambdaExpression((IEnumerable<SyntaxNode>)null, statements);
        }

        /// <summary>
        /// Creates a lambda parameter.
        /// </summary>
        public abstract SyntaxNode LambdaParameter(string identifier, SyntaxNode type = null);

        /// <summary>
        /// Creates a lambda parameter.
        /// </summary>
        public SyntaxNode LambdaParameter(string identifier, ITypeSymbol type)
        {
            return LambdaParameter(identifier, TypeExpression(type));
        }

        /// <summary>
        /// Creates an await expression.
        /// </summary>
        public abstract SyntaxNode AwaitExpression(SyntaxNode expression);

        #endregion
    }
}
