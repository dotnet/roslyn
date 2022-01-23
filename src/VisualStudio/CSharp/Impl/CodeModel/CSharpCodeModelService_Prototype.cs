// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal partial class CSharpCodeModelService
    {
        public override string GetPrototype(SyntaxNode node, ISymbol symbol, PrototypeFlags flags)
        {
            Debug.Assert(symbol != null);

            if (node == null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Field:
                        return GetVariablePrototype((IFieldSymbol)symbol, flags);
                    case SymbolKind.Method:
                        return GetFunctionPrototype((IMethodSymbol)symbol, flags);
                    case SymbolKind.Property:
                        return GetPropertyPrototype((IPropertySymbol)symbol, flags);
                    case SymbolKind.Event:
                        return GetEventPrototype((IEventSymbol)symbol, flags);
                    case SymbolKind.NamedType:
                        var namedType = (INamedTypeSymbol)symbol;
                        if (namedType.TypeKind == TypeKind.Delegate)
                        {
                            return GetDelegatePrototype((INamedTypeSymbol)symbol, flags);
                        }

                        break;
                }

                Debug.Fail("Invalid symbol kind: " + symbol.Kind);
                throw Exceptions.ThrowEUnexpected();
            }
            else
            {
                switch (node)
                {
                    case BaseMethodDeclarationSyntax methodDeclaration:
                        return GetFunctionPrototype(methodDeclaration, (IMethodSymbol)symbol, flags);
                    case BasePropertyDeclarationSyntax propertyDeclaration:
                        return GetPropertyPrototype(propertyDeclaration, (IPropertySymbol)symbol, flags);
                    case VariableDeclaratorSyntax variableDeclarator when symbol.Kind == SymbolKind.Field:
                        return GetVariablePrototype(variableDeclarator, (IFieldSymbol)symbol, flags);
                    case EnumMemberDeclarationSyntax enumMember:
                        return GetVariablePrototype(enumMember, (IFieldSymbol)symbol, flags);
                    case DelegateDeclarationSyntax delegateDeclaration:
                        return GetDelegatePrototype(delegateDeclaration, (INamedTypeSymbol)symbol, flags);
                }

                // Crazily, events for source are not implemented by the legacy C#
                // code model implementation, but they are for metadata events.

                Debug.Fail(string.Format("Invalid node/symbol kind: {0}/{1}", node.Kind(), symbol.Kind));
                throw Exceptions.ThrowENotImpl();
            }
        }

        private string GetDelegatePrototype(INamedTypeSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    // Note that we only throw E_FAIL in this case. All others throw E_INVALIDARG.
                    throw Exceptions.ThrowEFail();
                }

                // The unique signature is simply the node key.
                return GetExternalSymbolFullName(symbol);
            }

            var builder = new StringBuilder();

            AppendDelegatePrototype(builder, symbol, flags, symbol.Name);

            return builder.ToString();
        }

        private string GetDelegatePrototype(DelegateDeclarationSyntax node, INamedTypeSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                return GetNodeKey(node).Name;
            }

            var builder = new StringBuilder();

            AppendDelegatePrototype(builder, symbol, flags, GetName(node));

            return builder.ToString();
        }

        private string GetEventPrototype(IEventSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                flags = PrototypeFlags.FullName | PrototypeFlags.Type;
            }

            var builder = new StringBuilder();

            AppendEventPrototype(builder, symbol, flags, symbol.Name);

            return builder.ToString();
        }

        private string GetFunctionPrototype(IMethodSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                flags = PrototypeFlags.FullName | PrototypeFlags.Type | PrototypeFlags.ParameterTypes;
            }

            var builder = new StringBuilder();

            AppendFunctionPrototype(builder, symbol, flags, symbol.Name);

            return builder.ToString();
        }

        private string GetFunctionPrototype(BaseMethodDeclarationSyntax node, IMethodSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                return GetNodeKey(node).Name;
            }

            var builder = new StringBuilder();

            AppendFunctionPrototype(builder, symbol, flags, GetName(node));

            return builder.ToString();
        }

        private string GetPropertyPrototype(IPropertySymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                flags = PrototypeFlags.FullName | PrototypeFlags.Type;
            }

            var builder = new StringBuilder();

            AppendPropertyPrototype(builder, symbol, flags, symbol.Name);

            return builder.ToString();
        }

        private string GetPropertyPrototype(BasePropertyDeclarationSyntax node, IPropertySymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                return GetNodeKey(node).Name;
            }

            var builder = new StringBuilder();

            AppendPropertyPrototype(builder, symbol, flags, GetName(node));

            return builder.ToString();
        }

        private string GetVariablePrototype(IFieldSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                flags = PrototypeFlags.FullName | PrototypeFlags.Type;
            }

            var builder = new StringBuilder();

            AppendVariablePrototype(builder, symbol, flags, symbol.Name);

            return builder.ToString();
        }

        private string GetVariablePrototype(VariableDeclaratorSyntax node, IFieldSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                return GetNodeKey(node).Name;
            }

            var builder = new StringBuilder();

            AppendVariablePrototype(builder, symbol, flags, GetName(node));

            if ((flags & PrototypeFlags.Initializer) != 0 &&
                node.Initializer != null &&
                node.Initializer.Value != null &&
                !node.Initializer.Value.IsMissing)
            {
                builder.Append(" = ");
                builder.Append(node.Initializer.Value);
            }

            return builder.ToString();
        }

        private string GetVariablePrototype(EnumMemberDeclarationSyntax node, IFieldSymbol symbol, PrototypeFlags flags)
        {
            if ((flags & PrototypeFlags.Signature) != 0)
            {
                if (flags != PrototypeFlags.Signature)
                {
                    // vsCMPrototypeUniqueSignature can't be combined with anything else.
                    throw Exceptions.ThrowEInvalidArg();
                }

                // The unique signature is simply the node key.
                return GetNodeKey(node).Name;
            }

            var builder = new StringBuilder();

            AppendVariablePrototype(builder, symbol, flags, GetName(node));

            if ((flags & PrototypeFlags.Initializer) != 0 &&
                node.EqualsValue != null &&
                node.EqualsValue.Value != null &&
                !node.EqualsValue.Value.IsMissing)
            {
                builder.Append(" = ");
                builder.Append(node.EqualsValue.Value);
            }

            return builder.ToString();
        }

        private void AppendDelegatePrototype(StringBuilder builder, INamedTypeSymbol symbol, PrototypeFlags flags, string baseName)
        {
            builder.Append("delegate ");

            if ((flags & PrototypeFlags.Type) != 0)
            {
                builder.Append(GetAsStringForCodeTypeRef(symbol.DelegateInvokeMethod.ReturnType));

                if (((flags & PrototypeFlags.NameMask) != PrototypeFlags.NoName) ||
                    ((flags & (PrototypeFlags.ParameterNames | PrototypeFlags.ParameterTypes)) != 0))
                {
                    builder.Append(' ');
                }
            }

            var addSpace = true;

            switch (flags & PrototypeFlags.NameMask)
            {
                case PrototypeFlags.FullName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: false, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.TypeName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: true, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.BaseName:
                    builder.Append(baseName);
                    break;

                case PrototypeFlags.NoName:
                    addSpace = false;
                    break;
            }

            if ((flags & (PrototypeFlags.ParameterNames | PrototypeFlags.ParameterTypes)) != 0)
            {
                if (addSpace)
                {
                    builder.Append(' ');
                }

                builder.Append('(');

                AppendParametersPrototype(builder, symbol.DelegateInvokeMethod.Parameters, flags);

                builder.Append(')');
            }
        }

        private void AppendEventPrototype(StringBuilder builder, IEventSymbol symbol, PrototypeFlags flags, string baseName)
        {
            if ((flags & PrototypeFlags.Type) != 0)
            {
                builder.Append(GetAsStringForCodeTypeRef(symbol.Type));

                if ((flags & PrototypeFlags.NameMask) != PrototypeFlags.NoName)
                {
                    builder.Append(' ');
                }
            }

            switch (flags & PrototypeFlags.NameMask)
            {
                case PrototypeFlags.FullName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: false, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.TypeName:
                    AppendTypeNamePrototype(builder, includeNamespaces: false, includeGenerics: true, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.BaseName:
                    builder.Append(baseName);
                    break;
            }
        }

        private void AppendFunctionPrototype(StringBuilder builder, IMethodSymbol symbol, PrototypeFlags flags, string baseName)
        {
            if ((flags & PrototypeFlags.Type) != 0)
            {
                builder.Append(GetAsStringForCodeTypeRef(symbol.ReturnType));

                if ((flags & PrototypeFlags.NameMask) != PrototypeFlags.NoName)
                {
                    builder.Append(' ');
                }
            }

            var addSpace = true;

            switch (flags & PrototypeFlags.NameMask)
            {
                case PrototypeFlags.FullName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: false, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.TypeName:
                    AppendTypeNamePrototype(builder, includeNamespaces: false, includeGenerics: true, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.BaseName:
                    builder.Append(baseName);
                    break;

                case PrototypeFlags.NoName:
                    addSpace = false;
                    break;
            }

            if ((flags & (PrototypeFlags.ParameterNames | PrototypeFlags.ParameterTypes)) != 0)
            {
                if (addSpace)
                {
                    builder.Append(' ');
                }

                builder.Append('(');

                AppendParametersPrototype(builder, symbol.Parameters, flags);

                builder.Append(')');
            }
        }

        private void AppendPropertyPrototype(StringBuilder builder, IPropertySymbol symbol, PrototypeFlags flags, string baseName)
        {
            if ((flags & PrototypeFlags.Type) != 0)
            {
                builder.Append(GetAsStringForCodeTypeRef(symbol.Type));

                if ((flags & PrototypeFlags.NameMask) != PrototypeFlags.NoName)
                {
                    builder.Append(' ');
                }
            }

            switch (flags & PrototypeFlags.NameMask)
            {
                case PrototypeFlags.FullName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: false, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.TypeName:
                    AppendTypeNamePrototype(builder, includeNamespaces: false, includeGenerics: true, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.BaseName:
                    if (symbol.IsIndexer)
                    {
                        builder.Append("this[");
                        AppendParametersPrototype(builder, symbol.Parameters, PrototypeFlags.ParameterTypes | PrototypeFlags.ParameterNames);
                        builder.Append("]");
                    }
                    else
                    {
                        builder.Append(baseName);
                    }

                    break;
            }
        }

        private void AppendVariablePrototype(StringBuilder builder, IFieldSymbol symbol, PrototypeFlags flags, string baseName)
        {
            if ((flags & PrototypeFlags.Type) != 0)
            {
                builder.Append(GetAsStringForCodeTypeRef(symbol.Type));

                if ((flags & PrototypeFlags.NameMask) != PrototypeFlags.NoName)
                {
                    builder.Append(' ');
                }
            }

            switch (flags & PrototypeFlags.NameMask)
            {
                case PrototypeFlags.FullName:
                    AppendTypeNamePrototype(builder, includeNamespaces: true, includeGenerics: false, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.TypeName:
                    AppendTypeNamePrototype(builder, includeNamespaces: false, includeGenerics: true, symbol: symbol.ContainingSymbol);
                    builder.Append('.');
                    goto case PrototypeFlags.BaseName;

                case PrototypeFlags.BaseName:
                    builder.Append(baseName);
                    break;
            }
        }

        private void AppendParametersPrototype(StringBuilder builder, ImmutableArray<IParameterSymbol> parameters, PrototypeFlags flags)
        {
            var first = true;
            foreach (var parameter in parameters)
            {
                if (!first)
                {
                    builder.Append(", ");
                }

                AppendParameterPrototype(builder, flags, parameter);

                first = false;
            }
        }

        private void AppendParameterPrototype(StringBuilder builder, PrototypeFlags flags, IParameterSymbol parameter)
        {
            var addSpace = false;
            if ((flags & PrototypeFlags.ParameterTypes) != 0)
            {
                if (parameter.RefKind == RefKind.Ref)
                {
                    builder.Append("ref ");
                }
                else if (parameter.RefKind == RefKind.Out)
                {
                    builder.Append("out ");
                }
                else if (parameter.IsParams)
                {
                    builder.Append("params ");
                }

                builder.Append(GetAsStringForCodeTypeRef(parameter.Type));
                addSpace = true;
            }

            if ((flags & PrototypeFlags.ParameterNames) != 0)
            {
                if (addSpace)
                {
                    builder.Append(' ');
                }

                builder.Append(parameter.Name);
            }
        }

        private static void AppendTypeNamePrototype(StringBuilder builder, bool includeNamespaces, bool includeGenerics, ISymbol symbol)
        {
            var symbols = new Stack<ISymbol>();

            while (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Namespace)
                {
                    if (!includeNamespaces || ((INamespaceSymbol)symbol).IsGlobalNamespace)
                    {
                        break;
                    }
                }

                symbols.Push(symbol);

                symbol = symbol.ContainingSymbol;
            }

            var first = true;
            while (symbols.Count > 0)
            {
                var current = symbols.Pop();

                if (!first)
                {
                    builder.Append('.');
                }

                builder.Append(current.Name);

                if (current.Kind == SymbolKind.NamedType)
                {
                    var namedType = (INamedTypeSymbol)current;
                    if (includeGenerics && namedType.Arity > 0)
                    {
                        builder.Append('<');
                        builder.Append(',', namedType.Arity - 1);
                        builder.Append('>');
                    }
                }

                first = false;
            }
        }
    }
}
