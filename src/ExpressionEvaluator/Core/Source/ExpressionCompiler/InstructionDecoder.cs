// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class InstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol>
        where TCompilation : Compilation
        where TMethodSymbol : class, IMethodSymbol
        where TModuleSymbol : class, IModuleSymbol
        where TTypeSymbol : class, ITypeSymbol
        where TTypeParameterSymbol : class, ITypeParameterSymbol
    {
        internal static readonly SymbolDisplayFormat DisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private readonly bool _useReferencedAssembliesOnly;

        internal InstructionDecoder()
        {
            // Should be passed by the ExpressionCompiler as an argument to this constructor.
            _useReferencedAssembliesOnly = ExpressionCompiler.GetUseReferencedAssembliesOnlySetting();
        }

        internal MakeAssemblyReferencesKind GetMakeAssemblyReferencesKind()
        {
            return _useReferencedAssembliesOnly ? MakeAssemblyReferencesKind.AllReferences : MakeAssemblyReferencesKind.AllAssemblies;
        }

        internal abstract void AppendFullName(StringBuilder builder, TMethodSymbol method);

        internal virtual void AppendParameterTypeName(StringBuilder builder, IParameterSymbol parameter)
        {
            builder.Append(parameter.Type.ToDisplayString(DisplayFormat));
        }

        /// <summary>
        /// Constructs a method and any of its generic containing types using the specified <paramref name="typeArguments"/>.
        /// </summary>
        internal abstract TMethodSymbol ConstructMethod(TMethodSymbol method, ImmutableArray<TTypeParameterSymbol> typeParameters, ImmutableArray<TTypeSymbol> typeArguments);

        internal abstract ImmutableArray<TTypeParameterSymbol> GetAllTypeParameters(TMethodSymbol method);

        internal abstract TCompilation GetCompilation(DkmClrModuleInstance moduleInstance);

        internal abstract TMethodSymbol GetMethod(TCompilation compilation, DkmClrInstructionAddress instructionAddress);

        internal string GetName(TMethodSymbol method, bool includeParameterTypes, bool includeParameterNames, ArrayBuilder<string> argumentValues = null)
        {
            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            // "full name" of method...
            AppendFullName(builder, method);

            // parameter list...
            var parameters = method.Parameters;
            var includeArgumentValues = (argumentValues != null) && (parameters.Length == argumentValues.Count);
            if (includeParameterTypes || includeParameterNames || includeArgumentValues)
            {
                builder.Append('(');
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    var parameter = parameters[i];

                    if (includeParameterTypes)
                    {
                        AppendParameterTypeName(builder, parameter);
                    }

                    if (includeParameterNames)
                    {
                        if (includeParameterTypes)
                        {
                            builder.Append(' ');
                        }

                        builder.Append(parameter.Name);
                    }

                    if (includeArgumentValues)
                    {
                        var argumentValue = argumentValues[i];
                        if (argumentValue != null)
                        {
                            if (includeParameterTypes || includeParameterNames)
                            {
                                builder.Append(" = ");
                            }

                            builder.Append(argumentValue);
                        }
                    }
                }
                builder.Append(')');
            }

            return pooled.ToStringAndFree();
        }

        internal string GetReturnTypeName(TMethodSymbol method)
        {
            return method.ReturnType.ToDisplayString(DisplayFormat);
        }

        internal abstract TypeNameDecoder<TModuleSymbol, TTypeSymbol> GetTypeNameDecoder(TCompilation compilation, TMethodSymbol method);

        internal ImmutableArray<TTypeSymbol> GetTypeSymbols(TCompilation compilation, TMethodSymbol method, string[] serializedTypeNames)
        {
            if (serializedTypeNames == null)
            {
                return ImmutableArray<TTypeSymbol>.Empty;
            }

            var builder = ArrayBuilder<TTypeSymbol>.GetInstance();
            foreach (var name in serializedTypeNames)
            {
                // The list of type names will include null values if type arguments are not available.
                // It seems unlikely that only some type arguments will be missing (and it also seems
                // like very little incremental value to include only some of the arguments), so we'll
                // keep things simple and omit all type arguments if any are missing.
                if (name == null)
                {
                    builder.Free();
                    return ImmutableArray<TTypeSymbol>.Empty;
                }

                var typeNameDecoder = GetTypeNameDecoder(compilation, method);
                builder.Add(typeNameDecoder.GetTypeSymbolForSerializedType(name));
            }
            return builder.ToImmutableAndFree();
        }
    }
}
