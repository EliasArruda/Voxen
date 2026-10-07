# GitHub installation packages

The release workflow accepts a semantic version and builds Linux x64 and Windows x64 packages. It validates native playback on both platforms, compiles Windows Setup with Inno Setup, and publishes archives, Setup and SHA-256 checksums to GitHub Releases. Linux includes a per-user application-menu installer.

Local validation on 2026-10-07: Linux archive contents, isolated installation with a spaced path, and shell syntax passed. Windows Setup compilation requires the GitHub runner.

Publication is blocked by GitHub HTTP 500 responses on Git push, Git Data API, Contents API and release creation. No release was published. The prepared local branch is `feat/github-installers`.

Next: push this branch, open and attach a PR, wait for Linux/Windows CI, merge, then dispatch `release.yml` with version `0.1.0` and verify release assets.
