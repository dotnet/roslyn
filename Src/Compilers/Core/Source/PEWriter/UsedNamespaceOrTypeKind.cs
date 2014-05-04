// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal enum UsedNamespaceOrTypeKind
    {
        CSNamespace, // e.g. using System;
        CSNamespaceAlias, // e.g. using S = System;
        CSExternNamespace, //e.g. extern alias CorLib;
        CSTypeAlias, // e.g. using IntList = System.Collections.Generic.List<int>;        
        VBNamespace, // e.g. Imports System.Collection
        VBType, // e.g. Imports System.Collection.ArrayList
        VBNamespaceOrTypeAlias, // e.g. Imports Foo=System.Collection or Imports Foo=System.Collection.ArrayList
        VBXmlNamespace, // e.g. Imports <xmlns:ns="http://NewNamespace"> (VB only)
        VBCurrentNamespace, // the current namespace of the method's container
        VBDefaultNamespace, // the default namespace of the project
    }
}
