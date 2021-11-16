// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("58A26C00-BE6F-4B32-803A-98F5B7806C76")]
    internal interface IMethodXML2
    {
        /// <summary>
        /// Returns a string reader of the XML. Unlike IMethodXML, this doesn't require us to convert our XML string to
        /// BSTR and back.
        /// </summary>
        /// <returns>A System.IO.StringReader, even though we just say object here.</returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetXML();
    }
}
