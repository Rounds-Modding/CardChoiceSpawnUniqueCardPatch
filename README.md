# CardChoiceSpawnUniqueCardPatch
------------------

This is a utility mod which patches the erroneous `CardChoice.SpawnUniqueCard` method in the base game code.

Previously, the game would not properly check if the `allowMultiple` or `blacklistedcategories` fields of a card should have prevented it from being offered. Moreover, if it had done this, it would have been possible to crash the game since the method was called recursively with no garunteed exit.

This mod also adds the ability for custom cards (made with Unbound) to use CardCategories, even across mods. Simply use:

```
using using CardChoiceSpawnUniqueCardPatch.CustomCategories;

...

cardInfo.categories = new CardCategory[] { CustomCardCategories.instance.CardCategory("MySpecialCategory") };

```

then, if another card in another mod wants to be exclusive or incompatible with the example card above, it should simply use:

```
using using CardChoiceSpawnUniqueCardPatch.CustomCategories;

...

cardInfo.blacklistedCategories = new CardCategory[] { CustomCardCategories.instance.CardCategory("MySpecialCategory") };

```

### Patch Notes

- 1.6: performance improvements
