# Visual Studio code cleanup service

## Types used in the implementation

- Types Roslyn defines
  - [ICodeCleanupService](ICodeCleanupService.cs) interface that inherits from [ILanguageService](../../../../Workspaces/Core/Portable/Workspace/Host/ILanguageService.cs). This language service is what does all the work in code cleanup.
  - [AbstractCodeCleanupService](AbstractCodeCleanupService.cs) methods common to both C# and VB implementations of [ICodeCleanupService](ICodeCleanupService.cs)
    - [DiagnosticSet](DiagnosticSet.cs) type to group diagnostics that should all be fixed together (all block style settings for example) used by [AbstractCodeCleanupService](AbstractCodeCleanupService.cs)
    - [EnabledDiagnosticOptions](EnabledDiagnosticOptions.cs) represents the total set of things the code cleanup service should attempt to fix. This is needed (instead of just using [DiagnosticSet](DiagnosticSet.cs)) because items like whitespace formatting and organize-usings do not use analyzers
    - [OrganizeUsingsSet](OrganizeUsingsSettings.cs) a type that packages the relevant organize-usings settings as boolean flags
- Types Defined by Visual Studio that Roslyn implements
  - [ICodeCleanUpFixerProvider](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixerprovider) a factory that creates the [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) we want to use.
  - [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) the interface we need to implement if we want Visual Studio to work for our language.
