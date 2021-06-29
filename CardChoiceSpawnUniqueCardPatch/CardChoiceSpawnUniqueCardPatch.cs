using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll, and UnityEngine.AssetBundleModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace CardChoiceSpawnUniqueCardPatch
{
    [BepInPlugin(ModId, ModName, "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class CardChoiceSpawnUniqueCardPatch : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {

        }

        private const string ModId = "pykess.rounds.plugins.cardchoicespawnuniquecardpatch";

        private const string ModName = "CardChoiceSpawnUniqueCardPatch";
    }
    // stolen from PCE
    public sealed class Cards
    {
        // singleton design
        public static readonly Cards instance = new Cards();
        private Cards()
        {
            Cards instance = this;
        }
        public bool CardIsUniqueFromCards(CardInfo card, CardInfo[] cards)
        {
            bool unique = true;

            foreach (CardInfo otherCard in cards)
            {
                if (card.cardName == otherCard.cardName)
                {
                    unique = false;
                }
            }

            return unique;
        }

        public bool CardDoesNotConflictWithCards(CardInfo card, CardInfo[] cards)
        {
            bool conflicts = false;

            foreach (CardInfo otherCard in cards)
            {
                if (card.categories.Intersect(otherCard.blacklistedCategories).Any())
                {
                    conflicts = true;
                }
            }

            return conflicts;
        }

        public bool PlayerIsAllowedCard(Player player, CardInfo card)
        {
            bool blacklisted = false;

            foreach (CardInfo currentCard in player.data.currentCards)
            {
                if (card.categories.Intersect(currentCard.blacklistedCategories).Any())
                {
                    blacklisted = true;
                }
            }

            return !blacklisted && (card.allowMultiple || !player.data.currentCards.Where(cardinfo => cardinfo.name == card.name).Any());

        }
        public CardInfo GetRandomCardWithCondition(CardChoice cardChoice, Player player, Func<CardInfo, Player, bool> condition, int maxattempts = 1000)
        {

            CardInfo card = ((GameObject)typeof(CardChoice).InvokeMember("GetRanomCard",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, cardChoice, new object[] { })).GetComponent<CardInfo>();

            int i = 0;

            // draw a random card until it's an uncommon or the maximum number of attempts was reached
            while (!condition(card, player) && i < maxattempts)
            {
                card = ((GameObject)typeof(CardChoice).InvokeMember("GetRanomCard",
                           BindingFlags.Instance | BindingFlags.InvokeMethod |
                           BindingFlags.NonPublic, null, cardChoice, new object[] { })).GetComponent<CardInfo>();
                i++;
            }

            if (!condition(card, player))
            {
                return null;
            }
            else
            {
                return card;
            }

        }

        public int GetCardID(CardInfo card)
        {
            return Array.IndexOf(global::CardChoice.instance.cards, card);
        }
        public CardInfo GetCardWithID(int cardID)
        {
            return global::CardChoice.instance.cards[cardID];
        }
    }

    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "SpawnUniqueCard")]
    class CardChoicePatchSpawnUniqueCard
    {
        private static bool Prefix(ref GameObject __result, CardChoice __instance, Vector3 pos, Quaternion rot)
        {
            Player player;
            if ((PickerType)Traverse.Create(__instance).Field("pickerType").GetValue() == PickerType.Team)
            {
                player = PlayerManager.instance.GetPlayersInTeam(__instance.pickrID)[0];
            }
            else
            {
                player = PlayerManager.instance.players[__instance.pickrID];
            }

            CardInfo validCard = Cards.instance.GetRandomCardWithCondition(__instance, player, CardChoicePatchSpawnUniqueCard.GetCondition(__instance));

            if (validCard != null)
            {
                GameObject gameObject = (GameObject)typeof(CardChoice).InvokeMember("Spawn",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, __instance, new object[] { validCard.gameObject, pos, rot });
                gameObject.GetComponent<CardInfo>().sourceCard = validCard.GetComponent<CardInfo>();
                gameObject.GetComponentInChildren<DamagableEvent>().GetComponent<Collider2D>().enabled = false;

                __result = gameObject;
            }
            else
            {
                // there are no valid cards left - this is an extremely unlikely scenario, only achievable if most of the cards have been disabled

                // if any valid cards were found, just return one of those, even though its a duplicate

                
                List<GameObject> spawnedCards = (List<GameObject>)Traverse.Create(__instance).Field("spawnedCards").GetValue();

                GameObject card;

                if (spawnedCards.Count > 0)
                {
                    CardInfo cardInfo = Cards.instance.GetCardWithID(Cards.instance.GetCardID(spawnedCards[0].GetComponent<CardInfo>().sourceCard));
                    card = cardInfo.gameObject;
                }

                // if no valid cards could be found, then just get any card at all because that's better than crashing the game
                else
                {
                    card = ((GameObject)typeof(CardChoice).InvokeMember("GetRanomCard",
                            BindingFlags.Instance | BindingFlags.InvokeMethod |
                            BindingFlags.NonPublic, null, __instance, new object[] { }));
                }
                GameObject gameObject = (GameObject)typeof(CardChoice).InvokeMember("Spawn",
                            BindingFlags.Instance | BindingFlags.InvokeMethod |
                            BindingFlags.NonPublic, null, __instance, new object[] { card.gameObject, pos, rot });
                gameObject.GetComponent<CardInfo>().sourceCard = card.GetComponent<CardInfo>();
                gameObject.GetComponentInChildren<DamagableEvent>().GetComponent<Collider2D>().enabled = false;


                __result = gameObject;

            }

            return false; // do not run the original method (BAD IDEA)
        }
        private static Func<CardInfo, Player, bool> GetCondition(CardChoice instance)
        {
            return (card, player) => (CardChoicePatchSpawnUniqueCard.BaseCondition(instance)(card, player) && CardChoicePatchSpawnUniqueCard.CorrectedCondition(instance)(card, player));
        }
        private static Func<CardInfo, Player, bool> CorrectedCondition(CardChoice instance)
        {
            return (card, player) => (Cards.instance.PlayerIsAllowedCard(player, card));
        }
        private static Func<CardInfo, Player, bool> BaseCondition(CardChoice instance)
        {
            return (card, player) =>
            {
                List<GameObject> spawnedCards = (List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue();
                for (int i = 0; i < spawnedCards.Count; i++)
                {
                    bool flag = spawnedCards[i].GetComponent<CardInfo>().cardName == card.cardName;
                    if (instance.pickrID != -1)
                    {
                        Holdable holdable = player.data.GetComponent<Holding>().holdable;
                        if (holdable)
                        {
                            Gun component2 = holdable.GetComponent<Gun>();
                            Gun component3 = card.GetComponent<Gun>();
                            if (component3 && component2 && component3.lockGunToDefault && component2.lockGunToDefault)
                            {
                                flag = true;
                            }
                        }
                        for (int j = 0; j < player.data.currentCards.Count; j++)
                        {
                            CardInfo component4 = player.data.currentCards[j].GetComponent<CardInfo>();
                            for (int k = 0; k < component4.blacklistedCategories.Length; k++)
                            {
                                for (int l = 0; l < card.categories.Length; l++)
                                {
                                    if (card.categories[l] == component4.blacklistedCategories[k])
                                    {
                                        flag = true;
                                    }
                                }
                            }
                            if (!component4.allowMultiple && card.cardName == component4.cardName)
                            {
                                flag = true;
                            }
                        }
                    }
                    if (flag)
                    {
                        return false;
                    }
                }
                return true;
            };
        }
    }
}