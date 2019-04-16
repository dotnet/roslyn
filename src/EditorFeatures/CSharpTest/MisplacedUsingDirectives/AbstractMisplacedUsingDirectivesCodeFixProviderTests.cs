// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives
{
    /// <summary>
    /// Base test class for the <see cref="MisplacedUsingDirectivesCodeFixProvider"/>.
    /// </summary>
    public abstract class AbstractMisplacedUsingDirectivesCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal static readonly CodeStyleOption<AddImportPlacement> OutsidePreferPreservationOption =
           new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption.None);

        internal static readonly CodeStyleOption<AddImportPlacement> InsidePreferPreservationOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption.None);

        internal static readonly CodeStyleOption<AddImportPlacement> InsideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption.Error);

        internal static readonly CodeStyleOption<AddImportPlacement> OutsideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption.Error);


        protected const string ClassDefinition = @"public class TestClass
{
}";

        protected const string StructDefinition = @"public struct TestStruct
{
}";

        protected const string InterfaceDefinition = @"public interface TestInterface
{
}";

        protected const string EnumDefinition = @"public enum TestEnum
{
    TestValue
}";

        protected const string DelegateDefinition = @"public delegate void TestDelegate();";

        private protected Task TestDiagnosticMissingAsync(string initialMarkup, CodeStyleOption<AddImportPlacement> preferredPlacementOption)
        {
            var options = new Dictionary<OptionKey, object> { { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption } };
            return TestDiagnosticMissingAsync(initialMarkup, new TestParameters(options: options));
        }

        private protected Task TestMissingAsync(string initialMarkup, CodeStyleOption<AddImportPlacement> preferredPlacementOption)
        {
            var options = new Dictionary<OptionKey, object> { { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption } };
            return TestMissingAsync(initialMarkup, new TestParameters(options: options));
        }

        private protected Task TestInRegularAndScriptAsync(string initialMarkup, string expectedMarkup, CodeStyleOption<AddImportPlacement> preferredPlacementOption, bool placeSystemNamespaceFirst)
        {
            var options = new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredPlacementOption },
                { new OptionKey(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp), placeSystemNamespaceFirst }
            };
            return TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: options);
        }
    }
}
