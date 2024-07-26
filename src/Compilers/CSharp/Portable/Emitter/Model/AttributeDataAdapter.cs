// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract partial class CSharpAttributeData : Cci.ICustomAttribute
    {
        ImmutableArray<Cci.IMetadataExpression> Cci.ICustomAttribute.GetArguments(EmitContext context)
        {
            var commonArgs = this.CommonConstructorArguments;
            if (commonArgs.IsEmpty)
            {
                return ImmutableArray<Cci.IMetadataExpression>.Empty;
            }

            var builder = ArrayBuilder<Cci.IMetadataExpression>.GetInstance();
            foreach (var argument in commonArgs)
            {
                Debug.Assert(argument.Kind != TypedConstantKind.Error);
                builder.Add(CreateMetadataExpression(argument, context));
            }
            return builder.ToImmutableAndFree();
        }

        Cci.IMethodReference Cci.ICustomAttribute.Constructor(EmitContext context, bool reportDiagnostics)
        {
            if (this.AttributeConstructor.IsDefaultValueTypeConstructor())
            {
                // Default parameterless constructors for structs exist in symbol table, but are not emitted.
                // Produce an error since we cannot use it (instead of crashing).
                // Details: https://github.com/dotnet/roslyn/issues/19394

                if (reportDiagnostics)
                {
                    context.Diagnostics.Add(ErrorCode.ERR_NotAnAttributeClass, context.Location ?? NoLocation.Singleton, this.AttributeClass);
                }

                return null;
            }

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return (Cci.IMethodReference)moduleBeingBuilt.Translate(this.AttributeConstructor, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics);
        }

        ImmutableArray<Cci.IMetadataNamedArgument> Cci.ICustomAttribute.GetNamedArguments(EmitContext context)
        {
            var commonArgs = this.CommonNamedArguments;
            if (commonArgs.IsEmpty)
            {
                return ImmutableArray<Cci.IMetadataNamedArgument>.Empty;
            }

            var builder = ArrayBuilder<Cci.IMetadataNamedArgument>.GetInstance();
            foreach (var namedArgument in commonArgs)
            {
                builder.Add(CreateMetadataNamedArgument(namedArgument.Key, namedArgument.Value, context));
            }
            return builder.ToImmutableAndFree();
        }

        int Cci.ICustomAttribute.ArgumentCount
        {
            get
            {
                return this.CommonConstructorArguments.Length;
            }
        }

        ushort Cci.ICustomAttribute.NamedArgumentCount
        {
            get
            {
                return (ushort)this.CommonNamedArguments.Length;
            }
        }

        Cci.ITypeReference Cci.ICustomAttribute.GetType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            // PROTOTYPE validate once extension types are all allowed in attributes
            return moduleBeingBuilt.Translate(this.AttributeClass, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics, ExtensionsEraseMode.ExcludeSelf);
        }

        bool Cci.ICustomAttribute.AllowMultiple
        {
            get { return this.AttributeClass.GetAttributeUsageInfo().AllowMultiple; }
        }

        private Cci.IMetadataExpression CreateMetadataExpression(TypedConstant argument, EmitContext context)
        {
            if (argument.IsNull)
            {
                return CreateMetadataConstant(argument.TypeInternal, null, context);
            }

            switch (argument.Kind)
            {
                case TypedConstantKind.Array:
                    return CreateMetadataArray(argument, context);

                case TypedConstantKind.Primitive when argument.TypeInternal.SpecialType is SpecialType.System_String && argument.ValueInternal is TypeSymbol:
                    return CreateSerializedType(argument, context);

                case TypedConstantKind.Type:
                    return CreateType(argument, context);

                default:
                    return CreateMetadataConstant(argument.TypeInternal, argument.ValueInternal, context);
            }
        }

        private MetadataCreateArray CreateMetadataArray(TypedConstant argument, EmitContext context)
        {
            Debug.Assert(!argument.Values.IsDefault);
            var values = argument.Values;
            var arrayType = ((PEModuleBuilder)context.Module).Translate((ArrayTypeSymbol)argument.TypeInternal, eraseExtensions: true);

            if (values.Length == 0)
            {
                return new MetadataCreateArray(arrayType,
                                               arrayType.GetElementType(context),
                                               ImmutableArray<Cci.IMetadataExpression>.Empty);
            }

            var metadataExprs = new Cci.IMetadataExpression[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                metadataExprs[i] = CreateMetadataExpression(values[i], context);
            }

            return new MetadataCreateArray(arrayType,
                                           arrayType.GetElementType(context),
                                           metadataExprs.AsImmutableOrNull());
        }

        private static MetadataTypeOf CreateType(TypedConstant argument, EmitContext context)
        {
            Debug.Assert(argument.ValueInternal != null);
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var syntaxNodeOpt = (CSharpSyntaxNode)context.SyntaxNode;
            var diagnostics = context.Diagnostics;
            return new MetadataTypeOf(moduleBeingBuilt.Translate((TypeSymbol)argument.ValueInternal, syntaxNodeOpt, diagnostics, eraseExtensions: true),
                                      moduleBeingBuilt.Translate((TypeSymbol)argument.TypeInternal, syntaxNodeOpt, diagnostics, eraseExtensions: true));
        }

        private static MetadataSerializedType CreateSerializedType(TypedConstant argument, EmitContext context)
        {
            Debug.Assert(argument.ValueInternal is not null);
            Debug.Assert(((TypeSymbol)argument.TypeInternal).SpecialType == SpecialType.System_String);

            var moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var syntaxNodeOpt = (CSharpSyntaxNode)context.SyntaxNode;
            var diagnostics = context.Diagnostics;

            return new MetadataSerializedType(
                moduleBeingBuilt.Translate((TypeSymbol)argument.ValueInternal, syntaxNodeOpt, diagnostics, eraseExtensions: false),
                moduleBeingBuilt.Translate((NamedTypeSymbol)argument.TypeInternal, syntaxNodeOpt, diagnostics, ExtensionsEraseMode.None));
        }

        private static MetadataConstant CreateMetadataConstant(ITypeSymbolInternal type, object value, EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.CreateConstant((TypeSymbol)type, value, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics);
        }

        private Cci.IMetadataNamedArgument CreateMetadataNamedArgument(string name, TypedConstant argument, EmitContext context)
        {
            var symbol = LookupName(name);
            var value = CreateMetadataExpression(argument, context);
            TypeSymbol type;
            var fieldSymbol = symbol as FieldSymbol;
            if ((object)fieldSymbol != null)
            {
                type = fieldSymbol.Type;
            }
            else
            {
                type = ((PropertySymbol)symbol).Type;
            }

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return new MetadataNamedArgument(symbol, moduleBeingBuilt.Translate(type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics, eraseExtensions: true), value);
        }

        private Symbol LookupName(string name)
        {
            var type = this.AttributeClass;
            while ((object)type != null)
            {
                foreach (var member in type.GetMembers(name))
                {
                    if (member.DeclaredAccessibility == Accessibility.Public)
                    {
                        return member;
                    }
                }
                type = type.BaseTypeNoUseSiteDiagnostics;
            }

            Debug.Assert(false, "Name does not match an attribute field or a property.  How can that be?");
            return null;
        }
    }
}
