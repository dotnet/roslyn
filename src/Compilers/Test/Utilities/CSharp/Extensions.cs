// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal static partial class Extensions
    {
        public static AssemblySymbol GetReferencedAssemblySymbol(this CSharpCompilation compilation, MetadataReference reference)
        {
            return (AssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);
        }

        public static ModuleSymbol GetReferencedModuleSymbol(this CSharpCompilation compilation, MetadataReference reference)
        {
            return (ModuleSymbol)compilation.GetAssemblyOrModuleSymbol(reference);
        }

        public static TypeDeclarationSyntax AsTypeDeclarationSyntax(this SyntaxNode node)
        {
            return node as TypeDeclarationSyntax;
        }

        public static MethodDeclarationSyntax AsMethodDeclarationSyntax(this SyntaxNode node)
        {
            return node as MethodDeclarationSyntax;
        }

        public static SyntaxNodeOrToken FindNodeOrTokenByKind(this SyntaxTree syntaxTree, SyntaxKind kind, int occurrence = 1)
        {
            if (!(occurrence > 0))
            {
                throw new ArgumentException("Specified value must be greater than zero.", nameof(occurrence));
            }
            SyntaxNodeOrToken foundNode = default(SyntaxNodeOrToken);
            if (TryFindNodeOrToken(syntaxTree.GetCompilationUnitRoot(), kind, ref occurrence, ref foundNode))
            {
                return foundNode;
            }
            return default(SyntaxNodeOrToken);
        }

        private static bool TryFindNodeOrToken(SyntaxNodeOrToken node, SyntaxKind kind, ref int occurrence, ref SyntaxNodeOrToken foundNode)
        {
            if (node.IsKind(kind))
            {
                occurrence--;
                if (occurrence == 0)
                {
                    foundNode = node;
                    return true;
                }
            }

            // we should probably did into trivia if this is a Token, but we won't

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (TryFindNodeOrToken(child, kind, ref occurrence, ref foundNode))
                {
                    return true;
                }
            }

            return false;
        }

        public static AssemblySymbol[] BoundReferences(this AssemblySymbol @this)
        {
            return (from m in @this.Modules
                    from @ref in m.GetReferencedAssemblySymbols()
                    select @ref).ToArray();
        }

        public static SourceAssemblySymbol SourceAssembly(this CSharpCompilation @this)
        {
            return (SourceAssemblySymbol)@this.Assembly;
        }

        public static bool HasUnresolvedReferencesByComparisonTo(this AssemblySymbol @this, AssemblySymbol that)
        {
            var thisRefs = @this.BoundReferences();
            var thatRefs = that.BoundReferences();

            for (int i = 0; i < Math.Max(thisRefs.Length, thatRefs.Length); i++)
            {
                if (thisRefs[i].IsMissing && !thatRefs[i].IsMissing)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(this AssemblySymbol @this, AssemblySymbol that)
        {
            var thisPEAssembly = @this as Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEAssemblySymbol;

            if (thisPEAssembly != null)
            {
                var thatPEAssembly = that as Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEAssemblySymbol;

                return thatPEAssembly != null &&
                    ReferenceEquals(thisPEAssembly.Assembly, thatPEAssembly.Assembly) && @this.HasUnresolvedReferencesByComparisonTo(that);
            }

            var thisRetargetingAssembly = @this as Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingAssemblySymbol;

            if (thisRetargetingAssembly != null)
            {
                var thatRetargetingAssembly = that as Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingAssemblySymbol;

                if (thatRetargetingAssembly != null)
                {
                    return ReferenceEquals(thisRetargetingAssembly.UnderlyingAssembly, thatRetargetingAssembly.UnderlyingAssembly) &&
                        @this.HasUnresolvedReferencesByComparisonTo(that);
                }

                var thatSourceAssembly = that as SourceAssemblySymbol;

                return thatSourceAssembly != null && ReferenceEquals(thisRetargetingAssembly.UnderlyingAssembly, thatSourceAssembly) &&
                    @this.HasUnresolvedReferencesByComparisonTo(that);
            }

            return false;
        }

        private static ImmutableArray<string> SplitMemberName(string name)
        {
            if (name.StartsWith(".", StringComparison.Ordinal))
            {
                return ImmutableArray.Create(name);
            }

            var builder = ArrayBuilder<string>.GetInstance();
            string part = name;
            while (part.Length > 0)
            {
                builder.Add(MetadataHelpers.SplitQualifiedName(part, out part));
            }

            builder.ReverseContents();
            return builder.ToImmutableAndFree();
        }

        public static Symbol GetMember(this Compilation compilation, string qualifiedName)
        {
            return ((CSharpCompilation)compilation).GlobalNamespace.GetMember(qualifiedName);
        }

        public static T GetMember<T>(this Compilation compilation, string qualifiedName) where T : Symbol
        {
            return (T)((CSharpCompilation)compilation).GlobalNamespace.GetMember(qualifiedName);
        }

        public static ImmutableArray<Symbol> GetMembers(this Compilation compilation, string qualifiedName)
        {
            NamespaceOrTypeSymbol lastContainer;
            var members = GetMembers(((CSharpCompilation)compilation).GlobalNamespace, qualifiedName, out lastContainer);
            if (members.IsEmpty)
            {
                Assert.True(false, string.Format("Could not find member named '{0}'.  Available members:\r\n{1}",
                    qualifiedName, string.Join("\r\n", lastContainer.GetMembers().Select(m => "\t\t" + m.Name))));
            }
            return members;
        }

        private static ImmutableArray<Symbol> GetMembers(NamespaceOrTypeSymbol container, string qualifiedName, out NamespaceOrTypeSymbol lastContainer)
        {
            var parts = SplitMemberName(qualifiedName);

            lastContainer = container;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var nestedContainer = (NamespaceOrTypeSymbol)lastContainer.GetMember(parts[i]);
                if (nestedContainer == null)
                {
                    // If there wasn't a nested namespace or type with that name, assume it's a
                    // member name that includes dots (e.g. explicit interface implementation).
                    return lastContainer.GetMembers(string.Join(".", parts.Skip(i)));
                }
                else
                {
                    lastContainer = nestedContainer;
                }
            }

            return lastContainer.GetMembers(parts[parts.Length - 1]);
        }

        public static Symbol GetMember(this NamespaceOrTypeSymbol container, string qualifiedName)
        {
            NamespaceOrTypeSymbol lastContainer;
            var members = GetMembers(container, qualifiedName, out lastContainer);
            if (members.Length == 0)
            {
                return null;
            }
            else if (members.Length > 1)
            {
                Assert.True(false, "Found multiple members of specified name:\r\n" + string.Join("\r\n", members));
            }

            return members.Single();
        }

        public static T GetMember<T>(this NamespaceOrTypeSymbol symbol, string qualifiedName) where T : Symbol
        {
            return (T)symbol.GetMember(qualifiedName);
        }

        public static PropertySymbol GetProperty(this NamedTypeSymbol symbol, string name)
        {
            return (PropertySymbol)symbol.GetMembers(name).Single();
        }

        public static EventSymbol GetEvent(this NamedTypeSymbol symbol, string name)
        {
            return (EventSymbol)symbol.GetMembers(name).Single();
        }

        public static MethodSymbol GetMethod(this NamedTypeSymbol symbol, string name)
        {
            return (MethodSymbol)symbol.GetMembers(name).Single();
        }

        public static FieldSymbol GetField(this NamedTypeSymbol symbol, string name)
        {
            return (FieldSymbol)symbol.GetMembers(name).Single();
        }

        public static NamedTypeSymbol GetTypeMember(this NamespaceOrTypeSymbol symbol, string name)
        {
            return symbol.GetTypeMembers(name).Single();
        }

        public static string[] GetFieldNames(this ModuleSymbol module, string qualifiedTypeName)
        {
            var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(qualifiedTypeName);
            return type.GetMembers().OfType<FieldSymbol>().Select(f => f.Name).ToArray();
        }

        public static string[] GetFieldNamesAndTypes(this ModuleSymbol module, string qualifiedTypeName)
        {
            var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember(qualifiedTypeName);
            return type.GetMembers().OfType<FieldSymbol>().Select(f => f.Name + ": " + f.TypeWithAnnotations).ToArray();
        }

        public static IEnumerable<CSharpAttributeData> GetAttributes(this Symbol @this, NamedTypeSymbol c)
        {
            return @this.GetAttributes().Where(a => TypeSymbol.Equals(a.AttributeClass, c, TypeCompareKind.ConsiderEverything2));
        }

        public static IEnumerable<CSharpAttributeData> GetAttributes(this Symbol @this, string namespaceName, string typeName)
        {
            return @this.GetAttributes().Where(a => a.IsTargetAttribute(namespaceName, typeName));
        }

        public static IEnumerable<CSharpAttributeData> GetAttributes(this Symbol @this, AttributeDescription description)
        {
            return @this.GetAttributes().Where(a => a.IsTargetAttribute(@this, description));
        }

        public static CSharpAttributeData GetAttribute(this Symbol @this, NamedTypeSymbol c)
        {
            return @this.GetAttributes().Where(a => TypeSymbol.Equals(a.AttributeClass, c, TypeCompareKind.ConsiderEverything2)).First();
        }

        public static CSharpAttributeData GetAttribute(this Symbol @this, string namespaceName, string typeName)
        {
            return @this.GetAttributes().Where(a => a.IsTargetAttribute(namespaceName, typeName)).First();
        }

        public static CSharpAttributeData GetAttribute(this Symbol @this, MethodSymbol m)
        {
            return (from a in @this.GetAttributes()
                    where a.AttributeConstructor.Equals(m)
                    select a).ToList().First();
        }

        public static void VerifyValue<T>(this CSharpAttributeData attr, int i, TypedConstantKind kind, T v)
        {
            var arg = attr.CommonConstructorArguments[i];
            Assert.Equal(kind, arg.Kind);
            Assert.True(IsEqual(arg, v));
        }

        public static void VerifyNamedArgumentValue<T>(this CSharpAttributeData attr, int i, string name, TypedConstantKind kind, T v)
        {
            var namedArg = attr.CommonNamedArguments[i];
            Assert.Equal(namedArg.Key, name);
            var arg = namedArg.Value;
            Assert.Equal(arg.Kind, kind);
            Assert.True(IsEqual(arg, v));
        }

        internal static bool IsEqual(TypedConstant arg, object expected)
        {
            switch (arg.Kind)
            {
                case TypedConstantKind.Array:
                    return AreEqual(arg.Values, expected);
                case TypedConstantKind.Enum:
                    return expected.Equals(arg.Value);
                case TypedConstantKind.Type:
                    var typeSym = arg.Value as TypeSymbol;
                    if ((object)typeSym == null)
                    {
                        return false;
                    }

                    var expTypeSym = expected as TypeSymbol;
                    if (typeSym.Equals(expTypeSym))
                    {
                        return true;
                    }

                    // TODO: improve the comparison mechanism for generic types.
                    if (typeSym is NamedTypeSymbol { IsGenericType: true, Kind: SymbolKind.NamedType })
                    {
                        var s1 = typeSym.ToDisplayString(SymbolDisplayFormat.TestFormat);
                        var s2 = expected.ToString();
                        if ((s1 == s2))
                        {
                            return true;
                        }
                    }

                    var expType = expected as Type;
                    if (expType == null)
                    {
                        return false;
                    }
                    //Can't always simply compare string as <T>.ToString() is IL format
                    return IsEqual(typeSym, expType);
                default:
                    //Assert.Equal(expected, CType(arg.Value, T))
                    return expected == null ? arg.Value == null : expected.Equals(arg.Value);
            }
        }

        /// For argument is not simple 'Type' (generic or array)
        private static bool IsEqual(TypeSymbol typeSym, Type expType)
        {
            // namedType

            if ((typeSym.TypeKind == TypeKind.Interface || typeSym.TypeKind == TypeKind.Class || typeSym.TypeKind == TypeKind.Struct || typeSym.TypeKind == TypeKind.Delegate))
            {
                NamedTypeSymbol namedType = (NamedTypeSymbol)typeSym;
                // name should be same if it's not generic (NO ByRef in attribute)
                if ((namedType.Arity == 0))
                {
                    return typeSym.Name == expType.Name;
                }
                // generic
                if (!(expType.GetTypeInfo().IsGenericType))
                {
                    return false;
                }

                var nameOnly = expType.Name;
                //generic <Name>'1
                var idx = expType.Name.LastIndexOfAny(new char[] { '`' });
                if ((idx > 0))
                {
                    nameOnly = expType.Name.Substring(0, idx);
                }
                if (!(typeSym.Name == nameOnly))
                {
                    return false;
                }
                var expArgs = expType.GetGenericArguments();
                var actArgs = namedType.TypeArguments();
                if (!(expArgs.Count() == actArgs.Length))
                {
                    return false;
                }

                for (var i = 0; i <= expArgs.Count() - 1; i++)
                {
                    if (!IsEqual(actArgs[i], expArgs[i]))
                    {
                        return false;
                    }
                }
                return true;
                // array type
            }
            else if (typeSym.TypeKind == TypeKind.Array)
            {
                if (!expType.IsArray)
                {
                    return false;
                }
                var arySym = (ArrayTypeSymbol)typeSym;
                if (!IsEqual(arySym.ElementType, expType.GetElementType()))
                {
                    return false;
                }
                if (!IsEqual(arySym.BaseType(), expType.GetTypeInfo().BaseType))
                {
                    return false;
                }
                return arySym.Rank == expType.GetArrayRank();
            }

            return false;
        }

        // Compare an Object with a TypedConstant.  This compares the TypeConstant's value and ignores the TypeConstant's type.
        private static bool AreEqual(ImmutableArray<TypedConstant> tc, object o)
        {
            if (o == null)
            {
                return tc.IsDefault;
            }
            else if (tc.IsDefault)
            {
                return false;
            }

            if (!o.GetType().IsArray)
            {
                return false;
            }

            var a = (Array)o;
            bool ret = true;
            for (var i = 0; i <= a.Length - 1; i++)
            {
                var v = a.GetValue(i);
                var c = tc[i];
                ret = ret & IsEqual(c, v);
            }
            return ret;
        }

        public static void CheckAccessorShape(this MethodSymbol accessor, Symbol propertyOrEvent)
        {
            Assert.Same(propertyOrEvent, accessor.AssociatedSymbol);

            CheckAccessorModifiers(accessor, propertyOrEvent);

            Assert.Contains(accessor, propertyOrEvent.ContainingType.GetMembers(accessor.Name));

            var propertyOrEventType = propertyOrEvent.GetTypeOrReturnType().Type;
            switch (accessor.MethodKind)
            {
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    Assert.Equal(SpecialType.System_Void, accessor.ReturnType.SpecialType);
                    Assert.Equal(propertyOrEventType, accessor.Parameters.Single().Type);
                    break;
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    var property = (PropertySymbol)propertyOrEvent;
                    var isSetter = accessor.MethodKind == MethodKind.PropertySet;

                    if (isSetter)
                    {
                        Assert.Equal(SpecialType.System_Void, accessor.ReturnType.SpecialType);
                    }
                    else
                    {
                        Assert.Equal(propertyOrEventType, accessor.ReturnType);
                    }

                    var propertyParameters = property.Parameters;
                    var accessorParameters = accessor.Parameters;
                    Assert.Equal(propertyParameters.Length, accessorParameters.Length - (isSetter ? 1 : 0));
                    for (int i = 0; i < propertyParameters.Length; i++)
                    {
                        var propertyParam = propertyParameters[i];
                        var accessorParam = accessorParameters[i];
                        Assert.Equal(propertyParam.Type, accessorParam.Type);
                        Assert.Equal(propertyParam.RefKind, accessorParam.RefKind);
                        Assert.Equal(propertyParam.Name, accessorParam.Name);
                    }

                    if (isSetter)
                    {
                        var valueParameter = accessorParameters[propertyParameters.Length];
                        Assert.Equal(propertyOrEventType, valueParameter.Type);
                        Assert.Equal(RefKind.None, valueParameter.RefKind);
                        Assert.Equal(ParameterSymbol.ValueParameterName, valueParameter.Name);
                    }
                    break;
                default:
                    Assert.False(true, "Unexpected accessor kind " + accessor.MethodKind);
                    break;
            }
        }

        internal static void CheckAccessorModifiers(this MethodSymbol accessor, Symbol propertyOrEvent)
        {
            Assert.Equal(propertyOrEvent.DeclaredAccessibility, accessor.DeclaredAccessibility);
            Assert.Equal(propertyOrEvent.IsAbstract, accessor.IsAbstract);
            Assert.Equal(propertyOrEvent.IsOverride, accessor.IsOverride);
            Assert.Equal(propertyOrEvent.IsVirtual, accessor.IsVirtual);
            Assert.Equal(propertyOrEvent.IsSealed, accessor.IsSealed);
            Assert.Equal(propertyOrEvent.IsExtern, accessor.IsExtern);
            Assert.Equal(propertyOrEvent.IsStatic, accessor.IsStatic);
        }
    }
}

