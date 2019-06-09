// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CSharp.AddImport
{
    internal static class AddImportDiagnosticIds
    {
        /// <summary>
        /// name does not exist in context
        /// </summary>
        public const string CS0103 = nameof(CS0103);

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
        ///  There is no argument given that corresponds to the required formal parameter 'X' of 'Y'
        /// </summary>
        public const string CS7036 = nameof(CS7036);

        /// <summary>
        /// o Deconstruct instance or extension method was found for type 'X', with N out parameters
        /// </summary>
        public const string CS8129 = nameof(CS8129);

        /// <summary>
        /// Internal symbol inaccessible because public key is wrong
        /// </summary>
        public const string CS0281 = nameof(CS0281);

        public static ImmutableArray<string> FixableTypeIds =
            ImmutableArray.Create(
                CS0103,
                CS0246,
                CS0305,
                CS0308,
                CS0122,
                CS0307,
                CS0616,
                CS1580,
                CS1581,
                CS8129,
                IDEDiagnosticIds.UnboundIdentifierId);

        public static ImmutableArray<string> FixableDiagnosticIds =
            FixableTypeIds.Concat(ImmutableArray.Create(
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
                    CS0281));
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddImport), Shared]
    internal class CSharpAddImportCodeFixProvider : AbstractAddImportCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;

        [ImportingConstructor]
        public CSharpAddImportCodeFixProvider()
        {
        }

        /// <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
        internal CSharpAddImportCodeFixProvider(
            IPackageInstallerService installerService,
            ISymbolSearchService symbolSearchService)
            : base(installerService, symbolSearchService)
        {
        }
    }
}
