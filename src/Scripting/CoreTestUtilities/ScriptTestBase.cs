﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.TestUtilities;

public class ScriptTestBase : TestBase
{
    private readonly List<MetadataImageReference> _referenceList = new List<MetadataImageReference>();

    public ScriptOptions ScriptOptions { get; }
    public ScriptMetadataResolver ScriptMetadataResolver { get; }

    protected ScriptTestBase()
    {
        var runtimeMetadataReferenceResolver = RuntimeMetadataReferenceResolver.CreateCurrentPlatformResolver(
            searchPaths: [],
            baseDirectory: null,
            (path, properties) => CreateFromFile(path, PEStreamOptions.PrefetchEntireImage, properties));
        ScriptMetadataResolver = new ScriptMetadataResolver(runtimeMetadataReferenceResolver);
        ScriptOptions = ScriptOptions.Default
            .WithMetadataResolver(ScriptMetadataResolver)
            .WithCreateFromFileFunc(CreateFromFile);
    }

    private protected MetadataImageReference CreateFromFile(string filePath, PEStreamOptions options, MetadataReferenceProperties properties)
    {
        var reference = MetadataReference.CreateFromFile(filePath, options, properties);
        _referenceList.Add(reference);
        return reference;
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var reference in _referenceList)
        {
            reference.GetMetadataNoCopy().Dispose();
        }
    }
}
