// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos
{
    internal class NavInfoNode : IVsNavInfoNode
    {
        private readonly string _name;
        private readonly uint _listType;

        public NavInfoNode(string name, uint listType)
        {
            _name = name;
            _listType = listType;
        }

        public string Name
        {
            get { return _name; }
        }

        public uint ListType
        {
            get { return _listType; }
        }

        int IVsNavInfoNode.get_Name(out string pbstrName)
        {
            pbstrName = _name;
            return VSConstants.S_OK;
        }

        int IVsNavInfoNode.get_Type(out uint pllt)
        {
            pllt = _listType;
            return VSConstants.S_OK;
        }
    }
}
