// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator(LanguageNames.CSharp)]
    internal class TestServicesSourceGenerator : IIncrementalGenerator
    {
        private const string SourceSuffix = ".g.cs";

        private const string IVsTextManagerExtensionsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextManagerExtensions
    {
        public static Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
            => textManager.GetActiveViewAsync(joinableTaskFactory, mustHaveFocus: true, buffer: null, cancellationToken);

        public static async Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, bool mustHaveFocus, IVsTextBuffer? buffer, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(fMustHaveFocus: mustHaveFocus ? 1 : 0, pBuffer: buffer, ppView: out var vsTextView));

            return vsTextView;
        }
    }
}
";

        private const string IVsTextViewExtensionsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextViewExtensions
    {
        public static async Task<IWpfTextViewHost> GetTextViewHostAsync(this IVsTextView textView, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(((IVsUserData)textView).GetData(DefGuidList.guidIWpfTextViewHost, out var wpfTextViewHost));
            return (IWpfTextViewHost)wpfTextViewHost;
        }
    }
}
";

        private const string EditorInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;

    [TestService]
    internal partial class EditorInProcess
    {
        public async Task<IWpfTextView> GetActiveTextViewAsync(CancellationToken cancellationToken)
            => (await GetActiveTextViewHostAsync(cancellationToken)).TextView;

        private async Task<IWpfTextViewHost> GetActiveTextViewHostAsync(CancellationToken cancellationToken)
        {
            var activeVsTextView = await GetActiveVsTextViewAsync(cancellationToken);
            return await activeVsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
        }

        private async Task<IVsTextView> GetActiveVsTextViewAsync(CancellationToken cancellationToken)
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text
            // view.
            await WaitForApplicationIdleAsync(cancellationToken);

            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            return await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
        }
    }
}
";

        private const string ShellInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
    using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;
    using Task = System.Threading.Tasks.Task;

    [TestService]
    internal partial class ShellInProcess
    {
        public new Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {
            return base.GetRequiredGlobalServiceAsync<TService, TInterface>(cancellationToken);
        }

        public new Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {
            return base.GetComponentModelServiceAsync<TService>(cancellationToken);
        }

        public async Task<CommandID> PrepareCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandWindow = await GetRequiredGlobalServiceAsync<SVsCommandWindow, IVsCommandWindow>(cancellationToken);
            var result = new PREPARECOMMANDRESULT[1];
            ErrorHandler.ThrowOnFailure(commandWindow.PrepareCommand(command, out var commandGroup, out var commandId, out var cmdArg, result));

            Marshal.FreeCoTaskMem(cmdArg);

            return new CommandID(commandGroup, (int)commandId);
        }

        public async Task ExecuteCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandID = await PrepareCommandAsync(command, cancellationToken);
            await ExecuteCommandAsync(commandID, cancellationToken);
        }

        public Task ExecuteCommandAsync(CommandID command, CancellationToken cancellationToken)
            => ExecuteCommandAsync(command.Guid, (uint)command.ID, cancellationToken);

        public Task ExecuteCommandAsync(CommandID command, string argument, CancellationToken cancellationToken)
            => ExecuteCommandAsync(command.Guid, (uint)command.ID, argument, cancellationToken);

        public async Task ExecuteCommandAsync(Guid commandGuid, uint commandId, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
            ErrorHandler.ThrowOnFailure(dispatcher.Exec(commandGuid, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero));
        }

        public async Task ExecuteCommandAsync(Guid commandGuid, uint commandId, string argument, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);

            var pvaIn = Marshal.AllocHGlobal(Marshal.SizeOf<VariantHelper>());
            try
            {
                Marshal.GetNativeVariantForObject(argument, pvaIn);
                ErrorHandler.ThrowOnFailure(dispatcher.Exec(commandGuid, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, pvaIn, IntPtr.Zero));
            }
            finally
            {
                var variant = Marshal.PtrToStructure<VariantHelper>(pvaIn);
                variant.Clear();
                Marshal.FreeHGlobal(pvaIn);
            }
        }

        public async Task<string> GetActiveWindowCaptionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
            ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
            var windowFrame = (IVsWindowFrame)windowFrameObj;

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
            return $""{captionObj}"";
        }

        public async Task<Version> GetVersionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

            var fullVersion = versionProperty?.ToString() ?? string.Empty;
            var firstSpace = fullVersion.IndexOf(' ');
            if (firstSpace >= 0)
            {
                // e.g. ""17.1.31907.60 MAIN""
                fullVersion = fullVersion.Substring(0, firstSpace);
            }

            if (Version.TryParse(fullVersion, out var version))
            {
                return version;
            }

            throw new NotSupportedException($""Unexpected version format: {versionProperty}"");
        }
    }
}
";

        private const string ShellInProcessVariantHelperSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal partial class ShellInProcess
    {
        // Derived from System.Runtime.InteropServices.Variant
        // https://github.com/dotnet/runtime/blob/14c3a15145ef465f4f3a2e14270c930142249454/src/libraries/Common/src/System/Runtime/InteropServices/Variant.cs
        [StructLayout(LayoutKind.Explicit)]
        private partial struct VariantHelper
        {
            // Most of the data types in the VariantHelper are carried in _typeUnion
            [FieldOffset(0)] private TypeUnion _typeUnion;

            // Decimal is the largest data type and it needs to use the space that is normally unused in TypeUnion._wReserved1, etc.
            // Hence, it is declared to completely overlap with TypeUnion. A Decimal does not use the first two bytes, and so
            // TypeUnion._vt can still be used to encode the type.
            [FieldOffset(0)] private decimal _decimal;

            [StructLayout(LayoutKind.Sequential)]
            private struct TypeUnion
            {
                public ushort _vt;
                public ushort _wReserved1;
                public ushort _wReserved2;
                public ushort _wReserved3;

                public UnionTypes _unionTypes;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct Record
            {
                public IntPtr _record;
                public IntPtr _recordInfo;
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct UnionTypes
            {
                [FieldOffset(0)] public sbyte _i1;
                [FieldOffset(0)] public short _i2;
                [FieldOffset(0)] public int _i4;
                [FieldOffset(0)] public long _i8;
                [FieldOffset(0)] public byte _ui1;
                [FieldOffset(0)] public ushort _ui2;
                [FieldOffset(0)] public uint _ui4;
                [FieldOffset(0)] public ulong _ui8;
                [FieldOffset(0)] public int _int;
                [FieldOffset(0)] public uint _uint;
                [FieldOffset(0)] public short _bool;
                [FieldOffset(0)] public int _error;
                [FieldOffset(0)] public float _r4;
                [FieldOffset(0)] public double _r8;
                [FieldOffset(0)] public long _cy;
                [FieldOffset(0)] public double _date;
                [FieldOffset(0)] public IntPtr _bstr;
                [FieldOffset(0)] public IntPtr _unknown;
                [FieldOffset(0)] public IntPtr _dispatch;
                [FieldOffset(0)] public IntPtr _pvarVal;
                [FieldOffset(0)] public IntPtr _byref;
                [FieldOffset(0)] public Record _record;
            }

            /// <summary>
            /// Primitive types are the basic COM types. It includes valuetypes like ints, but also reference types
            /// like BStrs. It does not include composite types like arrays and user-defined COM types (IUnknown/IDispatch).
            /// </summary>
            public static bool IsPrimitiveType(VarEnum varEnum)
            {
                switch (varEnum)
                {
                    case VarEnum.VT_I1:
                    case VarEnum.VT_I2:
                    case VarEnum.VT_I4:
                    case VarEnum.VT_I8:
                    case VarEnum.VT_UI1:
                    case VarEnum.VT_UI2:
                    case VarEnum.VT_UI4:
                    case VarEnum.VT_UI8:
                    case VarEnum.VT_INT:
                    case VarEnum.VT_UINT:
                    case VarEnum.VT_BOOL:
                    case VarEnum.VT_ERROR:
                    case VarEnum.VT_R4:
                    case VarEnum.VT_R8:
                    case VarEnum.VT_DECIMAL:
                    case VarEnum.VT_CY:
                    case VarEnum.VT_DATE:
                    case VarEnum.VT_BSTR:
                        return true;
                }

                return false;
            }

            public void CopyFromIndirect(object value)
            {
                VarEnum vt = (VarEnum)(((int)this.VariantType) & ~((int)VarEnum.VT_BYREF));

                if (value == null)
                {
                    if (vt == VarEnum.VT_DISPATCH || vt == VarEnum.VT_UNKNOWN || vt == VarEnum.VT_BSTR)
                    {
                        Marshal.WriteIntPtr(this._typeUnion._unionTypes._byref, IntPtr.Zero);
                    }
                    return;
                }

                if ((vt & VarEnum.VT_ARRAY) != 0)
                {
                    IntPtr vArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VariantHelper)));
                    try
                    {
                        Marshal.GetNativeVariantForObject(value, vArray);
                        Marshal.WriteIntPtr(this._typeUnion._unionTypes._byref, Marshal.PtrToStructure<VariantHelper>(vArray)._typeUnion._unionTypes._byref);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(vArray);
                    }

                    return;
                }

                switch (vt)
                {
                    case VarEnum.VT_I1:
                        Marshal.WriteByte(this._typeUnion._unionTypes._byref, unchecked((byte)(sbyte)value));
                        break;

                    case VarEnum.VT_UI1:
                        Marshal.WriteByte(this._typeUnion._unionTypes._byref, (byte)value);
                        break;

                    case VarEnum.VT_I2:
                        Marshal.WriteInt16(this._typeUnion._unionTypes._byref, (short)value);
                        break;

                    case VarEnum.VT_UI2:
                        Marshal.WriteInt16(this._typeUnion._unionTypes._byref, unchecked((short)(ushort)value));
                        break;

                    case VarEnum.VT_BOOL:
                        // VARIANT_TRUE  = -1
                        // VARIANT_FALSE = 0
                        Marshal.WriteInt16(this._typeUnion._unionTypes._byref, (bool)value ? (short)-1 : (short)0);
                        break;

                    case VarEnum.VT_I4:
                    case VarEnum.VT_INT:
                        Marshal.WriteInt32(this._typeUnion._unionTypes._byref, (int)value);
                        break;

                    case VarEnum.VT_UI4:
                    case VarEnum.VT_UINT:
                        Marshal.WriteInt32(this._typeUnion._unionTypes._byref, unchecked((int)(uint)value));
                        break;

                    case VarEnum.VT_ERROR:
                        Marshal.WriteInt32(this._typeUnion._unionTypes._byref, ((ErrorWrapper)value).ErrorCode);
                        break;

                    case VarEnum.VT_I8:
                        Marshal.WriteInt64(this._typeUnion._unionTypes._byref, (long)value);
                        break;

                    case VarEnum.VT_UI8:
                        Marshal.WriteInt64(this._typeUnion._unionTypes._byref, unchecked((long)(ulong)value));
                        break;

                    case VarEnum.VT_R4:
                        Marshal.StructureToPtr((float)value, this._typeUnion._unionTypes._byref, false);
                        break;

                    case VarEnum.VT_R8:
                        Marshal.WriteInt64(this._typeUnion._unionTypes._byref, BitConverter.DoubleToInt64Bits((double)value));
                        break;

                    case VarEnum.VT_DATE:
                        Marshal.WriteInt64(this._typeUnion._unionTypes._byref, BitConverter.DoubleToInt64Bits(((DateTime)value).ToOADate()));
                        break;

                    case VarEnum.VT_UNKNOWN:
                        Marshal.WriteIntPtr(this._typeUnion._unionTypes._byref, Marshal.GetIUnknownForObject(value));
                        break;

                    case VarEnum.VT_DISPATCH:
                        Marshal.WriteIntPtr(this._typeUnion._unionTypes._byref, Marshal.GetIDispatchForObject(value));
                        break;

                    case VarEnum.VT_BSTR:
                        Marshal.WriteIntPtr(this._typeUnion._unionTypes._byref, Marshal.StringToBSTR((string)value));
                        break;

                    case VarEnum.VT_CY:
                        Marshal.WriteInt64(this._typeUnion._unionTypes._byref, decimal.ToOACurrency((decimal)value));
                        break;

                    case VarEnum.VT_DECIMAL:
                        Marshal.StructureToPtr((decimal)value, this._typeUnion._unionTypes._byref, false);
                        break;

                    case VarEnum.VT_VARIANT:
                        Marshal.GetNativeVariantForObject(value, this._typeUnion._unionTypes._byref);
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            /// <summary>
            /// Get the managed object representing the VariantHelper.
            /// </summary>
            /// <returns></returns>
            public object? ToObject()
            {
                // Check the simple case upfront
                if (IsEmpty)
                {
                    return null;
                }

                switch (VariantType)
                {
                    case VarEnum.VT_NULL:
                        return DBNull.Value;

                    case VarEnum.VT_I1: return AsI1;
                    case VarEnum.VT_I2: return AsI2;
                    case VarEnum.VT_I4: return AsI4;
                    case VarEnum.VT_I8: return AsI8;
                    case VarEnum.VT_UI1: return AsUi1;
                    case VarEnum.VT_UI2: return AsUi2;
                    case VarEnum.VT_UI4: return AsUi4;
                    case VarEnum.VT_UI8: return AsUi8;
                    case VarEnum.VT_INT: return AsInt;
                    case VarEnum.VT_UINT: return AsUint;
                    case VarEnum.VT_BOOL: return AsBool;
                    case VarEnum.VT_ERROR: return AsError;
                    case VarEnum.VT_R4: return AsR4;
                    case VarEnum.VT_R8: return AsR8;
                    case VarEnum.VT_DECIMAL: return AsDecimal;
                    case VarEnum.VT_CY: return AsCy;
                    case VarEnum.VT_DATE: return AsDate;
                    case VarEnum.VT_BSTR: return AsBstr;
                    case VarEnum.VT_UNKNOWN: return AsUnknown;
                    case VarEnum.VT_DISPATCH: return AsDispatch;

                    default:
                        var unmanagedCopy = Marshal.AllocHGlobal(Marshal.SizeOf(GetType()));
                        try
                        {
                            Marshal.StructureToPtr(this, unmanagedCopy, false);
                            return Marshal.GetObjectForNativeVariant(unmanagedCopy);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(unmanagedCopy);
                        }
                }
            }

            /// <summary>
            /// Release any unmanaged memory associated with the VariantHelper
            /// </summary>
            public void Clear()
            {
                // We do not need to call OLE32's VariantClear for primitive types or ByRefs
                // to save ourselves the cost of interop transition.
                // ByRef indicates the memory is not owned by the VARIANT itself while
                // primitive types do not have any resources to free up.
                // Hence, only safearrays, BSTRs, interfaces and user types are
                // handled differently.
                VarEnum vt = VariantType;
                if ((vt & VarEnum.VT_BYREF) != 0)
                {
                    VariantType = VarEnum.VT_EMPTY;
                }
                else if (((vt & VarEnum.VT_ARRAY) != 0)
                        || (vt == VarEnum.VT_BSTR)
                        || (vt == VarEnum.VT_UNKNOWN)
                        || (vt == VarEnum.VT_DISPATCH)
                        || (vt == VarEnum.VT_VARIANT)
                        || (vt == VarEnum.VT_RECORD))
                {
                    var unmanagedCopy = Marshal.AllocHGlobal(Marshal.SizeOf(GetType()));
                    try
                    {
                        Marshal.StructureToPtr(this, unmanagedCopy, false);
                        VariantClear(unmanagedCopy);
                        this = Marshal.PtrToStructure<VariantHelper>(unmanagedCopy);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(unmanagedCopy);
                    }

                    Debug.Assert(IsEmpty);
                }
                else
                {
                    VariantType = VarEnum.VT_EMPTY;
                }
            }

            public VarEnum VariantType
            {
                get => (VarEnum)_typeUnion._vt;
                set => _typeUnion._vt = (ushort)value;
            }

            public bool IsEmpty => _typeUnion._vt == ((ushort)VarEnum.VT_EMPTY);

            public bool IsByRef => (_typeUnion._vt & ((ushort)VarEnum.VT_BYREF)) != 0;

            public void SetAsNULL()
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_NULL;
            }

            // VT_I1

            public sbyte AsI1
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_I1);
                    return _typeUnion._unionTypes._i1;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_I1;
                    _typeUnion._unionTypes._i1 = value;
                }
            }

            // VT_I2

            public short AsI2
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_I2);
                    return _typeUnion._unionTypes._i2;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_I2;
                    _typeUnion._unionTypes._i2 = value;
                }
            }

            // VT_I4

            public int AsI4
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_I4);
                    return _typeUnion._unionTypes._i4;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_I4;
                    _typeUnion._unionTypes._i4 = value;
                }
            }

            // VT_I8

            public long AsI8
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_I8);
                    return _typeUnion._unionTypes._i8;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_I8;
                    _typeUnion._unionTypes._i8 = value;
                }
            }

            // VT_UI1

            public byte AsUi1
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UI1);
                    return _typeUnion._unionTypes._ui1;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UI1;
                    _typeUnion._unionTypes._ui1 = value;
                }
            }

            // VT_UI2

            public ushort AsUi2
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UI2);
                    return _typeUnion._unionTypes._ui2;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UI2;
                    _typeUnion._unionTypes._ui2 = value;
                }
            }

            // VT_UI4

            public uint AsUi4
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UI4);
                    return _typeUnion._unionTypes._ui4;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UI4;
                    _typeUnion._unionTypes._ui4 = value;
                }
            }

            // VT_UI8

            public ulong AsUi8
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UI8);
                    return _typeUnion._unionTypes._ui8;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UI8;
                    _typeUnion._unionTypes._ui8 = value;
                }
            }

            // VT_INT

            public int AsInt
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_INT);
                    return _typeUnion._unionTypes._int;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_INT;
                    _typeUnion._unionTypes._int = value;
                }
            }

            // VT_UINT

            public uint AsUint
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UINT);
                    return _typeUnion._unionTypes._uint;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UINT;
                    _typeUnion._unionTypes._uint = value;
                }
            }

            // VT_BOOL

            public bool AsBool
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_BOOL);
                    return _typeUnion._unionTypes._bool != 0;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    // VARIANT_TRUE  = -1
                    // VARIANT_FALSE = 0
                    VariantType = VarEnum.VT_BOOL;
                    _typeUnion._unionTypes._bool = value ? (short)-1 : (short)0;
                }
            }

            // VT_ERROR

            public int AsError
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_ERROR);
                    return _typeUnion._unionTypes._error;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_ERROR;
                    _typeUnion._unionTypes._error = value;
                }
            }

            // VT_R4

            public float AsR4
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_R4);
                    return _typeUnion._unionTypes._r4;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_R4;
                    _typeUnion._unionTypes._r4 = value;
                }
            }

            // VT_R8

            public double AsR8
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_R8);
                    return _typeUnion._unionTypes._r8;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_R8;
                    _typeUnion._unionTypes._r8 = value;
                }
            }

            // VT_DECIMAL

            public decimal AsDecimal
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_DECIMAL);
                    // The first byte of Decimal is unused, but usually set to 0
                    VariantHelper v = this;
                    v._typeUnion._vt = 0;
                    return v._decimal;
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_DECIMAL;
                    _decimal = value;
                    // _vt overlaps with _decimal, and should be set after setting _decimal
                    _typeUnion._vt = (ushort)VarEnum.VT_DECIMAL;
                }
            }

            // VT_CY

            public decimal AsCy
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_CY);
                    return decimal.FromOACurrency(_typeUnion._unionTypes._cy);
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_CY;
                    _typeUnion._unionTypes._cy = decimal.ToOACurrency(value);
                }
            }

            // VT_DATE

            public DateTime AsDate
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_DATE);
                    return DateTime.FromOADate(_typeUnion._unionTypes._date);
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_DATE;
                    _typeUnion._unionTypes._date = value.ToOADate();
                }
            }

            // VT_BSTR

            public string? AsBstr
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_BSTR);
                    if (_typeUnion._unionTypes._bstr == IntPtr.Zero)
                    {
                        return null;
                    }
                    return (string)Marshal.PtrToStringBSTR(this._typeUnion._unionTypes._bstr);
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_BSTR;
                    this._typeUnion._unionTypes._bstr = Marshal.StringToBSTR(value);
                }
            }

            // VT_UNKNOWN

            public object? AsUnknown
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_UNKNOWN);
                    if (_typeUnion._unionTypes._unknown == IntPtr.Zero)
                    {
                        return null;
                    }
                    return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._unknown);
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_UNKNOWN;
                    if (value == null)
                    {
                        _typeUnion._unionTypes._unknown = IntPtr.Zero;
                    }
                    else
                    {
                        _typeUnion._unionTypes._unknown = Marshal.GetIUnknownForObject(value);
                    }
                }
            }

            // VT_DISPATCH

            public object? AsDispatch
            {
                get
                {
                    Debug.Assert(VariantType == VarEnum.VT_DISPATCH);
                    if (_typeUnion._unionTypes._dispatch == IntPtr.Zero)
                    {
                        return null;
                    }
                    return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._dispatch);
                }
                set
                {
                    Debug.Assert(IsEmpty);
                    VariantType = VarEnum.VT_DISPATCH;
                    if (value == null)
                    {
                        _typeUnion._unionTypes._dispatch = IntPtr.Zero;
                    }
                    else
                    {
                        _typeUnion._unionTypes._dispatch = Marshal.GetIDispatchForObject(value);
                    }
                }
            }

            public IntPtr AsByRefVariant
            {
                get
                {
                    Debug.Assert(VariantType == (VarEnum.VT_BYREF | VarEnum.VT_VARIANT));
                    return _typeUnion._unionTypes._pvarVal;
                }
            }

            [DllImport(""oleaut32.dll"")]
            private static extern void VariantClear(IntPtr variant);
        }
    }
}
";

        private const string SolutionExplorerInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    [TestService]
    internal partial class SolutionExplorerInProcess
    {
        public async Task<bool> IsSolutionOpenAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        /// <summary>
        /// Close the currently open solution without saving.
        /// </summary>
        public async Task CloseSolutionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            if (!await IsSolutionOpenAsync(cancellationToken))
            {
                return;
            }

            using SemaphoreSlim semaphore = new SemaphoreSlim(1);
            await RunWithSolutionEventsAsync(
                async solutionEvents =>
                {
                    await semaphore.WaitAsync(cancellationToken);

                    void HandleAfterCloseSolution(object sender, EventArgs e)
                        => semaphore.Release();

                    solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
                    try
                    {
                        ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                        await semaphore.WaitAsync(cancellationToken);
                    }
                    finally
                    {
                        solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
                    }
                },
                cancellationToken);
        }

        private sealed partial class SolutionEvents : IVsSolutionEvents
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(JoinableTaskFactory joinableTaskFactory, IVsSolution solution)
            {
                Application.Current.Dispatcher.VerifyAccess();

                _joinableTaskFactory = joinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler? AfterCloseSolution;

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                AfterCloseSolution?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }
    }
}
";

        private const string WorkspaceInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    [TestService]
    internal partial class WorkspaceInProcess
    {
    }
}
";

        private const string SolutionExplorerInProcessSolutionEventsDisposeSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;

    internal partial class SolutionExplorerInProcess
    {
        private async Task RunWithSolutionEventsAsync(Func<SolutionEvents, Task> actionAsync, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);
            await actionAsync(solutionEvents);
        }

        private sealed partial class SolutionEvents : IDisposable
        {
            public void Dispose()
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                    ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
                });
            }
        }
    }
}
";

        private const string SolutionExplorerInProcessSolutionEventsDisposeAsyncSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncDisposable = System.IAsyncDisposable;

    internal partial class SolutionExplorerInProcess
    {
        private async Task RunWithSolutionEventsAsync(Func<SolutionEvents, Task> actionAsync, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            await using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);
            await actionAsync(solutionEvents);
        }

        private sealed partial class SolutionEvents : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
            }
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using Microsoft.VisualStudio.OperationProgress;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public async Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
        {
            var operationProgressStatus = await GetRequiredGlobalServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(cancellationToken);
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemPartialSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException(""Visual Studio 2019 version 16.0 includes SVsOperationProgress, but does not include IVsOperationProgressStatusService. Update Microsoft.VisualStudio.Shell.Framework to 16.1 or newer to support waiting for project system."");
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemLegacySource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
";

        private const string TestServiceAttributeSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class TestServiceAttribute : Attribute
    {
    }
}
";

        private const string ErrorHandlerSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio
{
    using System.Runtime.InteropServices;

    internal static class ErrorHandler
    {
        public static bool Succeeded(int hr)
            => hr >= 0;

        public static bool Failed(int hr)
            => hr < 0;

        public static int ThrowOnFailure(int hr)
        {
            if (Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return hr;
        }
    }
}
";

        private const string VSConstantsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio
{
    using System;

    internal static partial class VSConstants
    {
        // General HRESULTS

        /// <summary>HRESULT for FALSE (not an error).</summary>
        public const int S_FALSE = 0x00000001;
        /// <summary>HRESULT for generic success.</summary>
        public const int S_OK = 0x00000000;

        /// <summary>
        /// These element IDs are the only element IDs that can be used with the selection service.
        /// </summary>
        public enum VSSELELEMID
        {
            SEID_UndoManager = 0,
            SEID_WindowFrame = 1,
            SEID_DocumentFrame = 2,
            SEID_StartupProject = 3,
            SEID_PropertyBrowserSID = 4,
            SEID_UserContext = 5,
            SEID_ResultList = 6,
            SEID_LastWindowFrame = 7,
        }
    }
}
";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(context =>
            {
                context.AddSource($"IVsTextViewExtensions{SourceSuffix}", IVsTextViewExtensionsSource);
                context.AddSource($"IVsTextManagerExtensions{SourceSuffix}", IVsTextManagerExtensionsSource);
                context.AddSource($"EditorInProcess1{SourceSuffix}", EditorInProcessSource);
                context.AddSource($"SolutionExplorerInProcess1{SourceSuffix}", SolutionExplorerInProcessSource);
                context.AddSource($"ShellInProcess1{SourceSuffix}", ShellInProcessSource);
                context.AddSource($"ShellInProcess.VariantHelper{SourceSuffix}", ShellInProcessVariantHelperSource);
                context.AddSource($"WorkspaceInProcess1{SourceSuffix}", WorkspaceInProcessSource);
                context.AddSource($"TestServiceAttribute{SourceSuffix}", TestServiceAttributeSource);
            });

            var services = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, cancellationToken) =>
                {
                    if (node is AttributeSyntax attribute)
                    {
                        var unqualifiedName = GetUnqualifiedName(attribute.Name).Identifier.ValueText;
                        return unqualifiedName is "TestService" or "TestServiceAttribute";
                    }

                    return false;
                },
                transform: static (context, cancellationToken) =>
                {
                    var attribute = (AttributeSyntax)context.Node;

                    Accessibility accessibility;
                    string? serviceName;
                    string? baseTypeName;
                    string? implementingTypeName;
                    var target = attribute.Parent?.Parent;
                    if (target is ClassDeclarationSyntax classDeclarationSyntax
                        && context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken) is { } namedType)
                    {
                        accessibility = namedType.DeclaredAccessibility;
                        baseTypeName = namedType.BaseType is null or { SpecialType: SpecialType.System_Object }
                            ? null
                            : namedType.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        implementingTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (namedType.Name.EndsWith("InProcess"))
                        {
                            serviceName = namedType.Name.Substring(0, namedType.Name.Length - "InProcess".Length);
                        }
                        else
                        {
                            serviceName = namedType.Name;
                        }
                    }
                    else
                    {
                        accessibility = Accessibility.NotApplicable;
                        implementingTypeName = null;
                        baseTypeName = null;
                        serviceName = null;
                    }

                    if (serviceName is null || implementingTypeName is null)
                    {
                        return null;
                    }

                    return new ServiceDataModel(accessibility, serviceName, baseTypeName, implementingTypeName);
                });

            var referenceDataModel = context.CompilationProvider.Select(
                static (compilation, cancellationToken) =>
                {
                    var hasSAsyncServiceProvider = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider") is not null;
                    var hasThreadHelperJoinableTaskContext = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Shell.ThreadHelper") is { } threadHelper
                        && threadHelper.GetMembers("JoinableTaskContext").Any(member => member.Kind == SymbolKind.Property);
                    var canCancelJoinTillEmptyAsync = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Threading.JoinableTaskCollection") is { } joinableTaskCollection
                        && joinableTaskCollection.GetMembers("JoinTillEmptyAsync").Any(member => member is IMethodSymbol { Parameters.Length: 1 });
                    var hasJoinableTaskFactoryWithPriority = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Threading.DispatcherExtensions") is not null;
                    var hasAsyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1") is not null;
                    var hasErrorHandler = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.ErrorHandler") is not null;
                    var hasOperationProgress = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.OperationProgress.SVsOperationProgress") is not null;
                    var hasOperationProgressStatusService = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.OperationProgress.IVsOperationProgressStatusService") is not null;
                    var hasEditorConstants = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Editor.EditorConstants") is not null;

                    var editorConstantsCommandIDMissingGuid = true;
                    if (hasEditorConstants)
                    {
                        if (compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Editor.EditorConstants+EditorCommandID") is { } editorConstantsEditorCommandID
                            && editorConstantsEditorCommandID.GetAttributes().Any(attribute => attribute.AttributeClass.Name == nameof(GuidAttribute)))
                        {
                            editorConstantsCommandIDMissingGuid = false;
                        }
                    }

                    return new ReferenceDataModel(
                        hasSAsyncServiceProvider,
                        hasThreadHelperJoinableTaskContext,
                        canCancelJoinTillEmptyAsync,
                        hasJoinableTaskFactoryWithPriority,
                        hasAsyncEnumerable,
                        hasErrorHandler,
                        hasOperationProgress,
                        hasOperationProgressStatusService,
                        hasEditorConstants,
                        editorConstantsCommandIDMissingGuid);
                });

            context.RegisterSourceOutput(
                referenceDataModel,
                static (context, referenceDataModel) =>
                {
                    var usings = new List<string>
                    {
                        "System",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "System.Windows",
                        "System.Windows.Threading",
                        "global::Xunit",
                    };

                    if (!referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("global::Xunit.Harness");
                    }

                    usings.Add("global::Xunit.Threading");
                    usings.Add("Microsoft.VisualStudio.ComponentModelHost");
                    usings.Add("Microsoft.VisualStudio.Shell");

                    if (referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("Microsoft.VisualStudio.Shell.Interop");
                    }

                    usings.Add("Microsoft.VisualStudio.Threading");

                    string getServiceImpl;
                    if (referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider");
                        usings.Add("Task = System.Threading.Tasks.Task");
                        getServiceImpl = @"            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var serviceProvider = (IAsyncServiceProvider?)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SAsyncServiceProvider)).WithCancellation(cancellationToken);
            Assumes.Present(serviceProvider);

            var @interface = (TInterface?)await serviceProvider!.GetServiceAsync(typeof(TService)).WithCancellation(cancellationToken);
            Assumes.Present(@interface);
            return @interface!;";
                    }
                    else
                    {
                        usings.Add("Task = System.Threading.Tasks.Task");
                        getServiceImpl = @"            await TaskScheduler.Default;

            var @interface = await GetServiceCoreAsync(JoinableTaskFactory, cancellationToken).WithCancellation(cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            return @interface ?? throw new InvalidOperationException();

            static async Task<TInterface?> GetServiceCoreAsync(JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
            {
                await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return (TInterface?)GlobalServiceProvider.ServiceProvider.GetService(typeof(TService));
            }";
                    }

                    var inProcComponentSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{string.Join(Environment.NewLine, usings.Select(u => $"    using {u};"))}

    internal abstract class InProcComponent : IAsyncLifetime
    {{
        protected InProcComponent(TestServices testServices)
        {{
            TestServices = testServices ?? throw new ArgumentNullException(nameof(testServices));
        }}

        public TestServices TestServices {{ get; }}

        protected JoinableTaskFactory JoinableTaskFactory => TestServices.JoinableTaskFactory;

        Task IAsyncLifetime.InitializeAsync()
        {{
            return InitializeCoreAsync();
        }}

        Task IAsyncLifetime.DisposeAsync()
        {{
            return Task.CompletedTask;
        }}

        protected virtual Task InitializeCoreAsync()
        {{
            return Task.CompletedTask;
        }}

        protected async Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {{
{getServiceImpl}
        }}

        protected async Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {{
            var componentModel = await GetRequiredGlobalServiceAsync<SComponentModel, IComponentModel>(cancellationToken);
            return componentModel.GetService<TService>();
        }}

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        /// <param name=""cancellationToken"">The cancellation token that the operation will observe.</param>
        /// <returns>A <see cref=""Task""/> representing the asynchronous operation.</returns>
        internal static async Task WaitForApplicationIdleAsync(CancellationToken cancellationToken)
        {{
            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.ApplicationIdle);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            await Task.Factory.StartNew(
                () => {{ }},
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler);
        }}
    }}
}}
";

                    context.AddSource($"InProcComponent{SourceSuffix}", inProcComponentSource);

                    string shellInProcessExecuteCommandTEnumImpl;
                    if (referenceDataModel.HasEditorConstants && referenceDataModel.EditorConstantsCommandIDMissingGuid)
                    {
                        shellInProcessExecuteCommandTEnumImpl = @"    using System;
    using System.Threading;
    using Microsoft.VisualStudio.Editor;
    using Task = System.Threading.Tasks.Task;

    internal partial class ShellInProcess
    {
        public Task ExecuteCommandAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            var commandGuid = command switch
            {
                EditorConstants.EditorCommandID => EditorConstants.EditorCommandSet,
                _ => typeof(TEnum).GUID,
            };

            return ExecuteCommandAsync(commandGuid, Convert.ToUInt32(command), cancellationToken);
        }

        public Task ExecuteCommandAsync<TEnum>(TEnum command, string argument, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            var commandGuid = command switch
            {
                EditorConstants.EditorCommandID => EditorConstants.EditorCommandSet,
                _ => typeof(TEnum).GUID,
            };

            return ExecuteCommandAsync(commandGuid, Convert.ToUInt32(command), argument, cancellationToken);
        }
    }";
                    }
                    else
                    {
                        shellInProcessExecuteCommandTEnumImpl = @"    using System;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class ShellInProcess
    {
        public Task ExecuteCommandAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            return ExecuteCommandAsync(typeof(TEnum).GUID, Convert.ToUInt32(command), cancellationToken);
        }

        public Task ExecuteCommandAsync<TEnum>(TEnum command, string argument, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            return ExecuteCommandAsync(typeof(TEnum).GUID, Convert.ToUInt32(command), argument, cancellationToken);
        }
    }";
                    }

                    var shellInProcessExecuteCommandTEnumSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{shellInProcessExecuteCommandTEnumImpl}
}}
";
                    context.AddSource($"ShellInProcess_ExecuteCommandAsync_TEnum{SourceSuffix}", shellInProcessExecuteCommandTEnumSource);

                    string shellInProcessEnumerateWindowsImpl;
                    if (referenceDataModel.HasAsyncEnumerable)
                    {
                        shellInProcessEnumerateWindowsImpl = @"    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    internal partial class ShellInProcess
    {
        public async IAsyncEnumerable<IVsWindowFrame> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                {
                    yield break;
                }

                for (var i = 0; i < fetched; i++)
                {
                    yield return frameBuffer[i];
                }
            }
        }
    }";
                    }
                    else
                    {
                        shellInProcessEnumerateWindowsImpl = @"    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    internal partial class ShellInProcess
    {
        public async Task<ReadOnlyCollection<IVsWindowFrame>> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var result = new List<IVsWindowFrame>();
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                {
                    break;
                }

                result.AddRange(frameBuffer.Take((int)fetched));
            }

            return result.AsReadOnly();
        }
    }";
                    }

                    var shellInProcessEnumerateWindowsSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{shellInProcessEnumerateWindowsImpl}
}}
";
                    context.AddSource($"ShellInProcess_EnumerateWindowsAsync{SourceSuffix}", shellInProcessEnumerateWindowsSource);

                    if (referenceDataModel.HasAsyncEnumerable)
                    {
                        context.AddSource($"SolutionExplorerInProcess.SolutionEvents_IAsyncDisposable{SourceSuffix}", SolutionExplorerInProcessSolutionEventsDisposeAsyncSource);
                    }
                    else
                    {
                        context.AddSource($"SolutionExplorerInProcess.SolutionEvents_IDisposable{SourceSuffix}", SolutionExplorerInProcessSolutionEventsDisposeSource);
                    }

                    var usings2 = new List<string>
                    {
                        "System",
                        "System.Diagnostics.CodeAnalysis",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "System.Windows",
                    };

                    if (referenceDataModel.HasJoinableTaskFactoryWithPriority)
                    {
                        usings2.Add("System.Windows.Threading");
                    }

                    usings2.Add("global::Xunit");

                    if (!referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        usings2.Add("global::Xunit.Harness");
                    }

                    usings2.Add("global::Xunit.Sdk");

                    if (referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        usings2.Add("Microsoft.VisualStudio.Shell");
                    }
                    else
                    {
                        usings2.Add("Microsoft.VisualStudio.Shell.Interop");
                    }

                    usings2.Add("Microsoft.VisualStudio.Threading");
                    usings2.Add("Task = System.Threading.Tasks.Task");

                    string joinableTaskContextInitializer;
                    if (referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        joinableTaskContextInitializer = "            JoinableTaskContext = ThreadHelper.JoinableTaskContext;";
                    }
                    else
                    {
                        joinableTaskContextInitializer = @"            if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsTaskSchedulerService)) is IVsTaskSchedulerService2 taskSchedulerService)
            {
                JoinableTaskContext = (JoinableTaskContext)taskSchedulerService.GetAsyncTaskContext();
            }
            else
            {
                JoinableTaskContext = new JoinableTaskContext();
            }";
                    }

                    var joinTillEmpty = referenceDataModel.CanCancelJoinTillEmptyAsync
                        ? "await _joinableTaskCollection.JoinTillEmptyAsync(CleanupCancellationToken);"
                        : "await _joinableTaskCollection.JoinTillEmptyAsync().WithCancellation(CleanupCancellationToken);";

                    var createFactory = referenceDataModel.HasJoinableTaskFactoryWithPriority
                        ? "_joinableTaskFactory = value.CreateFactory(_joinableTaskCollection).WithPriority(Application.Current.Dispatcher, DispatcherPriority.Background);"
                        : "_joinableTaskFactory = value.CreateFactory(_joinableTaskCollection);";

                    var abstractIdeIntegrationTestSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{string.Join(Environment.NewLine, usings2.Select(u => $"    using {u};"))}

    /// <summary>
    /// Provides a base class for Visual Studio integration tests.
    /// </summary>
    /// <remarks>
    /// The following is the xunit execution order:
    ///
    /// <list type=""number"">
    /// <item><description>Instance constructor</description></item>
    /// <item><description><see cref=""IAsyncLifetime.InitializeAsync""/></description></item>
    /// <item><description><see cref=""BeforeAfterTestAttribute.Before""/></description></item>
    /// <item><description>Test method</description></item>
    /// <item><description><see cref=""BeforeAfterTestAttribute.After""/></description></item>
    /// <item><description><see cref=""IAsyncLifetime.DisposeAsync""/></description></item>
    /// <item><description><see cref=""IDisposable.Dispose""/></description></item>
    /// </list>
    /// </remarks>
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {{
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        public static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(4);

        /// <summary>
        /// A timeout used to avoid hangs during test cleanup. This is separate from <see cref=""HangMitigatingTimeout""/>
        /// to provide tests an opportunity to clean up state even if failure occurred due to timeout.
        /// </summary>
        private static readonly TimeSpan CleanupHangMitigatingTimeout = TimeSpan.FromMinutes(2);

        private readonly CancellationTokenSource _hangMitigatingCancellationTokenSource;
        private readonly CancellationTokenSource _cleanupCancellationTokenSource;

        private JoinableTaskContext? _joinableTaskContext;
        private JoinableTaskCollection? _joinableTaskCollection;
        private JoinableTaskFactory? _joinableTaskFactory;

        private TestServices? _testServices;

        /// <summary>
        /// Initializes a new instance of the <see cref=""AbstractIdeIntegrationTest""/> class.
        /// </summary>
        protected AbstractIdeIntegrationTest()
        {{
            Assert.True(Application.Current.Dispatcher.CheckAccess());

{joinableTaskContextInitializer}

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(HangMitigatingTimeout);
            _cleanupCancellationTokenSource = new CancellationTokenSource();
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskContext""/> context for use in integration tests.
        /// </summary>
        [NotNull]
        protected JoinableTaskContext? JoinableTaskContext
        {{
            get
            {{
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }}

            private set
            {{
                if (value == _joinableTaskContext)
                {{
                    return;
                }}

                if (value is null)
                {{
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }}
                else
                {{
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    {createFactory}
                }}
            }}
        }}

        [NotNull]
        private protected TestServices? TestServices
        {{
            get
            {{
                return _testServices ?? throw new InvalidOperationException();
            }}

            private set
            {{
                _testServices = value;
            }}
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskFactory""/> for use in integration tests.
        /// </summary>
        protected JoinableTaskFactory JoinableTaskFactory
            => _joinableTaskFactory ?? throw new InvalidOperationException();

        /// <summary>
        /// Gets a cancellation token for use in integration tests to avoid CI timeouts.
        /// </summary>
        protected CancellationToken HangMitigatingCancellationToken
            => _hangMitigatingCancellationTokenSource.Token;

        /// <remarks>
        /// ⚠️ Note that this token will not be cancelled prior to the call to <see cref=""DisposeAsync""/> (which starts
        /// the cancellation timer). Derived types are not likely to make use of this, so it's marked
        /// <see langword=""private""/>.
        /// </remarks>
        private CancellationToken CleanupCancellationToken
            => _cleanupCancellationTokenSource.Token;

        /// <inheritdoc/>
        public virtual async Task InitializeAsync()
        {{
            TestServices = await CreateTestServicesAsync();
        }}

        /// <summary>
        /// This method implements <see cref=""IAsyncLifetime.DisposeAsync""/>, and is used for releasing resources
        /// created by <see cref=""IAsyncLifetime.InitializeAsync""/>. This method is only called if
        /// <see cref=""InitializeAsync""/> completes successfully.
        /// </summary>
        public virtual async Task DisposeAsync()
        {{
            _cleanupCancellationTokenSource.CancelAfter(CleanupHangMitigatingTimeout);

            await TestServices.SolutionExplorer.CloseSolutionAsync(CleanupCancellationToken);

            if (_joinableTaskCollection is object)
            {{
                {joinTillEmpty}
            }}

            JoinableTaskContext = null;
        }}

        /// <summary>
        /// This method provides the implementation for <see cref=""IDisposable.Dispose""/>.
        /// This method is called via the <see cref=""IDisposable""/> interface if the constructor completes successfully.
        /// The <see cref=""InitializeAsync""/> may or may not have completed successfully.
        /// </summary>
        public virtual void Dispose()
        {{
            _hangMitigatingCancellationTokenSource.Dispose();
            _cleanupCancellationTokenSource.Dispose();
        }}

        private protected virtual async Task<TestServices> CreateTestServicesAsync()
            => await TestServices.CreateAsync(JoinableTaskFactory);
    }}
}}
";
                    context.AddSource($"AbstractIdeIntegrationTest{SourceSuffix}", abstractIdeIntegrationTestSource);

                    if (!referenceDataModel.HasErrorHandler)
                    {
                        context.AddSource($"ErrorHandler{SourceSuffix}", ErrorHandlerSource);
                        context.AddSource($"VSConstants{SourceSuffix}", VSConstantsSource);
                    }

                    if (referenceDataModel.HasOperationProgress)
                    {
                        if (referenceDataModel.HasOperationProgressStatusService)
                        {
                            context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemSource);
                        }
                        else
                        {
                            context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemPartialSource);
                        }
                    }
                    else
                    {
                        context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemLegacySource);
                    }
                });

            context.RegisterSourceOutput(
                services,
                static (context, service) =>
                {
                    if (service is null)
                    {
                        return;
                    }

                    var accessibility = service.Accessibility is Accessibility.Public ? "public" : "internal";
                    var namespaceName = service.ImplementingTypeName.Substring("global::".Length, service.ImplementingTypeName.LastIndexOf('.') - "global::".Length);
                    var typeName = service.ImplementingTypeName.Substring(service.ImplementingTypeName.LastIndexOf('.') + 1);
                    var baseTypeName = service.BaseTypeName ?? "global::Microsoft.VisualStudio.Extensibility.Testing.InProcComponent";
                    var usings = string.Empty;
                    if (namespaceName != "Microsoft.VisualStudio.Extensibility.Testing"
                        && !namespaceName.StartsWith("Microsoft.VisualStudio.Extensibility.Testing."))
                    {
                        usings = @"
    using Microsoft.VisualStudio.Extensibility.Testing;

";
                    }

                    var partialService = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace {namespaceName}
{{{usings}
    {accessibility} partial class {typeName} : {baseTypeName}
    {{
        public {typeName}(TestServices testServices)
            : base(testServices)
        {{
        }}
    }}
}}
";

                    context.AddSource($"{typeName}{SourceSuffix}", partialService);
                });

            context.RegisterSourceOutput(
                services.Collect(),
                static (context, services) =>
                {
                    var initializers = new List<string>();
                    var properties = new List<string>();
                    var asyncInitializers = new List<string>();
                    foreach (var service in services)
                    {
                        if (service is null)
                        {
                            continue;
                        }

                        initializers.Add($"{service.ServiceName} = new {service.ImplementingTypeName}(this);");

                        var accessibility = service.Accessibility is Accessibility.Public ? "public" : "internal";
                        properties.Add($"{accessibility} {service.ImplementingTypeName} {service.ServiceName} {{ get; }}");

                        asyncInitializers.Add($"await ((IAsyncLifetime){service.ServiceName}).InitializeAsync();");
                    }

                    var testServices = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
    using System.Threading.Tasks;
    using global::Xunit;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// Provides access to helpers for common integration test functionality.
    /// </summary>
    public sealed class TestServices
    {{
        private TestServices(JoinableTaskFactory joinableTaskFactory)
        {{
            JoinableTaskFactory = joinableTaskFactory;

{string.Join("\r\n", initializers.Select(initializer => "            " + initializer))}
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskFactory""/> for use in integration tests.
        /// </summary>
        public JoinableTaskFactory JoinableTaskFactory {{ get; }}

{string.Join("\r\n", properties.Select(property => "        " + property))}

        internal static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {{
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }}

        private async Task InitializeAsync()
        {{
{string.Join("\r\n", asyncInitializers.Select(initializer => "            " + initializer))}
        }}
    }}
}}
";

                    context.AddSource($"TestServices{SourceSuffix}", testServices);
                });
        }

        private static SimpleNameSyntax GetUnqualifiedName(NameSyntax name)
        {
            return name switch
            {
                SimpleNameSyntax simpleName => simpleName,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right,
                AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
                _ => throw new ArgumentException($"Unsupported syntax kind: {name.Kind()}", nameof(name)),
            };
        }

        private sealed record ServiceDataModel(
            Accessibility Accessibility,
            string ServiceName,
            string? BaseTypeName,
            string ImplementingTypeName);

        private sealed record ReferenceDataModel(
            bool HasSAsyncServiceProvider,
            bool HasThreadHelperJoinableTaskContext,
            bool CanCancelJoinTillEmptyAsync,
            bool HasJoinableTaskFactoryWithPriority,
            bool HasAsyncEnumerable,
            bool HasErrorHandler,
            bool HasOperationProgress,
            bool HasOperationProgressStatusService,
            bool HasEditorConstants,
            bool EditorConstantsCommandIDMissingGuid);
    }
}
