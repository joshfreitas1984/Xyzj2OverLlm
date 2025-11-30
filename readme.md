# Next Stop - Jianghu 2 English Patch

Install guide:

Extract the [Latest Release](https://github.com/joshfreitas1984/Xyzj2OverLlm/releases) into your `<Game Folder>` folder where 下一站江湖Ⅱ.exe is.

# Contacting us

You can join us here: [Discord](https://discord.gg/sqXd5ceBWT)

## Pre-requesites
The pre-reqs contains:
  - BepInEx
  - Unstripped Game DLLs
  - Configuration for BepinEx
  
### Name Changer

If you want to change your name because of an old playthrough with Autotranslator or you simply hate the name.

Press Keypad__Period aka . next to your numpad 0 key. This will bring up the UI with your current name. Type in what you want and hit Save.

### Custom Text Resizer

Pressing Keypad_- you will add the resizer under your current cursor to `BepInEx/resizers/zzAddedResizers.yaml` you can then tweak the properties on the resizer to your liking and reload them.

Pressing Keypad_+ will reload your resizers if something looks screwy.

You can use Keypad_* to add all text items on screen. Be warned it will grab a lot!

You can use * inside the path to indicate a wildcard (ie: match zero or more characters where the * is). This will help you do one resizer for lots of stuff.

Please note I include zzAddedResizers.yaml in the patch. So if  you want to keep them move them to another yaml file when your done. Please submit any resizers you think make sense!

Here are all the things you can do: (Not including it will keep the controls defaults)

```yaml
- path: "GameStart/GameUIRoot/*/FormRoot" # Gets everything that has a FormRoot in it starting with GameStart/GameUIRoot
  sampleText: "Commission"    # Dumped text so you know what the path was for
  idealFontSize: 30           # The font size you want
  allowWordWrap: false        # Allows word wrapping on component
  allowAutoSizing: false      # Lets the font change sizes depending on width given by dev
  AllowLeftTrimText: true     # Allow the text to be left trimmed
  adjustX: 0                  # Positive or negative number to adjust left and right 
  adjustY: 0                  # Positive or negative number to adjust up and down
  adjustWidth: 0              # Positive or negative number to adjust allowed width of control
  adjustHeight: 0             # Positive or negative number to adjust allowed height of control
  minFontSize: 0              # Min Font size when autosizing
  maxFontSize: 0              # Max Font size when autosizing
  fontPercentage: 0.70        # Percentage of font size to use (replaces max/min font size if set above 0)
  alignment: Center           # Control alignment on screen for TextMeshProGUI
  overflow: Overflow          # Overflow mode for TextMeshProGUI
```