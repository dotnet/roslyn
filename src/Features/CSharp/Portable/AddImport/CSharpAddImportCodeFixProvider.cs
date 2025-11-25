// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CSharp.AddImport;

internal static class AddImportDiagnosticIds
{
    /// <summary>
    /// name does not exist in context
    /// </summary>
    public const string CS0103 = nameof(CS0103);

    /// <summary>
    /// 'X' does not contain a definition for 'Y'
    /// </summary>
    public const string CS0117 = nameof(CS0117);

    /// <summary>
    /// The type or namespace name 'X' does not exist in the namespace 'Y' (are you missing an assembly reference?)
    /// </summary>
    public const string CS0234 = nameof(CS0234);

    /// <summary>
    /// type or namespace could not be found
    /// </summary>
    public const string CS0246 = nameof(CS0246);

    /// <summary>
    /// wrong number of type args
    /// </summary>
    public const string CS0305 = nameof(CS0305);

    /// <summary>
    /// type does not contain a definition of method or extension method
    /// </summary>
    public const string CS1061 = nameof(CS1061);

    /// <summary>
    /// cannot find implementation of query pattern
    /// </summary>
    public const string CS1935 = nameof(CS1935);

    /// <summary>
    /// The non-generic type 'A' cannot be used with type arguments
    /// </summary>
    public const string CS0308 = nameof(CS0308);

    /// <summary>
    /// 'A' is inaccessible due to its protection level
    /// </summary>
    public const string CS0122 = nameof(CS0122);

    /// <summary>
    /// The using alias 'A' cannot be used with type arguments
    /// </summary>
    public const string CS0307 = nameof(CS0307);

    /// <summary>
    /// 'A' is not an attribute class
    /// </summary>
    public const string CS0616 = nameof(CS0616);

    /// <summary>
    ///  No overload for method 'X' takes 'N' arguments
    /// </summary>
    public const string CS1501 = nameof(CS1501);

    /// <summary>
    /// cannot convert from 'int' to 'string'
    /// </summary>
    public const string CS1503 = nameof(CS1503);

    /// <summary>
    /// XML comment on 'construct' has syntactically incorrect cref attribute 'name'
    /// </summary>
    public const string CS1574 = nameof(CS1574);

    /// <summary>
    /// Invalid type for parameter 'parameter number' in XML comment cref attribute
    /// </summary>
    public const string CS1580 = nameof(CS1580);

    /// <summary>
    /// Invalid return type in XML comment cref attribute
    /// </summary>
    public const string CS1581 = nameof(CS1581);

    /// <summary>
    /// XML comment has syntactically incorrect cref attribute
    /// </summary>
    public const string CS1584 = nameof(CS1584);

    /// <summary>
    /// Type 'X' does not contain a valid extension method accepting 'Y'
    /// </summary>
    public const string CS1929 = nameof(CS1929);

    /// <summary>
    /// Property cannot be used like a method
    /// </summary>
    public const string CS1955 = nameof(CS1955);

    /// <summary>
    /// Cannot convert method group 'X' to non-delegate type 'Y'. Did you intend to invoke the method?
    /// </summary>
    public const string CS0428 = nameof(CS0428);

    /// <summary>
    ///  There is no argument given that corresponds to the required parameter 'X' of 'Y'
    /// </summary>
    public const string CS7036 = nameof(CS7036);

    /// <summary>
    /// (Error) No Deconstruct instance or extension method was found for type 'X', with N out parameters
    /// </summary>
    public const string CS8129 = nameof(CS8129);

    /// <summary>
    /// (Hidden) No Deconstruct instance or extension method was found for type 'X', with N out parameters
    /// </summary>
    public const string CS9344 = nameof(CS9344);

    /// <summary>
    /// Internal symbol inaccessible because public key is wrong
    /// </summary>
    public const string CS0281 = nameof(CS0281);

    /// <summary>
    /// 'X' does not contain a definition for 'Y' and no extension method 'Y' accepting a first argument of type 'X' could be found (are you missing a using directive for 'System'?)
    /// Specialized for WinRT
    /// </summary>
    public const string CS4036 = nameof(CS4036);

    /// <summary>
    /// foreach statement cannot operate on variables of type 'X' because 'X' does not contain a public instance or extension definition for 'GetEnumerator'
    /// </summary>
    public const string CS1579 = nameof(CS1579);

    /// <summary>
    /// foreach statement cannot operate on variables of type 'X' because 'X' does not contain a public instance or extension definition for 'GetEnumerator'. Did you mean 'await foreach' rather than 'foreach'?
    /// </summary>
    public const string CS8414 = nameof(CS8414);

    /// <summary>
    /// Asynchronous foreach statement cannot operate on variables of type 'X' because 'X' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
    /// </summary>
    public const string CS8411 = nameof(CS8411);

    /// <summary>
    /// Asynchronous foreach statement cannot operate on variables of type 'X' because 'X' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
    /// </summary>
    public const string CS8415 = nameof(CS8415);

    public static ImmutableArray<string> FixableDiagnosticIds = [
        CS0103,
        CS0117,
        CS0234,
        CS0246,
        CS0305,
        CS0308,
        CS0122,
        CS0307,
        CS0616,
        CS1580,
        CS1581,
        CS8129,
        CS9344,
        CS1061,
        CS1935,
        CS1501,
        CS1503,
        CS1574,
        CS1584,
        CS1929,
        CS1955,
        CS0428,
        CS7036,
        CS0281,
        CS4036,
        CS1579,
        CS8414,
        CS8411,
        CS8415,
        IDEDiagnosticIds.UnboundIdentifierId];
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddImport), Shared]
internal sealed class CSharpAddImportCodeFixProvider : AbstractAddImportCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddImportCodeFixProvider()
    {
    }

    /// <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    internal CSharpAddImportCodeFixProvider(
        IPackageInstallerService installerService,
        ISymbolSearchService symbolSearchService)
        : base(installerService, symbolSearchService)
    {
    }
}
