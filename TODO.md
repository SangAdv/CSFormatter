# TODO

Known issues, most damaging first, then planned work. No dates; delete a line when it is done.

## Bugs

- [ ] **Formatter rewrites string literal contents.** `Cleaning/WhitespaceCleaner.cs` and
      `Comments/CommentCleaner.cs` work on raw text, so blank lines, trailing spaces and `///`
      lines inside `"""` and `@"..."` literals are changed — the program keeps compiling but its
      data changes. Make both passes skip token spans taken from the parse tree.
- [ ] **A change typed while the formatter runs is lost.** `extension/src/extension.ts` reads the
      document, awaits the CLI, then replaces the whole document with the stale result. Capture
      `document.version` and bail or re-run if it changed before `applyEdit`.
- [ ] **Using sorting is silently skipped** when a directive (`#nullable enable`, `#region`) sits
      in front of the first `using`. `Cleaning/UsingSorter.cs` checks trivia of the first directive
      and gives up on the whole file.
- [ ] **Using sorting detaches comments from their using.** `UsingSorter.cs` swaps trivia by
      position, so `// about Zeta` can end up above `using System.IO;`. Keep each directive's own
      comment trivia and normalize only the trailing newline of the last one.
- [ ] **`using static` sorts in the wrong group.** `UsingSorter.KindGroup` runs after
      `SystemGroup`, so `using static System.Math;` lands above ordinary non-System usings,
      contradicting the class remark.

## Small fixes

- [ ] `--file` mode (`src/CSFormatter.Cli/Program.cs`) reads with BOM detection and writes UTF-8
      without a BOM, changing the encoding of UTF-16 or BOM'd files; I/O exceptions there also
      escape as a stack trace instead of a message.

## Tests

- [ ] Nothing covers the CLI JSON protocol (camelCase options, error path, exit code).
- [ ] Nothing covers using sorting with comments, directives, aliases or `using static`.
- [ ] Nothing covers string literals, raw strings, or nested initializers.

## Features

- [ ] **Format Razor pages (`.razor`, `.cshtml`).** Today the extension ignores every document whose
      language id is not `csharp`, and the CLI parses its whole input as C#, so a page is untouched.
      - Route the Razor language ids (`aspnetcorerazor`, `razor`, `cshtml`) in
        `extension/src/extension.ts`, then add a `CodeFormatter.FormatRazor` that the CLI selects.
      - Roslyn cannot parse markup. Start with formatting only the C# regions — block `@{ }`,
        `@code` and `@functions` bodies, directive attributes — and leave markup alone. The fuller
        path is to take the generated C# from `Microsoft.AspNetCore.Razor.Language` and map spans
        back, which is a new dependency and much more work.
      - Never run `WhitespaceCleaner` / `CommentCleaner` over the whole page: they would rewrite
        HTML attributes, JS, CSS and `<pre>` content. Restrict them to the C# regions, and keep
        them away from `<!-- -->` and `@* *@`.
      - Tests: a page with `@code`, inline `@{ }`, a `<pre>` block and a JS template literal, all
        byte-identical outside the C# regions.
