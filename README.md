# CSFormatter

A VS Code extension that formats C# files with CodeMaid's cleaning rules, plus a few single-line
rules of its own.

Right click in a C# editor and choose **Format with CSFormatter**, or run it from the command
palette.

## What it does

| Step | Implementation |
|---|---|
| Indentation, spacing, brace placement | Roslyn `Formatter` (replaces VS's `SmartFormat`) |
| Sort `using` directives | Roslyn syntax tree (replaces VS's Remove and Sort command) |
| Reflow `//` and `///` comment blocks, including XML doc tags | **Ported** from CodeMaid |
| Blank line before members | Rewritten on the Roslyn syntax tree (was the EnvDTE CodeModel) |
| Trailing whitespace, blank lines at top/bottom, blank line after `{`, before `}`, before `else`/`catch`/`finally`, after attributes, consecutive blank lines | **Ported** from CodeMaid |
| Blank line before `case` statements | **Ported** from CodeMaid |
| Object, collection and array initializer items on one line | New rule; initializers holding a comment, directive or multi-line literal are left alone |
| Class, struct and record constructor parameter lists on one line | New rule, primary and normal constructors alike; method parameter lists are left alone |

Files with syntax errors are not restructured — the text rules still run, but Roslyn is not allowed
to reindent a file it cannot parse.

## What it does not do

- **Remove unused usings.** Needs a full compilation; only sorting is done.
- **Member reordering / region generation.** That is CodeMaid's *Reorganizing* feature, a separate
  and much larger job.
- **File headers, explicit access modifiers, region padding.** Skipped: they need a template or a
  code model, and nobody asked for them.
- **General re-wrapping.** There is no line-width limit anywhere. Existing line breaks are kept
  except in the two lists in the table above, and nothing is ever broken onto more lines:
  `class C { void M() { } }` stays on one line.
- **Method parameter lists and argument lists.** Only constructor parameter lists are joined;
  `void M(a, b)` over several lines stays that way, as do `new Foo(a, b)` and `: base(a, b)`.
- **Razor pages.** `.razor` and `.cshtml` files are not formatted at all; see `TODO.md`.

## Layout

```
src/CSFormatter.Core   the formatter (net8.0)
src/CSFormatter.Cli    stdin/stdout JSON wrapper around it (net8.0)
tests/CSFormatter.Tests xunit
extension/             the VS Code extension (TypeScript)
```

The CLI protocol is a single JSON object in and out, so there is no argument parsing to get wrong:

```
in:  {"text": "...", "options": {"indentSize": 4, "commentWrapColumn": 100}}
out: {"text": "..."}     or     {"error": "..."}
```

For manual use: `csformatter --file path/to/File.cs` formats in place with default options.

## Build

```powershell
dotnet test                                   # formatter tests
cd extension
npm install
npm run compile                               # extension JS -> out/
npm run build:cli                             # csformatter.exe -> bin/
```

Then press F5 in VS Code (with this folder open) to launch an Extension Development Host.

To install it properly, package and install the VSIX:

```powershell
cd extension
npx @vscode/vsce package
code --install-extension csformatter-0.2.1.vsix   # match the file vsce just wrote
```

`build:cli` publishes a **self-contained** single file for win-x64 (~87 MB, ~37 MB compressed in the
VSIX), so users need no .NET runtime. For a framework-dependent build (~25 MB) add
`--self-contained false` and require the **.NET 8 runtime** on the user's machine.
Other platforms: change `-r win-x64` to `-r linux-x64` or `-r osx-arm64` and drop the `.exe` check in
`extension/src/extension.ts`.

## Settings

| Setting | Default |
|---|---|
| `csformatter.formatComments` | `true` |
| `csformatter.commentWrapColumn` | `100` |
| `csformatter.sortUsings` | `true` |
| `csformatter.padMembers` | `true` |

Indentation is taken from the editor's own tab size and insert-spaces setting for the file. Every
other option in `FormatOptions` is on with its default and is not exposed — that includes the two
single-line rules (`singleLineInitializers`, `singleLineConstructorParameters`).

## Provenance

The comment formatter (`src/CSFormatter.Core/Comments`) and the whitespace rules
(`Cleaning/WhitespaceCleaner.cs`) are ports of CodeMaid by Steve Cadwallader, which is licensed
under LGPL-3.0. This project is therefore LGPL-3.0 as well — see `LICENSE.txt`. Files ported
wholesale carry `#nullable disable` to match the original's null assumptions.
