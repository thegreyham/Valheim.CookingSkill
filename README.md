# Valheim.CookingSkill


## Your Cooking Skill will increase when you cook food from either:
- Cooking Station
- Cauldron
- Fermenter

## Buffs Granted:
- Food Health Buff based on Cooking Level. (Default .5% p/level)
- Food Stamina Buff based on Cooking Level. (Default .5% p/level)
- Food Duration Buff based on Cooking Level. (Default 1% p/level)
- Fermenter Duration Reduction based on Cooking Level. (Default .66% p/level)

## How xp is Gained:

- Cooking Station Grants 1 xp by default
- 1/4 of Cooking Station xp is granted when you add an item to the cooking station
- 3/4 of Cooking Station xp is granted when you have successfully cooked the item
- No xp is awarded when you create charcoal.
- Cauldron Grants 2 xp by default
- Fermenter grants 3 xp when you add an item to the fermenter
- Fermenter grants 3xp when you remove a fermented item as the fermenter takes a long time to ferment.
- xp amounts can be configured in the config.


## Buff Example at Cooking Skill Level 20:
- Food that grants you 20 Health now grants 22 Health at lv 20.
- Food that grants 30 Stamina now grants 33 stamina at lv 20.
- Food that lasts 300 seconds will now last 360 seconds at lv 20.
- Fermenter reduced to 2084 seconds from 2400 at lv 20.

## Installation
Ensure you have pipakin's SkillInjector Mod https://www.nexusmods.com/valheim/mods/341 as this utilizes that.
Download via Vortex or extract .dll file and assets folder to BepInEx/plugins 

## Configuration
XP gains and Food buffs can be adjusted in the config file.
To disable the buffs set the relevant Food Effects Multiplier to 0.
