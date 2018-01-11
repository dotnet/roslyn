// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class WellKnownTypes
    {
        public static INamedTypeSymbol ICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.ICollection");
        }

        public static INamedTypeSymbol GenericICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
        }

        public static INamedTypeSymbol IEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        }

        public static INamedTypeSymbol IEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
        }

        public static INamedTypeSymbol GenericIEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        }

        public static INamedTypeSymbol GenericIEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1");
        }

        public static INamedTypeSymbol IList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IList");
        }

        public static INamedTypeSymbol GenericIList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
        }

        public static INamedTypeSymbol Array(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Array);
        }

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

        public static INamedTypeSymbol NotImplementedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotImplementedException");
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

        public static INamedTypeSymbol OnDeserializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnDeserializingAttribute");
        }

        public static INamedTypeSymbol OnDeserializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnDeserializedAttribute");
        }

        public static INamedTypeSymbol OnSerializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnSerializingAttribute");
        }

        public static INamedTypeSymbol OnSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnSerializedAttribute");
        }

        public static INamedTypeSymbol SerializableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.SerializableAttribute");
        }

        public static INamedTypeSymbol NonSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NonSerializedAttribute");
        }

        public static INamedTypeSymbol Attribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Attribute");
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

        public static INamedTypeSymbol GenericEventHandler(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventHandler`1");
        }

        public static INamedTypeSymbol EventArgs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventArgs");
        }

        public static INamedTypeSymbol Uri(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Uri");
        }

        public static INamedTypeSymbol ComVisibleAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComVisibleAttribute");
        }

        public static INamedTypeSymbol NeutralResourcesLanguageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Resources.NeutralResourcesLanguageAttribute");
        }

        public static INamedTypeSymbol GeneratedCodeAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");
        }

        public static INamedTypeSymbol Console(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Console");
        }

        public static INamedTypeSymbol String(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_String);
        }

        public static INamedTypeSymbol Object(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Object);
        }

        public static INamedTypeSymbol Exception(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Exception");
        }

        public static INamedTypeSymbol InvalidOperationException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.InvalidOperationException");
        }

        public static INamedTypeSymbol ArgumentException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ArgumentException");
        }

        public static INamedTypeSymbol NotSupportedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotSupportedException");
        }

        public static INamedTypeSymbol KeyNotFoundException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.KeyNotFoundException");
        }

        public static INamedTypeSymbol GenericIEqualityComparer(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
        }

        public static INamedTypeSymbol GenericIEquatable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IEquatable`1");
        }

        public static INamedTypeSymbol IHashCodeProvider(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IHashCodeProvider");
        }

        public static INamedTypeSymbol IntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_IntPtr);
        }

        public static INamedTypeSymbol UIntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_UIntPtr);
        }

        public static INamedTypeSymbol HandleRef(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.HandleRef");
        }

        public static INamedTypeSymbol DataMemberAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.DataMemberAttribute");
        }

        public static INamedTypeSymbol ObsoleteAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
        }

        public static INamedTypeSymbol PureAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.PureAttribute");
        }

        public static INamedTypeSymbol MEFV1ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ExportAttribute");
        }

        public static INamedTypeSymbol MEFV2ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");
        }

        public static INamedTypeSymbol FieldOffsetAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
        }

        public static INamedTypeSymbol StructLayoutAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
        }

        public static INamedTypeSymbol IDbCommand(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Data.IDbCommand");
        }

        public static INamedTypeSymbol IDataAdapter(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Data.IDataAdapter");
        }
    }
}