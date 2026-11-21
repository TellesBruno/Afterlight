# Afterlight localization

The game supports English (`en`) and Brazilian Portuguese (`pt-BR`). English is the default. The Language/Linguagem button in the main menu and pause menu opens a selection modal. The choice is stored in `PlayerPrefs` under `afterlight.language`, independently of saved progress. The modal's buttons use the same `GUIButtonFeedback` colors and selection mark as the other menu buttons.

## Text tables

- `Assets/Resources/Localization/en.json` is the English source table.
- `Assets/Resources/Localization/pt-BR.json` contains matching Portuguese entries.
- Both files use an `entries` array with stable `key` and `text` values. Every key should exist in both files.

Quest and clue keys use their existing save IDs, for example `quest.<id>.title` and `clue.<id>.description`. Dialog options use `NPCData.id` plus each option's stable `localizationId`. Do not change save IDs to edit translations.

The current scene and prefab text is bound at runtime through the English table's `source` or `sourcePrefix` values. `sourcePrefix` is reserved for existing long, wrapped text such as the introduction and tutorial. When adding new static text, attach `LocalizedText` with an explicit key in the Inspector, or add a source mapping to the English table. Text generated in C# should use `LocalizationService.Get` and refresh on `LocalizationService.LanguageChanged`.

If a translation is missing, the English table is used. For a new quest, clue, or NPC that has no table entry yet, the serialized English field is used as the fallback.

## Review checklist

1. Keep the two tables' key sets equal.
2. Check language changes while a quest, clue, or dialog panel is open.
3. Check text wrapping in both languages at the smallest supported resolution.
4. Check new text for accented glyphs in its TextMesh Pro font asset.
5. Keep editor-only object names, IDs, and save data independent of displayed text.
