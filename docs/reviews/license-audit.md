# License and provenance audit

## Evidence reviewed

- `THIRD-PARTY-NOTICES.md` identifies `ajazz-control-center` as a GPL-3.0 protocol reference.
- The additional sources listed in `docs/upstream-comparison.md` were audited on 2026-07-24: Nibble commit `26568a9efe4003abc58b80e2739062d069cf1275` contains GPL-3.0-or-later terms; Aj179PStat commit `6c3be85598cd5f728530d729f7e4918a0b7b53f0` has no root license file.
- GPL-3.0-or-later was selected by the maintainer on 2026-07-24 and is declared in the root `LICENSE` and build metadata.
- The local repository history does not establish whether implementation code was independently written from protocol facts or derived from GPL-licensed source.
- A source-structure comparison was performed against upstream commit `03a29f7a90dd32057ca7cd0d6e6fa8f13d5400f3` on 2026-07-24. The upstream is a C++/Qt multi-device control centre; this repository is a C#/.NET tray monitor with distinct project structure and APIs. Both describe the same public hardware facts: the HID status poll, report identifier and response layout.

## What the comparison proves

The comparison supports that there is no same-language, file-for-file reuse. It does not prove clean-room authorship, because independently determining whether an implementation is a translation or adaptation requires an author provenance statement.

## Verdict

`PASS WITH GPL OBLIGATIONS`. GPL-3.0-or-later is compatible with the identified GPL sources and does not require an unsupported clean-room assertion.

Do not relabel the project as MIT or proprietary without a new provenance audit. Public distributions must retain the GPL license, corresponding source and applicable notices.

## Follow-up required

1. Retain the protocol-source provenance and identify any future copied or adapted code.
2. Distribute complete corresponding source and the GPL notice with each public binary release.
3. Re-run this audit before changing the license or adding new upstream code.
