﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class VsDebugName : IVsDebugName
    {
        private readonly string _name;
        private readonly string _document;
        private readonly TextSpan _textSpan;

        public VsDebugName(string name, string document, TextSpan textSpan)
        {
            _name = name;
            _document = document;
            _textSpan = textSpan;
        }

        public int GetLocation(out string pbstrMkDoc, TextSpan[] pspanLocation)
        {
            pbstrMkDoc = _document;

            if (pspanLocation != null && pspanLocation.Length > 0)
            {
                pspanLocation[0] = _textSpan;
            }

            return VSConstants.S_OK;
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = _name;
            return VSConstants.S_OK;
        }
    }
}
