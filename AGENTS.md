# AGENTS.md

Guidance for AI coding agents (and humans) working in this repository.

## What this repository is

This is **`TobiStr/csharpier-editorconfig`**, a long-lived fork of
[belav/csharpier](https://github.com/belav/csharpier). It is an **extension** of
upstream CSharpier, not a contribution branch:

- It tracks upstream and is periodically updated **from** upstream.
- It is **never merged back** into upstream. Do not prepare, suggest, or open pull
  requests against `belav/csharpier` from this repo.
- Its reason to exist is a set of additional `.editorconfig`-driven formatting options
  (brace/newline placement, trailing commas, etc.) that upstream deliberately does not
  offer. That extended functionality must survive every upstream sync.

Read the intro of `README.md` and the "EditorConfig" section of `docs/Configuration.md`
for the user-facing description of the extension.

## Git remotes and sync model

| Remote     | URL                                                  | Role                            |
|------------|------------------------------------------------------|---------------------------------|
| `origin`   | `https://github.com/TobiStr/csharpier-editorconfig`  | This fork. All work lands here. |
| `upstream` | `https://github.com/belav/csharpier`                 | Read-only source of updates.    |

Rules:

1. **Direction is one-way: `upstream/main` → `origin/main`.** Never push to `upstream`.
2. **Sync by merging, not rebasing.** `git fetch upstream && git merge upstream/main`
   on a branch off `main` (e.g. `sync/upstream-YYYY-MM`), then PR into `main`.
   Rebasing the fork's history onto upstream rewrites published commits and is not
   wanted.
3. **When resolving merge conflicts, upstream wins for everything that is not a fork
   feature; the fork wins for the fork features.** If an upstream change touches a
   printer the fork modified (see "Fork-specific code map"), re-apply the fork's
   behaviour on top of the new upstream code rather than dropping either side.
4. **Do not "clean up" or refactor upstream code.** Every unrelated diff from upstream
   becomes a future merge conflict. Keep the fork's footprint as small and as local as
   possible; prefer additive changes (new options, new branches in a printer) over
   restructuring.
5. **Do not rebrand.** Namespaces, package ids, tool names, project names and file
   layout stay as upstream has them. Only `README.md`, `Nuget/README.md` and the
   Configuration docs mention the fork explicitly.
6. Sponsor/Discord/branding references inherited from upstream are intentionally left
   in place.

After a sync, run the full verification below and make sure the fork's option tests in
`Src/CSharpier.Tests/OptionsProviderTests.cs` still pass and the option plumbing
(see the checklist) is still complete end to end. Then do a quick end-to-end run of the
built CLI against a sample with the fork options set — see "Build, test, and verify".

Handy command to see the current fork delta against upstream (should list only the
files in the code map below):

```bash
git diff --stat upstream/main
```

### Sync log

| Date       | Upstream commit | Notes                                                                   |
|------------|-----------------|-------------------------------------------------------------------------|
| 2025-11-11 | `92366899`      | Fork created (`feature/editorconfig`, PR #1)                            |
| 2026-08-25 | `c8ac0cb8`      | 113 upstream commits; see "Lessons from past syncs" for what had to change |

### Lessons from past syncs

Things upstream changed that required fork adjustments — expect more of the same:

- **`PrintingContext` → `CSharpPrintingContext`** (+ `BasePrintingContext`,
  `XmlPrintingContext`). Upstream dropped the `Options` object; the fork re-added a
  slimmer `CSharpPrintingContext.Options` holding only the fork options.
- **`PrinterOptions` constructor** gained `XmlWhitespaceSensitivity`; `Formatter` became
  get-only.
- **PublicApiAnalyzer** (`RS0016`/`RS0017`): every public symbol in `CSharpier.Core`
  must be listed in `Src/CSharpier.Core/PublicAPI.Unshipped.txt`. No comment lines in
  that file.
- **NUnit → TUnit + AwesomeAssertions**: `[TestCase(x)]` became `[Test]` +
  `[Arguments(x)]`; the options-provider test harness uses relative paths
  (`"./.editorconfig"`, `CreateProviderAndGetOptionsFor(".", "./test.cs")`).
- **Stricter analyzers**: `var` everywhere (`IDE0007`), `Enum.GetValues<T>()`
  (`CA2263`), ordinal string comparison (`CA1862`), `ToLowerInvariant()`
  (`CA1304`/`CA1311`).
- **.NET 11 SDK / Roslyn** applies the fork's `.editorconfig` brace settings to source
  generator output and reports `IDE0055` there regardless of severity settings, so the
  fork section in `.editorconfig` is followed by `[**/obj/**.cs] generated_code = true`.

## Fork-specific code map

These are the places that differ from upstream. Treat them as the fork's protected
surface during syncs and be deliberate when touching them.

**Option definition and plumbing**

- `Src/CSharpier.Core/BraceNewLine.cs` – `[Flags] enum BraceNewLine` (`Types`,
  `Methods`, `ControlBlocks`, `All`). Fork-only file.
- `Src/CSharpier.Core/CodeFormatterOptions.cs` – public API options (`NewLineBefore*`,
  `NewLineBetweenQueryExpressionClauses`, `UsePrettierStyleTrailingCommas`) and their
  mapping in `ToPrinterOptions()`.
- `Src/CSharpier.Core/PublicAPI.Unshipped.txt` – declares the fork's public symbols
  for the PublicApiAnalyzer.
- `Src/CSharpier.Core/PrinterOptions.cs` – internal printer options (same set).
- `Src/CSharpier.Core/CSharp/SyntaxPrinter/CSharpPrintingContext.cs` –
  `Options` property + nested `PrintingContextOptions` (with `From(PrinterOptions)`),
  defaults reproduce upstream output. This is what the printers read.
- `Src/CSharpier.Core/CSharp/CSharpFormatter.cs` – sets `Options =
  CSharpPrintingContext.PrintingContextOptions.From(printerOptions)` on both
  `CSharpPrintingContext` instances.

**`.editorconfig` parsing (CLI)**

- `Src/CSharpier.Cli/EditorConfig/Section.cs` – one property per `.editorconfig` key.
- `Src/CSharpier.Cli/EditorConfig/EditorConfigSections.cs` – parses/validates the
  values (`ResolvedConfiguration`, `ConvertToBraceNewLine`) and applies them to
  `PrinterOptions`.

**Printers that honour the options**
(under `Src/CSharpier.Core/CSharp/SyntaxPrinter/SyntaxNodePrinters/` unless noted)

- `Block.cs` – `csharp_new_line_before_open_brace`
- `BaseTypeDeclaration.cs` – open brace for types
- `IfStatement.cs` – `csharp_new_line_before_else`
- `TryStatement.cs` – `csharp_new_line_before_catch` / `_finally`
- `InitializerExpression.cs` – `csharp_new_line_before_members_in_object_initializers`
- `AnonymousObjectCreationExpression.cs` – `csharp_new_line_before_members_in_anonymous_types`
- `QueryBody.cs` – `csharp_new_line_between_query_expression_clauses`
- `../TrailingComma.cs` – `csharpier_use_prettier_style_trailing_commas`

**Tests and docs**

- `Src/CSharpier.Tests/OptionsProviderTests.cs` – `Should_Support_EditorConfig_*` tests
  for the fork options (parsing).
- `Src/CSharpier.Tests/CSharp/ForkFormattingOptionsTests.cs` – fork-owned file with
  formatting tests for the fork options (actual printer output). New fork tests belong
  here rather than in an upstream test file, so upstream merges do not conflict.
- `docs/Configuration.md` and `Src/Website/docs/Configuration.md` – **keep these two
  files identical**; the EditorConfig section documents the fork options.
- `README.md`, `Nuget/README.md` – fork intro and option list.
- `.editorconfig` (repo root) – fork section at the **end of the file** (dogfoods the
  options) followed by the `generated_code` section for `obj/`.

## Supported fork options (keep in sync with the docs)

| `.editorconfig` key                                       | Core option                                | Default |
|-----------------------------------------------------------|--------------------------------------------|---------|
| `csharp_new_line_before_open_brace`                       | `NewLineBeforeOpenBrace` (`BraceNewLine`)  | `All`   |
| `csharp_new_line_before_else`                             | `NewLineBeforeElse`                        | `true`  |
| `csharp_new_line_before_catch`                            | `NewLineBeforeCatch`                       | `true`  |
| `csharp_new_line_before_finally`                          | `NewLineBeforeFinally`                     | `true`  |
| `csharp_new_line_before_members_in_object_initializers`   | `NewLineBeforeMembersInObjectInitializers` | `null`  |
| `csharp_new_line_before_members_in_anonymous_types`       | `NewLineBeforeMembersInAnonymousTypes`     | `null`  |
| `csharp_new_line_between_query_expression_clauses`        | `NewLineBetweenQueryExpressionClauses`     | `null`  |
| `csharpier_use_prettier_style_trailing_commas`            | `UsePrettierStyleTrailingCommas`           | `true`  |
| `csharpier_include_generated`                             | `IncludeGenerated`                         | `false` |
| `csharpier_trim_initial_lines`                            | `TrimInitialLines`                         | `true`  |

Defaults are chosen so that **with no `.editorconfig` settings the output is identical
to upstream CSharpier**. Never change a default in a way that alters upstream-equivalent
output; the extension must be opt-in.

Naming convention: keys that mirror Roslyn/IDE formatting options use the official
`csharp_*` names from
<https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options>;
keys that are CSharpier-specific use the `csharpier_*` prefix.

## Adding or changing a fork option — checklist

Every option must be wired through all layers, otherwise it silently does nothing:

1. `CodeFormatterOptions` (public API) → `ToPrinterOptions()` mapping, and the
   matching `.get`/`.init` lines in `PublicAPI.Unshipped.txt`
2. `PrinterOptions` (internal)
3. `CSharpPrintingContext.PrintingContextOptions` + its `From(PrinterOptions)` mapping
4. `Section.cs` (raw key) → `EditorConfigSections.cs` (parse + validate + apply)
5. The printer(s) in `SyntaxNodePrinters/` that consume it — guard the new branch so
   the default still produces upstream output
6. `OptionsProviderTests.cs` – a `Should_Support_EditorConfig_<Option>` test
   (`[Test]` + `[Arguments(...)]`, relative `./` paths)
7. Formatting coverage: a test in `ForkFormattingOptionsTests.cs` that formats a
   snippet with the option on and off (the `TestFiles/` formatting tests always run
   with default options, so they cannot exercise fork options)
8. Docs: `docs/Configuration.md` **and** `Src/Website/docs/Configuration.md`,
   the `README.md` option list, and the table above

Fork options are `.editorconfig`-only by design; `.csharpierrc` support is not required.

## Build, test, and verify

Requires the .NET SDK pinned in `global.json` (currently **11.0.100-preview.6**,
`rollForward: latestFeature`). Target framework is `net11.0` (Core additionally
`net10.0` + `netstandard2.0`), nullable enabled, analyzers on with
`EnforceCodeStyleInBuild`. If the pinned SDK is not installed machine-wide, a
user-local install works:

```powershell
# one-time; no admin needed
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Version <version from global.json> -InstallDir "$env:USERPROFILE\.dotnet" -NoPath
# per shell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
```

```bash
# build (CI builds with warnings as errors — do the same before finishing)
dotnet build CSharpier.slnx -c Release -p:TreatWarningsAsErrors=true

# all tests (TUnit on Microsoft.Testing.Platform)
dotnet test CSharpier.slnx -c Release

# formatting check of the repo itself (tool from .config/dotnet-tools.json)
dotnet tool restore
dotnet csharpier check .

# end-to-end check of the fork options with the freshly built CLI
dotnet Src/CSharpier.Cli/bin/Release/net11.0/CSharpier.dll format <dir with .editorconfig + sample .cs>
```

Known environment quirk: `CSharpierProcess.Tests.CliTests.Format_Should_Handle_UnauthorizedAccessException_In_Subdirectory`
(upstream test) fails with `IdentityNotMappedException` on non-English Windows because
it hard-codes the account name `"Everyone"`. It is unrelated to the fork.

CI (`.github/workflows/ValidatePullRequest.yml`) runs: tests, no-warnings build,
Docker playground build, `csharpier check .`, a TODO check
(`ValidatePullRequest_CheckTodos.ps1` fails on `TODO <branch-name>` markers left in the
branch — the upstream convention for "must fix before merge" notes), and MSBuild
integration tests on Linux + Windows. Match that locally before declaring work done.

Publishing workflows (`publish_nuget.yml`, `publish_beta.yml`, `deploy_*.yml`) are
inherited from upstream and are not the fork's release path; do not trigger or "fix"
them unless explicitly asked.

## Testing conventions (inherited from upstream)

- Test framework is **TUnit** with **AwesomeAssertions** (`[Test]`, `[Arguments]`,
  `.Should()`).
- Formatting behaviour is tested with `.test` files in
  `Src/CSharpier.Tests/FormattingTests/TestFiles/{cs,csx,xml}`. A source generator
  creates one test per file; the formatter output must be idempotent and match the
  `.expected` file if present. Add such a file for any printer change.
- `Src/CSharpier.Tests/OptionsProviderTests.cs` covers `.csharpierrc` /
  `.editorconfig` resolution — this is where fork option parsing is tested.
- `Src/CSharpierProcess.Tests` and `Src/CSharpier.Tests/CommandLineFormatterTests` are
  the near-end-to-end tests.
- `Scripts/RunLinuxTests.ps1` and `Tests/MsBuild/Run.ps1` are the heavier suites.
- Use the playground (`Src/CSharpier.Playground`, `dotnet watch run`) to inspect the
  AST and Doc tree when debugging a printer.

See `CONTRIBUTING.md` for upstream's fuller development notes.

## Coding conventions

- Follow upstream's style exactly; the repo formats itself with CSharpier and the root
  `.editorconfig`. Run `dotnet csharpier format <changed files>` before building.
- Keep fork changes minimal and additive. Prefer a guarded `if (context.Options.X)`
  branch inside an existing printer over rewriting the printer.
- Put a one-line `// Fork (csharpier-editorconfig): ...` comment on fork-only code
  paths where the intent is not obvious from the option name, so that future syncs can
  recognise and preserve them.
- No new dependencies unless unavoidable; versions are centrally managed in
  `Directory.Packages.props`.
- Mark must-fix-before-merge notes as `TODO <branch-name>`; CI fails while any remain,
  so they cannot be forgotten.
- Don't edit `CHANGELOG.md` for fork work; it is upstream's changelog and is taken
  wholesale on sync.

## Do / Don't summary

**Do**
- Keep `.editorconfig`-driven behaviour opt-in and upstream-identical by default.
- Wire new options through every layer in the checklist and test them.
- Keep the two `Configuration.md` files and the README option list in sync.
- Merge from `upstream/main`; re-apply fork behaviour on conflicts; update the
  sync log above.

**Don't**
- Open PRs or push anything to `belav/csharpier`.
- Rebase, squash, or rewrite `main` history.
- Refactor, rename, or reformat upstream code beyond what the task requires.
- Remove or weaken a fork option to make an upstream merge easier.
- Change package ids, tool names, namespaces, or project layout.
