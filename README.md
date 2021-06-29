# CardChoiceSpawnUniqueCardPatch
------------------

This is a utility mod which patches the erroneous `CardChoice.SpawnUniqueCard` method in the base game code.

Previously, the game would not properly check if the `allowMultiple` or `blacklistedcategories` fields of a card should have prevented it from being offered. Moreover, if it had done this, it would have been possible to crash the game since the method was called recursively with no garunteed exit.
