# Contributing to SoundSwap

Thanks for helping! SoundSwap is MIT-licensed and welcomes issues and pull requests.

## Set up
```powershell
git clone https://github.com/Stevehunn/soundswap.git
cd soundswap
dotnet build SoundSwap.sln
dotnet test tests/SoundSwap.Core.Tests
dotnet run --project src/SoundSwap.App
```
You need Windows 10/11 and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## Where things live
See [docs/articles/architecture.md](docs/articles/architecture.md). Rule of thumb: logic goes in `SoundSwap.Core`
(no UI, no Windows API, unit-tested); Windows-specific audio in `SoundSwap.Audio`; UI in `SoundSwap.App`.

## Conventions
- Every **public** type and member of `SoundSwap.Core` and `SoundSwap.Audio` needs an XML doc comment
  (`dotnet build` warns with CS1591 otherwise). Explain *why* and the contract, not the obvious.
- Add or update a test for any change in `SoundSwap.Core`.
- Keep UI strings out of code: add a key to **all** `Localization/Lang/*.json` files (`en` is the fallback).
- Keep the PR focused; describe the problem and how you checked the fix.

## Translations
Copy `src/SoundSwap.App/Localization/Lang/en.json` to `xx.json`, translate the values, then add one line to
`Localizer.Languages`. Right-to-left languages are handled in `Localizer.FlowDirection`.

## Releasing (maintainers)
See [docs/articles/releasing.md](docs/articles/releasing.md).
