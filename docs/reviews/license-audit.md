# License and provenance audit

## Evidence reviewed

- `THIRD-PARTY-NOTICES.md` identifies `ajazz-control-center` as a GPL-3.0 protocol reference.
- The additional sources listed in `docs/upstream-comparison.md` were audited on 2026-07-24: Nibble commit `26568a9efe4003abc58b80e2739062d069cf1275` contains GPL-3.0-or-later terms; Aj179PStat commit `6c3be85598cd5f728530d729f7e4918a0b7b53f0` has no root license file.
- No root `LICENSE` file or explicit permission from the upstream copyright holder is present.
- The local repository history does not establish whether implementation code was independently written from protocol facts or derived from GPL-licensed source.
- A source-structure comparison was performed against upstream commit `03a29f7a90dd32057ca7cd0d6e6fa8f13d5400f3` on 2026-07-24. The upstream is a C++/Qt multi-device control centre; this repository is a C#/.NET tray monitor with distinct project structure and APIs. Both describe the same public hardware facts: the HID status poll, report identifier and response layout.

## What the comparison proves

The comparison supports that there is no same-language, file-for-file reuse. It does not prove clean-room authorship, because independently determining whether an implementation is a translation or adaptation requires an author provenance statement.

## Verdict

`BLOCKED` for public distribution. A correct project license cannot be selected from the available evidence without making an unsupported provenance assertion.

Do not label the project MIT, proprietary, or GPL solely to clear this gate. Before public publication, the maintainer must either document independent implementation from non-copyrightable protocol facts, obtain appropriate upstream permission, or adopt a license after confirming that all incorporated code is compatible with it.

## Follow-up required

1. Record the protocol-source provenance and any copied or adapted code.
2. Have the copyright holder or maintainer confirm that the implementation is independent, or identify the incorporated GPL code.
3. Have the copyright holder or maintainer select the project license based on that record.
4. Add the selected root `LICENSE`, update notices and re-run this audit.
