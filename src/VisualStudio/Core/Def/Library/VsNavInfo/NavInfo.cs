// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;

internal class NavInfo : IVsNavInfo, IVsNavInfo2
{
    private readonly NavInfoFactory _factory;
    private readonly string _libraryName;
    private readonly string _referenceOwnerName;
    private readonly string _namespaceName;
    private readonly string _className;
    private readonly string _memberName;

    private readonly _LIB_LISTTYPE _symbolType;

    private readonly ImmutableArray<NavInfoNode> _baseCanonicalNodes;
    private readonly ImmutableArray<NavInfoNode> _basePresentationNodes;

    private ImmutableArray<NavInfoNode> _canonicalEnumNodes;
    private ImmutableArray<NavInfoNode> _objectBrowserEnumNodes;
    private ImmutableArray<NavInfoNode> _classViewEnumNodes;

    /// <summary>
    /// Creates a new NavInfo object that implements <see cref="IVsNavInfo"/> and <see cref="IVsNavInfo2"/>.
    /// </summary>
    /// <param name="factory">The <see cref="NavInfoFactory"/> that created this NavInfo.</param>
    /// <param name="libraryName">The name of the library (project or assembly) to use for navigation.</param>
    /// <param name="referenceOwnerName">If this NavInfo is inside of an assembly or project reference, this is the name of the project
    /// that owns the reference. In general, this is only set when the NavInfo is constructed from the Class View window, where references
    /// are parented inside of projects.</param>
    /// <param name="namespaceName">The name of the namespace used for navigation.</param>
    /// <param name="className">The name of the class used for navigation (should be contained by <paramref name="namespaceName"/>).</param>
    /// <param name="memberName">The name of the member used for navigation (should be contained by <paramref name="memberName"/>).</param>
    public NavInfo(
        NavInfoFactory factory,
        string libraryName,
        string referenceOwnerName = null,
        string namespaceName = null,
        string className = null,
        string memberName = null)
    {
        _factory = factory;

        _libraryName = libraryName;
        _referenceOwnerName = referenceOwnerName;
        _namespaceName = namespaceName;
        _className = className;
        _memberName = memberName;

        _baseCanonicalNodes = CreateNodes(expandDottedNames: true);
        _basePresentationNodes = CreateNodes(expandDottedNames: false);

        _symbolType = _basePresentationNodes.Length > 0
            ? _basePresentationNodes[_basePresentationNodes.Length - 1].ListType
            : 0;
    }

    private ImmutableArray<NavInfoNode> CreateNodes(bool expandDottedNames)
    {
        var builder = ImmutableArray.CreateBuilder<NavInfoNode>();

        // Note: we only expand dotted names for namespaces and classes.

        if (_referenceOwnerName != null)
        {
            builder.Add(_referenceOwnerName, _LIB_LISTTYPE.LLT_PACKAGE);
            builder.Add(ServicesVSResources.Project_References, _LIB_LISTTYPE.LLT_HIERARCHY);
        }

        builder.Add(_libraryName, _LIB_LISTTYPE.LLT_PACKAGE);
        builder.Add(_namespaceName, _LIB_LISTTYPE.LLT_NAMESPACES, expandDottedNames);
        builder.Add(_className, _LIB_LISTTYPE.LLT_CLASSES, expandDottedNames);
        builder.Add(_memberName, _LIB_LISTTYPE.LLT_MEMBERS);

        return builder.ToImmutable();
    }

    private static NavInfoNodeEnum CreateEnum(ref ImmutableArray<NavInfoNode> nodes, ImmutableArray<NavInfoNode> baseNodes, bool isCanonical, bool isObjectBrowser)
    {
        if (nodes.IsDefault)
        {
            var builder = ImmutableArray.CreateBuilder<NavInfoNode>();

            var startIndex = 0;

            // In some cases, Class View presentation NavInfo objects will have extra nodes (LLT_PACKAGE & LLT_HIERARCHY) up front.
            // When this NavInfo is consumed by Object Browser (for 'Browse to Definition'), we need to skip first two nodes
            if (isObjectBrowser && !isCanonical && baseNodes is [_, { ListType: _LIB_LISTTYPE.LLT_HIERARCHY }, ..])
            {
                startIndex = 2;
            }

            for (var i = startIndex; i < baseNodes.Length; i++)
            {
                if (isCanonical && baseNodes[i].ListType == _LIB_LISTTYPE.LLT_HIERARCHY)
                {
                    continue;
                }

                builder.Add(baseNodes[i]);
            }

            nodes = builder.ToImmutable();
        }

        return new NavInfoNodeEnum(nodes);
    }

    public int EnumCanonicalNodes(out IVsEnumNavInfoNodes ppEnum)
    {
        ppEnum = CreateEnum(ref _canonicalEnumNodes, _baseCanonicalNodes, isCanonical: true, isObjectBrowser: false);
        return VSConstants.S_OK;
    }

    public int EnumPresentationNodes(uint dwFlags, out IVsEnumNavInfoNodes ppEnum)
    {
        ppEnum = dwFlags == (uint)_LIB_LISTFLAGS.LLF_NONE
            ? CreateEnum(ref _objectBrowserEnumNodes, _basePresentationNodes, isCanonical: false, isObjectBrowser: true)
            : CreateEnum(ref _classViewEnumNodes, _basePresentationNodes, isCanonical: false, isObjectBrowser: false);

        return VSConstants.S_OK;
    }

    public int GetLibGuid(out Guid pGuid)
    {
        pGuid = _factory.LibraryService.LibraryId;
        return VSConstants.S_OK;
    }

    public void GetPreferredLanguage(out uint pLanguage)
        => pLanguage = (uint)_factory.LibraryService.PreferredLanguage;

    public int GetSymbolType(out uint pdwType)
    {
        pdwType = (uint)_symbolType;
        return VSConstants.S_OK;
    }
}
