// Cồn Xanh – village economy: resources, production, food, taxes, market, happiness, migration, ranks, goals.
// Every number comes from Resources/Data/game_data.json. VTA Games · Võ Thái Anh
using System.Collections.Generic;
using UnityEngine;

namespace ConXanh
{
    public class Economy : MonoBehaviour
    {
        public static Economy I;
        public static readonly string[] Keys = { "money", "rice", "fish", "veg", "coco", "candy", "bamboo" };
        public static readonly string[] Names = { "Tiền", "Lúa", "Cá", "Rau", "Dừa", "Kẹo", "Tre" };
        static readonly string[] Foods = { "rice", "fish", "veg" };

        public readonly Dictionary<string, float> res = new Dictionary<string, float>();
        public float pop, happy = 60;
        public int goal, rank, ordersDone;
        public bool hungry;
        public int Housing { get; private set; }
        public int Jobs { get; private set; }
        public System.Action onChanged;

        GameData gd; float migrateT, sumT;
        float sWater, sMarket, sTemple, sSchool, sDecor;

        void Awake() { I = this; gd = GameData.Load(); ResetState(); }

        public void ResetState()
        {
            foreach (var k in Keys) res[k] = 0;
            res["money"] = gd.startMoney; res["rice"] = gd.startRice; res["bamboo"] = gd.startBamboo; pop = gd.startPop;
            happy = 60; goal = 0; rank = 0; ordersDone = 0; hungry = false; migrateT = 0;
        }

        public float Get(string k) => res.TryGetValue(k, out var v) ? v : 0;
        public void Add(string k, float v) { res[k] = Mathf.Max(0, Get(k) + v); onChanged?.Invoke(); }
        public static string NameOf(string k) { int i = System.Array.IndexOf(Keys, k); return i >= 0 ? Names[i] : k; }

        // ---------- building costs ----------
        public string RankName(int i) => gd.ranks != null && i >= 0 && i < gd.ranks.Length ? gd.ranks[i].name : "";
        int RankIndex(string id) { if (gd.ranks != null) for (int i = 0; i < gd.ranks.Length; i++) if (gd.ranks[i].id == id) return i; return 0; }
        public bool Unlocked(string id) { var b = gd.Build(id); return b == null || string.IsNullOrEmpty(b.unlock) || rank >= RankIndex(b.unlock); }
        public string LockText(string id) { var b = gd.Build(id); return b == null || Unlocked(id) ? null : "Mở ở " + RankName(RankIndex(b.unlock)); }

        /// <summary>Null when the player may build it now, otherwise the reason.</summary>
        public string CantBuild(string id)
        {
            var b = gd.Build(id); if (b == null) return null;
            if (!Unlocked(id)) return "Làng lên " + RankName(RankIndex(b.unlock)) + " mới xây được";
            if (Get("money") < b.cost.money) return "Thiếu tiền: cần " + b.cost.money + " đ";
            if (Get("bamboo") < b.cost.bamboo) return "Thiếu tre: cần " + b.cost.bamboo + " tre";
            return null;
        }
        public string CostText(string id)
        {
            var b = gd.Build(id); if (b == null) return "";
            string s = b.cost.money + "đ"; if (b.cost.bamboo > 0) s += " " + b.cost.bamboo + " tre"; return s;
        }
        public void Pay(string id) { var b = gd.Build(id); if (b == null) return; res["money"] -= b.cost.money; res["bamboo"] -= b.cost.bamboo; onChanged?.Invoke(); }
        public void Refund(string id, Vector3 at)
        {
            var b = gd.Build(id); if (b == null) return;
            int m = b.cost.money / 2, t = b.cost.bamboo / 2; Add("money", m); Add("bamboo", t);
            if (m + t > 0) GameUI.I?.Floater(at + Vector3.up * 0.5f, "+" + m + " đ" + (t > 0 ? "  +" + t + " tre" : ""));
        }

        // ---------- simulation ----------
        void Update()
        {
            float dt = Time.deltaTime; if (dt <= 0) return;
            Survey();
            Produce(dt);
            Eat(dt);
            res["money"] += pop * gd.taxPerPersonPerMin / 60f * dt * Mathf.Lerp(0.5f, 1.2f, happy / 100f);
            Sell(dt);
            Happiness(dt);
            Migrate(dt);
            Ranks();
            sumT += dt; if (sumT > 0.5f) { sumT = 0; onChanged?.Invoke(); ConXanhGame.I?.SyncVillagers(Mathf.FloorToInt(pop)); CheckGoal(); }
        }

        void Survey()
        {
            int h = 0, j = 0; sWater = sMarket = sTemple = sSchool = sDecor = 0;
            foreach (var p in Placed.All)
            {
                var b = p.def; if (b == null) continue;
                h += b.housing; j += b.jobs; sWater += b.water; sMarket += b.market; sTemple += b.temple; sSchool += b.school; sDecor += b.decor;
            }
            Housing = h; Jobs = j;
        }

        /// <summary>Share of jobs that have someone to work them (1 when nobody is short).</summary>
        public float Staffing => Jobs <= 0 ? 1 : Mathf.Clamp01(pop / Jobs);

