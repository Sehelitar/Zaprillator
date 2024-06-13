# Zaprillator

You don't know what to do with your freshly acquired ZapGun? You have that one friend that dies ALL THE TIME?

**Worry no more, I have THE solution!**

I present to you a new *defibrillator* prototype : the **Zaprillator** !<br/>
It's pretty simple to use :
1. Aim to the body of your very dead friend.
2. **SHOOT!** *ZAAAPPPPP!* BRRRRRR!
3. Your very dead friend is now very alive!

<span style="color:red;font-weight:bold;">Note : All players in the lobby MUST have this mod installed.</span> (or face the consequences of your rebellion)
<br/>Note 2 / Known bug : Sometimes the hitbox can be a bit tricky to hit. Try to move the corpse if you have trouble reviving your fellow crewmates.

## Mods Options
These options can be modified in the config file or via r2modman.

| Option                 | Type      | Description                                                                                                                                                                                      |
|:-----------------------|:----------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **RequiresFullCharge** | `boolean` | Define if a ZapGun has to be fully charged to revive a dead player.<br/>Attempting to revive a dead player without a full charge will drain the remaining energy anyway.<br/>Default is `false`. |
| **RelativeHealth**     | `boolean` | Define if the restored health is relative to the ZapGun charge.<br/>Default is `false`, meaning health is always fully restored.                                                                 |

## Compilation

In order to compile this project, you need to modify the `Zaprillator.csproj` file and change the `LethalCompanyPath` macro to target your local game install.