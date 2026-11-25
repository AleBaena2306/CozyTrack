using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CozyTrack
{
    // Pantallas de menú con el estilo B del lienzo de diseño:
    //   inicio («B · Home: toy box»): matrícula de nivel, monedas, 4 cofres, franja azul con el último coche, RACE / SURVIVAL / ONLINE;
    //   elegir carrera («B · Race: levels»): carrusel deslizable de niveles (cada nivel trae su pista), desafío diario y contrarreloj;
    //   elegir vehículo («B · Choose vehicle»): coche en la franja, flechas, estadísticas y pestañas Upgrades / Stickers / Paint;
    //   ajustes y álbum (vehículos y pegatinas).
    public partial class CozyGame
    {
        static readonly Vector3 ShowroomPos = new Vector3(-400, 0, 0); const int ShowLayer = 31;
        static void SetLayer(Transform t, int l) { t.gameObject.layer = l; foreach (Transform c in t) SetLayer(c, l); }
        Transform showRoot; GameObject showCar; Camera showCam; RenderTexture showRT;
        GameObject settingsPop;

        Sprite Icon(string name) => Resources.Load<Sprite>("UI/Icons/" + name);
        Sprite TexSprite(string key) { var t = CozyTex.Get(key); return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)); }
        static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = size; }

        // Tarjetas de la interfaz antigua (online, filas…), ya con aspecto de pegatina
        RectTransform Card(Transform p, Color face, string name, out Image faceImg, float depth = 12) => Sticker(p, face, out faceImg, 14, 3, Mathf.Clamp(depth / 2.5f, 2, 5), 0, name);
        Button CardButton(Transform p, Color face, string name, System.Action on, out Image faceImg, float depth = 12) => StickerButton(p, face, on, out faceImg, 16, 3.5f, Mathf.Clamp(depth / 2.5f, 2, 5), 0, name);
        Text Outlined(Text t, float w = 4) { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(w, -w); return t; }
        Image IconImg(Transform p, Sprite s, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var i = Img(p, c, s, "Icono"); i.type = Image.Type.Simple; i.preserveAspect = true; i.raycastTarget = false; Place(i.rectTransform, anchor, pos, size); return i;
        }
        void WideCard(Transform p, Color c, string title, string sub, System.Action on)
        {
            var b = CardButton(p, c, title, on, out var f, 12); b.gameObject.AddComponent<LayoutElement>().preferredHeight = 190;
            var t = TB(f.transform, title, 22, Ink, true, TextAnchor.MiddleLeft); TopBox(t.rectTransform, 16, 16, 10, 30);
            var s = TB(f.transform, sub, 12, Ink, false, TextAnchor.UpperLeft); Inset(s.rectTransform, 16, 44, 16, 8);
        }
        // la hora del día la pone el circuito
        public static string TodFor(string circuitId)
        {
            switch (circuitId) { case "jardin": return "atardecer"; case "salon": return "manana"; case "cocina": return "manana"; case "bano": return "noche"; }
            return "atardecer";
        }

        // ================= INICIO =================
        Text lvlChip, lvlNext, coinPill, raceNo, onlineSub, onlineBadge; Image lvlBar;
        readonly Image[] chestFace = new Image[4], chestImg = new Image[4]; readonly Text[] chestLabel = new Text[4], chestSub = new Text[4];
        void BuildHome()
        {
            var s = MakeScreen("menu"); var T0 = s.transform;
            Band(T0, 250, 210, -9);
            ShowroomImage(T0, 262, 300, 190);
            // matrícula de nivel
            var plate = Sticker(T0, Cream, out var pf, 10, 3, 4, -3, "Matricula"); TopAt(plate, 14, 18, 196, 56);
            var chip = Sticker(pf.transform, SunC, out var cf, 8, 2.5f, 0, 0, "LV"); TopAt(chip, 8, 8, 70, 34);
            lvlChip = TB(cf.transform, "LV 1", 22, Ink); Inset(lvlChip.rectTransform, 2, 0, 2, 0);
            lvlNext = Cap(pf.transform, "", 10, null, TextAnchor.MiddleLeft); TopBox(lvlNext.rectTransform, 86, 6, 9, 14);
            var br = R("Barra", pf.transform); TopBox(br, 86, 10, 28, 12); lvlBar = Bar(br, Coral, 0); Fill((RectTransform)lvlBar.transform.parent.parent);
            // ajustes y monedas
            var gear = StickerButton(T0, Cream, OpenSettings, out var gf, 12, 3, 3, -4, "Ajustes"); TopAt((RectTransform)gear.transform, 218, 20, 40, 40);
            IcnFill(gf.transform, B("gear"), 5);
            coinPill = CoinPill(T0, 14, 22, 108, 2.5f);
            // cofres: cajas de juguete con cinta
            var row = R("Cofres", T0); TopBox(row, 12, 12, 92, 104);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10 * K; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            float[] rots = { -3, 2, -1.5f, 3 };
            for (int k = 0; k < 4; k++)
            {
                int kk = k; var slot = R("Ranura " + k, row);
                var b = StickerButton(slot, Cream, () => ChestTap(kk), out chestFace[k], 14, 3, 3, rots[k], "Cofre"); Fill((RectTransform)b.transform);
                Tape(b.transform, 32);
                chestImg[k] = Icn(chestFace[k].transform, null, 0, 2, 10, 10); TopCenter(chestImg[k].rectTransform, 2, 56, 56);
                chestLabel[k] = TB(chestFace[k].transform, "", 15, Ink); TopBox(chestLabel[k].rectTransform, 2, 2, 58, 18);
                chestSub[k] = Cap(chestFace[k].transform, "", 9); TopBox(chestSub[k].rectTransform, 2, 2, 77, 14);
            }
            // RACE: pegatina amarilla con bandera a cuadros y el número del siguiente nivel
            var race = StickerButton(T0, SunC, () => OpenPick("race"), out var rf, 20, 3.5f, 6, -1.5f, "Race"); TopBox((RectTransform)race.transform, 20, 20, 624, 100);
            var chk = Checker(rf.transform); var ckr = chk.rectTransform; ckr.anchorMin = Vector2.zero; ckr.anchorMax = new Vector2(0, 1); ckr.pivot = new Vector2(0, 0.5f); ckr.sizeDelta = new Vector2(42 * K, 0); ckr.anchoredPosition = Vector2.zero;
            var ckl = Img(rf.transform, Ink, null, "Linea"); ckl.sprite = null; var cklr = ckl.rectTransform; cklr.anchorMin = new Vector2(0, 0); cklr.anchorMax = new Vector2(0, 1); cklr.pivot = new Vector2(0, 0.5f); cklr.sizeDelta = new Vector2(3.5f * K, 0); cklr.anchoredPosition = new Vector2(42 * K, 0);
            TB(rf.transform, "RACE", 48, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 62, 8, 80, 26));
            Cap(rf.transform, "LEVELS · DAILY", 12, null, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 64, 62, 80, 10));
            var no = Sticker(rf.transform, Cream, out var nf, 29, 3, 0, 0, "Numero"); no.anchorMin = no.anchorMax = new Vector2(1, 0.5f); no.sizeDelta = new Vector2(58 * K, 58 * K); no.anchoredPosition = new Vector2(-45 * K, 0);
            raceNo = TB(nf.transform, "01", 24, Ink); Fill(raceNo.rectTransform);
            // SURVIVAL y ONLINE
            var surv = StickerButton(T0, Coral, () => OpenPick("elim"), out var sf, 18, 3.5f, 5, 1.5f, "Survival"); HalfBox((RectTransform)surv.transform, true, 744, 76);
            TB(sf.transform, "SURVIVAL", 25, Cream, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 10, 6, 30));
            Cap(sf.transform, "LAST ONE OUT", 11, Cream, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 46, 6, 10));
            var onl = StickerButton(T0, Purple, OpenOnline, out var of, 18, 3.5f, 5, -1, "Online"); HalfBox((RectTransform)onl.transform, false, 744, 76);
            TB(of.transform, "ONLINE", 25, Cream, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 10, 6, 30));
            onlineSub = Cap(of.transform, "1 VS 1", 11, Cream, TextAnchor.MiddleLeft); Inset(onlineSub.rectTransform, 14, 46, 6, 10);
            var badge = Sticker(onl.transform, SunC, out var bf, 8, 2.5f, 0, 10, "Trofeos"); var bdr = badge; bdr.anchorMin = bdr.anchorMax = new Vector2(1, 1); bdr.sizeDelta = new Vector2(62 * K, 22 * K); bdr.anchoredPosition = new Vector2(-12 * K, 2 * K);
            onlineBadge = Cap(bf.transform, "0", 11); Fill(onlineBadge.rectTransform);
            BuildPick(); BuildCarPick();
        }
        // media columna (izquierda o derecha) con 20 px de margen exterior y 14 de hueco central
        static void HalfBox(RectTransform r, bool left, float top, float h)
        {
            r.anchorMin = new Vector2(left ? 0 : 0.5f, 1); r.anchorMax = new Vector2(left ? 0.5f : 1, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2((left ? 20 : 7) * K, -(top + h) * K); r.offsetMax = new Vector2(-(left ? 7 : 20) * K, -top * K);
        }

        void ChestTap(int k)
        {
            var sl = prof.chests[k];
            if (sl.kind == "") { Toast("Win races to fill your chest slots"); return; }
            if (sl.opening && Now < sl.readyAt) ChestAd(k); else TapChest(k);
        }

        void RefreshHome()
        {
            if (!lvlChip) return;
            int lv = Mathf.Max(1, prof.level), next = Mathf.Min(CozyData.LevelCount, prof.level + 1);
            lvlChip.text = "LV " + lv;
            lvlNext.text = prof.level >= CozyData.LevelCount ? "ALL LEVELS BEATEN!" : $"NEXT: LEVEL {next}";
            SetBar(lvlBar, prof.level / (float)CozyData.LevelCount);
            coinPill.text = N0(prof.coins);
            raceNo.text = next.ToString("00");
            int tr = NetTrophies; onlineSub.text = "1 VS 1 · " + Online.Leagues.Name(tr).ToUpper(); onlineBadge.text = N0(tr);
            if (carCoins) carCoins.text = N0(prof.coins);
            if (pickCoins) pickCoins.text = N0(prof.coins);
            UpdateChestsUI();
        }

        // tiempo de apertura de cada cofre
        public static long ChestSeconds(string tier) => tier == "legendary" ? 30 * 60 : tier == "epic" ? 20 * 60 : tier == "rare" ? 10 * 60 : 5 * 60;
        void UpdateChestsUI()
        {
            if (chestFace[0] == null || !screens.TryGetValue("menu", out var m) || !m.activeSelf) return;
            for (int k = 0; k < 4; k++)
            {
                var s = prof.chests[k]; long total = ChestSeconds(s.kind);
                chestImg[k].sprite = ChestSprite(s.kind == "" ? "common" : s.kind);
                chestImg[k].color = s.kind == "" ? new Color(1, 1, 1, 0.3f) : Color.white;
                if (s.kind == "") { chestLabel[k].text = "Empty"; chestSub[k].text = "WIN RACES"; chestFace[k].color = Cream; }
                else if (!s.opening) { chestLabel[k].text = total / 60 + "m"; chestSub[k].text = "TAP TO OPEN"; chestFace[k].color = Cream; }
                else if (Now < s.readyAt) { long r = s.readyAt - Now; chestLabel[k].text = $"{r / 60}:{r % 60:00}"; chestSub[k].text = "▶ NOW"; chestFace[k].color = SkyB; }
                else { chestLabel[k].text = "Ready!"; chestSub[k].text = "OPEN"; chestFace[k].color = CozyData.Hex("#A8E6C4"); }
            }
        }

        // ================= ELEGIR CARRERA: carrusel de niveles =================
        string pickMode = "race"; bool pickTime; int pickLevel; string pickCirc = "jardin"; bool pickDaily;
        Text pickTitleText, pickCoins, worldChip, dailyTitle, dailyGoalText, dailyStreakText; Image pickTitleBg, worldBg;
        RectTransform pickRail; ScrollRect pickScroll; LevelSnap pickSnap; Transform pickDots; GameObject dailyGo, timeGo;
        readonly List<(string id, int level)> pickItems = new List<(string, int)>();
        void BuildPick()
        {
            var s = MakeScreen("pick").transform;
            BackButton(s, ShowMenu);
            var tr = Sticker(s, SunC, out pickTitleBg, 14, 3.5f, 4, 1.5f, "Titulo"); TopCenter(tr, 16, 150, 50);
            pickTitleText = TB(pickTitleBg.transform, "RACE", 32, Ink); Inset(pickTitleText.rectTransform, 6, 0, 6, 0);
            pickCoins = CoinPill(s, 14, 21);
            var wr = Sticker(s, MintB, out worldBg, 10, 3, 3, -2, "Mundo"); TopCenter(wr, 92, 240, 30);
            worldChip = Cap(worldBg.transform, "", 13); Fill(worldChip.rectTransform);
            // carrusel horizontal con imán al centro
            var sv = R("Carrusel", s); TopBox(sv, 0, 0, 136, 470);
            pickScroll = sv.gameObject.AddComponent<ScrollRect>(); pickScroll.vertical = false; pickScroll.horizontal = true; pickScroll.movementType = ScrollRect.MovementType.Elastic; pickScroll.inertia = true; pickScroll.decelerationRate = 0.05f;
            var vp = R("Vista", sv); Fill(vp); vp.gameObject.AddComponent<RectMask2D>(); vp.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            pickRail = R("Niveles", vp); pickRail.anchorMin = new Vector2(0, 0); pickRail.anchorMax = new Vector2(0, 1); pickRail.pivot = new Vector2(0, 0.5f); pickRail.offsetMin = pickRail.offsetMax = Vector2.zero;
            var hl = pickRail.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 18 * K; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = false; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.padding = new RectOffset(0, 0, Mathf.RoundToInt(14 * K), Mathf.RoundToInt(30 * K));
            pickRail.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pickScroll.viewport = vp; pickScroll.content = pickRail;
            pickSnap = sv.gameObject.AddComponent<LevelSnap>(); pickSnap.scroll = pickScroll; pickSnap.step = (300 + 18) * K; pickSnap.onIndex = i => { pickIndex = i; RefreshPickChrome(); };
            pickDots = R("Puntos", s); TopCenter((RectTransform)pickDots, 610, 200, 12);
            var dh = pickDots.gameObject.AddComponent<HorizontalLayoutGroup>(); dh.spacing = 6 * K; dh.childAlignment = TextAnchor.MiddleCenter; dh.childControlWidth = dh.childControlHeight = true; dh.childForceExpandWidth = dh.childForceExpandHeight = false;
            // desafío diario
            var daily = StickerButton(s, SunC, () => { pickDaily = true; StartDaily(); }, out var df, 20, 3.5f, 5, 1, "Diario"); TopBox((RectTransform)daily.transform, 18, 18, 650, 92); dailyGo = daily.gameObject;
            Icn(df.transform, B("gift"), 10, 16, 54, 54);
            dailyTitle = TB(df.transform, "Daily challenge", 21, Ink, true, TextAnchor.MiddleLeft); Inset(dailyTitle.rectTransform, 74, 8, 10, 50);
            dailyGoalText = TB(df.transform, "", 12, Ink, false, TextAnchor.MiddleLeft); Inset(dailyGoalText.rectTransform, 74, 38, 10, 24);
            dailyStreakText = Cap(df.transform, "", 10, null, TextAnchor.MiddleLeft); Inset(dailyStreakText.rectTransform, 74, 62, 10, 8);
            // contrarreloj contra tu fantasma (en el nivel del centro)
            var tt = StickerButton(s, Cream, () => { if (!PickOpen(pickIndex)) { Toast("Beat the level before to race your ghost"); return; } pickTime = true; SetPickTarget(pickIndex); OpenCarPick(); }, out var tf, 16, 3, 3, -1, "Contrarreloj");
            TopBox((RectTransform)tt.transform, 18, 18, 762, 56); timeGo = tt.gameObject;
            Icn(tf.transform, B("clock"), 60, 14, 26, 26);
            TB(tf.transform, "Time trial vs your ghost", 19, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 96, 0, 10, 0));
        }
        int pickIndex;
        void OpenPick(string m)
        {
            pickMode = m; pickTime = false; pickDaily = false;
            ShowScreen("pick"); RefreshPick();
        }
        static readonly string[] CircuitBgs = { "#A8E6C4", "#F6C7B4", "#BFE6DA", "#BFE0F7" };
        static string WorldOf(int level) { CozyData.Level(level, out var key, out _); return key.world; }
        static Color WorldColor(string w) => CozyData.Hex(w == "Garden" ? "#A8E6C4" : w == "Snow" ? "#BFE0F7" : "#F6C7B4");
        bool PickOpen(int i) => i >= 0 && i < pickItems.Count && (pickItems[i].level == 0 || pickItems[i].level <= prof.level + 1 || allUnlocked);
        void SetPickTarget(int i)
        {
            var it = pickItems[Mathf.Clamp(i, 0, pickItems.Count - 1)];
            if (it.level > 0) { pickLevel = it.level; } else { pickLevel = 0; pickCirc = it.id; }
        }
        void RefreshPick()
        {
            bool race = pickMode == "race";
            pickTitleText.text = race ? "RACE" : "SURVIVAL"; pickTitleBg.color = race ? SunC : Coral; pickTitleText.color = race ? Ink : Cream;
            pickCoins.text = N0(prof.coins);
            dailyGo.SetActive(race); timeGo.SetActive(race);
            foreach (Transform t in pickRail) Destroy(t.gameObject);
            pickItems.Clear();
            if (race) for (int n = 1; n <= CozyData.LevelCount; n++) pickItems.Add(("nivel" + n, n));
            else foreach (var c in CozyData.Circuits) pickItems.Add((c.id, 0));
            float side = Mathf.Max(0, (pickScroll.viewport.rect.width - 300 * K) / 2);
            var hl = pickRail.GetComponent<HorizontalLayoutGroup>(); hl.padding.left = hl.padding.right = Mathf.RoundToInt(side);
            for (int i = 0; i < pickItems.Count; i++) BuildPickCard(i);
            int start = race ? Mathf.Clamp(prof.level, 0, CozyData.LevelCount - 1) : Mathf.Max(0, System.Array.FindIndex(CozyData.Circuits, c => c.id == pickCirc));
            pickSnap.count = pickItems.Count; pickSnap.JumpTo(start); pickIndex = start;
            var d = DailyDef(); bool done = prof.dailyLast == d.key;
            dailyTitle.text = "Daily challenge" + (done ? " ✓" : "");
            dailyGoalText.text = $"{CozyData.DailyGoals[d.goal]} with the {CozyData.Car(d.car).name} · {CozyData.Circuit(d.circ).n}";
            dailyStreakText.text = $"STREAK {StreakNow()} · 7 = LEGENDARY CHEST";
            RefreshPickChrome();
        }
        void BuildPickCard(int i)
        {
            var (id, level) = pickItems[i];
            bool open = PickOpen(i), done = level > 0 && level <= prof.level;
            string world = level > 0 ? WorldOf(level) : "";
            var def = level > 0 ? CozyData.Level(level, out var key, out _) : CozyData.Circuit(id);
            Color bg = !open ? Paper : level > 0 ? WorldColor(world) : CozyData.Hex(CircuitBgs[i % CircuitBgs.Length]);
            var holder = R("Tarjeta " + (i + 1), pickRail); var le = holder.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 300 * K; le.preferredHeight = 420 * K;
            var card = Sticker(holder, bg, out var f, 22, 3.5f, 5, i % 2 == 0 ? -1.2f : 1.2f, "Tarjeta"); Fill(card);
            Tape(card, 52, 0, -4, -6);
            string no = level > 0 ? level.ToString("00") : (i + 1).ToString("00");
            TB(f.transform, no, 54, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 16, 120, 12, 56));
            string st = !open ? "LOCKED" : level == 0 ? "SURVIVAL" : done ? "DONE" : "NEW";
            TagSticker(f.transform, st, Cream, 16, 22, 82, 26, 12, false, 0, true);
            // silueta de la pista
            var box = Sticker(f.transform, Cream, out var bxf, 16, 3, 0, 0, "Pista"); TopBox(box, 16, 16, 78, 170);
            var sil = R("Silueta", bxf.transform).gameObject.AddComponent<RawImage>(); Inset(sil.rectTransform, 12, 12, 12, 12); sil.raycastTarget = false;
            sil.texture = Silhouette(def.layout); sil.color = open ? Color.white : new Color(1, 1, 1, 0.35f);
            if (!open)
            {
                var lk = Img(bxf.transform, new Color(0.17f, 0.14f, 0.25f, 0.35f), null, "Candado"); Fill(lk.rectTransform); Radius(lk, 13); lk.raycastTarget = false;
                Icn(lk.transform, B("lock"), 0, 0, 54, 54).rectTransform.Let(r => { r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero; });
            }
            string name = !open ? "Locked" : level > 0 ? CozyData.Level(level, out var k2, out _).n.Split('·').Last().Trim() : def.n;
            string sub = level == 0 ? CozyData.Tod(TodFor(id)).n + " · " + def.desc : done ? "Beaten · race it again" : !open ? $"Beat level {level - 1} to unlock" : $"{world} · tighter curves";
            TB(f.transform, name, 24, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 16, 16, 256, 30));
            TB(f.transform, sub, 12, Ink, false, TextAnchor.UpperLeft).rectTransform.Let(r => TopBox(r, 16, 16, 288, 40));
            int ii = i;
            Color bb = !open ? Paper : done || level == 0 ? Cream : Coral;
            var b = StickerButton(f.transform, bb, () => { if (!PickOpen(ii)) { Toast("Beat the level before"); return; } pickTime = false; pickDaily = false; SetPickTarget(ii); OpenCarPick(); }, out var bf, 16, 3, 4, 0, "Correr");
            BotBox((RectTransform)b.transform, 16, 16, 16, 56);
            TB(bf.transform, !open ? "Locked" : level == 0 ? "Survive!" : done ? "Race again" : "Race!", 24, !open ? Muted : bb == Coral ? Cream : Ink);
        }
        void RefreshPickChrome()
        {
            if (pickItems.Count == 0) return;
            var (id, level) = pickItems[Mathf.Clamp(pickIndex, 0, pickItems.Count - 1)];
            if (level > 0)
            {
                string w = WorldOf(level); var worlds = CozyData.LevelKeys.Select(x => x.world).Distinct().ToList();
                worldChip.text = $"WORLD {worlds.IndexOf(w) + 1} · {w.ToUpper()}"; worldBg.color = WorldColor(w);
            }
            else { worldChip.text = "LAST CAR EACH LAP IS OUT"; worldBg.color = Peach; }
            foreach (Transform t in pickDots) Destroy(t.gameObject);
            for (int d = -3; d <= 3; d++)
            {
                int j = pickIndex + d; if (j < 0 || j >= pickItems.Count) continue;
                var dot = Sticker(pickDots, d == 0 ? Ink : Cream, out _, 5, 2, 0, 0, "Punto"); var le = dot.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = (d == 0 ? 26 : 10) * K; le.preferredHeight = 10 * K;
            }
        }

        // Silueta de una pista (vista desde arriba) para las tarjetas de nivel
        readonly Dictionary<string, Texture2D> silCache = new Dictionary<string, Texture2D>();
        Texture2D Silhouette(string[] layout)
        {
            string key = string.Join(",", layout);
            if (silCache.TryGetValue(key, out var tex)) return tex;
            var pts = new List<Vector2>(); Vector3 pos = Vector3.zero; float yaw = 0;
            foreach (var k in layout)
            {
                var p = Track.Parse(k); int steps = p.straight ? 1 : 8;
                for (int s = 0; s <= steps; s++)
                {
                    Track.Local(p, s / (float)steps, 0, out float lx, out float lz, out _);
                    var w = pos + Track.Right(yaw) * lx + Track.Fwd(yaw) * lz; pts.Add(new Vector2(w.x, w.z));
                }
                Track.Local(p, 1, 0, out float ex, out float ez, out _);
                pos += Track.Right(yaw) * ex + Track.Fwd(yaw) * ez; if (!p.straight) yaw += -p.s * p.A;
            }
            const int W = 360, H = 200; float pad = 18;
            Vector2 mn = new Vector2(pts.Min(v => v.x), pts.Min(v => v.y)), mx = new Vector2(pts.Max(v => v.x), pts.Max(v => v.y));
            float sc = Mathf.Min((W - 2 * pad) / Mathf.Max(0.01f, mx.x - mn.x), (H - 2 * pad) / Mathf.Max(0.01f, mx.y - mn.y));
            Vector2 off = new Vector2((W - (mx.x - mn.x) * sc) / 2, (H - (mx.y - mn.y) * sc) / 2);
            var q = pts.Select(v => (v - mn) * sc + off).ToList();
            var g = new Raster(W, H);
            for (int pass = 0; pass < 3; pass++)
            {
                float w = pass == 0 ? 22 : pass == 1 ? 15 : 2.2f; var c = pass == 0 ? Ink : pass == 1 ? CozyData.Hex("#5E5577") : Cream;
                for (int i = 1; i < q.Count; i++) { if (pass == 2 && i % 2 == 0) continue; g.Line(q[i - 1].x, q[i - 1].y, q[i].x, q[i].y, w, c); }
                for (int i = 0; i < q.Count && pass < 2; i++) g.Circle(q[i].x, q[i].y, w / 2, c);
            }
            tex = g.ToTexture(false); silCache[key] = tex; return tex;
        }

        // ================= ELEGIR VEHÍCULO =================
        Text carPickTitle, carCoins, carName, carRole; Transform carStickers, carUps, carBody; ScrollRect carScroll; string carTab = "up";
        readonly Dictionary<string, Image> carTabBtns = new Dictionary<string, Image>(); readonly Dictionary<string, Text> carTabTxt = new Dictionary<string, Text>();
        readonly Image[][] statPips = new Image[4][]; Image carRoleBg; Image carTitleBg;
        void BuildCarPick()
        {
            var s = MakeScreen("car").transform;
            Band(s, 120, 190, -9);
            ShowroomImage(s, 128, 270, 176);
            BackButton(s, () => { ShowScreen("pick"); RefreshPick(); });
            var tr = Sticker(s, MintB, out carTitleBg, 12, 3, 3, 1.5f, "Nivel"); TopCenter(tr, 22, 170, 34);
            carPickTitle = Cap(carTitleBg.transform, "", 13); Fill(carPickTitle.rectTransform);
            carCoins = CoinPill(s, 14, 21);
            foreach (var right in new[] { false, true })
            {
                var a = StickerButton(s, SunC, () => { CycleCar(right ? 1 : -1); RefreshCarPick(); }, out var af, 16, 3, 3, right ? 4 : -4, right ? "Siguiente" : "Anterior");
                TopAt((RectTransform)a.transform, 14, 196, 54, 54, right); IcnFill(af.transform, B(right ? "next" : "back"), 12);
            }
            // ficha del vehículo: etiqueta de caja de juguete
            var panel = Sticker(s, Cream, out var pf, 20, 3.5f, 5, -0.8f, "Ficha");
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = new Vector2(16 * K, 128 * K); panel.offsetMax = new Vector2(-16 * K, -366 * K);
            carName = TB(pf.transform, "", 30, Ink, true, TextAnchor.MiddleLeft); TopBox(carName.rectTransform, 16, 150, 12, 36);
            var role = Sticker(pf.transform, Coral, out carRoleBg, 9, 2.5f, 0, 0, "Rol"); TopAt(role, 14, 18, 140, 26, true);
            carRole = Cap(carRoleBg.transform, "", 10, Cream); Inset(carRole.rectTransform, 4, 0, 4, 0);
            for (int k = 0; k < 4; k++)
            {
                int col = k % 2, rowi = k / 2;
                var cell = R("Dato " + k, pf.transform); cell.anchorMin = new Vector2(col * 0.5f, 1); cell.anchorMax = new Vector2(col * 0.5f + 0.5f, 1);
                cell.offsetMin = new Vector2((col == 0 ? 16 : 7) * K, -(56 + rowi * 30 + 28) * K); cell.offsetMax = new Vector2(-(col == 0 ? 7 : 16) * K, -(56 + rowi * 30) * K);
                Cap(cell, CozyData.StatNames[k].ToUpper(), 10, null, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 0, 0, 0, 13));
                statPips[k] = new Image[5];
                for (int p = 0; p < 5; p++)
                {
                    var pip = Img(cell, Cream, null, "Pip"); Radius(pip, 3); var pr = pip.rectTransform; pr.anchorMin = new Vector2(p / 5f, 0); pr.anchorMax = new Vector2((p + 1) / 5f, 0); pr.pivot = new Vector2(0.5f, 0);
                    pr.offsetMin = new Vector2(1.5f * K, 0); pr.offsetMax = new Vector2(-1.5f * K, 10 * K); pip.gameObject.AddComponent<Outline>().effectColor = Ink; statPips[k][p] = pip;
                }
            }
            // pestañas: Upgrades · Stickers · Paint
            var tabs = R("Pestanas", pf.transform); TopBox(tabs, 14, 14, 122, 40);
            var th = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); th.spacing = 6 * K; th.childControlWidth = th.childControlHeight = true; th.childForceExpandWidth = th.childForceExpandHeight = true;
            foreach (var (id, label) in new[] { ("up", "Upgrades"), ("st", "Stickers"), ("sk", "Paint") })
            {
                var tid = id; var b = StickerButton(tabs, Cream, () => { carTab = tid; RefreshCarPick(); }, out var bf, 12, 2.5f, 2, 0, "Pestana");
                carTabBtns[id] = bf; carTabTxt[id] = TB(bf.transform, label, 16, Ink);
            }
            var holder = R("Contenido", pf.transform); holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one; holder.offsetMin = new Vector2(0, 10 * K); holder.offsetMax = new Vector2(0, -170 * K);
            var (sr, body) = Scroll(holder, 6); carScroll = sr; carBody = body;
            body.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Mathf.RoundToInt(14 * K), Mathf.RoundToInt(14 * K), 6, 20);
            // ¡A correr!
            var go = StickerButton(s, SunC, GoRace, out var gf, 20, 3.5f, 6, -1.2f, "Correr"); BotBox((RectTransform)go.transform, 20, 20, 24, 84);
            var chk = Checker(gf.transform, 7); var ckr = chk.rectTransform; ckr.anchorMin = Vector2.zero; ckr.anchorMax = new Vector2(0, 1); ckr.pivot = new Vector2(0, 0.5f); ckr.sizeDelta = new Vector2(40 * K, 0); ckr.anchoredPosition = Vector2.zero;
            TB(gf.transform, "RACE!", 40, Ink).rectTransform.Let(r => Inset(r, 40, 0, 0, 0));
        }
        void OpenCarPick()
        {
            if (!Usable(choice)) choice = CozyData.StartCars[0];
            ShowScreen("car"); UpdateShowroom(); RefreshCarPick();
        }
        void RefreshCarPick()
        {
            if (!carPickTitle) return;
            if (pickLevel > 0) { string w = WorldOf(pickLevel); carPickTitle.text = $"{(pickTime ? "TIME TRIAL · " : "")}LEVEL {pickLevel:00} · {w.ToUpper()}"; carTitleBg.color = pickTime ? SkyB : WorldColor(w); }
            else { carPickTitle.text = (pickMode == "elim" ? "SURVIVAL · " : pickTime ? "TIME TRIAL · " : "RACE · ") + CozyData.Circuit(pickCirc).n.ToUpper(); carTitleBg.color = pickMode == "elim" ? Peach : MintB; }
            carCoins.text = N0(prof.coins);
            var d = CozyData.Car(choice);
            carName.text = d.name; carRole.text = d.role.ToUpper(); carRoleBg.color = Coral;
            for (int k = 0; k < 4; k++) for (int p = 0; p < 5; p++) statPips[k][p].color = p < d.st[k] ? Coral : Cream;
            foreach (var kv in carTabBtns) { bool on = kv.Key == carTab; kv.Value.color = on ? SunC : Cream; }
            foreach (Transform t in carBody) Destroy(t.gameObject);
            if (carTab == "sk") { BuildPaintTab(carBody); RefreshHome(); return; }
            if (carTab == "st")
            {
                var sg = R("Pegatinas", carBody); var g = sg.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(46 * K, 46 * K); g.spacing = new Vector2(8 * K, 8 * K); g.childAlignment = TextAnchor.UpperCenter;
                sg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; carStickers = sg;
                string cur = StickerOf(choice);
                void AddSticker(string id, int i)
                {
                    bool owned = id == null || prof.stickers.Contains(id);
                    var cell = Sticker(carStickers, cur == id ? SunC : owned ? Color.white : Paper, out var cf, 23, 2.5f, 0, i % 2 == 0 ? -6 : 6, "Pegatina");
                    if (owned) cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { SetSticker(choice, id); UpdateShowroom(); RefreshCarPick(); });
                    if (id != null && owned) { var ri = R("Img", cf.transform).gameObject.AddComponent<RawImage>(); ri.texture = CozyTex.Get("sticker_" + id); Inset(ri.rectTransform, 4, 4, 4, 4); ri.raycastTarget = false; }
                    else TB(cf.transform, owned ? "—" : "?", 15, owned ? Ink : Muted, false);
                }
                AddSticker(null, 0); for (int i = 0; i < CozyData.StickerIds.Length; i++) AddSticker(CozyData.StickerIds[i], i + 1);
                RefreshHome(); return;
            }
            carUps = VList(carBody, 8 * K);
            var up = Upgrades(choice);
            for (int k = 0; k < 3; k++)
            {
                int kk = k; var row = R("Mejora", carUps); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 38 * K;
                TB(row, CozyData.UpgradeNames[k], 13, Ink, false, TextAnchor.MiddleLeft).rectTransform.Let(r => { r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f); r.sizeDelta = new Vector2(84 * K, 0); r.anchoredPosition = Vector2.zero; });
                for (int p = 0; p < 5; p++)
                {
                    var pip = Img(row, p < up[k] ? Blue : Cream, null, "Pip"); Radius(pip, 4); pip.gameObject.AddComponent<Outline>().effectColor = Ink;
                    var pr = pip.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0, 0.5f); pr.pivot = new Vector2(0, 0.5f); pr.sizeDelta = new Vector2(24 * K, 14 * K); pr.anchoredPosition = new Vector2((88 + p * 28) * K, 0);
                }
                bool max = up[k] >= 5;
                var b = StickerButton(row, max ? Paper : SunC, () => { if (max) return; if (BuyUpgrade(choice, kk)) { Toast("Upgraded!"); RefreshCarPick(); } else Toast("Not enough coins"); }, out var bf, 11, 2.5f, 2, 0, "Comprar");
                var br = (RectTransform)b.transform; br.anchorMin = br.anchorMax = new Vector2(1, 0.5f); br.pivot = new Vector2(1, 0.5f); br.sizeDelta = new Vector2(74 * K, 34 * K); br.anchoredPosition = Vector2.zero;
                if (!max) Icn(bf.transform, B("coin"), 5, 7, 16, 16);
                TB(bf.transform, max ? "MAX" : CozyData.UpgradeCost[up[k]].ToString(), 15, Ink, true, max ? TextAnchor.MiddleCenter : TextAnchor.MiddleRight).rectTransform.Let(r => Inset(r, 22, 0, 8, 0));
            }
            RefreshHome();
        }
        void GoRace()
        {
            PlayerPrefs.SetString("ctCar", choice);
            if (pickTime && pickLevel > 0) { mode = "time"; levelNum = 0; BuildCircuit(CozyData.Level(pickLevel, out _, out _)); StartRace(); return; }
            if (pickLevel > 0) { StartLevel(pickLevel); return; }
            mode = pickMode == "elim" ? "elim" : pickTime ? "time" : "race";
            levelNum = 0;
            BuildCircuit(CozyData.Circuit(pickCirc));
            StartRace();
        }

        // ================= AJUSTES =================
        readonly Dictionary<string, Image> lookBtns = new Dictionary<string, Image>(); readonly Dictionary<string, Text> lookTxt = new Dictionary<string, Text>();
        Image assistSw, unlockSw; RectTransform assistKnob, unlockKnob;
        void BuildSettings()
        {
            settingsPop = R("Ajustes", col).gameObject; Fill((RectTransform)settingsPop.transform);
            settingsPop.AddComponent<Image>().color = new Color(0.17f, 0.14f, 0.25f, 0.35f);
            var card = Sticker(settingsPop.transform, Cream, out var f, 22, 3, 5, -1, "Panel"); TopBox(card, 24, 24, 130, 470);
            Tape(card, 50, -60, -8, -10); Tape(card, 50, 70, 6, -10);
            TB(f.transform, "SETTINGS", 34, Ink).rectTransform.Let(r => TopBox(r, 10, 10, 18, 40));
            Cap(f.transform, "LOOK", 11, null, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 18, 18, 70, 16));
            var looks = R("Estilos", f.transform); TopBox(looks, 18, 18, 90, 60);
            var lh = looks.gameObject.AddComponent<HorizontalLayoutGroup>(); lh.spacing = 8 * K; lh.childControlWidth = lh.childControlHeight = true; lh.childForceExpandWidth = lh.childForceExpandHeight = true;
            foreach (var (id, n) in new[] { ("normal", "Normal"), ("toon", "Cel shading"), ("edge", "Comic") })
            {
                var i2 = id; StickerButton(looks, Cream, () => { SetLook(i2); RefreshSettings(); }, out var bf, 14, 3, 0, 0, "Estilo");
                lookBtns[id] = bf; lookTxt[id] = TB(bf.transform, n, 13, Ink, false);
            }
            ToggleRow(f.transform, 168, "Brake line", "Shows where to let go before curves", () => { assistOn = !assistOn; RefreshSettings(); }, out assistSw, out assistKnob);
            ToggleRow(f.transform, 228, "Everything unlocked", "Prototype: all vehicles and levels", () => { allUnlocked = !allUnlocked; PlayerPrefs.SetInt("ctAllUnlocked", allUnlocked ? 1 : 0); if (!Usable(choice)) choice = CozyData.StartCars[0]; MenuGrid(); RefreshSettings(); }, out unlockSw, out unlockKnob);
            profText = TB(f.transform, "", 12, Ink, false); TopBox(profText.rectTransform, 18, 18, 290, 34);
            var btns = R("Botones", f.transform); TopBox(btns, 18, 18, 338, 46);
            var bh = btns.gameObject.AddComponent<HorizontalLayoutGroup>(); bh.spacing = 10 * K; bh.childControlWidth = bh.childControlHeight = true; bh.childForceExpandWidth = bh.childForceExpandHeight = true;
            StickerButton(btns, Lilac, () => { settingsPop.SetActive(false); OpenAccount(); }, out var a1, 12, 3, 0, 0, "Cuenta"); Cap(a1.transform, "ACCOUNT", 12);
            StickerButton(btns, MintB, () => { settingsPop.SetActive(false); ShowScreen("album"); }, out var a2, 12, 3, 0, 0, "Album"); Cap(a2.transform, "ALBUM", 12);
            var done = StickerButton(f.transform, SunC, () => settingsPop.SetActive(false), out var df, 18, 3.5f, 5, 0, "Listo"); TopBox((RectTransform)done.transform, 18, 18, 394, 60);
            TB(df.transform, "DONE", 26, Ink);
            settingsPop.SetActive(false);
        }
        void ToggleRow(Transform p, float top, string title, string sub, System.Action on, out Image sw, out RectTransform knob)
        {
            var row = StickerButton(p, Cream, on, out var rf, 10, 0, 0, 0, "Opcion"); TopBox((RectTransform)row.transform, 18, 18, top, 50);
            row.transform.Find("Borde").GetComponent<Image>().color = new Color(0, 0, 0, 0);
            TB(rf.transform, title, 15, Ink, false, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 0, 2, 70, 24));
            TB(rf.transform, sub, 11, Ink, false, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 0, 28, 70, 2));
            var s = Sticker(rf.transform, MintB, out sw, 17, 3, 0, 0, "Interruptor"); s.anchorMin = s.anchorMax = new Vector2(1, 0.5f); s.sizeDelta = new Vector2(62 * K, 34 * K); s.anchoredPosition = new Vector2(-31 * K, 0);
            var kn = Sticker(sw.transform, Cream, out _, 11, 2.5f, 0, 0, "Bola"); kn.anchorMin = kn.anchorMax = new Vector2(0, 0.5f); kn.sizeDelta = new Vector2(22 * K, 22 * K); knob = kn;
        }
        void OpenSettings() { settingsPop.SetActive(true); RefreshSettings(); }
        void RefreshSettings()
        {
            foreach (var kv in lookBtns) { bool on = kv.Key == look; kv.Value.color = on ? Ink : Cream; lookTxt[kv.Key].color = on ? Cream : Ink; }
            void Sw(Image sw, RectTransform kn, bool v) { sw.color = v ? MintB : Paper; kn.anchoredPosition = new Vector2((v ? 42 : 14) * K, 0); }
            Sw(assistSw, assistKnob, assistOn); Sw(unlockSw, unlockKnob, allUnlocked);
            int owned = CozyData.Cars.Count(c => prof.owned.Contains(c.name));
            profText.text = $"{N0(prof.coins)} coins · {owned}/{CozyData.Cars.Length} vehicles · level {prof.level}/{CozyData.LevelCount}";
        }

        // ================= ÁLBUM (vehículos y pegatinas) =================
        Text albumSum, albumTabV, albumTabS; Image albumTabVBg, albumTabSBg; Transform albumBody; string albumTab = "v";
        void BuildAlbum()
        {
            var s = MakeScreen("album").transform;
            BackButton(s, ShowMenu);
            TB(s, "ALBUM", 36, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 12, 40));
            albumSum = Cap(s, "", 11, null, TextAnchor.MiddleRight); TopBox(albumSum.rectTransform, 120, 16, 52, 16);
            var tabs = R("Pestanas", s); TopBox(tabs, 20, 20, 92, 42);
            var th = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); th.spacing = 10 * K; th.childControlWidth = th.childControlHeight = true; th.childForceExpandWidth = th.childForceExpandHeight = true;
            StickerButton(tabs, Ink, () => { albumTab = "v"; RefreshAlbum(); }, out albumTabVBg, 12, 3, 3, -1, "Vehiculos"); albumTabV = Cap(albumTabVBg.transform, "VEHICLES", 13);
            StickerButton(tabs, Cream, () => { albumTab = "s"; RefreshAlbum(); }, out albumTabSBg, 12, 3, 3, 1, "Pegatinas"); albumTabS = Cap(albumTabSBg.transform, "STICKERS", 13);
            var holder = R("Contenido", s); holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one; holder.offsetMin = Vector2.zero; holder.offsetMax = new Vector2(0, -146 * K);
            var (_, body) = Scroll(holder, 10); albumBody = body;
            body.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Mathf.RoundToInt(14 * K), Mathf.RoundToInt(14 * K), 10, 60);
            body.GetComponent<VerticalLayoutGroup>().spacing = 16 * K;
        }
        void RefreshAlbum()
        {
            if (albumBody == null) return;
            foreach (Transform t in albumBody) Destroy(t.gameObject);
            int owned = CozyData.Cars.Count(c => Usable(c.name));
            albumSum.text = albumTab == "v" ? $"{owned} / {CozyData.Cars.Length} VEHICLES" : $"STICKERS {prof.stickers.Count} / {CozyData.StickerIds.Length}";
            albumTabVBg.color = albumTab == "v" ? Ink : Cream; albumTabV.color = albumTab == "v" ? Cream : Ink;
            albumTabSBg.color = albumTab == "s" ? Ink : Cream; albumTabS.color = albumTab == "s" ? Cream : Ink;
            albumTabV.text = albumTab == "v" ? "VEHICLES" : $"VEHICLES {owned}/{CozyData.Cars.Length}";
            albumTabS.text = albumTab == "s" ? "STICKERS" : $"STICKERS {prof.stickers.Count}/{CozyData.StickerIds.Length}";
            if (albumTab == "v")
            {
                foreach (var grp in CozyData.Cars.GroupBy(c => c.cat))
                {
                    Cap(albumBody, grp.Key.ToUpper(), 11, null, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().preferredHeight = 16 * K;
                    var shelf = R("Estante", albumBody); var g = shelf.gameObject.AddComponent<GridLayoutGroup>();
                    g.cellSize = new Vector2(80 * K, 92 * K); g.spacing = new Vector2(8 * K, 8 * K); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 4; g.childAlignment = TextAnchor.UpperCenter;
                    shelf.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    int i = 0;
                    foreach (var cd in grp)
                    {
                        bool own = Usable(cd.name); int p = own ? 3 : Pieces(cd.name);
                        var cell = Sticker(shelf, own ? Cream : Paper, out var cf, 12, 3, 0, i++ % 2 == 0 ? -1.5f : 1.5f, cd.name);
                        var sw = Sticker(cf.transform, own ? cd.color : CozyData.Hex("#C9C5D3"), out _, 8, 2.5f, 0, 0, "Color"); TopCenter(sw, 14, 46, 30);
                        TB(cf.transform, cd.name, 11, own ? Ink : Muted, false).rectTransform.Let(r => TopBox(r, 3, 3, 50, 16));
                        Cap(cf.transform, own ? "OWNED" : $"{p}/3 " + new string('●', p) + new string('○', 3 - p), 9, own ? Ink : Muted).rectTransform.Let(r => TopBox(r, 3, 3, 66, 14));
                    }
                    var plank = Img(albumBody, CozyData.Hex("#9A6A3E"), null, "Tabla"); plank.sprite = null; plank.gameObject.AddComponent<LayoutElement>().preferredHeight = 8 * K;
                    plank.gameObject.AddComponent<Shadow>().effectColor = Ink;
                }
            }
            else
            {
                var paper = Sticker(albumBody, Cream, out var pf, 18, 3, 5, -0.8f, "Hoja");
                var g = R("Rejilla", pf.transform); Inset(g, 12, 16, 12, 16);
                var gl = g.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(96 * K, 96 * K); gl.spacing = new Vector2(12 * K, 12 * K); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3; gl.childAlignment = TextAnchor.UpperCenter;
                int rows = Mathf.CeilToInt(CozyData.StickerIds.Length / 3f);
                paper.gameObject.AddComponent<LayoutElement>().preferredHeight = (rows * 108 + 32) * K;
                for (int k = 0; k < CozyData.StickerIds.Length; k++)
                {
                    bool has = prof.stickers.Contains(CozyData.StickerIds[k]);
                    var cell = Sticker(g, has ? Color.white : Paper, out var cf, 16, has ? 3 : 2, 0, ((k * 37) % 7) - 3, CozyData.StickerNames[k]);
                    if (has) { var ri = R("Img", cf.transform).gameObject.AddComponent<RawImage>(); ri.texture = CozyTex.Get("sticker_" + CozyData.StickerIds[k]); Inset(ri.rectTransform, 10, 8, 10, 22); ri.raycastTarget = false; }
                    else TB(cf.transform, "?", 34, Muted).rectTransform.Let(r => Inset(r, 0, 18, 0, 6));
                    Cap(cf.transform, has ? CozyData.StickerNames[k].ToUpper() : "???", 10).rectTransform.Let(r => BotBox(r, 2, 2, 4, 14));
                }
                var note = TB(albumBody, "Stickers come from chests. Put one on your car's roof before a race.", 12, Ink, false); note.gameObject.AddComponent<LayoutElement>().preferredHeight = 40 * K;
            }
        }

        // ================= ESCAPARATE 3D (se ve dentro de la franja azul) =================
        RawImage ShowroomImage(Transform p, float top, float w, float h)
        {
            if (!showRT) { showRT = new RenderTexture(1024, 640, 24) { name = "Escaparate", antiAliasing = 2 }; }
            var ri = R("Coche", p).gameObject.AddComponent<RawImage>(); ri.texture = showRT; ri.raycastTarget = false; TopCenter(ri.rectTransform, top, w, h);
            return ri;
        }
        void BuildShowroom()
        {
            if (showRoot) return;
            showRoot = new GameObject("Escaparate").transform; showRoot.position = ShowroomPos;
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.35f, 0), new Vector3(6.4f, 0.3f, 6.4f), showRoot, 0, Mat(CozyData.Hex("#fff3dc"), null, false, false, null, 1));
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.06f, 0), new Vector3(5.2f, 0.04f, 5.2f), showRoot, 0, Mat(CozyData.Hex("#ffd64d")));
            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(0, -0.03f, 0), new Vector3(4.6f, 0.04f, 4.6f), showRoot, 0, Mat(CozyData.Hex("#fff8ec")));
            showCam = new GameObject("Camara escaparate").AddComponent<Camera>();
            showCam.targetTexture = showRT; showCam.clearFlags = CameraClearFlags.SolidColor; showCam.backgroundColor = new Color(Blue.r, Blue.g, Blue.b, 0);
            showCam.fieldOfView = 30; showCam.nearClipPlane = 0.3f; showCam.farClipPlane = 30;
            showCam.fieldOfView = 24; showCam.cullingMask = 1 << ShowLayer;
            showCam.transform.position = ShowroomPos + new Vector3(0, 2.4f, -9.5f); showCam.transform.LookAt(ShowroomPos + new Vector3(0, 0.25f, 0));
            SetLayer(showRoot, ShowLayer); if (cam) cam.cullingMask &= ~(1 << ShowLayer);
        }
        void UpdateShowroom()
        {
            BuildShowroom();
            if (showCar) Destroy(showCar);
            showCar = SpawnCar(choice, showRoot, true); SetLayer(showCar.transform, ShowLayer);
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
    }

    // Carrusel horizontal que se queda centrado en una tarjeta al soltar (como el scroll-snap del diseño)
    public class LevelSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public ScrollRect scroll; public float step; public int count; public System.Action<int> onIndex;
        bool dragging; int target = -1, last = -1;
        public void OnBeginDrag(PointerEventData e) { dragging = true; target = -1; }
        public void OnEndDrag(PointerEventData e)
        {
            dragging = false; if (scroll == null) return;
            float x = -scroll.content.anchoredPosition.x - scroll.velocity.x * 0.12f;
            target = Mathf.Clamp(Mathf.RoundToInt(x / step), 0, Mathf.Max(0, count - 1)); scroll.velocity = Vector2.zero;
        }
        public void JumpTo(int i)
        {
            Canvas.ForceUpdateCanvases(); target = -1;
            if (scroll) { scroll.velocity = Vector2.zero; scroll.content.anchoredPosition = new Vector2(-i * step, scroll.content.anchoredPosition.y); }
            last = i; onIndex?.Invoke(i);
        }
        void Update()
        {
            if (scroll == null || count == 0) return;
            if (!dragging && target >= 0)
            {
                var p = scroll.content.anchoredPosition; float want = -target * step;
                p.x = Mathf.Lerp(p.x, want, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12)); scroll.content.anchoredPosition = p;
                if (Mathf.Abs(p.x - want) < 0.5f) { p.x = want; scroll.content.anchoredPosition = p; target = -1; }
            }
            int cur = Mathf.Clamp(Mathf.RoundToInt(-scroll.content.anchoredPosition.x / step), 0, count - 1);
            if (cur != last) { last = cur; onIndex?.Invoke(cur); }
        }
    }
}