        void Produce(float dt)
        {
            var season = Weather.Season;
            float night = Mathf.Lerp(1, gd.nightProdMult, DayNight.Night);
            foreach (var p in Placed.All)
            {
                var b = p.def; if (b == null || b.cycle <= 0 || string.IsNullOrEmpty(b.outRes)) continue;
                float m = night * (b.jobs > 0 ? Staffing : 1) * SeasonMult(season, b.outRes);
                if (b.outRes == "rice" && Weather.Flood > 0.1f) m *= 0.5f;   // ruộng ngập
                p.prog += dt * m / b.cycle;
                if (p.prog < 1) continue;
                if (!string.IsNullOrEmpty(b.inRes) && Get(b.inRes) < b.inAmt) { p.prog = 1; continue; }   // chờ nguyên liệu
                p.prog -= 1;
                if (!string.IsNullOrEmpty(b.inRes)) res[b.inRes] -= b.inAmt;
                res[b.outRes] = Get(b.outRes) + b.outAmt;
                GameUI.I?.Floater(p.transform.position + Vector3.up * 0.6f, "+" + b.outAmt + " " + NameOf(b.outRes).ToLower());
            }
        }
        static float SeasonMult(SeasonDef s, string r)
        {
            if (s == null) return 1;
            return r switch { "rice" => s.rice, "fish" => s.fish, "veg" => s.veg, "bamboo" => s.bamboo, _ => 1 };
        }

        void Eat(float dt)
        {
            float need = pop * gd.eatPerPersonPerMin / 60f * dt;
            foreach (var f in Foods) { float take = Mathf.Min(need, Get(f)); res[f] -= take; need -= take; if (need <= 0) break; }
            hungry = need > 0.0001f;
        }

        void Sell(float dt)
        {
            int markets = Placed.Count("cho"); if (markets == 0) return;
            float budget = markets * gd.sellPerMarketPerMin / 60f * dt * Staffing;
            float reserve = pop * gd.foodReservePerPerson;
            foreach (var k in new[] { "candy", "coco", "fish", "veg", "rice" })
            {
                float keep = System.Array.IndexOf(Foods, k) >= 0 ? reserve : 0;
                float extra = Get(k) - keep; if (extra <= 0) continue;
                float sell = Mathf.Min(extra, budget); res[k] -= sell; res["money"] += sell * gd.Price(k); budget -= sell;
                if (budget <= 0) break;
            }
        }

        /// <summary>0..100: base + fed + water/market/temple/school per head + decor.</summary>
        public float TargetHappy()
        {
            float n = Mathf.Max(1, pop);
            float h = 20 + (hungry ? -15 : 25);
            h += Mathf.Min(15, sWater * 15 / (n * 0.6f));
            h += Mathf.Min(10, sMarket * 10 / (n * 0.4f));
            h += Mathf.Min(10, sTemple * 10 / (n * 0.3f));
            h += Mathf.Min(10, sSchool * 10 / (n * 0.3f));
            h += Mathf.Min(10, sDecor * 0.8f);
            if (pop > Housing) h -= 15;
            return Mathf.Clamp(h, 0, 100);
        }
        void Happiness(float dt) { happy = Mathf.MoveTowards(happy, TargetHappy(), dt * 2f); }

        void Migrate(float dt)
        {
            migrateT += dt; if (migrateT < gd.immigrateEvery) return; migrateT = 0;
            if (happy >= gd.arriveAboveHappy && pop + 1 <= Housing && !hungry)
            {
                pop += 1; GameUI.I?.Toast("Có người mới dọn về cồn!"); Sound.I?.Pluck(4, 0.2f);
            }
            else if ((happy < gd.leaveBelowHappy || pop > Housing + 1) && pop > 1)
            {
                pop -= 1; GameUI.I?.Toast(hungry ? "Thiếu ăn, có người bỏ cồn đi rồi" : "Dân buồn, có người dọn đi rồi");
            }
        }

        void Ranks()
        {
            if (gd.ranks == null) return;
            int r = 0; for (int i = 0; i < gd.ranks.Length; i++) if (pop >= gd.ranks[i].pop) r = i;
            if (r > rank) { rank = r; GameUI.I?.Toast("Chúc mừng! Làng đã lên " + RankName(r) + " – mở công trình mới", 4f); Sound.I?.Bell(); Builder.I?.onChanged?.Invoke(); }
            else if (r < rank) rank = r;
        }

        // ---------- goals ----------
        public GoalDef Goal => gd.goals != null && goal < gd.goals.Length ? gd.goals[goal] : null;
        public string GoalText()
        {
            var g = Goal; if (g == null) return "Làng đã thành Thị trấn – cứ thong thả xây tiếp nha";
            if (!string.IsNullOrEmpty(g.build)) return g.text + " (" + Mathf.Min(Placed.Count(g.build), g.count) + "/" + g.count + ")";
            return g.text + " (" + Mathf.FloorToInt(pop) + "/" + g.pop + ")";
        }
        void CheckGoal()
        {
            var g = Goal; if (g == null) return;
            bool done = !string.IsNullOrEmpty(g.build) ? Placed.Count(g.build) >= g.count : pop >= g.pop;
            if (!done) return;
            goal++; res["money"] += 40;
            GameUI.I?.Toast("Xong mục tiêu! Thưởng 40 đồng", 3f); Sound.I?.Place();
        }
    }
}
