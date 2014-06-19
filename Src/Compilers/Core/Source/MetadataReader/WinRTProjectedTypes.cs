using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.MetadataReader
{
    internal enum TypeDefTreatment : byte
    {
        TreatmentMask = 0x0f,
        None = 0x01,
        NormalNonAttribute = 0x02,
        NormalAttribute = 0x03,
        UnmangleWinRTName = 0x04,
        PrefixWinRTName = 0x05,
        RedirectedToCLRType = 0x06,
        RedirectedToCLRAttribute = 0x07,

        MarkAbstractFlag = 0x10,
        MarkInternalFlag = 0x20
    }

    internal enum MethodDefTreatment : byte
    {
        NotYetInitialized = 0x00,
        TreatmentMask = 0x0f,
        Other = 0x01,
        Delegate = 0x02,
        Attribute = 0x03,
        Interface = 0x04,
        Implementation = 0x05,
        HiddenImpl = 0x06,
        UnusedFlag = 0x07,
        RenameToDisposeMethod = 0x08,

        MarkAbstractFlag = 0x10,
        MarkPublicFlag = 0x20,
        MarkSpecialName = 0x40
    }

    internal enum TypeRefTreatment : byte
    {
        None = 0,
        SystemDelegate = 1,
        SystemAttribute = 2
    }

    /// <summary>
    /// Stores a lookup table for type projections for types imported from WinMD
    /// files. For the most part, this table performs the projections supplied by
    /// the CLR's WinMD metadata adapter.
    /// </summary>
    internal sealed class WinRTProjectedTypes
    {
        private static Dictionary<string, ProjectionInfo> lazyTable = null;

        private struct ProjectionInfo
        {
            public readonly string WinRTNamespace;
            public readonly string DotNetNamespace;
            public readonly string DotNetName;
            public readonly uint DotNetAssemblyOffset;
            public readonly TypeDefTreatment Treatment;
            public readonly bool IsIDisposable;

            public ProjectionInfo(
                string winRtNamespace,
                string dotNetNamespace,
                string dotNetName,
                uint dotNetAssemblyOffset,
                TypeDefTreatment treatment = TypeDefTreatment.RedirectedToCLRType,
                bool isIDisposable = false)
            {
                this.WinRTNamespace = winRtNamespace;
                this.DotNetNamespace = dotNetNamespace;
                this.DotNetName = dotNetName;
                this.DotNetAssemblyOffset = dotNetAssemblyOffset;
                this.Treatment = treatment;
                this.IsIDisposable = isIDisposable;
            }
        }

        // Enumerates all of the types that need to be identified if in a .winmd file,
        // these must have some changes applied to them.
        public enum WinMdSpecialType
        {
            None = 0,
            Delegate = 1,
            Attribute = 2,
            Enum = 3
        }

        // Stores the ordering of the extra assemblies required for Windows Runtime types
        public static class WinMdAssemblyOffsets
        {
            public const uint SystemRuntimeWindowsRuntime = 0;
            public const uint SystemRuntime = 1;
            public const uint SystemObject = 2;
            public const uint SystemRuntimeWindowsUiXaml = 3;
            public const uint SystemRuntimeInterop = 4;
        }

        internal static TypeDefTreatment GetTypeDefinitionTreatment(string name, string namespaceName)
        {
            ProjectionInfo info;
            if (!GetLookupTable().TryGetValue(name, out info)) 
            {
                return TypeDefTreatment.None;
            }

            if (namespaceName == info.DotNetNamespace)
            {
                return info.Treatment;
            }

            // TODO (tomat): we can save this comparison if info.DotNetNamespace == info.WinRtNamespace
            if (namespaceName == info.WinRTNamespace)
            {
                return info.Treatment | TypeDefTreatment.MarkInternalFlag;
            }

            return TypeDefTreatment.None;
        }

        internal static bool ResolveWinRTTypeReference(string name, string namespaceName, out string clrName, out string clrNamespace, out uint clrAssemblyOffset)
        {
            ProjectionInfo info;
            if (GetLookupTable().TryGetValue(name, out info) && info.WinRTNamespace == namespaceName)
            {
                clrNamespace = info.DotNetNamespace;
                clrName = info.DotNetName;
                clrAssemblyOffset = info.DotNetAssemblyOffset;
                return true;
            }
            else
            {
                clrNamespace = null;
                clrName = null;
                clrAssemblyOffset = 0;
                return false;
            }
        }

        internal static bool IsWinRTTypeReference(string name, string namespaceName, out bool isIDisposable)
        {
            ProjectionInfo info;
            if (GetLookupTable().TryGetValue(name, out info) && info.WinRTNamespace == namespaceName)
            {
                isIDisposable = info.IsIDisposable;
                return true;
            }
            else
            {
                isIDisposable = false;
                return false;
            }
        }

        private static Dictionary<string, ProjectionInfo> GetLookupTable()
        {
            if (lazyTable == null)
            {
                var table = new Dictionary<string, ProjectionInfo>(42);
                //        WinRT Name                                               { WinRT Namespace                       .NET Namespace                                   .NET Name                              .NET Assembly Offset }
                table.Add("AttributeUsageAttribute", new ProjectionInfo("Windows.Foundation.Metadata", "System", "AttributeUsageAttribute", WinMdAssemblyOffsets.SystemRuntime, treatment: TypeDefTreatment.RedirectedToCLRAttribute));
                table.Add("AttributeTargets", new ProjectionInfo("Windows.Foundation.Metadata", "System", "AttributeTargets", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("Color", new ProjectionInfo("Windows.UI", "Windows.UI", "Color", WinMdAssemblyOffsets.SystemRuntimeWindowsRuntime));
                table.Add("CornerRadius", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "CornerRadius", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("DateTime", new ProjectionInfo("Windows.Foundation", "System", "DateTimeOffset", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("EventHandler`1", new ProjectionInfo("Windows.Foundation", "System", "EventHandler`1", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("EventRegistrationToken", new ProjectionInfo("Windows.Foundation", "System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken", WinMdAssemblyOffsets.SystemRuntimeInterop));
                table.Add("HResult", new ProjectionInfo("Windows.Foundation", "System", "Exception", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IReference`1", new ProjectionInfo("Windows.Foundation", "System", "Nullable`1", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("Point", new ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Point", WinMdAssemblyOffsets.SystemRuntimeWindowsRuntime));
                table.Add("Rect", new ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Rect", WinMdAssemblyOffsets.SystemRuntimeWindowsRuntime));
                table.Add("Size", new ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Size", WinMdAssemblyOffsets.SystemRuntimeWindowsRuntime));
                table.Add("TimeSpan", new ProjectionInfo("Windows.Foundation", "System", "TimeSpan", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("Uri", new ProjectionInfo("Windows.Foundation", "System", "Uri", WinMdAssemblyOffsets.SystemRuntime));

                table.Add("IClosable", new ProjectionInfo("Windows.Foundation", "System", "IDisposable", WinMdAssemblyOffsets.SystemRuntime, isIDisposable: true));

                table.Add("IIterable`1", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IEnumerable`1", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IVector`1", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IList`1", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IVectorView`1", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IReadOnlyList`1", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IMap`2", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IDictionary`2", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IMapView`2", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IReadOnlyDictionary`2", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IKeyValuePair`2", new ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "KeyValuePair`2", WinMdAssemblyOffsets.SystemRuntime));

                table.Add("ICommand", new ProjectionInfo("Windows.UI.Xaml.Input", "System.Windows.Input", "ICommand", WinMdAssemblyOffsets.SystemObject));

                table.Add("IBindableIterable", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections", "IEnumerable", WinMdAssemblyOffsets.SystemRuntime));
                table.Add("IBindableVector", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections", "IList", WinMdAssemblyOffsets.SystemRuntime));

                table.Add("INotifyCollectionChanged", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "INotifyCollectionChanged", WinMdAssemblyOffsets.SystemObject));
                table.Add("NotifyCollectionChangedEventHandler", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", WinMdAssemblyOffsets.SystemObject));
                table.Add("NotifyCollectionChangedEventArgs", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", WinMdAssemblyOffsets.SystemObject));
                table.Add("NotifyCollectionChangedAction", new ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedAction", WinMdAssemblyOffsets.SystemObject));

                table.Add("INotifyPropertyChanged", new ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "INotifyPropertyChanged", WinMdAssemblyOffsets.SystemObject));
                table.Add("PropertyChangedEventHandler", new ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "PropertyChangedEventHandler", WinMdAssemblyOffsets.SystemObject));
                table.Add("PropertyChangedEventArgs", new ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "PropertyChangedEventArgs", WinMdAssemblyOffsets.SystemObject));

                table.Add("Duration", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "Duration", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("DurationType", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "DurationType", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("GridLength", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "GridLength", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("GridUnitType", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "GridUnitType", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("Thickness", new ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "Thickness", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));

                table.Add("TypeName", new ProjectionInfo("Windows.UI.Xaml.Interop", "System", "Type", WinMdAssemblyOffsets.SystemRuntime));

                table.Add("GeneratorPosition", new ProjectionInfo("Windows.UI.Xaml.Controls.Primitives", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));

                table.Add("Matrix", new ProjectionInfo("Windows.UI.Xaml.Media", "Windows.UI.Xaml.Media", "Matrix", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));

                table.Add("KeyTime", new ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "KeyTime", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("RepeatBehavior", new ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));
                table.Add("RepeatBehaviorType", new ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));

                table.Add("Matrix3D", new ProjectionInfo("Windows.UI.Xaml.Media.Media3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", WinMdAssemblyOffsets.SystemRuntimeWindowsUiXaml));

                lazyTable = table;
            }

            return lazyTable;
        }

    }
}
