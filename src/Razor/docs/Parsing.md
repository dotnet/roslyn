# Introduction

This doc aims to keep a list of decisions that have been made around how razor syntax is parsed into a syntax tree.

## Whitespace handling

Whitespace handling is currently differently parsed depending on the chosen emit strategy (runtime or design time).

When in DesignTime whitespace between a CSharp and HTML node is generally parsed as an HTML node, whereas in Runtime the whitespace is parsed as part of a CSharp node. This ensures that at runtime arbitrary whitespace isn't incorrectly emitted as part of the HTML, but in design time the editor will only identify the actual code portion as being CSharp.

An example of this can be seen here: <https://github.com/dotnet/razor/blob/9f10012f7bbee0c17be26de048aee3e5adbc6c80/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/Language/Legacy/CSharpCodeParser.cs#L743>

As part of the transition to use only runtime code generation, we had to make some subtle changes to the parsing of whitespace to ensure that the existing behavior in the editor continues to function as before.

Specifically we changed the parsing of trailing whitespace of razor code block directives (i.e. `@code`, `@function` and `@section`). Previously the whitespace was attached to a meta node that included the closing `}`

Using `^` to indicate whitespace:

```csharp
@code {
    // code
}^^^

```

This would previously be conceptually parsed as something like:

```text
CSharpCode
    RazorDirective
        CSharpTransition
        RazorDirectiveBody
            RazorMetaCode
                Identifier
            CSharpCode
                ...
            RazorMetaCode
                Literal: }
                Literal: ^^^\r\n
```

Thus when looking at the length of the RazorDirective, it includes the `^^^\r\n`. This causes issues with editor features like code folding. The user only want to fold the directive, not the directive and the following new line. (see <https://github.com/dotnet/razor/issues/10358>)

Instead, we now break the trailing whitespace into its own RazorMetaCode node, which is not a part of the directive itself. Conceptually something like:

```text
CSharpCode
    RazorDirective
        CSharpTransition
        RazorDirectiveBody
            RazorMetaCode
                Identifier
            CSharpCode
                ...
            RazorMetaCode
                Literal: }
    RazorMetaCode
        Literal: ^^^\r\n
```

In this way we keep the whitespace as belonging to the overall CSharpCode node, but don't make it part of the directive itself, ensuring the editor sees the correct length for the directive.

We apply a very similar fix to `@using` directives, to ensure that the newline is treated as metacode of the overall block, rather than being a part of the `using` itself.