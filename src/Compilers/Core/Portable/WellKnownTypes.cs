// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Ids of well known runtime types.
    /// Values should not intersect with SpecialType enum!
    /// </summary>
    /// <remarks></remarks>
    internal enum WellKnownType
    {
        // Value 0 represents an unknown type
        Unknown = SpecialType.None,

        First = SpecialType.Count + 1,

        // The following type ids should be in sync with names in WellKnownTypes.metadataNames array.
        System_Math = First,
        System_Array,
        System_Attribute,
        System_CLSCompliantAttribute,
        System_Convert,
        System_Exception,
        System_FlagsAttribute,
        System_FormattableString,
        System_Guid,
        System_IFormattable,
        System_RuntimeTypeHandle,
        System_RuntimeFieldHandle,
        System_RuntimeMethodHandle,
        System_MarshalByRefObject,
        System_Type,
        System_Reflection_AssemblyKeyFileAttribute,
        System_Reflection_AssemblyKeyNameAttribute,
        System_Reflection_MethodInfo,
        System_Reflection_ConstructorInfo,
        System_Reflection_MethodBase,
        System_Reflection_FieldInfo,
        System_Reflection_MemberInfo,
        System_Reflection_Missing,
        System_Runtime_CompilerServices_FormattableStringFactory,
        System_Runtime_CompilerServices_RuntimeHelpers,
        System_Runtime_ExceptionServices_ExceptionDispatchInfo,
        System_Runtime_InteropServices_StructLayoutAttribute,
        System_Runtime_InteropServices_UnknownWrapper,
        System_Runtime_InteropServices_DispatchWrapper,
        System_Runtime_InteropServices_CallingConvention,
        System_Runtime_InteropServices_ClassInterfaceAttribute,
        System_Runtime_InteropServices_ClassInterfaceType,
        System_Runtime_InteropServices_CoClassAttribute,
        System_Runtime_InteropServices_ComAwareEventInfo,
        System_Runtime_InteropServices_ComEventInterfaceAttribute,
        System_Runtime_InteropServices_ComInterfaceType,
        System_Runtime_InteropServices_ComSourceInterfacesAttribute,
        System_Runtime_InteropServices_ComVisibleAttribute,
        System_Runtime_InteropServices_DispIdAttribute,
        System_Runtime_InteropServices_GuidAttribute,
        System_Runtime_InteropServices_InterfaceTypeAttribute,
        System_Runtime_InteropServices_Marshal,
        System_Runtime_InteropServices_TypeIdentifierAttribute,
        System_Runtime_InteropServices_BestFitMappingAttribute,
        System_Runtime_InteropServices_DefaultParameterValueAttribute,
        System_Runtime_InteropServices_LCIDConversionAttribute,
        System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute,
        System_Activator,
        System_Threading_Tasks_Task,
        System_Threading_Tasks_Task_T,
        System_Threading_Interlocked,
        System_Threading_Monitor,
        System_Threading_Thread,
        Microsoft_CSharp_RuntimeBinder_Binder,
        Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,
        Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags,
        Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
        Microsoft_VisualBasic_CallType,
        Microsoft_VisualBasic_Embedded,
        Microsoft_VisualBasic_CompilerServices_Conversions,
        Microsoft_VisualBasic_CompilerServices_Operators,
        Microsoft_VisualBasic_CompilerServices_NewLateBinding,
        Microsoft_VisualBasic_CompilerServices_EmbeddedOperators,
        Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute,
        Microsoft_VisualBasic_CompilerServices_Utils,
        Microsoft_VisualBasic_CompilerServices_LikeOperator,
        Microsoft_VisualBasic_CompilerServices_ProjectData,
        Microsoft_VisualBasic_CompilerServices_ObjectFlowControl,
        Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,
        Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,
        Microsoft_VisualBasic_CompilerServices_StringType,
        Microsoft_VisualBasic_CompilerServices_IncompleteInitialization,
        Microsoft_VisualBasic_CompilerServices_Versioned,
        Microsoft_VisualBasic_CompareMethod,
        Microsoft_VisualBasic_Strings,
        Microsoft_VisualBasic_ErrObject,
        Microsoft_VisualBasic_FileSystem,
        Microsoft_VisualBasic_ApplicationServices_ApplicationBase,
        Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase,
        Microsoft_VisualBasic_Information,
        Microsoft_VisualBasic_Interaction,

        // standard Func delegates - must be ordered by arity
        System_Func_T,
        System_Func_T2,
        System_Func_T3,
        System_Func_T4,
        System_Func_T5,
        System_Func_T6,
        System_Func_T7,
        System_Func_T8,
        System_Func_T9,
        System_Func_T10,
        System_Func_T11,
        System_Func_T12,
        System_Func_T13,
        System_Func_T14,
        System_Func_T15,
        System_Func_T16,
        System_Func_T17,
        System_Func_TMax = System_Func_T17,

        // standard Action delegates - must be ordered by arity
        System_Action,
        System_Action_T,
        System_Action_T2,
        System_Action_T3,
        System_Action_T4,
        System_Action_T5,
        System_Action_T6,
        System_Action_T7,
        System_Action_T8,
        System_Action_T9,
        System_Action_T10,
        System_Action_T11,
        System_Action_T12,
        System_Action_T13,
        System_Action_T14,
        System_Action_T15,
        System_Action_T16,
        System_Action_TMax = System_Action_T16,

        System_AttributeUsageAttribute,
        System_ParamArrayAttribute,
        System_NonSerializedAttribute,
        System_STAThreadAttribute,
        System_Reflection_DefaultMemberAttribute,
        System_Runtime_CompilerServices_DateTimeConstantAttribute,
        System_Runtime_CompilerServices_DecimalConstantAttribute,
        System_Runtime_CompilerServices_IUnknownConstantAttribute,
        System_Runtime_CompilerServices_IDispatchConstantAttribute,
        System_Runtime_CompilerServices_ExtensionAttribute,
        System_Runtime_CompilerServices_INotifyCompletion,
        System_Runtime_CompilerServices_InternalsVisibleToAttribute,
        System_Runtime_CompilerServices_CompilerGeneratedAttribute,
        System_Runtime_CompilerServices_AccessedThroughPropertyAttribute,
        System_Runtime_CompilerServices_CompilationRelaxationsAttribute,
        System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,
        System_Runtime_CompilerServices_UnsafeValueTypeAttribute,
        System_Runtime_CompilerServices_FixedBufferAttribute,
        System_Runtime_CompilerServices_DynamicAttribute,
        System_Runtime_CompilerServices_CallSiteBinder,
        System_Runtime_CompilerServices_CallSite,
        System_Runtime_CompilerServices_CallSite_T,

        System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
        System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,
        System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,

        Windows_Foundation_IAsyncAction,
        Windows_Foundation_IAsyncActionWithProgress_T,
        Windows_Foundation_IAsyncOperation_T,
        Windows_Foundation_IAsyncOperationWithProgress_T2,

        System_Diagnostics_Debugger,
        System_Diagnostics_DebuggerDisplayAttribute,
        System_Diagnostics_DebuggerNonUserCodeAttribute,
        System_Diagnostics_DebuggerHiddenAttribute,
        System_Diagnostics_DebuggerBrowsableAttribute,
        System_Diagnostics_DebuggerStepThroughAttribute,
        System_Diagnostics_DebuggerBrowsableState,
        System_Diagnostics_DebuggableAttribute,
        System_Diagnostics_DebuggableAttribute__DebuggingModes,

        System_ComponentModel_DesignerSerializationVisibilityAttribute,

        System_IEquatable_T,

        System_Collections_IList,
        System_Collections_ICollection,
        System_Collections_Generic_EqualityComparer_T,
        System_Collections_Generic_List_T,
        System_Collections_Generic_IDictionary_KV,
        System_Collections_Generic_IReadOnlyDictionary_KV,
        System_Collections_ObjectModel_Collection_T,
        System_Collections_ObjectModel_ReadOnlyCollection_T,
        System_Collections_Specialized_INotifyCollectionChanged,
        System_ComponentModel_INotifyPropertyChanged,
        System_ComponentModel_EditorBrowsableAttribute,
        System_ComponentModel_EditorBrowsableState,

        System_Linq_Enumerable,
        System_Linq_Expressions_Expression,
        System_Linq_Expressions_Expression_T,
        System_Linq_Expressions_ParameterExpression,
        System_Linq_Expressions_ElementInit,
        System_Linq_Expressions_MemberBinding,
        System_Linq_Expressions_ExpressionType,
        System_Linq_IQueryable,
        System_Linq_IQueryable_T,

        System_Xml_Linq_Extensions,
        System_Xml_Linq_XAttribute,
        System_Xml_Linq_XCData,
        System_Xml_Linq_XComment,
        System_Xml_Linq_XContainer,
        System_Xml_Linq_XDeclaration,
        System_Xml_Linq_XDocument,
        System_Xml_Linq_XElement,
        System_Xml_Linq_XName,
        System_Xml_Linq_XNamespace,
        System_Xml_Linq_XObject,
        System_Xml_Linq_XProcessingInstruction,

        System_Security_UnverifiableCodeAttribute,
        System_Security_Permissions_SecurityAction,
        System_Security_Permissions_SecurityAttribute,
        System_Security_Permissions_SecurityPermissionAttribute,

        System_NotSupportedException,

        System_Runtime_CompilerServices_ICriticalNotifyCompletion,
        System_Runtime_CompilerServices_IAsyncStateMachine,
        System_Runtime_CompilerServices_AsyncVoidMethodBuilder,
        System_Runtime_CompilerServices_AsyncTaskMethodBuilder,
        System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,
        System_Runtime_CompilerServices_AsyncStateMachineAttribute,
        System_Runtime_CompilerServices_IteratorStateMachineAttribute,

        System_Windows_Forms_Form,
        System_Windows_Forms_Application,

        System_Environment,

        System_Runtime_GCLatencyMode,
        System_IFormatProvider,

        CSharp7Sentinel = System_IFormatProvider, // all types that were known before CSharp7 should remain above this sentinel

        System_ValueTuple_T1,
        System_ValueTuple_T2,
        System_ValueTuple_T3,
        System_ValueTuple_T4,
        System_ValueTuple_T5,

        ExtSentinel, // Not a real type, just a marker for types above 255 and strictly below 512

        System_ValueTuple_T6,
        System_ValueTuple_T7,
        System_ValueTuple_TRest,

        System_Runtime_CompilerServices_TupleElementNamesAttribute,

        Microsoft_CodeAnalysis_Runtime_Instrumentation,
        System_Runtime_CompilerServices_NullableAttribute,
        System_Runtime_CompilerServices_NullableContextAttribute,
        System_Runtime_CompilerServices_NullablePublicOnlyAttribute,
        System_Runtime_CompilerServices_ReferenceAssemblyAttribute,

        System_Runtime_CompilerServices_IsReadOnlyAttribute,
        System_Runtime_CompilerServices_IsByRefLikeAttribute,
        System_Runtime_InteropServices_InAttribute,
        System_ObsoleteAttribute,
        System_Span_T,
        System_ReadOnlySpan_T,
        System_Runtime_InteropServices_UnmanagedType,
        System_Runtime_CompilerServices_IsUnmanagedAttribute,

        Microsoft_VisualBasic_Conversion,
        System_Runtime_CompilerServices_NonNullTypesAttribute,
        System_AttributeTargets,
        Microsoft_CodeAnalysis_EmbeddedAttribute,
        System_Runtime_CompilerServices_ITuple,

        System_Index,
        System_Range,

        System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute,
        System_IAsyncDisposable,
        System_Collections_Generic_IAsyncEnumerable_T,
        System_Collections_Generic_IAsyncEnumerator_T,
        System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T,
        System_Threading_Tasks_Sources_ValueTaskSourceStatus,
        System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
        System_Threading_Tasks_Sources_IValueTaskSource_T,
        System_Threading_Tasks_Sources_IValueTaskSource,
        System_Threading_Tasks_ValueTask_T,
        System_Threading_Tasks_ValueTask,
        System_Runtime_CompilerServices_AsyncIteratorMethodBuilder,
        System_Threading_CancellationToken,
        System_Threading_CancellationTokenSource,

        System_InvalidOperationException,
        System_Runtime_CompilerServices_SwitchExpressionException,
        System_Collections_Generic_IEqualityComparer_T,

        NextAvailable,

        // Remember to update the AllWellKnownTypes tests when making changes here
    }

    internal static class WellKnownTypes
    {
        /// <summary>
        /// Number of well known types in WellKnownType enum
        /// </summary>
        internal const int Count = WellKnownType.NextAvailable - WellKnownType.First;

        /// <summary>
        /// Array of names for types.
        /// The names should correspond to ids from WellKnownType enum so
        /// that we could use ids to index into the array
        /// </summary>
        /// <remarks></remarks>
        private static readonly string[] s_metadataNames = new string[]
        {
            "System.Math",
            "System.Array",
            "System.Attribute",
            "System.CLSCompliantAttribute",
            "System.Convert",
            "System.Exception",
            "System.FlagsAttribute",
            "System.FormattableString",
            "System.Guid",
            "System.IFormattable",
            "System.RuntimeTypeHandle",
            "System.RuntimeFieldHandle",
            "System.RuntimeMethodHandle",
            "System.MarshalByRefObject",
            "System.Type",
            "System.Reflection.AssemblyKeyFileAttribute",
            "System.Reflection.AssemblyKeyNameAttribute",
            "System.Reflection.MethodInfo",
            "System.Reflection.ConstructorInfo",
            "System.Reflection.MethodBase",
            "System.Reflection.FieldInfo",
            "System.Reflection.MemberInfo",
            "System.Reflection.Missing",
            "System.Runtime.CompilerServices.FormattableStringFactory",
            "System.Runtime.CompilerServices.RuntimeHelpers",
            "System.Runtime.ExceptionServices.ExceptionDispatchInfo",
            "System.Runtime.InteropServices.StructLayoutAttribute",
            "System.Runtime.InteropServices.UnknownWrapper",
            "System.Runtime.InteropServices.DispatchWrapper",
            "System.Runtime.InteropServices.CallingConvention",
            "System.Runtime.InteropServices.ClassInterfaceAttribute",
            "System.Runtime.InteropServices.ClassInterfaceType",
            "System.Runtime.InteropServices.CoClassAttribute",
            "System.Runtime.InteropServices.ComAwareEventInfo",
            "System.Runtime.InteropServices.ComEventInterfaceAttribute",
            "System.Runtime.InteropServices.ComInterfaceType",
            "System.Runtime.InteropServices.ComSourceInterfacesAttribute",
            "System.Runtime.InteropServices.ComVisibleAttribute",
            "System.Runtime.InteropServices.DispIdAttribute",
            "System.Runtime.InteropServices.GuidAttribute",
            "System.Runtime.InteropServices.InterfaceTypeAttribute",
            "System.Runtime.InteropServices.Marshal",
            "System.Runtime.InteropServices.TypeIdentifierAttribute",
            "System.Runtime.InteropServices.BestFitMappingAttribute",
            "System.Runtime.InteropServices.DefaultParameterValueAttribute",
            "System.Runtime.InteropServices.LCIDConversionAttribute",
            "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute",
            "System.Activator",
            "System.Threading.Tasks.Task",
            "System.Threading.Tasks.Task`1",
            "System.Threading.Interlocked",
            "System.Threading.Monitor",
            "System.Threading.Thread",
            "Microsoft.CSharp.RuntimeBinder.Binder",
            "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo",
            "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags",
            "Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags",
            "Microsoft.VisualBasic.CallType",
            "Microsoft.VisualBasic.Embedded",
            "Microsoft.VisualBasic.CompilerServices.Conversions",
            "Microsoft.VisualBasic.CompilerServices.Operators",
            "Microsoft.VisualBasic.CompilerServices.NewLateBinding",
            "Microsoft.VisualBasic.CompilerServices.EmbeddedOperators",
            "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute",
            "Microsoft.VisualBasic.CompilerServices.Utils",
            "Microsoft.VisualBasic.CompilerServices.LikeOperator",
            "Microsoft.VisualBasic.CompilerServices.ProjectData",
            "Microsoft.VisualBasic.CompilerServices.ObjectFlowControl",
            "Microsoft.VisualBasic.CompilerServices.ObjectFlowControl+ForLoopControl",
            "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag",
            "Microsoft.VisualBasic.CompilerServices.StringType",
            "Microsoft.VisualBasic.CompilerServices.IncompleteInitialization",
            "Microsoft.VisualBasic.CompilerServices.Versioned",
            "Microsoft.VisualBasic.CompareMethod",
            "Microsoft.VisualBasic.Strings",
            "Microsoft.VisualBasic.ErrObject",
            "Microsoft.VisualBasic.FileSystem",
            "Microsoft.VisualBasic.ApplicationServices.ApplicationBase",
            "Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase",
            "Microsoft.VisualBasic.Information",
            "Microsoft.VisualBasic.Interaction",

            "System.Func`1",
            "System.Func`2",
            "System.Func`3",
            "System.Func`4",
            "System.Func`5",
            "System.Func`6",
            "System.Func`7",
            "System.Func`8",
            "System.Func`9",
            "System.Func`10",
            "System.Func`11",
            "System.Func`12",
            "System.Func`13",
            "System.Func`14",
            "System.Func`15",
            "System.Func`16",
            "System.Func`17",
            "System.Action",
            "System.Action`1",
            "System.Action`2",
            "System.Action`3",
            "System.Action`4",
            "System.Action`5",
            "System.Action`6",
            "System.Action`7",
            "System.Action`8",
            "System.Action`9",
            "System.Action`10",
            "System.Action`11",
            "System.Action`12",
            "System.Action`13",
            "System.Action`14",
            "System.Action`15",
            "System.Action`16",

            "System.AttributeUsageAttribute",
            "System.ParamArrayAttribute",
            "System.NonSerializedAttribute",
            "System.STAThreadAttribute",
            "System.Reflection.DefaultMemberAttribute",
            "System.Runtime.CompilerServices.DateTimeConstantAttribute",
            "System.Runtime.CompilerServices.DecimalConstantAttribute",
            "System.Runtime.CompilerServices.IUnknownConstantAttribute",
            "System.Runtime.CompilerServices.IDispatchConstantAttribute",
            "System.Runtime.CompilerServices.ExtensionAttribute",
            "System.Runtime.CompilerServices.INotifyCompletion",
            "System.Runtime.CompilerServices.InternalsVisibleToAttribute",
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
            "System.Runtime.CompilerServices.AccessedThroughPropertyAttribute",
            "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
            "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
            "System.Runtime.CompilerServices.UnsafeValueTypeAttribute",
            "System.Runtime.CompilerServices.FixedBufferAttribute",
            "System.Runtime.CompilerServices.DynamicAttribute",
            "System.Runtime.CompilerServices.CallSiteBinder",
            "System.Runtime.CompilerServices.CallSite",
            "System.Runtime.CompilerServices.CallSite`1",

            "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken",
            "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1",
            "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal",

            "Windows.Foundation.IAsyncAction",
            "Windows.Foundation.IAsyncActionWithProgress`1",
            "Windows.Foundation.IAsyncOperation`1",
            "Windows.Foundation.IAsyncOperationWithProgress`2",

            "System.Diagnostics.Debugger",
            "System.Diagnostics.DebuggerDisplayAttribute",
            "System.Diagnostics.DebuggerNonUserCodeAttribute",
            "System.Diagnostics.DebuggerHiddenAttribute",
            "System.Diagnostics.DebuggerBrowsableAttribute",
            "System.Diagnostics.DebuggerStepThroughAttribute",
            "System.Diagnostics.DebuggerBrowsableState",
            "System.Diagnostics.DebuggableAttribute",
            "System.Diagnostics.DebuggableAttribute+DebuggingModes",

            "System.ComponentModel.DesignerSerializationVisibilityAttribute",

            "System.IEquatable`1",

            "System.Collections.IList",
            "System.Collections.ICollection",
            "System.Collections.Generic.EqualityComparer`1",
            "System.Collections.Generic.List`1",
            "System.Collections.Generic.IDictionary`2",
            "System.Collections.Generic.IReadOnlyDictionary`2",
            "System.Collections.ObjectModel.Collection`1",
            "System.Collections.ObjectModel.ReadOnlyCollection`1",
            "System.Collections.Specialized.INotifyCollectionChanged",
            "System.ComponentModel.INotifyPropertyChanged",
            "System.ComponentModel.EditorBrowsableAttribute",
            "System.ComponentModel.EditorBrowsableState",

            "System.Linq.Enumerable",
            "System.Linq.Expressions.Expression",
            "System.Linq.Expressions.Expression`1",
            "System.Linq.Expressions.ParameterExpression",
            "System.Linq.Expressions.ElementInit",
            "System.Linq.Expressions.MemberBinding",
            "System.Linq.Expressions.ExpressionType",
            "System.Linq.IQueryable",
            "System.Linq.IQueryable`1",

            "System.Xml.Linq.Extensions",
            "System.Xml.Linq.XAttribute",
            "System.Xml.Linq.XCData",
            "System.Xml.Linq.XComment",
            "System.Xml.Linq.XContainer",
            "System.Xml.Linq.XDeclaration",
            "System.Xml.Linq.XDocument",
            "System.Xml.Linq.XElement",
            "System.Xml.Linq.XName",
            "System.Xml.Linq.XNamespace",
            "System.Xml.Linq.XObject",
            "System.Xml.Linq.XProcessingInstruction",

            "System.Security.UnverifiableCodeAttribute",
            "System.Security.Permissions.SecurityAction",
            "System.Security.Permissions.SecurityAttribute",
            "System.Security.Permissions.SecurityPermissionAttribute",

            "System.NotSupportedException",

            "System.Runtime.CompilerServices.ICriticalNotifyCompletion",
            "System.Runtime.CompilerServices.IAsyncStateMachine",
            "System.Runtime.CompilerServices.AsyncVoidMethodBuilder",
            "System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
            "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1",
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
            "System.Runtime.CompilerServices.IteratorStateMachineAttribute",

            "System.Windows.Forms.Form",
            "System.Windows.Forms.Application",

            "System.Environment",

            "System.Runtime.GCLatencyMode",

            "System.IFormatProvider",

            "System.ValueTuple`1",
            "System.ValueTuple`2",
            "System.ValueTuple`3",
            "System.ValueTuple`4",
            "System.ValueTuple`5",

            "", // extension marker

            "System.ValueTuple`6",
            "System.ValueTuple`7",
            "System.ValueTuple`8",

            "System.Runtime.CompilerServices.TupleElementNamesAttribute",

            "Microsoft.CodeAnalysis.Runtime.Instrumentation",

            "System.Runtime.CompilerServices.NullableAttribute",
            "System.Runtime.CompilerServices.NullableContextAttribute",
            "System.Runtime.CompilerServices.NullablePublicOnlyAttribute",
            "System.Runtime.CompilerServices.ReferenceAssemblyAttribute",

            "System.Runtime.CompilerServices.IsReadOnlyAttribute",
            "System.Runtime.CompilerServices.IsByRefLikeAttribute",
            "System.Runtime.InteropServices.InAttribute",
            "System.ObsoleteAttribute",
            "System.Span`1",
            "System.ReadOnlySpan`1",
            "System.Runtime.InteropServices.UnmanagedType",
            "System.Runtime.CompilerServices.IsUnmanagedAttribute",

            "Microsoft.VisualBasic.Conversion",
            "System.Runtime.CompilerServices.NonNullTypesAttribute",
            "System.AttributeTargets",
            "Microsoft.CodeAnalysis.EmbeddedAttribute",
            "System.Runtime.CompilerServices.ITuple",

            "System.Index",
            "System.Range",

            "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
            "System.IAsyncDisposable",
            "System.Collections.Generic.IAsyncEnumerable`1",
            "System.Collections.Generic.IAsyncEnumerator`1",
            "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1",
            "System.Threading.Tasks.Sources.ValueTaskSourceStatus",
            "System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags",
            "System.Threading.Tasks.Sources.IValueTaskSource`1",
            "System.Threading.Tasks.Sources.IValueTaskSource",
            "System.Threading.Tasks.ValueTask`1",
            "System.Threading.Tasks.ValueTask",
            "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder",
            "System.Threading.CancellationToken",
            "System.Threading.CancellationTokenSource",

            "System.InvalidOperationException",
            "System.Runtime.CompilerServices.SwitchExpressionException",
            "System.Collections.Generic.IEqualityComparer`1",
        };

        private readonly static Dictionary<string, WellKnownType> s_nameToTypeIdMap = new Dictionary<string, WellKnownType>((int)Count);

        static WellKnownTypes()
        {
            AssertEnumAndTableInSync();

            for (int i = 0; i < s_metadataNames.Length; i++)
            {
                var name = s_metadataNames[i];
                var typeId = (WellKnownType)(i + WellKnownType.First);
                s_nameToTypeIdMap.Add(name, typeId);
            }
        }

        [Conditional("DEBUG")]
        private static void AssertEnumAndTableInSync()
        {
            for (int i = 0; i < s_metadataNames.Length; i++)
            {
                var name = s_metadataNames[i];
                var typeId = (WellKnownType)(i + WellKnownType.First);

                string typeIdName;
                switch (typeId)
                {
                    case WellKnownType.First:
                        typeIdName = "System.Math";
                        break;
                    case WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl:
                        typeIdName = "Microsoft.VisualBasic.CompilerServices.ObjectFlowControl+ForLoopControl";
                        break;
                    case WellKnownType.CSharp7Sentinel:
                        typeIdName = "System.IFormatProvider";
                        break;
                    case WellKnownType.ExtSentinel:
                        typeIdName = "";
                        break;
                    default:
                        typeIdName = typeId.ToString().Replace("__", "+").Replace('_', '.');
                        break;
                }

                int separator = name.IndexOf('`');
                if (separator >= 0)
                {
                    // Ignore type parameter qualifier for generic types.
                    name = name.Substring(0, separator);
                    typeIdName = typeIdName.Substring(0, separator);
                }

                Debug.Assert(name == typeIdName, "Enum name and type name must match");
            }

            Debug.Assert((int)WellKnownType.ExtSentinel == 255);
            Debug.Assert((int)WellKnownType.NextAvailable <= 512, "Time for a new sentinel");
        }

        public static bool IsWellKnownType(this WellKnownType typeId)
        {
            Debug.Assert(typeId != WellKnownType.ExtSentinel);
            return typeId >= WellKnownType.First && typeId < WellKnownType.NextAvailable;
        }

        public static bool IsValueTupleType(this WellKnownType typeId)
        {
            Debug.Assert(typeId != WellKnownType.ExtSentinel);
            return typeId >= WellKnownType.System_ValueTuple_T1 && typeId <= WellKnownType.System_ValueTuple_TRest;
        }

        public static bool IsValid(this WellKnownType typeId)
        {
            return typeId >= WellKnownType.First && typeId < WellKnownType.NextAvailable && typeId != WellKnownType.ExtSentinel;
        }

        public static string GetMetadataName(this WellKnownType id)
        {
            return s_metadataNames[(int)(id - WellKnownType.First)];
        }

        public static WellKnownType GetTypeFromMetadataName(string metadataName)
        {
            WellKnownType id;

            if (s_nameToTypeIdMap.TryGetValue(metadataName, out id))
            {
                return id;
            }

            Debug.Assert(WellKnownType.First != 0);
            return WellKnownType.Unknown;
        }

        // returns WellKnownType.Unknown if given arity isn't available:
        internal static WellKnownType GetWellKnownFunctionDelegate(int invokeArgumentCount)
        {
            Debug.Assert(invokeArgumentCount >= 0);
            return (invokeArgumentCount <= WellKnownType.System_Func_TMax - WellKnownType.System_Func_T) ?
                (WellKnownType)((int)WellKnownType.System_Func_T + invokeArgumentCount) :
                WellKnownType.Unknown;
        }

        // returns WellKnownType.Unknown if given arity isn't available:
        internal static WellKnownType GetWellKnownActionDelegate(int invokeArgumentCount)
        {
            Debug.Assert(invokeArgumentCount >= 0);

            return (invokeArgumentCount <= WellKnownType.System_Action_TMax - WellKnownType.System_Action) ?
                (WellKnownType)((int)WellKnownType.System_Action + invokeArgumentCount) :
                WellKnownType.Unknown;
        }
    }
}
