// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

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
        {
            return compilation.GetTypeByMetadataName("System.Attribute");
        }

        public static INamedTypeSymbol ExceptionType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Exception");
        }

        public static INamedTypeSymbol DesignerCategoryAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");
        }

        public static INamedTypeSymbol DesignerGeneratedAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");
        }

        public static INamedTypeSymbol HideModuleNameAttribute(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");
        }

        public static INamedTypeSymbol EventArgsType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventArgs");
        }

        public static INamedTypeSymbol NotImplementedExceptionType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotImplementedException");
        }

        public static INamedTypeSymbol EqualityComparerOfTType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.EqualityComparer`1");
        }

        public static INamedTypeSymbol ActionType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Action");
        }

        public static INamedTypeSymbol ExpressionOfTType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        }

        public static INamedTypeSymbol EditorBrowsableAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.EditorBrowsableAttribute");
        }

        public static INamedTypeSymbol EditorBrowsableStateType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.EditorBrowsableState");
        }

        public static INamedTypeSymbol TaskType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        }

        public static INamedTypeSymbol TaskOfTType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        }

        public static INamedTypeSymbol SerializableAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.SerializableAttribute");
        }

        public static INamedTypeSymbol CoClassType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CoClassAttribute");
        }

        public static INamedTypeSymbol ComAliasNameAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComAliasNameAttribute");
        }

        public static INamedTypeSymbol SuppressMessageAttributeType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute");
        }

        public static INamedTypeSymbol ConvertType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Convert");
        }

        public static INamedTypeSymbol IConvertibleType(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IConvertible");
        }
    }
}
