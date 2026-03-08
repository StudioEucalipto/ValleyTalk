# ValleyTalk Plus

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-30319-orange)](https://www.nexusmods.com/stardewvalley/mods/30319)
[![Validation](https://github.com/StudioEucalipto/ValleyTalk/actions/workflows/validate.yml/badge.svg)](https://github.com/StudioEucalipto/ValleyTalk/actions/workflows/validate.yml)

`ValleyTalk Plus` is a fork of `ValleyTalk` focused on lore-faithful long-form social roleplay in Stardew Valley.

The current design target is a recurring Saturday-night social session in the Stardrop Saloon, where the player can roleplay extended conversation with villagers through ValleyTalk-powered dialogue.

This branch is the transition from the original ValleyTalk foundation into a more session-driven social-roleplay architecture.

## Direction

- ValleyTalk remains the dialogue engine.
- Saturday-night saloon social play is the main roleplay loop.
- NPC behavior should be driven by compact baseline traits plus compact nightly state.
- Mature social realism is allowed, but it stays grounded and character-appropriate.
- The simulation should stay shallow enough to fit Stardew Valley's tone and keep prompts compact.

See [docs/Architecture.md](/Users/leon/Library/CloudStorage/Dropbox/Development Projects/Stardew Valley/ValleyTalk Plus/docs/Architecture.md) for the current implementation plan.

## Current foundation

- existing ValleyTalk dialogue generation stack
- new `Social` scaffolding for Saturday-session orchestration
- seeded adult NPC saloon profile data
- GitHub-side repository validation without requiring local tooling on this Mac

## Development

### Building from Source

```bash
# Build locally if you have the game and .NET installed
dotnet build src/ValleyTalk.csproj
```

### Tooling note

This Mac does not have Stardew Valley or the .NET SDK installed, and this repo is being kept intentionally light on local dependencies.

That means:

- repo edits, branching, and pushing work normally
- GitHub can run lightweight validation workflows
- full SMAPI compile verification still requires access to Stardew Valley assemblies, which stock GitHub runners do not provide

### Project Structure

```
ValleyTalk Plus/
├── src/                    # Main mod source code
│   ├── llms/              # AI provider implementations
│   ├── config/            # Files related to mod configuration
│   ├── Generation/        # Dialogue generation logic
│   ├── Social/            # Saturday social-session foundation
│   ├── Patches/           # Harmony patches
│   ├── Interop/           # API for interaction with other mods
│   └── UI/                # User interface components
├── ContentPack/           # Base content pack
│   └── assets/            # Character bios and prompts
├── docs/                  # Design and implementation notes
└── Extensions/            # Mod extensions (SVE support)
```

## License

This project is available under LGPL v3.

## Support

- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/dandm1/ValleyTalk/issues)
- **Nexus Mods**: Community discussion on the [mod page](https://www.nexusmods.com/stardewvalley/mods/30319)

## Acknowledgments

- Built with [SMAPI](https://smapi.io/) by Pathoschild
- Uses Harmony for runtime patching
- Stardew Valley by ConcernedApe
- Community translations and feedback
