﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmClrDebuggerVisualizerAttribute : DkmClrEvalAttribute
    {
        /// <summary>
        /// Constructor for mock DkmClrDebuggerVisualizerAttribute.
        /// </summary>
        /// <param name="targetMember">[Optional] Should be null. Not supported for the DebuggerVisualizer attribute</param>
        /// <param name="uiSideVisualizerTypeName">[Required] The full name of the UI-side visualizer type</param>
        /// <param name="uiSideVisualizerAssemblyName">[Required] The full name of the UI-side visualizer assembly</param>
        /// <param name="uiSideVisualizerAssemblyLocation">[Required] The location of the UI-side visualizer assembly</param>
        /// <param name="debuggeeSideVisualizerTypeName">[Required] The full name of the debuggee-side visualizer type</param>
        /// <param name="debuggeeSideVisualizerAssemblyName">[Required] The full name of the debuggee-side visualizer assembly</param>
        /// <param name="visualizerDescription">[Required] The visualizer description</param>
        internal DkmClrDebuggerVisualizerAttribute(string targetMember,
            string uiSideVisualizerTypeName,
            string uiSideVisualizerAssemblyName,
            DkmClrCustomVisualizerAssemblyLocation uiSideVisualizerAssemblyLocation,
            string debuggeeSideVisualizerTypeName,
            string debuggeeSideVisualizerAssemblyName,
            string visualizerDescription) :
            base(null)
        {
            UISideVisualizerTypeName = uiSideVisualizerTypeName;
            UISideVisualizerAssemblyName = uiSideVisualizerAssemblyName;
            UISideVisualizerAssemblyLocation = uiSideVisualizerAssemblyLocation;
            DebuggeeSideVisualizerTypeName = debuggeeSideVisualizerTypeName;
            DebuggeeSideVisualizerAssemblyName = debuggeeSideVisualizerAssemblyName;
            VisualizerDescription = visualizerDescription;
        }

        public readonly string UISideVisualizerTypeName;
        public readonly string UISideVisualizerAssemblyName;
        public readonly DkmClrCustomVisualizerAssemblyLocation UISideVisualizerAssemblyLocation;
        public readonly string DebuggeeSideVisualizerTypeName;
        public readonly string DebuggeeSideVisualizerAssemblyName;
        public readonly string VisualizerDescription;
    }
}

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    //
    // Summary:
    //     Enum that describes the location of the visualizer assembly. This API was introduced
    //     in Visual Studio 14 RTM (DkmApiVersion.VS14RTM).
    public enum DkmClrCustomVisualizerAssemblyLocation
    {
        //
        // Summary:
        //     Location unknown.
        Unknown,
        //
        // Summary:
        //     The ...\Documents\...\Visual Studio X\Visualizers directory.
        UserDirectory,
        //
        // Summary:
        //     The ...\Common7\Packages\Debugger\Visualizers directory.
        SharedDirectory,
        //
        // Summary:
        //     Present on an assembly loaded by the debuggee.
        Debuggee
    }
}
