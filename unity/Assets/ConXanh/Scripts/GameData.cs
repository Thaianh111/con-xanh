// Cồn Xanh – all tunable numbers live in Resources/Data/game_data.json (this file only defines the shape).
// VTA Games · Võ Thái Anh
using System;
using UnityEngine;

namespace ConXanh
{
    [Serializable] public class Cost { public int money, bamboo; }
    [Serializable] public class BuildDef
    {
        public string id; public Cost cost = new Cost();
        public int housing, jobs; public float cycle;
        public string outRes; public int outAmt; public string inRes; public int inAmt;
        public float water, market, temple, school, decor;
        public string unlock;
    }
    [Serializable] public class RankDef { public string id, name; public int pop; }
    [Serializable] public class SeasonDef { public string id, name; public int days; public float rain, flood, rice = 1, fish = 1, veg = 1, bamboo = 1; }
    [Serializable] public class OrderDef { public string res, need; public int min, max; public float mult; }
    [Serializable] public class GoalDef { public string id, text, build; public int count, pop; }
    [Serializable] public class Prices { public float rice = 1, fish = 2, veg = 1.5f, coco = 1.5f, candy = 6, bamboo = 1; }

    [Serializable]
    public class GameData
    {
        public int startMoney = 250, startRice = 30, startBamboo = 50, startPop = 3;
        public float eatPerPersonPerMin = 1f, taxPerPersonPerMin = 1.2f, immigrateEvery = 5f;
        public float leaveBelowHappy = 25, arriveAboveHappy = 45, sellPerMarketPerMin = 40, foodReservePerPerson = 3;
        public float dayLength = 240, nightProdMult = 0.5f;
        public float traderFirst = 40, traderEvery = 90, traderWait = 150;
        public Prices prices = new Prices();
        public BuildDef[] builds; public RankDef[] ranks; public SeasonDef[] seasons; public OrderDef[] orders; public GoalDef[] goals;

        static GameData cached;
        public static GameData Load()
        {
            if (cached != null) return cached;
            var ta = Resources.Load<TextAsset>("Data/game_data");
            cached = ta != null ? JsonUtility.FromJson<GameData>(ta.text) : new GameData();
            return cached;
        }
        public BuildDef Build(string id) { if (builds != null) foreach (var b in builds) if (b.id == id) return b; return null; }
        public float Price(string r) => r switch { "rice" => prices.rice, "fish" => prices.fish, "veg" => prices.veg, "coco" => prices.coco, "candy" => prices.candy, "bamboo" => prices.bamboo, _ => 1 };
    }
}
