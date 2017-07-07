﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    /// <summary>
    /// Redefine IVsContainedLanguageHost so we can call InsertImportsDirective which would 
    /// otherwise expect the namespace string as a ushort.
    /// </summary>
    [Guid("0429916F-69E1-4336-AB7E-72086FB0D6BC")]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsContainedLanguageHostInternal
    {
        // These Reserved* methods are here to use up space in the vtable
        void Reserved1();
        void Reserved2();
        void Reserved3();
        void Reserved4();
        void Reserved5();
        void Reserved6();
        void Reserved7();
        void Reserved8();
        void Reserved9();
        void Reserved10();
        void Reserved11();
        void Reserved12();

        [PreserveSig]
        int InsertImportsDirective([MarshalAs(UnmanagedType.LPWStr)] string pwcImportP);

        void Reserved13();
        void Reserved14();
    }
}
