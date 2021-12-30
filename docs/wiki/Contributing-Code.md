### Contributing Code

Before submitting a feature or substantial code contribution, please discuss it with the team and ensure it follows the product [roadmap](Roadmap.md). The team rigorously reviews and tests all code submissions. The submissions must meet an extremely high bar for quality, design, and roadmap appropriateness.

The Roslyn project is a member of the [.NET Foundation](https://github.com/orgs/dotnet) and follows the same [developer guide](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md).  The team enforces this by regularly running the [.NET code formatter tool](https://github.com/dotnet/codeformatter) on the code base.  Contributors should ensure they follow these guidelines when making submissions.  

For now, the team has set the following limits on pull requests:

- Contributions beyond the level of a bug fix must be discussed with the team first, or they will likely be declined. As our process matures and our experience grows, the team expects to take larger contributions.
- Only contributions against the main branch will be accepted. Authors submitting pull requests that target experimental feature branches or release branches will likely be asked target their pull request at the main branch.
- Pull requests that do not merge easily with the tip of the main branch will be declined. The author will be asked to merge with tip and update the pull request.
- Submissions must meet functional and performance expectations, including scenarios for which the team doesn't yet have open source tests. This means you may be asked to fix and resubmit your pull request against a new open test case if it fails one of these tests.
- Submissions must follow the [.editorconfig](http://editorconfig.org/) settings for each directory. For the most part, these follow the rules stated in the [.NET Foundation Coding Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) with the exception that most Roslyn projects prefer to use 'var' everywhere.
- Contributors must sign the [.NET CLA](https://cla.dotnetfoundation.org/)

When you are ready to proceed with making a change, get set up to [build](Building-Testing-and-Debugging.md) the code and familiarize yourself with our workflow and our coding conventions. These two blogs posts on contributing code to open source projects are good too: Open Source Contribution Etiquette by Miguel de Icaza and Don’t “Push” Your Pull Requests by Ilya Grigorik.

You must sign a [Contributor License Agreement (CLA)](http://cla.dotnetfoundation.org) before submitting your pull request. To complete the CLA, submit a request via the form and electronically sign the CLA when you receive the email containing the link to the document. You need to complete the CLA only once to cover all .NET Foundation projects.

### Developer Workflow

1. Work item is assigned to a developer during the triage process
2. Both Roslyn and external contributors are expected to do their work in a local fork and submit code for consideration via a pull request.
3. When the pull request process deems the change ready it will be merged directly into the tree. 

### Getting started coding in Visual Studio

See our getting started guide [here](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md).

### Creating New Issues

Please follow these guidelines when creating new issues in the issue tracker:

- Use a descriptive title that identifies the issue to be addressed or the requested feature. For example when describing an issue where the compiler is not behaving as expected, write your bug title in terms of what the compiler should do rather than what it is doing – “C# compiler should report CS1234 when Xyz is used in Abcd.”
- Do not set any bug fields other than Impact.
- Specify a detailed description of the issue or requested feature.
- For bug reports, please also:
    - Describe the expected behavior and the actual behavior. If it is not self-evident such as in the case of a crash, provide an explanation for why the expected behavior is expected.
    - Provide example code that reproduces the issue.
    - Specify any relevant exception messages and stack traces.
- Subscribe to notifications for the created issue in case there are any follow up questions.

### Coding Conventions

- Use the coding style outlined in the [.NET Foundation Coding Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)
- Use plain code to validate parameters at public boundaries. Do not use Contracts or magic helpers.

    ```csharp
    if (argument == null)
    {
        throw new ArgumentNullException(nameof(argument));
    }
    ```

- Use `Debug.Assert()` for checks not needed in retail builds. Always include a “message” string in your assert to identify failure conditions. Add assertions to document assumptions on non-local program state or parameter values, e.g. “At this point in parsing the scanner should have been advanced to a ‘.’ token by the caller”.
- Avoid allocations in compiler hot paths:
    - Avoid LINQ.
    - Avoid using foreach over collections that do not have a struct enumerator.
    - Consider using an object pool. There are many usages of object pools in the compiler to see an example.

### Code Formatter

The Roslyn team regularly uses the [.NET code formatter tool](https://github.com/dotnet/codeformatter) to ensure the code base maintains a consistent style over time.  The specific options we pass to this tool are the following:

- `/nounicode`: In general we follow this rule of not having unicode characters embedded in string literals. However there are a few cases where this is needed to verify compiler behavior hence this option is disabled for now. 
- `/copyright`: The default copyright is MIT.  Roslyn is released under Apache2 hence we need to override this option. 

### Visual Basic Conventions and Rules

For all of the C# guidelines which have analogs in Visual Basic, the team applies the spirit of the guideline to Visual Basic. Guidelines surrounding spacing, indentation, parameter names, and the use of named parameters are all generally applicable to Visual Basic. ‘Dim’ statements should also follow the guidelines for the use of ‘var’ in C#. Specific to Visual Basic, field names should begin with ‘m_’ or ‘_’. And the team prefers that all field declarations be placed at the beginning of a type definition. The Visual Studio members dropdown does not show fields in VB. Having them at the beginning of the type aids in navigation.

IDE features should generally be made for both C# and VB.  The exceptions are:

1. If the feature has no appropriate VB analog. For example 'patterns' are C#-only, so specific features around patterns do not generally need equivalent VB work.
2. The feature is prohibitively expensive to also do for VB.  In this case, ask the team if it's acceptable to not do a VB version and a decision can be made.  In general though, writing features to work for both C# and VB is usually only a little more expensive than just writing it to work on a single language (especially if the multi-language case is considered up front), so it should normally be done.

When creating IDE features that work for both C# and VB, attempt to share as much code as is reasonable.  There are numerous examples and existing components to make that possible.  If help is needed, reach out to the team for advice.

### Tips 'n' Tricks

Our team finds using [this enhanced source view](http://sourceroslyn.io/) of Roslyn helpful when developing.

Many team members can be reached at <https://gitter.im/dotnet/roslyn>, <https://gitter.im/dotnet/csharplang>, and <https://discord.gg/csharp> (#roslyn).
