// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace ImplementNotifyPropertyChangedCS
{
    internal static class CodeGeneration
    {
        internal static CompilationUnitSyntax ImplementINotifyPropertyChanged(CompilationUnitSyntax root, SemanticModel model, IEnumerable<ExpandablePropertyInfo> properties, Workspace workspace)
        { 
            var typeDeclaration = properties.First().PropertyDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            var backingFieldLookup = Enumerable.ToDictionary(properties, info => info.PropertyDeclaration, info => info.BackingFieldName);

            root = root.ReplaceNodes(
                properties.Select(p => p.PropertyDeclaration as SyntaxNode).Concat(new[] { typeDeclaration }),
                    (original, updated) => original.IsKind(SyntaxKind.PropertyDeclaration)
                    ? ExpandProperty((PropertyDeclarationSyntax)original, backingFieldLookup[(PropertyDeclarationSyntax)original]) as SyntaxNode
                    : ExpandType((TypeDeclarationSyntax)original, (TypeDeclarationSyntax)updated, properties.Where(p => p.NeedsBackingField), model, workspace));

            return root
                .WithUsing("System.Collections.Generic")
                .WithUsing("System.ComponentModel");
        }

        private static CompilationUnitSyntax WithUsing(this CompilationUnitSyntax root, string name)
        {
            if (!root.Usings.Any(u => u.Name.ToString() == name))
            {
                root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name)).WithAdditionalAnnotations(Formatter.Annotation));
            }

            return root;
        }

        private static TypeDeclarationSyntax ExpandType(TypeDeclarationSyntax original, TypeDeclarationSyntax updated, IEnumerable<ExpandablePropertyInfo> properties, SemanticModel model, Workspace workspace)
        {
            Debug.Assert(original != updated);

            return updated
                .WithBackingFields(properties, workspace)
                .WithBaseType(original, model)
                .WithPropertyChangedEvent(original, model, workspace)
                .WithSetPropertyMethod(original, model, workspace);
        }

        private static TypeDeclarationSyntax WithBackingFields(this TypeDeclarationSyntax node, IEnumerable<ExpandablePropertyInfo> properties, Workspace workspace)
        {
            foreach (var property in properties)
            {
                // When the getter doesn't have a body (i.e. an auto-prop), we'll need to generate a new field.
                var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                    attributes: null,
                    accessibility: Microsoft.CodeAnalysis.Accessibility.Private,
                    modifiers: new SymbolModifiers(),
                    type: property.Type,
                    name: property.BackingFieldName);

                node = CodeGenerator.AddFieldDeclaration(node, newField, workspace);
            }

            return node;
        }

        private static PropertyDeclarationSyntax ExpandProperty(PropertyDeclarationSyntax property, string backingFieldName)
        {
            AccessorDeclarationSyntax getter, setter;
            if (!ExpansionChecker.TryGetAccessors(property, out getter, out setter))
            {
                throw new ArgumentException();
            }

            if (getter.Body == null)
            {
                var returnFieldStatement = SyntaxFactory.ParseStatement(string.Format("return {0};", backingFieldName));
                getter = getter
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList(returnFieldStatement)));
            }

            getter = getter
                .WithSemicolonToken(default(SyntaxToken));

            var setPropertyStatement = SyntaxFactory.ParseStatement(string.Format("SetProperty(ref {0}, value, \"{1}\");", backingFieldName, property.Identifier.ValueText));
            setter = setter
                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList(setPropertyStatement)))
                .WithSemicolonToken(default(SyntaxToken));

            var newProperty = property
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getter, setter })))
                .WithAdditionalAnnotations(Formatter.Annotation);

            return newProperty;
        }

        private const string InterfaceName = "System.ComponentModel.INotifyPropertyChanged";

        private static TypeDeclarationSyntax WithBaseType(this TypeDeclarationSyntax node, TypeDeclarationSyntax original, SemanticModel semanticModel)
        {
            var classSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(original);
            var interfaceSymbol = semanticModel.Compilation.GetTypeByMetadataName(InterfaceName);

            // Does this class already implement INotifyPropertyChanged? If not, add it to the base list.
            if (!classSymbol.AllInterfaces.Contains(interfaceSymbol))
            {
                var baseTypeName = SyntaxFactory.ParseTypeName(InterfaceName)
                    .WithAdditionalAnnotations(Simplifier.Annotation);

                node = node.IsKind(SyntaxKind.ClassDeclaration)
                   ? ((ClassDeclarationSyntax)node).AddBaseListTypes(baseTypeName) as TypeDeclarationSyntax
                   : ((StructDeclarationSyntax)node).AddBaseListTypes(baseTypeName);

                // Add a formatting annotation to the base list to ensure that it gets formatted properly.
                node = node.ReplaceNode(
                    node.BaseList,
                    node.BaseList.WithAdditionalAnnotations(Formatter.Annotation));
            }

            return node;
        }

        private static TypeDeclarationSyntax WithPropertyChangedEvent(this TypeDeclarationSyntax node, TypeDeclarationSyntax original, SemanticModel semanticModel, Workspace workspace)
        {
            var classSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(original);
            var interfaceSymbol = semanticModel.Compilation.GetTypeByMetadataName(InterfaceName);
            var propertyChangedEventSymbol = (IEventSymbol)interfaceSymbol.GetMembers("PropertyChanged").Single();
            var propertyChangedEvent = classSymbol.FindImplementationForInterfaceMember(propertyChangedEventSymbol);

            // Does this class contain an implementation for the PropertyChanged event? If not, add it.
            if (propertyChangedEvent == null)
            {
                node = CodeGenerator.AddEventDeclaration(
                    node,
                    GeneratePropertyChangedEvent(semanticModel.Compilation),
                    workspace);
            }

            return node;
        }

        internal static IEventSymbol GeneratePropertyChangedEvent(Compilation compilation)
        {
            var propertyChangedEventHandlerType = compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangedEventHandler");

            return CodeGenerationSymbolFactory.CreateEventSymbol(
                attributes: null,
                accessibility: Microsoft.CodeAnalysis.Accessibility.Public,
                modifiers: new SymbolModifiers(),
                type: propertyChangedEventHandlerType,
                explicitInterfaceSymbol: null,
                name: "PropertyChanged");
        }

        private static IMethodSymbol FindSetPropertyMethod(this INamedTypeSymbol classSymbol, Compilation compilation)
        {
            // Find SetProperty<T>(ref T, T, string) method.
            var setPropertyMethod = classSymbol
                .GetMembers("SetProperty")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 3 && m.TypeParameters.Length == 1);

            if (setPropertyMethod != null)
            {
                var parameters = setPropertyMethod.Parameters;
                var typeParameter = setPropertyMethod.TypeParameters[0];
                var stringType = compilation.GetSpecialType(SpecialType.System_String);

                if (setPropertyMethod.ReturnsVoid &&
                    parameters[0].RefKind == RefKind.Ref &&
                    parameters[0].Type.Equals(typeParameter) &&
                    parameters[1].Type.Equals(typeParameter) &&
                    parameters[2].Type.Equals(stringType))
                {
                    return setPropertyMethod;
                }
            }

            return null;
        }

        private static TypeDeclarationSyntax WithSetPropertyMethod(this TypeDeclarationSyntax node, TypeDeclarationSyntax original, SemanticModel semanticModel, Workspace workspace)
        {
            var classSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(original);
            var interfaceSymbol = semanticModel.Compilation.GetTypeByMetadataName(InterfaceName);
            var propertyChangedEventSymbol = (IEventSymbol)interfaceSymbol.GetMembers("PropertyChanged").Single();
            var propertyChangedEvent = classSymbol.FindImplementationForInterfaceMember(propertyChangedEventSymbol);

            var setPropertyMethod = classSymbol.FindSetPropertyMethod(semanticModel.Compilation);
            if (setPropertyMethod == null)
            {
                node = CodeGenerator.AddMethodDeclaration(
                                        node,
                                        GenerateSetPropertyMethod(semanticModel.Compilation),
                                        workspace);
            }

            return node;
        }

        internal static IMethodSymbol GenerateSetPropertyMethod(Compilation compilation)
        {
            var body = SyntaxFactory.ParseStatement(
                @"if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
{
    field = value;

    var handler = PropertyChanged;
    if (handler != null)
    {
        handler(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}");

            body = body.WithAdditionalAnnotations(Simplifier.Annotation);

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);

            var typeParameter = CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T");

            var parameter1 = CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: null,
                refKind: RefKind.Ref,
                isParams: false,
                type: typeParameter,
                name: "field");

            var parameter2 = CodeGenerationSymbolFactory.CreateParameterSymbol(typeParameter, "value");
            var parameter3 = CodeGenerationSymbolFactory.CreateParameterSymbol(stringType, "name");

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: null,
                accessibility: Microsoft.CodeAnalysis.Accessibility.Private,
                modifiers: new SymbolModifiers(),
                returnType: voidType,
                explicitInterfaceSymbol: null,
                name: "SetProperty",
                typeParameters: new[] { typeParameter },
                parameters: new[] { parameter1, parameter2, parameter3 },
                statements: new[] { body });
        }
    }
}
