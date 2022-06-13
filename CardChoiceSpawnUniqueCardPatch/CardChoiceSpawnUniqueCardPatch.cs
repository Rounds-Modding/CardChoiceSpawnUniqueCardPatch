using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll, and UnityEngine.AssetBundleModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using ModdingUtils.Utils;
using UnboundLib; // requires unboundlib
using UnboundLib.Cards;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace CardChoiceSpawnUniqueCardPatch
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "0.1.8")]
    [BepInProcess("Rounds.exe")]
    public class CardChoiceSpawnUniqueCardPatch : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            CustomCard.BuildCard<NullCard>(cardInfo => CardChoiceSpawnUniqueCardPatch.NullCard = cardInfo);
        }

        private const string ModId = "pykess.rounds.plugins.cardchoicespawnuniquecardpatch";

        private const string ModName = "CardChoiceSpawnUniqueCardPatch";

        internal static CardInfo NullCard;
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

            CardInfo validCard = null;
            if (CardChoice.instance.cards.Length > 0)
            {
                validCard = Cards.instance.GetRandomCardWithCondition(player, null, null, null, null, null, null, null, CardChoicePatchSpawnUniqueCard.GetCondition(__instance));
            }

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

                // return a blank card
                GameObject gameObject = (GameObject)typeof(CardChoice).InvokeMember("Spawn",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, __instance, new object[] { CardChoiceSpawnUniqueCardPatch.NullCard.gameObject, pos, rot });
                gameObject.GetComponent<CardInfo>().sourceCard = CardChoiceSpawnUniqueCardPatch.NullCard.GetComponent<CardInfo>();
                gameObject.GetComponentInChildren<DamagableEvent>().GetComponent<Collider2D>().enabled = false;
                __result = gameObject;
            }

            return false; // do not run the original method (BAD IDEA)
        }
        private static Func<CardInfo, Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, bool> GetCondition(CardChoice instance)
        {
            return (card, player, gun, gunAmmo, data, health, gravity, block, stats) => (CardChoicePatchSpawnUniqueCard.ModifiedBaseCondition(instance)(card, player) && CardChoicePatchSpawnUniqueCard.CorrectedCondition(instance)(card, player));
        }
        private static Func<CardInfo, Player, bool> CorrectedCondition(CardChoice instance)
        {
            return (card, player) => (Cards.instance.PlayerIsAllowedCard(player, card));
        }
        private static Func<CardInfo, Player, bool> ModifiedBaseCondition(CardChoice instance)
        {
            return (card, player) =>
            {
                List<GameObject> spawnedCards = (List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue();
                for (int i = 0; i < spawnedCards.Count; i++)
                {
                    // slightly modified condition that if the card has the CanDrawMultipleCategory, then its okay that its a duplicate
                    bool flag = !card.categories.Contains(CustomCategories.CustomCardCategories.CanDrawMultipleCategory) && spawnedCards[i].GetComponent<CardInfo>().gameObject.name.Replace("(Clone)","") == card.gameObject.name;
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
                            if (!component4.allowMultiple && card.gameObject.name == component4.gameObject.name)
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
    public class NullCard : CustomCard
    {
        public const string cardName = "  ";
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.sourceCard = CardChoiceSpawnUniqueCardPatch.NullCard;
            cardInfo.categories = cardInfo.categories.ToList().Concat(new List<CardCategory>() { CustomCategories.CustomCardCategories.CanDrawMultipleCategory }).ToArray();
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            this.gameObject.GetComponent<CardInfo>().sourceCard = CardChoiceSpawnUniqueCardPatch.NullCard;
        }
        public override void OnRemoveCard()
        {
        }

        protected override string GetTitle()
        {
            return cardName;
        }
        protected override string GetDescription()
        {
            return "";
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }

        protected override CardInfoStat[] GetStats()
        {
            return null;
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
        }
        public override bool GetEnabled()
        {
            return false;
        }
        public override string GetModName()
        {
            return "NULL";
        }
    }
}