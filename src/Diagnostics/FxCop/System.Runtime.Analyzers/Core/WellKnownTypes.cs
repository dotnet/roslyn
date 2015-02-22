// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    internal static class WellKnownTypes
    {
        public static INamedTypeSymbol FlagsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.FlagsAttribute");
        }

        public static INamedTypeSymbol StringComparison(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.StringComparison");
        }

        public static INamedTypeSymbol CharSet(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CharSet");
        }

        public static INamedTypeSymbol DllImportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
        }

        public static INamedTypeSymbol MarshalAsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MarshalAsAttribute");
        }

        public static INamedTypeSymbol StringBuilder(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        }

        public static INamedTypeSymbol UnmanagedType(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedType");
        }

        public static INamedTypeSymbol MarshalByRefObject(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.MarshalByRefObject");
        }

        public static INamedTypeSymbol ExecutionEngineException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ExecutionEngineException");
        }

        public static INamedTypeSymbol OutOfMemoryException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.OutOfMemoryException");
        }

        public static INamedTypeSymbol StackOverflowException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.StackOverflowException");
        }

        public static INamedTypeSymbol MemberInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.MemberInfo");
        }

        public static INamedTypeSymbol ParameterInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.ParameterInfo");
        }

        public static INamedTypeSymbol Thread(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Threading.Thread");
        }

        public static INamedTypeSymbol WebUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.UI.Control");
        }

        public static INamedTypeSymbol WinFormsUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Windows.Forms.Control");
        }

        public static INamedTypeSymbol IDisposable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IDisposable");
        }

        public static INamedTypeSymbol ISerializable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable");
        }

        public static INamedTypeSymbol SerializationInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.SerializationInfo");
        }

        public static INamedTypeSymbol StreamingContext(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.StreamingContext");
        }

        public static INamedTypeSymbol SerializableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.SerializableAttribute");
        }

        public static INamedTypeSymbol NonSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NonSerializedAttribute");
        }

        public static INamedTypeSymbol AttributeUsageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        }

        public static INamedTypeSymbol AssemblyVersionAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.AssemblyVersionAttribute");
        }

        public static INamedTypeSymbol CLSCompliantAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.CLSCompliantAttribute");
        }

        public static INamedTypeSymbol ConditionalAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Diagnostics.ConditionalAttribute");
        }

        public static INamedTypeSymbol IComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IComparable");
        }

        public static INamedTypeSymbol GenericIComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IComparable`1");
        }

        public static INamedTypeSymbol ComSourceInterfaceAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComSourceInterfacesAttribute");
        }

        public static INamedTypeSymbol EventHandler(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventHandler");
        }

        public static INamedTypeSymbol GenericEventHandler(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventHandler`1");
        }

        public static INamedTypeSymbol EventArgs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventArgs");
        }

        public static INamedTypeSymbol ComVisibleAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComVisibleAttribute");
        }

        public static INamedTypeSymbol NotImplementedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotImplementedException");
        }

        public static INamedTypeSymbol Attribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Attribute");
        }
    }
}
