# Editorconfig settings editor UI

Most of the code in this folder is in service of creating a set of [`IWpfTableControl4`](https://docs.microsoft.com/otnet/api/microsoft.visualstudio.shell.tablecontrol.iwpftablecontrol4) controls populated with our own custom data.  This is accomplished via several steps.

For each table of data we need to implement an [ITableDataSource](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itabledatasource). The majority of the logic for how that is done is centralized in [SettingsViewModelBase](Common/SettingsViewModelBase.cs) with each ViewModel inheriting from it. The basic order of operations is:

- Register our data source with the [ITableManager](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itablemanager) instance in Visual Studio
- Push new data to [ITableDataSink](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itabledatasink)'s when its received from the `ISettingsProvider<T>` (this is an interface we've defined ourselves that provides a list of settings)
- Remove ourselves from the [ITableManager](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itablemanager) when our window closes.

The majority of the logic that is not in the [SettingsViewModelBase](Common/SettingsViewModelBase.cs) type is for creating column definitions. In brief: our data source needs describe how many columns it has and how to contruct WPF elements that represent a view of a cell of that data.
These columns are defined and referred to via strings. To ensure we are consistent all the strings that are used are defined in [ColumnDefinitions.cs](Common/ColumnDefinitions.cs)
Each column definition then needs to be MEF exported as an [`ITableColumnDefinition`](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablecontrol.itablecolumndefinition).

Here is an example of all the colum definition exports for the analyzers data table:

- Analyzers
  - [AnalyzerCategoryColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerCategoryColumnDefinition.cs)
  - [AnalyzerDescriptionColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerDescriptionColumnDefinition.cs)
  - [AnalyzerEnabledColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerEnabledColumnDefinition.cs)
  - [AnalyzerIdColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerIdColumnDefinition.cs)
  - [AnalyzerLocationColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerLocationColumnDefinition.cs)
  - [AnalyzerSeverityColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerSeverityColumnDefinition.cs)
  - [AnalyzerTitleColumnDefinition.cs](Analyzers\View\ColumnDefinitions\AnalyzerTitleColumnDefinition.cs)

Note that some of these types are relatively empty such as [AnalyzerTitleColumnDefinition](Analyzers\View\ColumnDefinitions\AnalyzerTitleColumnDefinition.cs). This column is a string so it doesn't need any special logic. Visual Studio will implicitly create a view of any data that is a string. However the column that represents severity ([AnalyzerSeverityColumnDefinition](Analyzers\View\ColumnDefinitions\AnalyzerSeverityColumnDefinition.cs)) is editable and creates its own WPF controls to display this in the `TryCreateColumnContent` section. We can get specific data about our [`ITableEntryHandle`](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablecontrol.itableentryhandle) by passing a string as a key.

These string keys are our column definitions that we exported earlier. They are used in the [ITableDataSource](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itabledatasource) to map columns to data. Examples of mapping a string entry to is corresponding data can be found here:

- [AnalyzerSettingsViewModel.SettingsEntriesSnapshot.cs](Analyzers/ViewModel/AnalyzerSettingsViewModel.SettingsEntriesSnapshot.cs)
- [CodeStyleSettingsViewModel.SettingsEntriesSnapshot.cs](CodeStyle/ViewModel/CodeStyleSettingsViewModel.SettingsEntriesSnapshot.cs)
- [NamingStyleSettingsViewModel.SettingsEntriesSnapshot.cs](NamingStyle/ViewModel/NamingStyleSettingsViewModel.SettingsEntriesSnapshot.cs)
- [WhitespaceViewModel.SettingsEntriesSnapshot.cs](Whitespace/ViewModel/WhitespaceViewModel.SettingsEntriesSnapshot.cs)

Once we've implemented an [ITableDataSource](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.tablemanager.itabledatasource), exported all of our column definitions correctly and created whatever custom views over that data we think are applicable we are all set!

## Editor factory

These types are responsible for registering with Visual Studio as the default editorfactory for `.editorconfig` files.

### Registration

