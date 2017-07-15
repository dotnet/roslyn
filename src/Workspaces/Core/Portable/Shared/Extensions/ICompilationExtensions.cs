// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ICompilationExtensions
    {
        public static ImmutableArray<Compilation> GetReferencedCompilations(this Compilation compilation)
        {
            var builder = ArrayBuilder<Compilation>.GetInstance();

            foreach (var reference in compilation.References.OfType<CompilationReference>())
            {
                builder.Add(reference.Compilation);
            }

            var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            while (previous != null)
            {
                builder.Add(previous);
                previous = previous.ScriptCompilationInfo?.PreviousScriptCompilation;
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<IAssemblySymbol> GetReferencedAssemblySymbols(this Compilation compilation)
        {
            var builder = ArrayBuilder<IAssemblySymbol>.GetInstance();

            // The first module of every assembly is its source module and the source
            // module always has the list of all referenced assemblies.
            builder.AddRange(compilation.Assembly.Modules.First().ReferencedAssemblySymbols);

            var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            while (previous != null)
            {
                builder.Add(previous.Assembly);
                previous = previous.ScriptCompilationInfo?.PreviousScriptCompilation;
            }

            return builder.ToImmutableAndFree();
        }

        public static INamedTypeSymbol AttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Attribute).FullName);

        public static INamedTypeSymbol ExceptionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Exception).FullName);

        public static INamedTypeSymbol DesignerCategoryAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");

        public static INamedTypeSymbol DesignerGeneratedAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");

        public static INamedTypeSymbol HideModuleNameAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");

        public static INamedTypeSymbol EventArgsType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EventArgs).FullName);

        public static INamedTypeSymbol NotImplementedExceptionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(NotImplementedException).FullName);

        public static INamedTypeSymbol EqualityComparerOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName);

        public static INamedTypeSymbol ActionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Action).FullName);

        public static INamedTypeSymbol ExpressionOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

        public static INamedTypeSymbol EditorBrowsableAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableAttribute).FullName);

        public static INamedTypeSymbol EditorBrowsableStateType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableState).FullName);

        public static INamedTypeSymbol TaskType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Task).FullName);

        public static INamedTypeSymbol TaskOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Task<>).FullName);

        public static INamedTypeSymbol ValueTaskOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        public static INamedTypeSymbol IEnumerableOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName);

        public static INamedTypeSymbol SerializableAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.SerializableAttribute");

        public static INamedTypeSymbol CoClassType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(CoClassAttribute).FullName);

        public static INamedTypeSymbol ComAliasNameAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComAliasNameAttribute");

        public static INamedTypeSymbol SuppressMessageAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName);

        public static INamedTypeSymbol TupleElementNamesAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(TupleElementNamesAttribute).FullName);

        public static INamedTypeSymbol DynamicAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.DynamicAttribute");
    }
}
