# Next plan

Current state:

- v8 tester kit is released and stable for RaceMenu overlay/progress and outfit enforcement.
- Known issue: the overlay logo may not render on some setups.

Next practical work (recommended order):

1) **Character-first join flow**
   - Character selection UI on launch (5 slots, 10 for admins).
   - First-time rules acknowledgement gate.
   - Queue handling when server is full.

2) **Persistence correctness (v0 requirements)**
   - Per-character appearance/race + inventory + position.
   - Org membership.
   - Integer gold with an auditable ledger.

3) **Reserved slots + deletion policy**
   - Soft delete.
   - Weekly hard delete tooling.
   - Reserved slots enforcement.

4) **Spawn region + starter kit polish**
   - Multiple `startPoints` for the Vokun RP region.
   - Starter kit idempotency and better UX.

Reference specs live in the SkyMP fork repo:

- `skymp-SkyV\docs\skyv\ROADMAP.md`
- `skymp-SkyV\docs\skyv\VOKUN_RP_V0.md`

