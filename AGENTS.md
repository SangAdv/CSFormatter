# AGENTS.md

A VS Code extension that formats C# files with CodeMaid's cleaning rules plus a few single-line
rules of its own. A .NET 8 formatter (`CSFormatter.Core`) does the work, a tiny CLI
(`CSFormatter.Cli`) wraps it in JSON, and the TypeScript extension (`extension/`) ships the
published CLI inside the VSIX.

## Layout

| Path | What it is |
|---|---|
| `src/CSFormatter.Core` | the formatter, `CodeFormatter.Format(string, FormatOptions)` |
| `src/CSFormatter.Cli` | stdin/stdout JSON wrapper; `--file <path>` formats in place |
| `tests/CSFormatter.Tests` | xunit, one test per rule plus idempotency |
| `extension/src/extension.ts` | command, options, spawns `extension/bin/csformatter.exe` |

## Commands

```powershell
dotnet test                                   # from the repo root
dotnet run --project src/CSFormatter.Cli -- --file path/to/File.cs
'{"text":"class C{}","options":{"newLine":"\n"}}' | dotnet run --project src/CSFormatter.Cli
```

```powershell
cd extension
npm install
npm run compile        # tsc -> out/
npm run build:cli      # Release self-contained win-x64 -> extension/bin (~90 MB, slow)
```

`build:cli` must be re-run after any Core change, or the installed extension keeps the old rules.
F5 in VS Code launches an Extension Development Host (`.vscode/launch.json`).

Release: bump `version` in `extension/package.json`, then `npm run compile`, `npm run build:cli`,
`npx --yes @vscode/vsce@latest package`, `code --install-extension csformatter-<version>.vsix`.
Tell the user to reload their window. Keep the install command in `README.md` in sync.

## How a format run is ordered

`CodeFormatter.Format` — the order matters, and the passes mix syntax trees with plain text:

1. newlines normalized to `\n` (restored at the end from the dominant style or `FormatOptions.NewLine`)
2. parse; if there is a syntax error, **all** tree passes are skipped and only text passes run
3. `UsingSorter` — Roslyn tree
4. `FormatCode` — Roslyn `Formatter` (indentation, spacing, brace placement)
5. `WhitespaceCleaner` — regexes over the text (CodeMaid port)
6. `ListFlattener` — joins initializer items and constructor parameter lists onto one line
7. `BlankLinePadder` — re-parses, because step 5 moved lines
8. `CommentCleaner` — reflows `//` and `///` blocks
9. `Join` — trims top/bottom blank lines, inserts the final newline, restores the newline style

Anything added after step 5 sees text, not syntax. Never let a text rule rewrite what is inside a
string literal; `ListFlattener` is the reference for doing this token-aware.

## Conventions

- Every rule is an option on `FormatOptions`, on by default, and must survive the `IsIdempotent`
  test. Only some options are exposed in `extension/package.json`; the rest stay at their defaults.
- Options cross the CLI as camelCase JSON (`PropertyNameCaseInsensitive`), so a renamed property
  silently stops applying in the extension.
- `ListFlattener` skips lists with a comment, a preprocessor directive or a multi-line literal, and
  never touches method, delegate or lambda parameter lists.
- Add a test for each rule, and check real files through the CLI: unit tests pass while passes
  still corrupt each other (spans, trailing trivia, blank lines).
- Ported CodeMaid files carry `#nullable disable` and a `Ported from CodeMaid ...` remark. The
  project is LGPL-3.0; do not paste code from another license or from a decompiled assembly.
- The user calls constructor parameter lists "initialisers".
- Three docs, three audiences: `README.md` is user-facing (what it does, build, settings),
  `TODO.md` is the backlog, this file is for agents. When a rule, option, command or version
  changes, update all three and keep the `csformatter-<version>.vsix` example in step with
  `extension/package.json`.

## Gotchas

- `--file` writes UTF-8 without a BOM, so a UTF-16 or BOM'd file changes encoding.
- The extension replaces the whole document in one edit, so a change typed while the CLI runs is lost
  (the document version is not checked).
- Using sorting is skipped entirely when a directive (e.g. `#nullable enable`) sits in front of the
  first `using`.
- `bin/`, `out/` and `*.vsix` are gitignored, so a fresh clone has no formatter executable and no
  compiled extension until `npm run build:cli` and `npm run compile` run once.
