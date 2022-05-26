// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    /// <summary>
    /// A redefinition of the EnvDTE.CodeElements interface. The interface, as defined in the PIA does not do
    /// PreserveSig for the Item function. WinForms, specifically, uses the Item property when generating methods to see
    /// if a method already exists. The only way it sees if something exists is if the call returns E_INVALIDARG. With
    /// the normal PIAs though, this would result in a first-chance exception. Therefore, the WinForms team has their
    /// own definition for CodeElements which also [PreserveSig]s Item. We do this here to make their work still
    /// worthwhile.
    /// </summary>
    [ComImport]
    [Guid("0CFBC2B5-0D4E-11D3-8997-00C04F688DDE")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ICodeElements : IEnumerable
    {
        [DispId(-4)]
        [TypeLibFunc(TypeLibFuncFlags.FRestricted)]
        new IEnumerator GetEnumerator();

        [DispId(1)]
        EnvDTE.DTE DTE { [return: MarshalAs(UnmanagedType.Interface)] get; }

        [DispId(2)]
        object Parent { [return: MarshalAs(UnmanagedType.IDispatch)] get; }

        [DispId(0)]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        int Item(object index, [MarshalAs(UnmanagedType.Interface)] out EnvDTE.CodeElement element);

        [DispId(3)]
        int Count { get; }

        [TypeLibFunc(TypeLibFuncFlags.FHidden | TypeLibFuncFlags.FRestricted)]
        [DispId(4)]
        void Reserved1(object element);

        [DispId(5)]
        bool CreateUniqueID([MarshalAs(UnmanagedType.BStr)] string prefix, [MarshalAs(UnmanagedType.BStr)] ref string newName);
    }
}
