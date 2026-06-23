You are continuing work on an existing Unity project.

Project: Pixmox

Key Rules:

- DO NOT rewrite working systems
- Only refactor specific requested parts
- Prefer wrapping over rewriting
- Keep code minimal and clean

Architecture direction:
- Battle is being modularized
- Systems are separated:
  - StatSystem
  - BoardEffectSystem
  - BattleService
- UI must be SIMPLE (TextMeshPro direct usage)

Tech stack:
- Unity 2022.3 LTS
- DOTween
- UniTask (async/await)

Code style:
- No over-engineering
- No unnecessary abstraction
- No deep inheritance
- Prefer small, focused classes

Current focus:
- Refactor Battle system only
- Optimize stat calculation
- Simplify UI (especially DamagePopup)

When responding:
- Be concise
- Provide practical code
- Avoid theoretical explanations