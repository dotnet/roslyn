using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    internal class GenerateConstructorFromMembersCSharp : AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        [ImportingConstructor]
        public GenerateConstructorFromMembersCSharp() : this(null)
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        internal GenerateConstructorFromMembersCSharp(IPickMembersService pickMembersService_forTesting)
            : base(pickMembersService_forTesting)
        {
        }

        protected override bool GetNullCheckOptionEnabled(DocumentOptionSet optionSet)
        => optionSet.GetOption(GenerateConstructorFromMembersOptions.AddNullChecks);
    }
}
