# How do I translate it?

Mimir's own text is in `Translations/English/mimir.json`. That covers the default unlock
message, the Skill Abilities page, stat names and the console command. Skill and ability
names and descriptions come from the mods that add them, so you translate those in each
mod's own files.

1. Make a folder next to `English`, named with the game's word for your language:
   `German`, `French`, `Spanish`, and so on.
2. Copy `mimir.json` into it and translate the text.
3. Launch the game. The file loads on its own.

Only translate the text on the right of each line, and leave the names on the left as
they are. Keep every `$1`: it is replaced by a name or a number, and you can move it
anywhere in the sentence. Anything you leave out shows in English.