// This is deliberately declared in the global namespace so that it will always be discoverable (regardless of usings).
internal static class Extensions
{
    /// <summary>
    /// This method is provided as a convenience for testing the SemanticModel.GetDeclaredSymbol implementation.
    /// </summary>
    /// <param name="declaration">This parameter will be type checked, and a NotSupportedException will be thrown if the type is not currently supported by an overload of GetDeclaredSymbol.</param>
    internal static Symbol GetDeclaredSymbolFromSyntaxNode(this CSharpSemanticModel model, Microsoft.CodeAnalysis.SyntaxNode declaration, CancellationToken cancellationToken = default(CancellationToken))
    {
        // NOTE: Do not add types to this condition unless you have verified that there is an overload of SemanticModel.GetDeclaredSymbol
        //       that supports the type you're adding.
        if (!(
            declaration is AnonymousObjectCreationExpressionSyntax ||
            declaration is AnonymousObjectMemberDeclaratorSyntax ||
            declaration is BaseTypeDeclarationSyntax ||
            declaration is CatchDeclarationSyntax ||
            declaration is ExternAliasDirectiveSyntax ||
            declaration is ForEachStatementSyntax ||
            declaration is JoinIntoClauseSyntax ||
            declaration is LabeledStatementSyntax ||
            declaration is MemberDeclarationSyntax ||
            declaration is NamespaceDeclarationSyntax ||
            declaration is ParameterSyntax ||
            declaration is QueryClauseSyntax ||
            declaration is QueryContinuationSyntax ||
            declaration is SwitchLabelSyntax ||
            declaration is TypeParameterSyntax ||
            declaration is UsingDirectiveSyntax ||
            declaration is VariableDeclaratorSyntax))
        {
            throw new NotSupportedException("This node type is not supported.");
        }

        return (Symbol)model.GetDeclaredSymbol(declaration, cancellationToken);
    }

    public static NamedTypeSymbol BaseType(this TypeSymbol symbol)
    {
        return symbol.BaseTypeNoUseSiteDiagnostics;
    }

    public static ImmutableArray<NamedTypeSymbol> Interfaces(this TypeSymbol symbol)
    {
        return symbol.InterfacesNoUseSiteDiagnostics();
    }

    public static ImmutableArray<NamedTypeSymbol> AllInterfaces(this TypeSymbol symbol)
    {
        return symbol.AllInterfacesNoUseSiteDiagnostics;
    }

    public static ImmutableArray<TypeSymbol> TypeArguments(this NamedTypeSymbol symbol)
    {
        return TypeMap.AsTypeSymbols(symbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
    }

    public static ImmutableArray<TypeSymbol> ConstraintTypes(this TypeParameterSymbol symbol)
    {
        return TypeMap.AsTypeSymbols(symbol.ConstraintTypesNoUseSiteDiagnostics);
    }
}
