# How do I translate it?

Mimir's own text, the default unlock notice, "New abilities unlocked" and the console
command, is in `Translations/English/mimir.json`. Skill and ability names, and abilities'
own unlock notices, come from the mods that add them, in their own translation files.

1. Make a folder beside `English` named with the game's own word for your language:
   `German`, `French`, `Spanish`, and so on.
2. Copy `mimir.json` into it and translate the values.
3. Launch the game. Jotunn loads the file on its own.

Keep the keys exactly as they are, and keep any `$1` or `$2`: those are filled in with a
name or a number, and can move anywhere in the sentence. A key you leave out shows in
English.
