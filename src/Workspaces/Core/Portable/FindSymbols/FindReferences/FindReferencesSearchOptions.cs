// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// <param name="AssociatePropertyReferencesWithSpecificAccessor">
/// When searching for property, associate specific references we find to the relevant
/// accessor symbol (if there is one).  For example, in C#, this would result in:
/// 
///     P = 0;     // A reference to the P.set accessor
///     var v = P; // A reference to the P.get accessor
///     P++;       // A reference to P.get and P.set accessors
///     nameof(P); // A reference only to P.  Not associated with a particular accessor.
///     
/// The default for this is false.  With that default, all of the above references
/// are associated with the property P and not the accessors.
/// </param>
/// <param name="Cascade">
/// Whether or not we should cascade from the original search symbol to new symbols as we're
/// doing the find-references search.
/// </param>
/// <param name="Explicit">
/// Whether or not this find ref operation was explicitly invoked or not.  If explicit invoked, the find
/// references operation may use more resources to get the results faster.
/// 
/// Features that run automatically should consider setting this to <see langword="false"/> to avoid
/// unnecessarily impacting the user while they are doing other work.
/// </param>
/// <param name="UnidirectionalHierarchyCascade">
/// When cascading if we should only travel in a consistent direction away from the starting symbol.  For
/// example, starting on a virtual method, this would cascade upwards to implemented interface methods, and
/// downwards to overridden methods.  However, it would not then travel back down to other implementations of
/// those interface methods.  This is useful for cases where the client only wants references that could lead to
/// this symbol actually being called into at runtime.
/// 
/// There are cases where a client will not want this behavior.  An example of that is 'Rename'.  In rename,
/// there is a implicit link between members in a hierarchy with the same name (and appropriate signature).  For example, in:
///
/// <code>
/// interface I { void Goo(); }
/// class C1 : I { public void Goo() { } }
/// class C2 : I { public void Goo() { } }
/// </code>
/// 
/// If <c>C1.Goo</c> is renamed, this will need to rename <c>C2.Goo</c> as well to keep the code properly
/// compiling.  So, by default 'Rename' will cascade to all of these so it can appropriately update them.  This
/// option is the more relevant with knowing if a particular reference would actually result in a call to the
/// original member, not if it has a relation to the original member.
/// </param>
/// <param name="DisplayAllDefinitions">
/// Displays all definitions regardless of whether they have a reference or not.
/// </param>
/// </summary>

[DataContract]
internal readonly record struct FindReferencesSearchOptions(
    [property: DataMember(Order = 0)] bool AssociatePropertyReferencesWithSpecificAccessor = false,
    [property: DataMember(Order = 1)] bool Cascade = true,
    [property: DataMember(Order = 2)] bool Explicit = true,
    [property: DataMember(Order = 3)] bool UnidirectionalHierarchyCascade = false,
    [property: DataMember(Order = 4)] bool DisplayAllDefinitions = false)
{
    public FindReferencesSearchOptions()
        : this(AssociatePropertyReferencesWithSpecificAccessor: false)
    {
    }

    public static readonly FindReferencesSearchOptions Default = new();

    /// <summary>
    /// Returns the appropriate options for a given symbol for the specific 'Find References' feature.  This should
    /// not be used for other features (like 'Rename').  For the 'Find References' feature, if the user starts
    /// searching on an accessor, then we want to give results associated with the specific accessor.  Otherwise, if
    /// they search on a property, then associate everything with the property.  We also only want to travel an
    /// inheritance hierarchy unidirectionally so that we only see potential references that could actually reach
    /// this particular member.
    /// </summary>
    public static FindReferencesSearchOptions GetFeatureOptionsForStartingSymbol(ISymbol symbol)
        => Default with
        {
            AssociatePropertyReferencesWithSpecificAccessor = symbol.IsPropertyAccessor(),
            UnidirectionalHierarchyCascade = true
        };
}
