# ValleyTalk Plus Architecture

## Recommendation

Use **custom SMAPI session state on the normal Stardrop Saloon map** as the core Saturday-social implementation.

This is the best fit because the feature is:

- repeatable every Saturday
- free-roam instead of strongly scripted
- centered on long LLM conversation
- easier to maintain than a single event chain or festival map

## Tradeoffs

### Event system

Good for authored scenes, but a poor fit for this feature because events are linear and restrictive.

### Festival-style temporary map

Good for staging, but it pushes the design toward a bespoke minigame and adds a lot of authoring overhead.

### Custom SMAPI state

Best fit for this mod. It lets the saloon remain a normal place while layering in:

- attendance rolls
- compact nightly NPC state
- room vibe
- session memory
- ValleyTalk prompt context injection

## Design rules

- Keep the simulation shallow.
- Give NPCs strong baseline personality.
- Store only the state the LLM can use well.
- Let relationship tension modify behavior instead of disabling it.

## Core models

- `NpcProfile`
- `NpcNightState`
- `RoomMoodState`
- `AttendanceRoll`
- `InteractionRecord`
- `DrinkOrder`
- `MealOrder`
- `SocialSession`

## Service boundaries

- `SaturdaySocialManager`
- `AttendanceService`
- `SessionStateService`
- `NpcProfileService`
- `NpcNightStateService`
- `IntoxicationService`
- `CommerceService`
- `ConsequenceEngine`
- `SessionMemoryService`
- `RoomMoodService`
- `ValleyTalkContextBridge`

## Persistence split

Persistent:

- config
- baseline NPC profiles

Session-only:

- attendance
- nightly state
- room vibe
- current-night interaction memory

## Phase roadmap

### Phase 1

- Saturday trigger
- attendance roll
- NPC placement plan
- ValleyTalk context bridge

### Phase 2

- drinks and meals
- buzz levels
- room mood shifts

### Phase 3

- dance invitations
- arguments
- private conversations
- jealousy/rivalry expression

### Phase 4

- balancing
- prompt refinement
- integration polish
