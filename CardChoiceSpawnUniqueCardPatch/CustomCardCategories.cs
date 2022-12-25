using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll, and UnityEngine.AssetBundleModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using ModdingUtils.Utils;
using UnboundLib;
using UnboundLib.Utils;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace CardChoiceSpawnUniqueCardPatch.CustomCategories
{
    public class CustomCardCategories
    {
        // custom card class for cards that can be drawn multiple times in a single hand
        public static CardCategory CanDrawMultipleCategory => CustomCategories.CustomCardCategories.instance.CardCategory("__CanDrawMultiple__");

        // singleton design, so that the categories are only created once
        public static readonly CustomCardCategories instance = new CustomCardCategories();

        private List<CardCategory> cardCategories = new List<CardCategory>() { };

        private CustomCardCategories()
        {
            CustomCardCategories instance = this;

            CardInfo[] vanilla = Resources.LoadAll<GameObject>("0 Cards/").Where(obj => obj.GetComponent<CardInfo>()).Select(obj2 => obj2.GetComponent<CardInfo>()).ToArray();

            foreach (CardInfo card in vanilla)
            {
                UpdateAndPullCategoriesFromCard(card);
            }

            foreach (CardInfo card in CardManager.cards.Values.Select(c => c.cardInfo))
            {
                UpdateAndPullCategoriesFromCard(card);
            }

            CardManager.AddAllCardsCallback(FirstStartAction);

            UnityEngine.Debug.Log("CustomCardCategories Setup");
        }

        private void FirstStartAction(CardInfo[] cards)
        {
            foreach (CardInfo card in cards)
            {
                UpdateAndPullCategoriesFromCard(card);
            }
            UnityEngine.Debug.Log("FirstStartAction run");
        }

        public CardCategory[] GetCategoriesFromCard(CardInfo card)
        {
            return card.categories;
        }
        public CardCategory[] GetBlacklistedCategoriesFromCard(CardInfo card)
        {
            return card.blacklistedCategories;
        }

        public void UpdateAndPullCategoriesFromCard(CardInfo card)
        {
            List<CardCategory> goodCategories = new List<CardCategory>();
            for (int i = 0; i < card.categories.Length; i++)
            {
                CardCategory category = card.categories[i];

                if (category == null)
                {
                    continue;
                }

                if (!this.cardCategories.Contains(category))
                {
                    var storedCategory = GetCategoryWithName(category.name);

                    if (storedCategory != null)
                    {
                        card.categories[i] = storedCategory;
                        category = card.categories[i];
                    }
                    else
                    {
                        this.cardCategories.Add(category);
                    }
                }
                goodCategories.Add(category);
            }
            card.categories = goodCategories.ToArray();
            goodCategories = new List<CardCategory>();
            for (int i = 0; i < card.blacklistedCategories.Length; i++)
            {
                CardCategory category = card.blacklistedCategories[i];

                if (category == null)
                {
                    continue;
                }

                if (!this.cardCategories.Contains(category))
                {
                    var storedCategory = GetCategoryWithName(category.name);

                    if (storedCategory != null)
                    {
                        card.blacklistedCategories[i] = storedCategory;
                        category = card.blacklistedCategories[i];
                    }
                    else
                    {
                        this.cardCategories.Add(category);
                    }
                }
                goodCategories.Add(category);
            }
            card.blacklistedCategories = goodCategories.ToArray();
        }

        public CardInfo[] GetActiveCardsFromCategory(CardCategory cardCategory)
        {
            return Cards.instance.GetAllCardsWithCondition(((ObservableCollection<CardInfo>)typeof(CardManager).GetField("activeCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToArray(), null, (card, player) => card.categories.Intersect(new CardCategory[] { cardCategory }).Any());
        }
        public CardInfo[] GetInactiveCardsFromCategory(CardCategory cardCategory)
        {
            return Cards.instance.GetAllCardsWithCondition(((List<CardInfo>)typeof(CardManager).GetField("inactiveCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToArray(), null, (card, player) => card.categories.Intersect(new CardCategory[] { cardCategory }).Any());
        }
        public CardInfo[] GetAllCardsFromCategory(CardCategory cardCategory)
        {
            return this.GetActiveCardsFromCategory(cardCategory).Concat(this.GetInactiveCardsFromCategory(cardCategory)).ToArray();
        }

        private CardCategory GetCategoryWithName(string categoryName)
        {

            foreach (CardCategory category in this.cardCategories)
            {
                // not case-sensitive
                if (category != null && category.name != null && category.name.ToLower() == categoryName.ToLower())
                {
                    return category;
                }
            }

            return null;

        }

        public CardCategory CardCategory(string categoryName)
        {
            CardCategory category = this.GetCategoryWithName(categoryName);

            if (category == null)
            {
                CardCategory newCategory = ScriptableObject.CreateInstance<CardCategory>();
                newCategory.name = categoryName.ToLower();
                this.cardCategories.Add(newCategory);

                category = newCategory;
            }

            return category;
        }

        public void MakeCardsExclusive(CardInfo card1, CardInfo card2)
        {
            string name1 = "__" + card1.name + "_" + card2.name + "_EXCLUSIVE__";
            string name2 = "__" + card2.name + "_" + card1.name + "_EXCLUSIVE__";
            CardCategory category1 = this.CardCategory(name1);
            CardCategory category2 = this.CardCategory(name2);
            card1.categories = card1.categories.Concat(new CardCategory[] { category1 }).ToArray();
            card2.categories = card2.categories.Concat(new CardCategory[] { category2 }).ToArray();
            card1.blacklistedCategories = card1.blacklistedCategories.Concat(new CardCategory[] { category2 }).ToArray();
            card2.blacklistedCategories = card2.blacklistedCategories.Concat(new CardCategory[] { category1 }).ToArray();
        }

    }
}