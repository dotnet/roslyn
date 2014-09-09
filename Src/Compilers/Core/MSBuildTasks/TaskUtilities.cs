// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Build.Tasks.Hosting;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal static class TaskUtilities
    {
        /// <summary>
        /// If not design time build and the globalSessionGuid property was set then add a /globalsessionguid:{guid}
        /// </summary>
        internal static string AppendSessionGuidUnlessDesignTime(string response, string vsSessionGuid, Build.Utilities.Task task)
        {
            if (string.IsNullOrEmpty(vsSessionGuid)) return response;

            var hostObject = task.HostObject;
            if (hostObject == null) return response;

            var csHost = hostObject as ICscHostObject;
            bool isDesignTime = (csHost != null && csHost.IsDesignTime());
            if (!isDesignTime)
            {
                var vbHost = hostObject as IVbcHostObject;
                isDesignTime = (vbHost != null && vbHost.IsDesignTime());
            }

            return isDesignTime ? response : string.Format("{0} /sqmsessionguid:{1}", response, vsSessionGuid);
        }
    }
}
