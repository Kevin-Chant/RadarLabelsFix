# RadarLabelsFix

Fixes the ship monitor (radar) showing one player's name while it is actually tracking a
different player. Since the teleporter beams whoever the monitor is *really* tracking, the
wrong labels made people teleport the wrong crewmate — often themselves.

## The bug

The game keeps a list of radar targets (players and radar boosters). Whenever a radar
booster is added, the game re-sorts that whole list, players included, by network ID. Some
code paths afterwards write player names by player slot number instead of by the player
the entry tracks, so after a re-sort the names can land on the wrong entries. It's most
likely after someone joins or rejoins mid-session in a lobby that has radar boosters around
(including modded scrap boosters).

## What this mod does

Whenever the radar list changes, player names get (re)assigned, or a label is about to be
read (switching the monitor, or `switch <name>` at the terminal), every player entry's label
is reset to the name of the player that entry actually tracks. If the label currently shown
on the monitor changes, the monitor text is refreshed too.

- Client-side only, no networking. Players without it simply keep the vanilla behaviour.
- Doesn't change which player is targeted, only the names shown for each entry.
- Logs each correction to the BepInEx log (`Radar target #N: label 'X' -> 'Y'`).

## Notes

Targets can also be listed in a different order on different players' games, which can make
one teleporter press beam several people. That is a separate issue; LobbyControl's
`sync_radar_names` setting makes the host align everyone's order when someone joins.
