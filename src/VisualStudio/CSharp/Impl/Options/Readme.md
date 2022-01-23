# Adding Options

To add an option to the options page, follow these instructions.

1. Determine what page it goes on (Advanced Options, IntelliSenseOptions, NamingStyles, etc)
2. Add the control to the appropriate xaml file
3. Bind the control in the backing cs file. Example from [AdvancedOptionPageControl](https://github.com/dotnet/roslyn/blob/591e899025f1d4cf9bbb6e9af3ef82506b46f501/src/VisualStudio/CSharp/Impl/Options/AdvancedOptionPageControl.xaml.cs#L43)
```csharp
// BindToOption helper binds known controls, in this case "PlaceSystemNamespaceFirst"
// to the backing "GenerationOptions.PlaceSystemNamespaceFirst" for the language "CSharp"
BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
```
4. If you want the option to be searchable in the search bar, add to [VSPackage.resx](https://github.com/dotnet/roslyn/blob/591e899025f1d4cf9bbb6e9af3ef82506b46f501/src/VisualStudio/CSharp/Impl/VSPackage.resx) in the appropriate block. Each block of terms has a comment describing what page it's for.
