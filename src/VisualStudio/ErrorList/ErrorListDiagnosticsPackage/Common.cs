// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:StatementMustNotUseUnnecessaryParenthesis", Justification = "Reviewed.")]
    internal static class Common
    {
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider s_globalServiceProvider;

        internal static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (s_globalServiceProvider == null)
                {
                    s_globalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)(Package.GetGlobalService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)));
                }

                return s_globalServiceProvider;
            }

            // Exposed for unit testing.
            set
            {
                s_globalServiceProvider = value;
            }
        }

        internal static InterfaceType GetService<InterfaceType, ServiceType>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
        {
            return (InterfaceType)GetService(serviceProvider, typeof(ServiceType).GUID, false);
        }

        internal static object GetService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            Guid guidInterface = VSConstants.IID_IUnknown;
            IntPtr ptrObject = IntPtr.Zero;
            object service = null;

            int hr = serviceProvider.QueryService(ref guidService, ref guidInterface, out ptrObject);
            if (hr >= 0 && ptrObject != IntPtr.Zero)
            {
                try
                {
                    if (unique)
                    {
                        service = Marshal.GetUniqueObjectForIUnknown(ptrObject);
                    }
                    else
                    {
                        service = Marshal.GetObjectForIUnknown(ptrObject);
                    }
                }
                finally
                {
                    Marshal.Release(ptrObject);
                }
            }

            return service;
        }

        /// <summary>
        /// Helper method to query for an <see cref="IComponentModel"/> and initialize it.
        /// </summary>
        internal static IComponentModel GetComponentModel(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            return Common.GetService<IComponentModel, SComponentModel>(serviceProvider);
        }

        internal static T GetMefService<T>() where T : class
        {
            IComponentModel componentModel = Common.GetComponentModel(Common.GlobalServiceProvider);
            if (componentModel != null)
            {
                return componentModel.GetService<T>();
            }

            return null;
        }

        internal static IEnumerable<T> GetMefExtensions<T>() where T : class
        {
            IComponentModel componentModel = Common.GetComponentModel(Common.GlobalServiceProvider);
            if (componentModel != null)
            {
                return componentModel.GetExtensions<T>();
            }

            return null;
        }
    }
}
