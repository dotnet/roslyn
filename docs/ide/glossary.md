# Glossary of IDE terminolgy

* **IntelliSense** - A blanket term for a collection of features, including **completion**,
**signature help**, **quick info**, and **smart tags**.

* **Completion** (aka **autocomplete** or **statement completion**) - The dropdown of possible
items to insert into the editor at the current location.  Generally triggered automatically when
typing or pressing `Ctrl+J` or `Ctrl+Space`.  Can be in one of two modes, either *Completion mode*
or *Suggestion mode*.  In *Completion mode* (the default) where typing any character that isn't
part of the currently selected entry "commits" the current entry and inserts its text. *Suggestion
mode* completion shows a "builder", and the current item is "soft selected" (outlined, instead of
fully selected).  In suggestion mode typing a character that isn't in the list doesn't commit the
item, in order to commit you have to do it explicitly with tab/enter/double click.  You can toggle
between *completion mode* and *suggestion mode* by pressing `Ctrl+Alt+Space`. In cases where it's
ambiguous whether you are inserting a new identifier or referring to an existing one, the IDE
generally forces *suggestion mode* so that you don't accidentally commit an existing item when
trying to declare a new one (for example, in a context where a delegate is expected, suggestion
mode is forced to allow typing a lambda parameter name).

* **Completion builder** - When in *suggestion mode*, completion shows an extra entry at the top
of the list containing a watermark with what sort of declaration can appear if no text is in the
editor, or else an entry matching the currently typed text.  This is intended to represent that
completion will not commit to another item in the but will instead preserve your current text
unless commit is forced (with tab/enter).

* **Signature Help** (aka **Parameter Help**) - The tooltip showing the possible overloads that
shows when typing a method call or the generic parameters of a type/method).  Normally invoked
with `Ctrl+K, Ctrl+P` or `Ctrl+Shift+Space`.

* **Quick Info** - The tooltip that appears when hovering over an identifier showing information
about it.  Can also be invoked with `Ctrl+K, Ctrl+I`.

* **Smart Tag** - The small blue or red square that appeared under an identifier in VS 2005 to
VS 2013 indicating that the IDE could help perform an action.  Replaced with Light bulbs in VS 2015.

* **Light Bulbs** - An indication that the IDE can help with some action in the form of a light
bulb icon in the left margin, or in the Quick Info tooltip.  Normally invoked with `Ctrl+.`.  Menu
items are generically called `Code Actions`, which come in two flavors - either `Quick Fixes` for
diagnostics, or `Refactorings`.  Quick Fixes show the lightbulb whenever the cursor is on the line
with the diagnostic, whereas refactorings are shown on demand when `Ctrl+.` is invoked.

* **Margins** - Reserved space around the core editor window.  Most margins appear on the left
side of the editor, for example: Line number, track changes, outlining, breakpoints, lightbulb, etc.

* **Outlining** - The feature that allows collapsing regions of code.

* **Classification** - The process by which syntax highlighting happens.  In Roslyn, there are two
types of classification - *Syntactic* Classification happens synchronously (though potentially
based on a stale syntax tree), and *Semantic* Classification, which happens on a slight delay and
is responsible for coloring things like type names, `var`, `dynamic`, etc.

* **Squiggles** - The wavy underlines under identifiers that have some sort of diagnostic.
Normally red for errrors, green for warnings, purple for Edit and Continue rude edits.

* **Navigation Bar** - The three dropdowns at the top of the code editor that allow you to select
the project context, type, and member.

* **Tagging** - A general term in the VS Editor API to allow extensions like Roslyn to return
collections of spans to the editor.  Many IDE features are built on top of tagging: classification,
outlining, squiggles, line separators, etc.

* **Adornments** - A general term in the VS Editor API for adding visual elements to the editor,
often in response to the existince of tags.  Many of the adornments seen for C# and VB are
actually implemented in the Core Editor (squiggles, classification, outlining, etc), but a few are
in Roslyn (suggestions, line separators).
