// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmCustomUIVisualizerInfo
    {
        public uint Id;
        public string MenuName;
        public string Description;
        public string Metric;
        public string UISideVisualizerTypeName;
        public string UISideVisualizerAssemblyName;
        public DkmClrCustomVisualizerAssemblyLocation UISideVisualizerAssemblyLocation;
        public string DebuggeeSideVisualizerTypeName;
        public string DebuggeeSideVisualizerAssemblyName;
        public Guid ExtensionPartId;

        public static DkmCustomUIVisualizerInfo Create(uint Id, string MenuName, string Description, string Metric)
        {
            return new DkmCustomUIVisualizerInfo
            {
                Id = Id,
                MenuName = MenuName,
                Description = Description,
                Metric = Metric
            };
        }

        public static DkmCustomUIVisualizerInfo Create(uint Id,
            string MenuName,
            string Description,
            string Metric,
            string UISideVisualizerTypeName,
            string UISideVisualizerAssemblyName,
            DkmClrCustomVisualizerAssemblyLocation UISideVisualizerAssemblyLocation,
            string DebuggeeSideVisualizerTypeName,
            string DebuggeeSideVisualizerAssemblyName)
        {
            return new DkmCustomUIVisualizerInfo
            {
                Id = Id,
                MenuName = MenuName,
                Description = Description,
                Metric = Metric,
                UISideVisualizerTypeName = UISideVisualizerTypeName,
                UISideVisualizerAssemblyName = UISideVisualizerAssemblyName,
                UISideVisualizerAssemblyLocation = UISideVisualizerAssemblyLocation,
                DebuggeeSideVisualizerTypeName = DebuggeeSideVisualizerTypeName,
                DebuggeeSideVisualizerAssemblyName = DebuggeeSideVisualizerAssemblyName
            };
        }

        public static DkmCustomUIVisualizerInfo Create(uint Id,
            string MenuName,
            string Description,
            string Metric,
            string UISideVisualizerTypeName,
            string UISideVisualizerAssemblyName,
            DkmClrCustomVisualizerAssemblyLocation UISideVisualizerAssemblyLocation,
            string DebuggeeSideVisualizerTypeName,
            string DebuggeeSideVisualizerAssemblyName,
            Guid ExtensionPartId)
        {
            return new DkmCustomUIVisualizerInfo
            {
                Id = Id,
                MenuName = MenuName,
                Description = Description,
                Metric = Metric,
                UISideVisualizerTypeName = UISideVisualizerTypeName,
                UISideVisualizerAssemblyName = UISideVisualizerAssemblyName,
                UISideVisualizerAssemblyLocation = UISideVisualizerAssemblyLocation,
                DebuggeeSideVisualizerTypeName = DebuggeeSideVisualizerTypeName,
                DebuggeeSideVisualizerAssemblyName = DebuggeeSideVisualizerAssemblyName,
                ExtensionPartId = ExtensionPartId
            };
        }
    }
}
