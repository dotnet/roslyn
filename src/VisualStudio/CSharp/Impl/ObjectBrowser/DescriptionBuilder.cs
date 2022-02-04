// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    internal class DescriptionBuilder : AbstractDescriptionBuilder
    {
        public DescriptionBuilder(
            IVsObjectBrowserDescription3 description,
            ObjectBrowserLibraryManager libraryManager,
            ObjectListItem listItem,
            Project project)
            : base(description, libraryManager, listItem, project)
        {
        }

        protected override void BuildNamespaceDeclaration(INamespaceSymbol namespaceSymbol, _VSOBJDESCOPTIONS options)
        {
            AddText("namespace ");
            AddName(namespaceSymbol.ToDisplayString());
        }

        protected override void BuildDelegateDeclaration(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options)
        {
            Debug.Assert(typeSymbol.TypeKind == TypeKind.Delegate);

            BuildTypeModifiers(typeSymbol);
            AddText("delegate ");

            var delegateInvokeMethod = typeSymbol.DelegateInvokeMethod;

            AddTypeLink(delegateInvokeMethod.ReturnType, LinkFlags.None);
            AddText(" ");

            var typeQualificationStyle = (options & _VSOBJDESCOPTIONS.ODO_USEFULLNAME) != 0
                ? SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
                : SymbolDisplayTypeQualificationStyle.NameOnly;

            var typeNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: typeQualificationStyle,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

            AddName(typeSymbol.ToDisplayString(typeNameFormat));

            AddText("(");
            BuildParameterList(delegateInvokeMethod.Parameters);
            AddText(")");

            if (typeSymbol.IsGenericType)
            {
                BuildGenericConstraints(typeSymbol);
            }
        }

        protected override void BuildTypeDeclaration(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options)
        {
            BuildTypeModifiers(typeSymbol);

            switch (typeSymbol.TypeKind)
            {
                case TypeKind.Enum:
                    AddText("enum ");
                    break;

                case TypeKind.Struct:
                    AddText("struct ");
                    break;

                case TypeKind.Interface:
                    AddText("interface ");
                    break;

                case TypeKind.Class:
                    AddText("class ");
                    break;

                default:
                    Debug.Fail("Invalid type kind encountered: " + typeSymbol.TypeKind.ToString());
                    break;
            }

            var typeNameFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

            AddName(typeSymbol.ToDisplayString(typeNameFormat));

            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                var underlyingType = typeSymbol.EnumUnderlyingType;
                if (underlyingType != null)
                {
                    if (underlyingType.SpecialType != SpecialType.System_Int32)
                    {
                        AddText(" : ");
                        AddTypeLink(underlyingType, LinkFlags.None);
                    }
                }
            }
            else
            {
                var baseType = typeSymbol.BaseType;
                if (baseType != null)
                {
                    if (baseType.SpecialType is not SpecialType.System_Object and
                        not SpecialType.System_Delegate and
                        not SpecialType.System_MulticastDelegate and
                        not SpecialType.System_Enum and
                        not SpecialType.System_ValueType)
                    {
                        AddText(" : ");
                        AddTypeLink(baseType, LinkFlags.None);
                    }
                }
            }

            if (typeSymbol.IsGenericType)
            {
                BuildGenericConstraints(typeSymbol);
            }
        }

        private void BuildAccessibility(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    AddText("public ");
                    break;

                case Accessibility.Private:
                    AddText("private ");
                    break;

                case Accessibility.Protected:
                    AddText("protected ");
                    break;

                case Accessibility.Internal:
                    AddText("internal ");
                    break;

                case Accessibility.ProtectedOrInternal:
                    AddText("protected internal ");
                    break;

                case Accessibility.ProtectedAndInternal:
                    AddText("private protected ");
                    break;

                default:
                    AddText("internal ");
                    break;
            }
        }

        private void BuildTypeModifiers(INamedTypeSymbol typeSymbol)
        {
            BuildAccessibility(typeSymbol);

            if (typeSymbol.IsStatic)
            {
                AddText("static ");
            }

            if (typeSymbol.IsAbstract &&
                typeSymbol.TypeKind != TypeKind.Interface)
            {
                AddText("abstract ");
            }

            if (typeSymbol.IsSealed &&
                typeSymbol.TypeKind != TypeKind.Struct &&
                typeSymbol.TypeKind != TypeKind.Enum &&
                typeSymbol.TypeKind != TypeKind.Delegate)
            {
                AddText("sealed ");
            }
        }

        protected override void BuildMethodDeclaration(IMethodSymbol methodSymbol, _VSOBJDESCOPTIONS options)
        {
            BuildMemberModifiers(methodSymbol);

            if (methodSymbol.MethodKind is not MethodKind.Constructor and
                not MethodKind.Destructor and
                not MethodKind.StaticConstructor and
                not MethodKind.Conversion)
            {
                AddTypeLink(methodSymbol.ReturnType, LinkFlags.None);
                AddText(" ");
            }

            if (methodSymbol.MethodKind == MethodKind.Conversion)
            {
                switch (methodSymbol.Name)
                {
                    case WellKnownMemberNames.ImplicitConversionName:
                        AddName("implicit operator ");
                        break;

                    case WellKnownMemberNames.ExplicitConversionName:
                        AddName("explicit operator ");
                        break;
                }

                AddTypeLink(methodSymbol.ReturnType, LinkFlags.None);
            }
            else
            {
                var methodNameFormat = new SymbolDisplayFormat(
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

                AddName(methodSymbol.ToDisplayString(methodNameFormat));
            }

            AddText("(");

            if (methodSymbol.IsExtensionMethod)
            {
                AddText("this ");
            }

            BuildParameterList(methodSymbol.Parameters);
            AddText(")");

            if (methodSymbol.IsGenericMethod)
            {
                BuildGenericConstraints(methodSymbol);
            }
        }

        private void BuildMemberModifiers(ISymbol memberSymbol)
        {
            if (memberSymbol.ContainingType != null && memberSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                return;
            }

            var methodSymbol = memberSymbol as IMethodSymbol;
            var fieldSymbol = memberSymbol as IFieldSymbol;

            if (methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.Destructor)
            {
                return;
            }

            if (fieldSymbol != null &&
                fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                return;
            }

            // TODO: 'new' modifier isn't exposed on symbols. Do we need it?

            // Note: we don't display the access modifier for static constructors
            if (methodSymbol == null ||
                methodSymbol.MethodKind != MethodKind.StaticConstructor)
            {
                BuildAccessibility(memberSymbol);
            }

            if (memberSymbol.RequiresUnsafeModifier())
            {
                AddText("unsafe ");
            }

            // Note: we don't display 'static' for constant fields
            if (memberSymbol.IsStatic &&
                (fieldSymbol == null || !fieldSymbol.IsConst))
            {
                AddText("static ");
            }

            if (memberSymbol.IsExtern)
            {
                AddText("extern ");
            }

            if (fieldSymbol != null && fieldSymbol.IsReadOnly)
            {
                AddText("readonly ");
            }

            if (fieldSymbol != null && fieldSymbol.IsConst)
            {
                AddText("const ");
            }

            if (fieldSymbol != null && fieldSymbol.IsVolatile)
            {
                AddText("volatile ");
            }

            if (memberSymbol.IsAbstract)
            {
                AddText("abstract ");
            }
            else if (memberSymbol.IsOverride)
            {
                if (memberSymbol.IsSealed)
                {
                    AddText("sealed ");
                }

                AddText("override ");
            }
            else if (memberSymbol.IsVirtual)
            {
                AddText("virtual ");
            }
        }

        private void BuildGenericConstraints(INamedTypeSymbol typeSymbol)
        {
            foreach (var typeParameterSymbol in typeSymbol.TypeParameters)
            {
                BuildConstraints(typeParameterSymbol);
            }
        }

        private void BuildGenericConstraints(IMethodSymbol methodSymbol)
        {
            foreach (var typeParameterSymbol in methodSymbol.TypeParameters)
            {
                BuildConstraints(typeParameterSymbol);
            }
        }

        private void BuildConstraints(ITypeParameterSymbol typeParameterSymbol)
        {
            if (typeParameterSymbol.ConstraintTypes.Length == 0 &&
                !typeParameterSymbol.HasConstructorConstraint &&
                !typeParameterSymbol.HasReferenceTypeConstraint &&
                !typeParameterSymbol.HasValueTypeConstraint)
            {
                return;
            }

            AddLineBreak();
            AddText("\t");
            AddText("where ");
            AddName(typeParameterSymbol.Name);
            AddText(" : ");

            var isFirst = true;

            if (typeParameterSymbol.HasReferenceTypeConstraint)
            {
                if (!isFirst)
                {
                    AddComma();
                }

                AddText("class");
                isFirst = false;
            }

            if (typeParameterSymbol.HasValueTypeConstraint)
            {
                if (!isFirst)
                {
                    AddComma();
                }

                AddText("struct");
                isFirst = false;
            }

            foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
            {
                if (!isFirst)
                {
                    AddComma();
                }

                AddTypeLink(constraintType, LinkFlags.None);
                isFirst = false;
            }

            if (typeParameterSymbol.HasConstructorConstraint)
            {
                if (!isFirst)
                {
                    AddComma();
                }

                AddText("new()");
            }
        }

        private void BuildParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            var count = parameters.Length;
            if (count == 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    AddComma();
                }

                var current = parameters[i];

                if (current.IsOptional)
                {
                    AddText("[");
                }

                if (current.RefKind == RefKind.Ref)
                {
                    AddText("ref ");
                }
                else if (current.RefKind == RefKind.Out)
                {
                    AddText("out ");
                }

                if (current.IsParams)
                {
                    AddText("params ");
                }

                AddTypeLink(current.Type, LinkFlags.None);
                AddText(" ");
                AddParam(current.Name);

                if (current.HasExplicitDefaultValue)
                {
                    AddText(" = ");
                    if (current.ExplicitDefaultValue == null)
                    {
                        AddText("null");
                    }
                    else
                    {
                        AddText(current.ExplicitDefaultValue.ToString());
                    }
                }

                if (current.IsOptional)
                {
                    AddText("]");
                }
            }
        }

        protected override void BuildFieldDeclaration(IFieldSymbol fieldSymbol, _VSOBJDESCOPTIONS options)
        {
            BuildMemberModifiers(fieldSymbol);

            if (fieldSymbol.ContainingType.TypeKind != TypeKind.Enum)
            {
                AddTypeLink(fieldSymbol.Type, LinkFlags.None);
                AddText(" ");
            }

            AddName(fieldSymbol.Name);
        }

        protected override void BuildPropertyDeclaration(IPropertySymbol propertySymbol, _VSOBJDESCOPTIONS options)
        {
            BuildMemberModifiers(propertySymbol);

            AddTypeLink(propertySymbol.Type, LinkFlags.None);
            AddText(" ");

            if (propertySymbol.IsIndexer)
            {
                AddName("this");
                AddText("[");
                BuildParameterList(propertySymbol.Parameters);
                AddText("]");
            }
            else
            {
                AddName(propertySymbol.Name);
            }

            AddText(" { ");

            if (propertySymbol.GetMethod != null)
            {
                if (propertySymbol.GetMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                {
                    BuildAccessibility(propertySymbol.GetMethod);
                }

                AddText("get; ");
            }

            if (propertySymbol.SetMethod != null)
            {
                if (propertySymbol.SetMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                {
                    BuildAccessibility(propertySymbol.SetMethod);
                }

                AddText("set; ");
            }

            AddText("}");
        }

        protected override void BuildEventDeclaration(IEventSymbol eventSymbol, _VSOBJDESCOPTIONS options)
        {
            BuildMemberModifiers(eventSymbol);

            AddText("event ");

            AddTypeLink(eventSymbol.Type, LinkFlags.None);
            AddText(" ");

            AddName(eventSymbol.Name);
        }
    }
}
