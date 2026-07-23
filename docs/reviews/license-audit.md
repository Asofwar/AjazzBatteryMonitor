# License and provenance audit

## Evidence reviewed

- `THIRD-PARTY-NOTICES.md` identifies `ajazz-control-center` as a GPL-3.0 protocol reference.
- No root `LICENSE` file or explicit permission from the upstream copyright holder is present.
- The local repository history does not establish whether implementation code was independently written from protocol facts or derived from GPL-licensed source.

## Verdict

`BLOCKED` for public distribution. A correct project license cannot be selected from the available evidence without making an unsupported provenance assertion.

Do not label the project MIT, proprietary, or GPL solely to clear this gate. Before public publication, the maintainer must either document independent implementation from non-copyrightable protocol facts, obtain appropriate upstream permission, or adopt a license after confirming that all incorporated code is compatible with it.

## Follow-up required

1. Record the protocol-source provenance and any copied or adapted code.
2. Have the copyright holder or maintainer select the project license based on that record.
3. Add the selected root `LICENSE`, update notices and re-run this audit.
