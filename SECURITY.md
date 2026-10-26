# Security policy

## Reporting a vulnerability
Please **do not open a public issue**. Use GitHub's private reporting
(*Security* tab → *Report a vulnerability*) on https://github.com/Stevehunn/soundswap.
You will get an answer as soon as possible.

## What is in scope
- The **updater** (downloads an installer from GitHub and runs it): host allow-list, size and SHA-256 verification.
- Anything that lets another program or website make SoundSwap run code or change settings.

## How updates are protected
- Only `https` URLs on `github.com`, `*.github.com` and `*.githubusercontent.com` are contacted or opened, including after redirects.
- The downloaded file must match the size announced by GitHub and, when GitHub publishes one, its SHA-256 digest.
- Installers are built by the public GitHub Actions workflow `release.yml` from a tagged commit.
- Installers are **not yet code-signed**; Windows SmartScreen may warn. Signing is planned.
