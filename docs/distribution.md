# GitHub installation packages

The release workflow builds Linux x64 and Windows x64 packages, validates native playback, compiles Windows Setup with Inno Setup, and publishes archives and SHA-256 checksums.

To publish a version, run **Publish installers** in GitHub Actions from `main` and enter a semantic version such as `0.1.0`. Publishing occurs only after both platform jobs succeed. Each version must be new.

Linux packages include a per-user application-menu installer. Windows Setup installs per user and supports uninstallation through Windows Settings. User library data stays separate from application files.
