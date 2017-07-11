// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    public partial class CodeFunction
    {
        public string GetXML()
        {
            using (Logger.LogBlock(FunctionId.WinformDesigner_GenerateXML, CancellationToken.None))
            {
                return CodeModelService.GetMethodXml(LookupNode(), GetSemanticModel());
            }
        }

        public int SetXML(string bstrXML)
        {
            // This doesn't need to be implemented since nothing in VS currently uses it.
            throw new NotImplementedException();
        }

        public int GetBodyPoint(out object ppUnk)
        {
            throw new NotImplementedException();
        }

        object IMethodXML2.GetXML()
        {
            return new StringReader(GetXML());
        }
    }
}
