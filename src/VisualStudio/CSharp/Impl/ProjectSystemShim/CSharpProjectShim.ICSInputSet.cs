// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSInputSet
    {
        public ICSCompiler GetCompiler()
        {
            throw new NotImplementedException();
        }

        public void AddSourceFile(string filename)
        {
            // Nothing to do here. We watch addition/removal of source files via the ICSharpProjectSite methods.
        }

        public void RemoveSourceFile(string filename)
        {
            // Nothing to do here. We watch addition/removal of source files via the ICSharpProjectSite methods.
        }

        public void RemoveAllSourceFiles()
        {
            throw new NotImplementedException();
        }

        public void AddResourceFile(string filename, string ident, bool embed, bool vis)
        {
            throw new NotImplementedException();
        }

        public void RemoveResourceFile(string filename, string ident, bool embed, bool vis)
        {
            throw new NotImplementedException();
        }

        public void SetWin32Resource(string filename)
        {
            // This file is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public void SetOutputFileName(string filename)
        {
            SetOutputPathAndRelatedData(filename);
        }

        public void SetOutputFileType(OutputFileType fileType)
        {
            OutputKind newOutputKind;
            switch (fileType)
            {
                case OutputFileType.Console:
                    newOutputKind = OutputKind.ConsoleApplication;
                    break;

                case OutputFileType.Windows:
                    newOutputKind = OutputKind.WindowsApplication;
                    break;

                case OutputFileType.Library:
                    newOutputKind = OutputKind.DynamicallyLinkedLibrary;
                    break;

                case OutputFileType.Module:
                    newOutputKind = OutputKind.NetModule;
                    break;

                case OutputFileType.AppContainer:
                    newOutputKind = OutputKind.WindowsRuntimeApplication;
                    break;

                case OutputFileType.WinMDObj:
                    newOutputKind = OutputKind.WindowsRuntimeMetadata;
                    break;

                default:

                    throw new ArgumentException("fileType was not a valid OutputFileType", "fileType");
            }

            SetOption(ref _outputKind, newOutputKind);
        }

        public void SetImageBase(uint imageBase)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public void SetMainClass(string fullyQualifiedClassName)
        {
            SetOption(ref _mainTypeName, fullyQualifiedClassName);
        }

        public void SetWin32Icon(string iconFileName)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public void SetFileAlignment(uint align)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public void SetImageBase2(ulong imageBase)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public void SetPdbFileName(string filename)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        public string GetWin32Resource()
        {
            throw new NotImplementedException();
        }

        public void SetWin32Manifest(string manifestFileName)
        {
            // This option is used only during emit. Since we no longer use our in-proc workspace to emit, we can ignore this value.
        }

        private void SetOption<T>(ref T value, T newValue)
        {
            if (!object.Equals(value, newValue))
            {
                value = newValue;
                UpdateOptions();
            }
        }
    }
}