[RoslynPackage.cs](../RoslynPackage.cs) is where we register [SettingsEditorFactory](SettingsEditorFactory.cs) with Visual Studio. An example of that can be seen [here](https://github.com/dotnet/roslyn/blob/8a92d8abaffdebe23ff777f692ee0cf26103fdf5/src/VisualStudio/Core/Def/RoslynPackage.cs#L182-L183). That is not sufficient for things to work however. The GUID that is exported on [SettingsEditorFactory](SettingsEditorFactory.cs) is used in [PackageRegistration.pkgdef](../PackageRegistration.pkgdef) to add metadata about what file types it applies to (i.e files that end in `editorconfig`). An example of what that looks like have be found [here](https://github.com/dotnet/roslyn/blob/8a92d8abaffdebe23ff777f692ee0cf26103fdf5/src/VisualStudio/Core/Def/PackageRegistration.pkgdef#L57-L66)

### [SettingsEditorFactory](SettingsEditorFactory.cs)

This class is mostly boilerplate for setting up an editor. It does check that we are in a C# or VB project before constructing the editor so we don't try and show options for C++ or some other language we don't yet understand the options for. This type also implements [`IVsEditorFactory4`](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.shell.interop.ivseditorfactory4) for the `ShouldDeferUntilIntellisenseIsReady` method since there is no point in queried the workspace for settings information until things are fully populated.

### [SettingsEditorPane](SettingsEditorPane.cs)

Also mostly boilerplate. This type is the window pane that our wpf controls are hosted in. Its responsible for setting up the VS search functionality that you see in the final window.

[SettingsEditorControl](SettingsEditorControl.xaml) ([code behind](SettingsEditorControl.xaml.cs))

Contains the majority of the layout logic for the final view. Exposes [`IWpfTableControl4`](https://docs.microsoft.com/otnet/api/microsoft.visualstudio.shell.tablecontrol.iwpftablecontrol4)s so the owning SettingsEditorPane can search them with the VS api.

## Other Relevant files

### Custom UI Helpers

View Models for language specific types are defined in this folder. As of right now there is not Other XAML based UI files to consider

- [`src/VisualStudio/CSharp/Impl/EditorConfigSettings`](../../../CSharp/Impl/EditorConfigSettings)
  - [BinaryOperatorSpacingOptionsViewModel](../../../CSharp/Impl/EditorConfigSettings/BinaryOperatorSpacingOptionsViewModel.cs)
  - [LabelPositionOptionsViewModel](../../../CSharp/Impl/EditorConfigSettings/LabelPositionOptionsViewModel.cs)

### Data Providers

Services that provide the data this UI displays live here

- [`src\EditorFeatures\Core\EditorConfigSettings`](../../../../EditorFeatures/Core/EditorConfigSettings/readme.md)
  - [AnalyzerSettingsProvider](../../../../EditorFeatures/Core/EditorConfigSettings/DataProvider/Analyzer/AnalyzerSettingsProvider.cs)
  - [NamingStyleSettingsProvider](../../../../EditorFeatures/Core/EditorConfigSettings/DataProvider/NamingStyles/NamingStyleSettingsProvider.cs)
- [`src\VisualStudio\Core\Def\EditorConfigSettings`](../EditorConfigSettings)
  - [CommonCodeStyleSettingsProvider](../EditorConfigSettings/DataProvider/CodeStyle/CommonCodeStyleSettingsProvider.cs)
  - [CommonWhitespaceSettingsProvider](../EditorConfigSettings/DataProvider/Whitespace/CommonWhitespaceSettingsProvider.cs)
- [`src\VisualStudio\CSharp\Impl\EditorConfigSettings`](../../../CSharp/Impl/EditorConfigSettings)
  - [CSharpCodeStyleSettingsProvider](../../../CSharp/Impl/EditorConfigSettings/DataProvider/CodeStyle/CSharpCodeStyleSettingsProvider.cs)
  - [CSharpWhitespaceSettingsProvider](../../../CSharp/Impl/EditorConfigSettings/DataProvider/Whitespace/CSharpWhitespaceSettingsProvider.cs)