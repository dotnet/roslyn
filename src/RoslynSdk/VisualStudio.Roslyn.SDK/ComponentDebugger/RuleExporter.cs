// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Roslyn.ComponentDebugger
{
    internal static class RuleExporter
    {
        /// <summary>
        /// Used to export the XAML rule via MEF
        /// </summary>
        [ExportPropertyXamlRuleDefinition(
            xamlResourceAssemblyName: "Roslyn.ComponentDebugger, Version=" + AssemblyVersion.Version + ", Culture=neutral, PublicKeyToken=31bf3856ad364e35",
            xamlResourceStreamName: "XamlRuleToCode:ComponentDebuggerLaunchProfile.xaml",
            context: PropertyPageContexts.Project)]
        [AppliesTo(Constants.RoslynComponentCapability)]
#pragma warning disable CS0649
        public static int LaunchProfileRule;
#pragma warning restore CS0649
    }
}
