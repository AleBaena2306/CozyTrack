using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CozyTrack
{
    // Pantalla de inicio al estilo de los juegos de coches para móvil (referencia de Pinterest), en versión cozy:
    // barra superior con nivel y monedas, 4 ranuras de cofre, ajustes y 1 vs 1 a la izquierda, desafío diario a la derecha,
    // el coche elegido girando en su plataforma, selectores de circuito y modo, «¡A correr!» + Taller y barra de pestañas.
    public partial class CozyGame
    {
        static readonly Vector3 ShowroomPos = new Vector3(-400, 0, 0);
        Transform showRoot; GameObject showCar;
        Text lvlText, coinPill, carName, carRole, circPill, modePill, dailyGoal, dailyStreak;
        Image lvlFill; readonly Image[] statFills = new Image[4];
        readonly Text[] chestState = new Text[4], chestBtnText = new Text[4]; readonly Image[] chestIcon = new Image[4], chestBar = new Image[4], chestBtn = new Image[4];
        GameObject settingsPop;
        static readonly string[] ModeIds = { "race", "time", "elim" }, ModeNames = { "Carrera", "Contrarreloj", "Eliminación" };

        Sprite Icon(string name) => Resources.Load<Sprite>("UI/Icons/" + name);
        Sprite TexSprite(string key) { var t = CozyTex.Get(key); return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)); }
        static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = size; }

        // tarjeta con relieve: canto más oscuro debajo y cara encima
        RectTransform Card(Transform p, Color face, string name, out Image faceImg, float depth = 12)
        {
            var edge = Img(p, Color.Lerp(face, Ink, 0.3f), null, name);
            faceImg = Img(edge.transform, face, null, "Cara"); Fill(faceImg.rectTransform, 0, depth, 0, 0);
            return edge.rectTransform;
        }
        Button CardButton(Transform p, Color face, string name, System.Action on, out Image faceImg, float depth = 12)
        {
            var r = Card(p, face, name, out faceImg, depth);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = faceImg; b.onClick.AddListener(() => on());
            var cb = b.colors; cb.pressedColor = new Color(0.85f, 0.85f, 0.85f); b.colors = cb;
            return b;
        }
        Text Outlined(Text t, float w = 4) { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(w, -w); return t; }
        Image IconImg(Transform p, Sprite s, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var i = Img(p, c, s, "Icono"); i.type = Image.Type.Simple; i.preserveAspect = true; i.raycastTarget = false; Place(i.rectTransform, anchor, pos, size); return i;
        }

        // ---------- inicio: barra superior, cofres, el último coche usado y tres botones ----------
        void BuildHome()
        {
            var s = MakeScreen("menu", false);
            var T0 = s.transform;
            BuildTopBar(T0);
            var race = CardButton(T0, SunC, "Race", () => OpenPick("race"), out var rf, 16);
            var rr = (RectTransform)race.transform; rr.anchorMin = new Vector2(0, 0); rr.anchorMax = new Vector2(1, 0); rr.pivot = new Vector2(0.5f, 0); rr.offsetMin = new Vector2(60, 470); rr.offsetMax = new Vector2(-60, 690);
            var rt = Outlined(T(rf.transform, "RACE", 96, Color.white), 6); Fill(rt.rectTransform, 10, 0, 10, 0); rt.font = fontBlack;
            var surv = CardButton(T0, Coral, "Survival", () => OpenPick("elim"), out var sf, 14);
            var sr = (RectTransform)surv.transform; sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 0); sr.pivot = new Vector2(0.5f, 0); sr.offsetMin = new Vector2(60, 270); sr.offsetMax = new Vector2(-60, 440);
            var st = Outlined(T(sf.transform, "SURVIVAL", 76, Color.white), 5); Fill(st.rectTransform, 10, 0, 10, 0); st.font = fontBlack;
            var onl = CardButton(T0, CozyData.Hex("#4a4860"), "Online", () => Toast("El online llega con el servidor del estudio"), out var of, 12);
            var orr = (RectTransform)onl.transform; orr.anchorMin = new Vector2(0, 0); orr.anchorMax = new Vector2(1, 0); orr.pivot = new Vector2(0.5f, 0); orr.offsetMin = new Vector2(60, 90); orr.offsetMax = new Vector2(-60, 240);
            var ot = T(of.transform, "ONLINE  <size=34>· próximamente</size>", 64, Cream); Fill(ot.rectTransform, 10, 0, 10, 0); ot.font = fontBlack;
            BuildPick(); BuildCarPick();
        }

        void BuildTopBar(Transform T0)
        {
            var lvl = Card(T0, Cream, "Nivel", out var lvlFace, 8); Place(lvl, new Vector2(0, 1), new Vector2(28, -28), new Vector2(470, 108));
            var star = Img(lvlFace.transform, SunC, CozyTex.CircleSprite(), "Medalla"); Place(star.rectTransform, new Vector2(0, 0.5f), new Vector2(-6, 0), new Vector2(112, 112));
            IconImg(star.transform, Icon("star"), Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62));
            lvlText = T(lvlFace.transform, "", 34, Ink, TextAnchor.MiddleLeft); Place(lvlText.rectTransform, new Vector2(0, 1), new Vector2(124, -8), new Vector2(320, 46)); lvlText.font = fontBlack;
            var bar = Img(lvlFace.transform, new Color(0.9f, 0.88f, 0.83f), null, "Barra"); Place(bar.rectTransform, new Vector2(0, 0), new Vector2(124, 16), new Vector2(320, 26));
            lvlFill = Img(bar.transform, Coral, null, "Relleno"); var lf = lvlFill.rectTransform; lf.anchorMin = Vector2.zero; lf.anchorMax = new Vector2(0, 1); lf.offsetMin = lf.offsetMax = Vector2.zero;
            var coins = Card(T0, Cream, "Monedas", out var coinFace, 8); Place(coins, new Vector2(1, 1), new Vector2(-28, -28), new Vector2(330, 108));
            var coinIcon = Img(coinFace.transform, Color.white, TexSprite("ic_coin"), "Moneda"); coinIcon.type = Image.Type.Simple; Place(coinIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(84, 84));
            coinPill = T(coinFace.transform, "0", 46, Ink, TextAnchor.MiddleRight); Fill(coinPill.rectTransform, 100, 4, 24, 4); coinPill.font = fontBlack;
            var chestRow = R("Cofres", T0); chestRow.anchorMin = new Vector2(0, 1); chestRow.anchorMax = new Vector2(1, 1); chestRow.pivot = new Vector2(0.5f, 1); chestRow.offsetMin = new Vector2(28, -460); chestRow.offsetMax = new Vector2(-28, -164);
            var hl = chestRow.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 18; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            for (int k = 0; k < 4; k++)
            {
                int kk = k; var slot = R("Ranura " + k, chestRow);
                var card = Card(slot, Cream, "Cofre", out var face, 10); card.anchorMin = new Vector2(0, 0.28f); card.anchorMax = Vector2.one; card.offsetMin = card.offsetMax = Vector2.zero;
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => TapChest(kk));
                chestIcon[k] = Img(face.transform, Color.white, TexSprite("ic_chest"), "Cofre"); chestIcon[k].type = Image.Type.Simple; chestIcon[k].preserveAspect = true; chestIcon[k].raycastTarget = false;
                Place(chestIcon[k].rectTransform, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(120, 110));
                chestState[k] = T(face.transform, "", 30, Ink); Place(chestState[k].rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(220, 44)); chestState[k].font = fontBlack;
                var pb = Img(face.transform, new Color(0.9f, 0.88f, 0.83f), null, "Barra"); Place(pb.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(200, 18)); pb.raycastTarget = false;
                chestBar[k] = Img(pb.transform, CozyData.Hex("#5be38a"), null, "Relleno"); var cr = chestBar[k].rectTransform; cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(0, 1); cr.offsetMin = cr.offsetMax = Vector2.zero;
                var btn = CardButton(slot, CozyData.Hex("#ff9e40"), "Boton", () => { var sl = prof.chests[kk]; if (sl.opening && Now < sl.readyAt) ChestAd(kk); else TapChest(kk); }, out chestBtn[k], 8);
                var br = (RectTransform)btn.transform; br.anchorMin = Vector2.zero; br.anchorMax = new Vector2(1, 0.25f); br.offsetMin = br.offsetMax = Vector2.zero;
                chestBtnText[k] = Outlined(T(chestBtn[k].transform, "", 28, Color.white), 3); Fill(chestBtnText[k].rectTransform, 6, 2, 6, 2); chestBtnText[k].font = fontBlack;
            }
        }

        // ---------- elegir a qué correr (Race o Survival) ----------
        string pickMode = "race"; bool pickTime; int pickLevel; string pickCirc = "jardin"; bool pickDaily;
        Text pickTitle, timeChip; Transform pickList;
        void BuildPick()
        {
            var s = MakeScreen("pick");
            var (_, c) = Scroll(s.transform, 40);
            var head = Row(c, 120); Btn(head, "‹ Volver", Cream, Ink, ShowMenu, 110, 38).GetComponent<LayoutElement>().preferredWidth = 260;
            pickTitle = T(head, "", 72, Ink); pickTitle.font = fontBlack; pickTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3;
            pickList = VList(c, 22);
        }
        void OpenPick(string m)
        {
            pickMode = m; pickTime = false;
            ShowScreen("pick"); RefreshPick();
        }
        void RefreshPick()
        {
            foreach (Transform t in pickList) Destroy(t.gameObject);
            pickTitle.text = pickMode == "race" ? "RACE" : "SURVIVAL";
            if (pickMode == "race")
            {
                int next = Mathf.Min(CozyData.LevelCount, prof.level + 1);
                WideCard(pickList, CozyData.Hex("#73e0a6"), $"Niveles · nivel {next}", $"{prof.level}/{CozyData.LevelCount} superados. Gana para abrir el siguiente.", () => { pickLevel = next; pickDaily = false; OpenCarPick(); });
                var d = DailyDef(); bool done = prof.dailyLast == d.key;
                WideCard(pickList, SunC, "Desafío diario" + (done ? " · superado" : ""), $"{CozyData.DailyGoals[d.goal]} con {CozyData.Car(d.car).name} en {CozyData.Circuit(d.circ).n}. Racha: {StreakNow()}.", () => { pickDaily = true; StartDaily(); });
                var tb = Btn(pickList, "", Cream, Ink, () => { pickTime = !pickTime; timeChip.text = (pickTime ? "✓ " : "") + "Contrarreloj contra tu fantasma"; }, 100, 36); timeChip = tb.GetComponentInChildren<Text>();
                timeChip.text = "Contrarreloj contra tu fantasma";
            }
            else LayoutText(pickList, "Al cerrar cada vuelta el líder, el último queda fuera.", 34, Muted, 70, FontStyle.Normal);
            var grid = R("Circuitos", pickList); var g = grid.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(482, 330); g.spacing = new Vector2(16, 16); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 2;
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = 2 * 346;
            string[] cols = { "#a8d8b9", "#f2d6c4", "#9fd8c0", "#8fc8f0" };
            for (int k = 0; k < CozyData.Circuits.Length; k++)
            {
                var cd = CozyData.Circuits[k];
                var b = CardButton(grid, CozyData.Hex(cols[k % cols.Length]), cd.n, () => { pickCirc = cd.id; pickLevel = 0; pickDaily = false; OpenCarPick(); }, out var f, 12);
                var n = T(f.transform, cd.n, 54, Ink); Place(n.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(440, 70)); n.font = fontBlack;
                var tod = T(f.transform, CozyData.Tod(TodFor(cd.id)).n, 28, Ink, TextAnchor.MiddleCenter, FontStyle.Normal); Place(tod.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(440, 40));
                var de = T(f.transform, cd.desc, 26, Ink, TextAnchor.UpperCenter, FontStyle.Normal); Place(de.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(430, 170));
            }
        }
        void WideCard(Transform p, Color c, string title, string sub, System.Action on)
        {
            var b = CardButton(p, c, title, on, out var f, 12); b.gameObject.AddComponent<LayoutElement>().preferredHeight = 190;
            var t = T(f.transform, title, 50, Ink); Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(900, 64)); t.font = fontBlack;
            var s = T(f.transform, sub, 30, Ink, TextAnchor.MiddleCenter, FontStyle.Normal); Place(s.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(900, 90));
        }
        // la hora del día la pone el circuito
        public static string TodFor(string circuitId)
        {
            switch (circuitId) { case "jardin": return "atardecer"; case "salon": return "manana"; case "cocina": return "manana"; case "bano": return "noche"; }
            return "atardecer";
        }

        // ---------- elegir vehículo y personalizarlo (como la selección antes de correr en Crash Team Racing) ----------
        Text carPickTitle, carCoins; Transform carStickers, carUps;
        void BuildCarPick()
        {
            var s = MakeScreen("car", false); var T0 = s.transform;
            var back = CardButton(T0, Cream, "Volver", () => { ShowScreen("pick"); RefreshPick(); }, out var bf, 8); Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(28, -28), new Vector2(220, 100));
            var bt = T(bf.transform, "‹ Volver", 36, Ink); Fill(bt.rectTransform); bt.font = fontBlack;
            var coins = Card(T0, Cream, "Monedas", out var cf, 8); Place(coins, new Vector2(1, 1), new Vector2(-28, -28), new Vector2(300, 100));
            carCoins = T(cf.transform, "", 40, Ink); Fill(carCoins.rectTransform, 10, 4, 10, 4); carCoins.font = fontBlack;
            carPickTitle = T(T0, "", 40, Ink); Place(carPickTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1000, 60)); carPickTitle.font = fontBlack;
            // nombre, rol y stats
            var name = Card(T0, Cream, "Coche", out var nameFace, 8); Place(name, new Vector2(0.5f, 0), new Vector2(0, 880), new Vector2(600, 150));
            carName = T(nameFace.transform, "", 50, Ink); Place(carName.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(560, 60)); carName.font = fontBlack;
            carRole = T(nameFace.transform, "", 28, Muted, TextAnchor.MiddleCenter, FontStyle.Normal); Place(carRole.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(560, 36));
            for (int k = 0; k < 4; k++)
            {
                var sb = Img(nameFace.transform, new Color(0.9f, 0.88f, 0.83f), null, CozyData.StatNames[k]); Place(sb.rectTransform, new Vector2(0, 0), new Vector2(40 + k * 132, 16), new Vector2(116, 16)); sb.raycastTarget = false;
                statFills[k] = Img(sb.transform, Coral, null, "Relleno"); var sr = statFills[k].rectTransform; sr.anchorMin = Vector2.zero; sr.anchorMax = new Vector2(0, 1); sr.offsetMin = sr.offsetMax = Vector2.zero;
            }
            ArrowButton(T0, new Vector2(0.5f, 0), new Vector2(-380, 900), false, () => { CycleCar(-1); RefreshCarPick(); });
            ArrowButton(T0, new Vector2(0.5f, 0), new Vector2(380, 900), true, () => { CycleCar(1); RefreshCarPick(); });
            // personalización: pegatina y mejoras
            var panel = Card(T0, Cream, "Personalizacion", out var pf, 10); var pr = panel; pr.anchorMin = new Vector2(0, 0); pr.anchorMax = new Vector2(1, 0); pr.pivot = new Vector2(0.5f, 0); pr.offsetMin = new Vector2(28, 300); pr.offsetMax = new Vector2(-28, 850);
            var v = VList(pf.transform, 10, new RectOffset(24, 24, 18, 18)); Fill(v);
            LayoutText(v, "Pegatina", 32, Ink, 44);
            var sg = R("Pegatinas", v); var g = sg.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(84, 84); g.spacing = new Vector2(8, 8); g.childAlignment = TextAnchor.MiddleCenter;
            sg.gameObject.AddComponent<LayoutElement>().preferredHeight = 180; carStickers = sg;
            LayoutText(v, "Mejoras", 32, Ink, 44);
            carUps = VList(v, 6);
            var go = CardButton(T0, SunC, "Correr", GoRace, out var gf, 16); var gr = (RectTransform)go.transform; gr.anchorMin = new Vector2(0, 0); gr.anchorMax = new Vector2(1, 0); gr.pivot = new Vector2(0.5f, 0); gr.offsetMin = new Vector2(60, 80); gr.offsetMax = new Vector2(-60, 260);
            var gt = Outlined(T(gf.transform, "¡A CORRER!", 84, Color.white), 6); Fill(gt.rectTransform, 10, 0, 10, 0); gt.font = fontBlack;
        }
        void OpenCarPick()
        {
            if (!Usable(choice)) choice = CozyData.StartCars[0];
            ShowScreen("car"); UpdateShowroom(); RefreshCarPick();
        }
        void RefreshCarPick()
        {
            if (!carPickTitle) return;
            carPickTitle.text = pickLevel > 0 ? $"Nivel {pickLevel}" : (pickMode == "elim" ? "Survival · " : pickTime ? "Contrarreloj · " : "Race · ") + CozyData.Circuit(pickCirc).n;
            carCoins.text = $"{prof.coins} monedas";
            foreach (Transform t in carStickers) Destroy(t.gameObject);
            foreach (Transform t in carUps) Destroy(t.gameObject);
            string cur = StickerOf(choice);
            void AddSticker(string id)
            {
                bool owned = id == null || prof.stickers.Contains(id);
                var img = Img(carStickers, cur == id ? Ink : Color.white, null, "Pegatina");
                if (owned) img.gameObject.AddComponent<Button>().onClick.AddListener(() => { SetSticker(choice, id); UpdateShowroom(); RefreshCarPick(); });
                else img.color = new Color(0.9f, 0.88f, 0.83f);
                if (id != null && owned) { var ri = R("Img", img.transform).gameObject.AddComponent<RawImage>(); ri.texture = CozyTex.Get("sticker_" + id); Fill(ri.rectTransform, 6, 6, 6, 6); ri.raycastTarget = false; }
                else { var t = T(img.transform, owned ? "—" : "?", 36, owned ? (cur == id ? Cream : Ink) : Muted); Fill(t.rectTransform); }
            }
            AddSticker(null); foreach (var id in CozyData.StickerIds) AddSticker(id);
            var up = Upgrades(choice);
            for (int k = 0; k < 3; k++)
            {
                int kk = k; var row = Row(carUps, 70, 10);
                Label(row, CozyData.UpgradeNames[k], 230).fontSize = 30;
                var pips = R("Niveles", row); pips.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                for (int p = 0; p < 5; p++) { var pip = Img(pips, p < up[k] ? Coral : new Color(0.9f, 0.88f, 0.83f)); var ppr = pip.rectTransform; ppr.anchorMin = ppr.anchorMax = new Vector2(0, 0.5f); ppr.pivot = new Vector2(0, 0.5f); ppr.anchoredPosition = new Vector2(p * 52, 0); ppr.sizeDelta = new Vector2(44, 20); }
                string label = up[k] >= 5 ? "Máximo" : $"+ {CozyData.UpgradeCost[up[k]]}";
                var b = Btn(row, label, up[k] >= 5 ? new Color(0.9f, 0.88f, 0.83f) : SunC, Ink, () => { if (BuyUpgrade(choice, kk)) { Toast("¡Mejorado!"); RefreshCarPick(); } else Toast("Te faltan monedas"); }, 64, 30);
                b.GetComponent<LayoutElement>().preferredWidth = 220; b.GetComponent<LayoutElement>().flexibleWidth = 0;
            }
            RefreshHome();
        }
        void GoRace()
        {
            PlayerPrefs.SetString("ctCar", choice);
            if (pickLevel > 0) { StartLevel(pickLevel); return; }
            mode = pickMode == "elim" ? "elim" : pickTime ? "time" : "race";
            levelNum = 0;
            BuildCircuit(CozyData.Circuit(pickCirc));
            StartRace();
        }

        void SideButton(Transform p, string label, Sprite icon, Vector2 pos, System.Action on)
        {
            var b = CardButton(p, Cream, label, on, out var f, 10); Place((RectTransform)b.transform, new Vector2(0, 1), pos, new Vector2(180, 190));
            IconImg(f.transform, icon, Ink, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(86, 86));
            var t = T(f.transform, label, 28, Ink); Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(170, 70)); t.font = fontBlack;
        }
        void ArrowButton(Transform p, Vector2 anchor, Vector2 pos, bool right, System.Action on)
        {
            var b = CardButton(p, Cream, right ? "Siguiente" : "Anterior", on, out var f, 8); Place((RectTransform)b.transform, anchor, pos, new Vector2(110, 110));
            var ic = IconImg(f.transform, Icon("arrowLeft"), Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            if (right) ic.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        void Selector(Transform p, Vector2 anchor, Vector2 pos, out Text label, System.Action<int> step)
        {
            var card = Card(p, Cream, "Selector", out var f, 8); Place(card, anchor, pos, new Vector2(500, 110));
            label = T(f.transform, "", 38, Ink); Fill(label.rectTransform, 100, 4, 100, 4); label.font = fontBlack;
            foreach (var d in new[] { -1, 1 })
            {
                int dd = d; var b = CardButton(f.transform, SunC, "Flecha", () => step(dd), out var bf, 6);
                Place((RectTransform)b.transform, new Vector2(d < 0 ? 0 : 1, 0.5f), new Vector2(d < 0 ? 10 : -10, 3), new Vector2(84, 84));
                var ic = IconImg(bf.transform, Icon("arrowLeft"), Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46)); if (d > 0) ic.rectTransform.localScale = new Vector3(-1, 1, 1);
            }
        }
        void Tab(Transform p, string label, Sprite icon, bool selected, System.Action on)
        {
            var img = Img(p, selected ? SunC : new Color(0, 0, 0, 0), null, "Pestana " + label);
            var b = img.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => on());
            IconImg(img.transform, icon, Ink, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(70, 70));
            var t = T(img.transform, label, 28, Ink); Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(220, 40)); t.font = fontBlack;
        }

        void BuildSettings()
        {
            settingsPop = R("Ajustes", col).gameObject; Fill((RectTransform)settingsPop.transform); settingsPop.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.24f, 0.6f);
            var card = Card(settingsPop.transform, Cream, "Panel", out var face, 14); card.anchorMin = new Vector2(0.05f, 0.25f); card.anchorMax = new Vector2(0.95f, 0.78f); card.offsetMin = card.offsetMax = Vector2.zero;
            var v = VList(face.transform, 18, new RectOffset(36, 36, 30, 30)); Fill(v);
            var title = LayoutText(v, "Ajustes", 70, Ink, 100); title.font = fontBlack;
            var r1 = Row(v, 100); Label(r1, "Ambiente", 220); foreach (var t in CozyData.Tods) { var id = t.id; Btn(r1, t.n, Cream, Ink, () => ApplyTod(id), 100, 34, "tod:" + id); }
            var r2 = Row(v, 100); Label(r2, "Estilo", 220); foreach (var (id, n) in new[] { ("normal", "Normal"), ("toon", "Cel shading"), ("edge", "Cómic") }) { var i2 = id; Btn(r2, n, Cream, Ink, () => SetLook(i2), 100, 34, "look:" + id); }
            var ab = Btn(v, "", Cream, Ink, () => { assistOn = !assistOn; RefreshMenuSelections(); }, 100, 34); assistTxt = ab.GetComponentInChildren<Text>();
            var ub = Btn(v, "", Cream, Ink, () => { allUnlocked = !allUnlocked; PlayerPrefs.SetInt("ctAllUnlocked", allUnlocked ? 1 : 0); if (!Usable(choice)) choice = CozyData.StartCars[0]; MenuGrid(); RefreshMenuSelections(); }, 100, 34); unlockTxt = ub.GetComponentInChildren<Text>();
            profText = LayoutText(v, "", 32, Muted, 60, FontStyle.Normal);
            Btn(v, "Cerrar", Coral, Color.white, () => settingsPop.SetActive(false), 120, 46);
            settingsPop.SetActive(false);
        }

        // ---------- escaparate 3D ----------
        void BuildShowroom()
        {
            if (showRoot) return;
            showRoot = new GameObject("Escaparate").transform; showRoot.position = ShowroomPos;
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.35f, 0), new Vector3(6.4f, 0.3f, 6.4f), showRoot, 0, Mat(CozyData.Hex("#fff3dc"), null, false, false, null, 1));
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.06f, 0), new Vector3(5.2f, 0.04f, 5.2f), showRoot, 0, Mat(CozyData.Hex("#ffd64d")));
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.03f, 0), new Vector3(4.6f, 0.04f, 4.6f), showRoot, 0, Mat(CozyData.Hex("#fff8ec")));
            // fondo: suelo amplio y pared curva suave
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.6f, 6), new Vector3(60, 0.1f, 60), showRoot, 0, Mat(CozyData.Hex("#a8d8b9"), null, false, false, null, 0));
            for (int k = 0; k < 14; k++)
            {
                float a = (k - 6.5f) * 0.16f; var pos = new Vector3(Mathf.Sin(a) * 16, 4, Mathf.Cos(a) * 16);
                var bub = Prim(PrimitiveType.Sphere, Color.white, pos + new Vector3(0, Random.Range(-3f, 4f), 0), Vector3.one * Random.Range(1.5f, 3.5f), showRoot, 0, Mat(Color.Lerp(CozyData.Hex("#ffd9b8"), Color.white, Random.value * 0.5f), null, false, false, null, 0));
            }
        }
        void UpdateShowroom()
        {
            BuildShowroom();
            if (showCar) Destroy(showCar);
            showCar = Spawn("Car_" + choice, showRoot, true, true); ApplySticker(showCar, choice);
            var rs = showCar.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float sc = Mathf.Min(2.5f / Mathf.Max(b.size.x, b.size.z), 1.5f / Mathf.Max(0.01f, b.size.y));
            showCar.transform.localScale = Vector3.one * sc;
            RefreshHome();
        }
        void CycleCar(int d)
        {
            var list = CozyData.Cars.Select(c => c.name).Where(Usable).ToList(); if (list.Count == 0) return;
            int i = list.IndexOf(choice); choice = list[((i < 0 ? 0 : i) + d + list.Count) % list.Count];
            UpdateShowroom();
        }
        void CycleCircuit(int d)
        {
            var cs = CozyData.Circuits; int i = System.Array.FindIndex(cs, c => c.id == circId); if (i < 0) i = 0;
            BuildCircuit(cs[(i + d + cs.Length) % cs.Length]);
        }
        void RefreshHome()
        {
            if (!lvlText) return;
            lvlText.text = $"Nivel {prof.level}/{CozyData.LevelCount}";
            lvlFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(prof.level / (float)CozyData.LevelCount), 1);
            coinPill.text = prof.coins.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
            var d = CozyData.Car(choice);
            if (carName) { carName.text = d.name; carRole.text = d.role; }
            var up = Upgrades(choice);
            for (int k = 0; k < 4; k++) if (statFills[k]) { statFills[k].color = d.color; statFills[k].rectTransform.anchorMax = new Vector2(d.st[k] / 5f, 1); }
        }

        void UpdateChestsUI()
        {
            if (state != "menu" || !screens["menu"].activeSelf || chestState[0] == null) return;
            for (int k = 0; k < 4; k++)
            {
                var s = prof.chests[k]; long total = s.kind == "oro" ? 30 * 60 : 5 * 60;
                chestIcon[k].sprite = s.kind == "oro" ? TexSpriteCached("ic_chest_gold") : TexSpriteCached("ic_chest");
                chestIcon[k].color = s.kind == "" ? new Color(1, 1, 1, 0.25f) : Color.white;
                float fillA; string st, bt; Color bc;
                if (s.kind == "") { st = "Vacía"; fillA = 0; bt = "Gana carreras"; bc = new Color(0.79f, 0.76f, 0.84f); }
                else if (!s.opening) { st = total / 60 + ":00"; fillA = 0; bt = "Abrir"; bc = CozyData.Hex("#ff9e40"); }
                else if (Now < s.readyAt) { long r = s.readyAt - Now; st = $"{r / 60}:{r % 60:00}"; fillA = 1 - r / (float)total; bt = "▶ Ya"; bc = CozyData.Hex("#73bfff"); }
                else { st = "¡Listo!"; fillA = 1; bt = "Abrir"; bc = CozyData.Hex("#5be38a"); }
                chestState[k].text = st; chestBar[k].rectTransform.anchorMax = new Vector2(fillA, 1); chestBtnText[k].text = bt; chestBtn[k].color = bc;
                chestBtn[k].transform.parent.GetComponent<Image>().color = Color.Lerp(bc, Ink, 0.3f);
            }
        }
        readonly System.Collections.Generic.Dictionary<string, Sprite> spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
        Sprite TexSpriteCached(string key) => spriteCache.TryGetValue(key, out var s) ? s : spriteCache[key] = TexSprite(key);
    }
}