- Types Implemented by Visual Studio used by Roslyn
  - Attributes used to add data to a [MEF](https://docs.microsoft.com/dotnet/framework/mef/) export of a `FixId`
    - [FixIdAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.fixidattribute) as string representing the identify of a something that an [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) can fix. We use the Diagnostic Id (such as `IDE001`) for these values.
    - [NameAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.utilities.nameattribute) as string representing the name of something that an [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) can fix. We also use the Diagnostic Id (such as `IDE001`) for these values since we don't have a concept of an Id being different than the Name.
    - [OrderAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.utilities.orderattribute) an attribute used to order items in the UI. You can ask for an item to be ordered before or after something by referring to its `FixId` (the string given to the [FixIdAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.fixidattribute))
    - [ConfigurationKeyAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.configurationkeyattribute) unused by us, we have our own configuration mechanisms for roslyn. We always export a [ConfigurationKeyAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.configurationkeyattribute) with the value of `"unused"`.
    - [HelpLinkAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.helplinkattribute) url to direct the user to. should be a page on docs.microsoft.com.
    - [LocalizedNameAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.utilities.localizednameattribute) The string that will be used in the VS UI.
  - [ICodeCleanUpScope](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupscope) exchange type passed to our [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) implementation used to tell us what to fix (document, project, solution, etc)
  - [ICodeCleanUpExecutionContext](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupexecutioncontext)exchange type passed to our [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) implementation used to tell us what `FixId`s (aka Diagnostic Ids for us) that the user has chosen to fix in the UI.

## How these types fit together

The implementations for [AbstractCodeCleanupService](AbstractCodeCleanupService.cs) are [CSharpCodeCleanupService](../../../CSharp/Portable/CodeCleanup/CSharpCodeCleanupService.cs) and [VisualBasicCodeCleanupService](../../../VisualBasic/Portable/CodeCleanup/VisualBasicCodeCleanupService.vb). The only things these implementations are responsible for is computing the ImmutableArray of [DiagnosticSet](DiagnosticSet.cs)s that is used to determine what diagnostics code cleanup can fix. In summary: if a Diagnostic Id is not listed when calling `GetDiagnosticSets` it will not be fixed.

The Code Cleanup language service is then consumed by our [ICodeCleanUpFixer](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.icodecleanupfixer) implementations: [CSharpCodeCleanUpFixer](../../../../VisualStudio/CSharp/Impl/LanguageService/CSharpCodeCleanupFixer.cs) and [VisualBasicCodeCleanUpFixer](../../../../VisualStudio/VisualBasic/Impl/LanguageService/VisualBasicCodeCleanupFixer.vb) (with nearly all the logic for them living in [AbstractCodeCleanUpFixer](../../../../VisualStudio/Core/Def/Implementation/CodeCleanup/AbstractCodeCleanUpFixer.cs)).

In order for any of our codefixes to appear in the code cleanup UI in Visual Studio  we need to export a [FixIdAttribute](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.language.codecleanup.fixidattribute) on a field so Visual Studio can compose the set of diagnostic ids we care about. The common diagnostic ids that are used in both Visual Basic and C# are defined in [CommonCodeCleanUpFixerDiagnosticIds](../../../../VisualStudio/Core/Def/Implementation/CodeCleanup/CommonCodeCleanUpFixerDiagnosticIds.cs). C# specific diagnostic ids are exported in [CSharpCodeCleanUpFixerDiagnosticIds](../../../../VisualStudio/CSharp/Impl/LanguageService/CSharpCodeCleanupFixerDiagnosticIds.cs) and Visual Basic specific diagnostic ids are exported in [VisualBasicCodeCleanUpFixerDiagnosticIds](../../../../VisualStudio/VisualBasic/Impl/LanguageService/VisualBasicCodeCleanupFixerDiagnosticIds.vb).

## Adding a new codefix to the code cleanup service

1. Add the Diagnostic Id to the CodeCleanupService
    1. If the code fixer should work for C# add it to [CSharpCodeCleanupService](../../../CSharp/Portable/CodeCleanup/CSharpCodeCleanupService.cs#L22)
        1. If it logically fits in with an existing `DiagnosticSet` add the new diagnostic Id there
        1. If there are no `DiagnosticSet`s that make sense add a new `DiagnosticSet` to the `s_diagnosticSets` `ImmutableArray`.
            1. If the codefix is used across both languages add a new resource string to [`FeaturesResources`](../FeaturesResources.resx) with a description for your `DiagnosticSet`
            1. If it only apples to a single language add a new resource string to [`CSharpFeaturesResources`](../../../CSharp/Portable/CSharpFeaturesResources.resx)with a description for your `DiagnosticSet`
    1. If the code fixer should work for Visual Basic add it to [VisualBasicCodeCleanupService](../../../VisualBasic/Portable/CodeCleanup/VisualBasicCodeCleanupService.vb#L21)
        1. If it logically fits in with an existing `DiagnosticSet` add the new diagnostic Id there
        1. If there are no `DiagnosticSet`s that make sense add a new `DiagnosticSet` to the `s_diagnosticSets` `ImmutableArray`.
            1. If the codefix is used across both languages add a new resource string to [`FeaturesResources`](../FeaturesResources.resx) with a description for your `DiagnosticSet`
            1. If it only apples to a single language add a new resource string to [`VBFeaturesResources`](../../../VisualBasic/Portable/VBFeaturesResources.resx)with a description for your `DiagnosticSet`
1. Export the `FixIdDefinition` for Visual Studio to show
    1. If the code fixer works for both Visual Basic and C# add a new `public` `static` field of type `FixIdDefinition` to [CommonCodeCleanUpFixerDiagnosticIds](../../../../VisualStudio/Core/Def/Implementation/CodeCleanup/CommonCodeCleanUpFixerDiagnosticIds.cs)
        1. Add the following attributes to the field
            1. `[Export]`
            1. `[FixId(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)]`
            1. `[Name(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)]`
            1. `[ConfigurationKey("unused")]`
            1. If the feature is not experimental add the help link url `[HelpLink($"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.NewDiagnosticIdThatYouAdded}")]`
            1. `[LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.string_you_added_in_1_a_ii_i))]`
    1. If the code fixer only in C# add a new `public` `static` field of type `FixIdDefinition` to [CSharpCodeCleanupFixerDiagnosticIds](../../../../VisualStudio/CSharp/Impl/LanguageService/CSharpCodeCleanupFixerDiagnosticIds.cs)
        1. Add the following attributes to the field
            1. `[Export]`
            1. `[FixId(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)]`
            1. `[Name(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)]`
            1. `[ConfigurationKey("unused")]`
            1. If the feature is not experimental add the help link url `[HelpLink($"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{IDEDiagnosticIds.NewDiagnosticIdThatYouAdded}")]`
            1. `[LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.string_you_added_in_1_a_ii_ii))]`
    1. If the code fixer only in Visual Basic add a new `Public` `Shared` field of type `FixIdDefinition` to [VisualBasicCodeCleanUpFixerDiagnosticIds](../../../../VisualStudio/VisualBasic/Impl/LanguageService/VisualBasicCodeCleanupFixerDiagnosticIds.vb)
        1. Add the following attributes to the field
            1. `<Export>`
            1. `<FixId(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)>`
            1. `<Name(IDEDiagnosticIds.NewDiagnosticIdThatYouAdded)>`
            1. `<ConfigurationKey("unused")>`
            1. If the feature is not experimental add the help link url `<HelpLink($"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/NewDiagnosticIdThatYouAdded")>`
            1. `<LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.string_you_added_in_1_b_ii_i))>`
