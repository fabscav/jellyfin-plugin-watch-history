# WatchHistoryRating - Ready-to-publish release

This repository contains a Jellyfin plugin that shows users' watch history and allows ratings (1-10).
This release-ready package includes a GitHub Actions CI workflow that builds the plugin and creates a zip artifact
suitable for publishing as a GitHub Release asset.

## How the CI works
- The workflow `.github/workflows/release.yml` runs on tag pushes matching `v*.*.*` (for example `v1.0.0`).
- It sets up .NET 6, restores, builds, publishes the project, then packages the published files together with `plugin.json` and the `UI/` folder into a zip file `WatchHistoryRating-<tag>.zip`.
- On tag triggers the workflow creates a GitHub Release and attaches the zip file as a release asset.

## Manual build steps (locally)
1. Ensure .NET 6 SDK is installed.
2. From the repository root run:
   ```bash
   ./build_and_package.sh
   ```
   The script will produce `WatchHistoryRating-YYYY.MM.DD.zip`.

## Publishing on GitHub
1. Commit and push all changes.
2. Tag a release: `git tag v1.0.0 && git push --tags` (adjust version as needed).
3. The GitHub Actions workflow will run and create a release with the compiled zip attached.

## Notes & Caveats
- The project uses embedded UI files. Make sure your `.csproj` includes the UI files as embedded resources if desired.
- The published output contains compiled DLL(s) and dependencies; the plugin zip is laid out with a top-level folder `package/WatchHistoryRating/` that contains the files to be copied into the Jellyfin `plugins/WatchHistoryRating/` directory on the server.
- For Windows runners or other runtimes, adjust the `dotnet publish` args in the workflow if you want RID-specific builds.

## Next steps (optional)
- Add a `release` GitHub Action to automatically bump the version in `plugin.json` from the tag name.
- Add unit tests and a test matrix for multiple .NET versions.
- Migrate rating storage to SQLite for production use.
