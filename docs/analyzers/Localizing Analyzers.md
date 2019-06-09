Introduction
============

This document covers the following:
* Setting up your project to use localized resources
 * Using localized resources in Analyzers
 * Using localized resources in Code Fixes
* Producing localized resource assemblies
* Packaging resource assemblies in NuGet packages and VSIXes
* Testing the localization at the command line and in VS

Setting up the Project
=======================

You'll need to create a .resx file to hold your localizable resources. For the purposes of this document, we'll assume you are putting them in a file named 'AnalyzerResources.resx'.

Using Localized Resources in Analyzers
--------------------------------------

Normally, .NET applications can simply access the resource through the type generated from the .resx file:
``` C#
string message = AnalyzerResources.Message;
```
and the framework will use the culture specified in `System.Globalization.CultureInfo.CurrentUICulture` to automatically find and return the appropriately localized resource.

However, the C# and VB compilers provide the `/preferreduilang` switch to specify the use of resources for a different culture from that in `CurrentUICulture`. Because of that accessing `AnalyzerResources.Message` directly within an analyzer will not return the correct resource. 

Instead you need to access these resources through the `LocalizableResourceString` type. For example:
``` C#
LocalizableResourceString message = new LocalizableResourceString(nameof(AnalyzerResources.Message), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
```

Using Localized Resources in Code Fixes
---------------------------------------

Code Fixes, on the other hand, are not affected by the `/preferreduilang` switch. Resources in Code Fixes should be accessed in the standard way:
``` C#
string codeFixMessage = AnalyzerResources.CodeFixMessage;
```

Producing Localized Resource Assemblies
=======================================

To create resource assemblies, you simply need to add additional .resx files with the desired [language name](https://msdn.microsoft.com/en-us/library/windows/desktop/dd318696(v=vs.85).aspx) in the file name. For example, if you wanted to create a resource assembly with German strings, you would add AnalyzerResources.de.resx. For Japanese, you add AnalyzerResources.ja.resx, and so on.

Inside these files you add items where the names match those in AnalyzerResources.resx and the values have the localized content. If AnalyzerResources.resx contains the following:

Name    | Value
--------|-------
Message | Hello!

Then AnalyzerResources.de.resx should contain this:

Name    | Value
--------|-----------
Message | Guten Tag!

Now when you build resource assemblies are automatically produced in language-specific sub folders:

- bin\Debug\
  - MyAnalyzer.dll
  - MyAnalyzer.pdb
  - de\
    - MyAnalyzer.resources.dll
  - ja\
    - MyAnalyzer.resources.dll

This layout is also what the CLR expects at runtime: for a given assembly Foo.dll, the localized resources should be in language-specific folders next to the assembly.

Packaging Resource Assemblies
=============================

NuGet
-----
NuGet packaging is straight-forward, as you simply need to duplicate the structure of the build output:
``` XML
<files>
  <file src="MyAnalyzer.dll" target="tools\analyzers" />
  <file src="de\MyAnalyzer.resources.dll" target="tools\analyzers\de" />
  <file src="ja\MyAnalyzer.resources.dll" target="tools\analyzers\ja" />
  ...
</files>
```

VSIX
----

Extension projects automatically incorporate satellite assemblies into the resulting .vsix file.

Testing Analyzer Localization
=============================

At the command line
-------------------
Once the NuGet package is installed, you can force the build to use localized resources by passing the `/preferreduilang` flag to csc.exe or vbc.exe:

    csc.exe /preferreduilang:de /t:library /analyzer:... Foo.cs
    
Alternatively, you can pass it to MSBuild as a property:

    msbuild /p:PreferredUILang=de
    
or set it as a property in the *proj file itself:

    <PreferredUILang>de</PreferredUILang>

In VS
-----

In VS analyzers use the selected UI language. This can be changed by selecting "Options..." from the "Tools" menu, and navigating to Environment -> International Settings.