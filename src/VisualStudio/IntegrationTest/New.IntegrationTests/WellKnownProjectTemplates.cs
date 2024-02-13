// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class WellKnownProjectTemplates
    {
        public const string ClassLibrary = nameof(ClassLibrary);
        public const string ConsoleApplication = nameof(ConsoleApplication);
        public const string Website = nameof(Website);
        public const string WinFormsApplication = nameof(WinFormsApplication);
        public const string WpfApplication = nameof(WpfApplication);
        public const string WebApplication = nameof(WebApplication);
        public const string CSharpNetCoreClassLibrary = "Microsoft.CSharp.NETCore.ClassLibrary";
        public const string VisualBasicNetCoreClassLibrary = "Microsoft.VisualBasic.NETCore.ClassLibrary";
        public const string CSharpNetCoreConsoleApplication = "Microsoft.CSharp.NETCore.ConsoleApplication";
        public const string VisualBasicNetCoreConsoleApplication = "Microsoft.VisualBasic.NETCore.ConsoleApplication";
        public const string CSharpNetStandardClassLibrary = "Microsoft.CSharp.NETStandard.ClassLibrary";
        public const string VisualBasicNetStandardClassLibrary = "Microsoft.VisualBasic.NETStandard.ClassLibrary";
        public const string CSharpNetCoreUnitTest = "Microsoft.CSharp.NETCore.UnitTest";
        public const string CSharpNetCoreXUnitTest = "Microsoft.CSharp.NETCore.XUnitTest";
        public const string Blazor = "Microsoft.WAP.CSharp.ASPNET.Blazor";

        /// <summary>
        /// The .cs file created by the <see cref="CSharpNetCoreClassLibrary"/> template.
        /// </summary>
        public const string CSharpNetCoreClassLibraryClassFileName = "Class1.cs";

        /// <summary>
        /// The .vb file created by the <see cref="VisualBasicNetCoreClassLibrary"/> template.
        /// </summary>
        public const string VisualBasicNetCoreClassLibraryClassFileName = "Class1.vb";
    }
}
