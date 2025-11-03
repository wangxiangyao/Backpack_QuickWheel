# Repository Guidelines

Use this guide as the quick reference for extending the Backpack QuickWheel mod while keeping the repo consistent and shippable.

## Project Structure & Module Organization
- `src/` holds the production C# code; each system (e.g., `AttachmentSystem/`, `ShortcutSystem/`, `VoiceWheelSystem/`) owns its domain logic, while `ModBehaviour.cs` orchestrates startup.
- `Textures/` and `VoiceWheelSystem/Audio/` contain embedded assets referenced by the project file.
- `GameSource/` stores decompiled Duckov game assemblies for reference only; the `.csproj` excludes it from builds.
- `Documents/DevelopmentNotes.md` tracks design decisions and TODOs; sync any significant architectural changes there.

## Build, Test, and Development Commands
- `dotnet restore src/Backpack_QuickWheel.csproj` – ensures Harmony and other NuGet dependencies are ready before the first build.
- `dotnet build src/Backpack_QuickWheel.csproj -c Release /p:DuckovPath="D:\steam\steamapps\common\Escape from Duckov"` – compiles to `$(DuckovPath)\Duckov_Data\Mods\Backpack_QuickWheel`; override the path per your local install.
- For in-game smoke tests, launch Escape from Duckov with mod loading enabled after a successful build; the output folder is hot-reloaded on game restart.

## Coding Style & Naming Conventions
- Target framework is `netstandard2.1` with nullable reference types on; treat warnings as issues to fix.
- Follow existing C# conventions: PascalCase for types and public members, `_camelCase` for private fields, and static readonly fields in PascalCase.
- Keep classes focused by domain folder; share utilities through `ReflectionExtensions.cs` or new files under `src/`.
- When touching UI logs, keep bilingual logging intact unless you replace it with localized strings via `Localization/`.

## Testing Guidelines
- Automated tests are not yet in place; cover new features with scoped manual checks in-game (loading, equipping attachments, and tag export flows).
- When adding regression fixes, document manual repro steps in the PR description and consider adding lightweight validation helpers under `AttachmentSystem/` if reuse emerges.

## Commit & Pull Request Guidelines
- Use Conventional Commit prefixes observed in history (`feat:`, `fix:`, etc.), keeping messages concise; include Chinese context if it clarifies gameplay impact.
- PRs should summarize gameplay changes, list manual test scenarios, attach before/after screenshots for UI work, and link the relevant Trello or issue tracker item when available.
- Request review from maintainers responsible for the touched subsystem (Attachment, Backpack, Shortcut, VoiceWheel) and highlight any Harmony patch risks.
