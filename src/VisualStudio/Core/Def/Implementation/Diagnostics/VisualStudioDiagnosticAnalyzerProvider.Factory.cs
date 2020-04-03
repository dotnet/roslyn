// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal partial class VisualStudioDiagnosticAnalyzerProvider
    {
        /// <summary>
        /// A separate factory class is required since <see cref="SVsServiceProvider"/> needs different version of MEF than <see cref="WorkspaceEventListener"/>.
        /// </summary>
        [Export(typeof(Factory))]
        internal sealed class Factory
        {
            public VisualStudioDiagnosticAnalyzerProvider Provider { get; }

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            {
                var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));

                // Microsoft.VisualStudio.ExtensionManager is non-versioned, so we need to dynamically load it, depending on the version of VS we are running on
                // this will allow us to build once and deploy on different versions of VS SxS.
                var vsDteVersion = Version.Parse(dte.Version.Split(' ')[0]); // DTE.Version is in the format of D[D[.D[D]]][ (?+)], so we need to split out the version part and check for uninitialized Major/Minor below

                var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager, Version={(vsDteVersion.Major == -1 ? 0 : vsDteVersion.Major)}.{(vsDteVersion.Minor == -1 ? 0 : vsDteVersion.Minor)}.0.0, PublicKeyToken=b03f5f7f11d50a3a");
                Contract.ThrowIfNull(assembly);

                var typeIExtensionContent = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.IExtensionContent");
                Contract.ThrowIfNull(typeIExtensionContent);

                var type = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager");
                Contract.ThrowIfNull(type);

                var extensionManager = serviceProvider.GetService(type);
                Contract.ThrowIfNull(extensionManager);

                Provider = new VisualStudioDiagnosticAnalyzerProvider(extensionManager, typeIExtensionContent);
            }
        }
    }
}
