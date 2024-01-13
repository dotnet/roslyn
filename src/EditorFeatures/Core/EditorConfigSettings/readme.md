# Editorconfig settings providers and updaters

## About

The code in this folder is responsible for providing various catagories of editorconfig options with the intent that some UI or tool is going to use this information to update or modify these options. Since these some of these options can be defined as either coming from an editorconfig file or the default Visual Studio setting there is some abstraction over these types so UIs built on top of this do not need to understand where the options are coming from in order to edit them.

## Other Relevant files

Additional, language specific data providers can be found here:

- src\VisualStudio\CSharp\Impl\EditorConfigSettings\DataProvider\CodeStyle\CSharpCodeStyleSettingsProvider.cs
- src\VisualStudio\CSharp\Impl\EditorConfigSettings\DataProvider\Whitespace\CSharpWhitespaceSettingsProvider.cs

## Tests

Tests for this folder live here

- src\EditorFeatures\CSharpTest\EditorConfigSettings\Updater\SettingsUpdaterTests.cs
  - Verifies that all of options defined in the `Data` folder here can be updated
- src\VisualStudio\CSharp\Test\EditorConfigSettings\DataProvider\DataProviderTests.cs
  - Verifies that we are able to correctly discover and provide editorconfig data at the workspace and language service level
  - Also verifies that if new options are added the corresponding data provider updates are made.