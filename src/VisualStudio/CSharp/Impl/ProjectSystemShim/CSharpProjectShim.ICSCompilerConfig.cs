// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSCompilerConfig
    {
        public int GetOptionCount()
        {
            // NOTE: We return the length minus 1 to ensure that we're never called
            // with LARGEST_OPTION_ID.
            return (int)CompilerOptions.LARGEST_OPTION_ID - 1;
        }

        public void GetOptionInfoAt(int index, out CompilerOptions optionID, out string switchName, out string switchDescription, out uint flags)
        {
            throw new NotImplementedException();
        }

        public void GetOptionInfoAtEx(int index, out CompilerOptions optionID, out string shortSwitchName, out string longSwitchName, out string descriptiveSwitchName, out string switchDescription, out uint flags)
        {
            throw new NotImplementedException();
        }

        public void ResetAllOptions()
        {
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_CCSYMBOLS] = string.Empty;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_KEYFILE] = string.Empty;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_NOWARNLIST] = string.Empty;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_WARNASERRORLIST] = string.Empty;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_WARNNOTASERRORLIST] = string.Empty;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_UNSAFE] = false;
            VisualStudioProjectOptionsProcessor[CompilerOptions.OPTID_XML_DOCFILE] = string.Empty;
        }

        public int SetOption(CompilerOptions optionID, HACK_VariantStructure value)
        {
            VisualStudioProjectOptionsProcessor[optionID] = value.ConvertToObject();

            if (optionID == CompilerOptions.OPTID_COMPATIBILITY)
            {
                // HACK: we want the project system to use the out-of-proc compiler rather than
                // us, because we really don't build much of anything yet. We can say we don't
                // support pretty much anything we want to do this. Let's just say we don't
                // support any version of C# yet

                return VSConstants.S_FALSE;
            }

            return VSConstants.S_OK;
        }

        public void GetOption(CompilerOptions optionID, IntPtr variant)
        {
            Marshal.GetNativeVariantForObject(VisualStudioProjectOptionsProcessor[optionID], variant);
        }

        public int CommitChanges(ref ICSError error)
        {
            // We shall say we succeeded
            return VSConstants.S_OK;
        }

        public IntPtr GetWarnNumbers(out int count)
        {
            // The native consumer of this in CCscMSBuildHostObject::ConstructAndSetWarningsData
            // seems to expect that we "return" a pointer to an array of warning numbers of length
            // "count", but it treats this as an immutable array which it doesn't free or anything.
            // Implementing such a oddity is quite a mess in managed code, and so we allocated an
            // (empty) array during the creation of this, which we will clean up in the finalizer.

            count = 0;
            return _warningNumberArrayPointer;
        }

        public string GetWarnInfo(int warnIndex)
        {
            throw new NotImplementedException();
        }
    }
}
