# Security Fixes Summary — 2026-05-28

## Problem

Hardcoded secrets were committed to `appsettings.json` with no `.gitignore` in place, exposing:

- **OpenAI API key** (`sk-proj-...`) — full production key in plaintext
- **JWT signing key** (`RoxXiz14mh7a...`) — used to sign all auth tokens
- **bin/obj build artifacts** — tracked in git, containing copies of the secrets

## Changes Made

### 1. Replaced secrets with placeholders

**File:** `RetroMask.API/appsettings.json` (and `bin/Debug/net8.0/appsettings.json`)

| Setting | Before | After |
|---------|--------|-------|
| `JwtSettings:SecretKey` | Real key | `YOUR_JWT_SECRET_KEY_MIN_32_CHARS` |
| `OpenAI:ApiKey` | Real key (`sk-proj-...`) | `YOUR_OPENAI_API_KEY` |

### 2. Created `.gitignore`

Added a root `.gitignore` covering:

- `bin/`, `obj/`, `Debug/`, `Release/` — build outputs
- `.vs/`, `.idea/`, `*.user` — IDE files
- `appsettings.Development.json`, `appsettings.Local.json` — local config overrides
- `uploads/` — runtime-generated files

### 3. Updated `README.md`

- Added a security warning about never committing secrets
- Restructured the Configuration section to start with `dotnet user-secrets init`
- All sensitive config now uses the .NET User Secrets approach

## Remaining Action Items

- [ ] **Rotate the OpenAI API key** at [platform.openai.com](https://platform.openai.com) — treat it as compromised
- [ ] **Generate a new JWT secret** — the old one is in git history
- [ ] **Remove tracked build artifacts from git** — run: `git rm -r --cached **/bin/ **/obj/`
- [ ] **Scrub git history** — use [BFG Repo Cleaner](https://rtyley.github.io/bfg-repo-cleaner/) to remove secrets from past commits if the repo was ever shared or public
