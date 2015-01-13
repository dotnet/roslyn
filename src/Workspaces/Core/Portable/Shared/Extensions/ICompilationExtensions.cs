// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ICompilationExtensions
    {
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
    }
}