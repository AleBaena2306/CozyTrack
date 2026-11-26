using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CozyTrack
{
    // Interfaz hecha por código con uGUI, con el estilo B («caja de juguetes») del lienzo de diseño.
    // Piezas de estilo en CozyGame.Style.cs; pantallas de inicio en CozyGame.Home.cs; online en CozyGame.Online.cs.
    public partial class CozyGame
    {
        Canvas canvas; RectTransform canvasRect, col; Font font, fontBlack, fontBig;
        readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>();
        ScrollRect menuScroll; GameObject uiBg;
        Text posText, rivalText, lapText, coinText, timeText, curveLbl, itemName, itemHint, countText, countSub, toastText;
        Image fillImg, itemBg, flashImg, itemIcon, toastBg, curveBg, countDisc; RectTransform fillRect, safeRect, itemRect, speedLayer, toastRect, countRoot, hintRoot, rivalRoot;
        GameObject bubbles, adOverlay, rewardPop, pauseBtn, pausePop, bestChip, deltaRoot, timeRoot; Text bestText, deltaText; Image deltaBg;
        readonly Dictionary<string, Image> selButtons = new Dictionary<string, Image>();
        Text assistTxt, unlockTxt, profText;
        Text gName; Transform gStickers;
        Transform levelsGrid; Transform tallerRows; Text tallerTitle, tallerCoins;

        static readonly Color Cream = CozyData.Cream, Ink = CozyData.Ink, Coral = CozyData.Coral, SunC = CozyData.Sun, Muted = new Color(0.43f, 0.41f, 0.5f);

        // ---------- piezas básicas ----------
        RectTransform R(string name, Transform parent) { var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform; }
        static void Fill(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t); }
        Image Img(Transform p, Color c, Sprite s = null, string name = "Fondo")
        {
            var r = R(name, p); var i = r.gameObject.AddComponent<Image>(); i.color = c; i.sprite = s ? s : CozyTex.Round(); i.type = Image.Type.Sliced; return i;
        }
        Text T(Transform p, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, FontStyle st = FontStyle.Bold)
        {
            var r = R("Texto", p); Fill(r); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st == FontStyle.Bold ? FontStyle.Normal : st; t.raycastTarget = false;
            if (st == FontStyle.Bold && fontBlack) t.font = fontBlack;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, size / 2); t.resizeTextMaxSize = size;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.supportRichText = true;
            return t;
        }
        Text LayoutText(Transform p, string s, int size, Color c, float h, FontStyle st = FontStyle.Bold)
        {
            var t = T(p, s, size, c, TextAnchor.MiddleCenter, st); var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h; return t;
        }
        // Botón de lista (pantallas con layout): pegatina con contorno de tinta y sombra dura
        Button Btn(Transform p, string label, Color bg, Color fg, Action on, float h = 120, int size = 44, string key = null)
        {
            var img = Img(p, bg, null, "Boton " + label); Radius(img, 14); var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var rt = img.rectTransform; b.onClick.AddListener(() => { StartCoroutine(Press(rt)); on(); });
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h; le.flexibleWidth = 1;
            var t = T(img.transform, label, size, fg); Fill(t.rectTransform, 14, 6, 14, 6); t.font = fontBig;
            var ol = img.gameObject.AddComponent<Outline>(); ol.effectColor = Ink; ol.effectDistance = new Vector2(3 * K, 3 * K);
            var sh = img.gameObject.AddComponent<Shadow>(); sh.effectColor = Ink; sh.effectDistance = new Vector2(4 * K, -4 * K);
            if (key != null) selButtons[key] = img;
            return b;
        }
        RectTransform Row(Transform p, float h, float spacing = 14)
        {
            var r = R("Fila", p); var hl = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = spacing; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true; hl.childControlWidth = true; hl.childControlHeight = true; hl.childAlignment = TextAnchor.MiddleCenter;
            var le = r.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h; return r;
        }
        Text Label(Transform row, string s, float w) { var t = T(row, s, 40, Ink); var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = w; le.flexibleWidth = 0; return t; }
        RectTransform VList(Transform p, float spacing, RectOffset pad = null)
        {
            var r = R("Lista", p); var vl = r.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childForceExpandWidth = true; vl.childAlignment = TextAnchor.UpperCenter;
            if (pad != null) vl.padding = pad;
            return r;
        }
        // Pantalla entera: por defecto con cartón kraft de fondo
        GameObject MakeScreen(string name, bool backdrop = true)
        {
            var r = R(name, col); Fill(r);
            if (backdrop) KraftBg(r);
            screens[name] = r.gameObject; return r.gameObject;
        }
        (ScrollRect sr, RectTransform content) Scroll(Transform p, float padTop = 40)
        {
            var sv = R("Scroll", p); Fill(sv);
            var sr = sv.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            var vp = R("Vista", sv); Fill(vp); vp.gameObject.AddComponent<RectMask2D>(); var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0);
            var content = VList(vp, 22, new RectOffset(40, 40, (int)padTop, 60));
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp; sr.content = content;
            return (sr, content);
        }
        // Cabecera estándar: atrás, título en pegatina y (opcional) monedas
        Text Header(Transform p, string title, Color titleBg, Action back, bool coins = true, float rot = 1.5f)
        {
            BackButton(p, back);
            var r = Sticker(p, titleBg, out var f, 14, 3.5f, 4, rot, "Titulo"); TopCenter(r, 16, 150, 50);
            var t = TB(f.transform, title, 30, Ink); Inset(t.rectTransform, 6, 0, 6, 0);
            return coins ? CoinPill(p, 14, 21) : null;
        }

        // ---------- montaje ----------
        void BuildUI()
        {
            font = Resources.Load<Font>("Fonts/Nunito-ExtraBold"); fontBlack = Resources.Load<Font>("Fonts/Nunito-Black"); fontBig = Resources.Load<Font>("Fonts/Fredoka-Bold");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (!fontBlack) fontBlack = font; if (!fontBig) fontBig = fontBlack;
            var cg = new GameObject("Interfaz", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cg.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 1;
            canvasRect = (RectTransform)cg.transform;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            uiBg = KraftBg(cg.transform).gameObject;   // cartón a pantalla completa detrás de la columna (pantallas de menú)
            col = R("Columna", cg.transform); col.gameObject.AddComponent<RectMask2D>(); col.anchorMin = new Vector2(0.5f, 0); col.anchorMax = new Vector2(0.5f, 1); col.sizeDelta = new Vector2(1080, 0);

            // capas de efectos (debajo de todo lo demás)
            speedLayer = R("Lineas de velocidad", col); Fill(speedLayer);
            bubbles = R("Pompas", col).gameObject; Fill((RectTransform)bubbles.transform);
            var bubbleSpots = new[] { (-0.06f, 0.2f, 0.44f), (0.5f, 0.1f, 0.36f), (0.3f, 0.45f, 0.3f), (0.6f, 0.5f, 0.5f), (0.1f, 0.62f, 0.22f), (0.7f, 0.28f, 0.26f), (0.35f, 0.75f, 0.33f) };
            foreach (var (x, y, w) in bubbleSpots)
            {
                var im = Img(bubbles.transform, new Color(0.75f, 0.9f, 1f, 0.45f), CozyTex.CircleSprite(), "Pompa"); im.raycastTarget = false;
                var r = im.rectTransform; r.anchorMin = r.anchorMax = new Vector2(x + w / 2, 1 - y - w * 0.28f); r.sizeDelta = Vector2.one * w * 1080;
            }
            bubbles.SetActive(false);

            BuildHome(); BuildOnline(); BuildHud(); BuildResults(); BuildGarageUI(); BuildAlbum(); BuildLevels(); BuildTaller(); BuildSettings(); BuildPause();

            flashImg = Img(col, new Color(1, 1, 1, 0), null, "Destello"); Fill(flashImg.rectTransform); Soft(flashImg); flashImg.raycastTarget = false;
            // aviso: pegatina que aparece encima del coche
            var tr = Sticker(col, Cream, out toastBg, 14, 3, 4, -4, "Aviso"); toastRect = tr; TopCenter(tr, 236, 250, 46);
            toastText = TB(toastBg.transform, "", 24, Ink); Inset(toastText.rectTransform, 12, 0, 12, 0);
            tr.gameObject.SetActive(false);
            BuildCountdown();
            BuildChestPopup(); BuildPhotoPopup();
            adOverlay = R("Anuncio", col).gameObject; Fill((RectTransform)adOverlay.transform); adOverlay.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.95f);
            var at = TB(adOverlay.transform, "Test ad…\n(the publisher build plays its rewarded ad here)", 20, Cream); Inset(at.rectTransform, 30, 0, 30, 0);
            adOverlay.SetActive(false);
        }

        void LateUpdate()
        {
            if (col) col.sizeDelta = new Vector2(Mathf.Min(1080, canvasRect.rect.width), 0);
        }

        // ---------- HUD (diseño «In the race · HUD») ----------
        void BuildHud()
        {
            var s = MakeScreen("hud", false).transform;
            // puesto
            var pr = Sticker(s, Cream, out var pf, 16, 3, 4, -3, "Puesto"); TopAt(pr, 12, 14, 124, 66);
            posText = TB(pf.transform, "", 54, Ink, true, TextAnchor.MiddleLeft); Inset(posText.rectTransform, 12, 0, 6, 0); posText.resizeTextForBestFit = false; posText.verticalOverflow = VerticalWrapMode.Overflow; posText.horizontalOverflow = HorizontalWrapMode.Overflow;
            // pausa
            var pb = StickerButton(s, Cream, OpenPause, out var pbf, 12, 3, 3, 0, "Pausa"); TopCenter((RectTransform)pb.transform, 18, 44, 44); pauseBtn = pb.gameObject;
            IcnFill(pbf.transform, B("pause"), 7);
            // vuelta, monedas y tiempo (a la derecha, apilados)
            var lr = Sticker(s, Cream, out var lf, 12, 3, 4, 2, "Vuelta"); TopAt(lr, 12, 14, 96, 36, true);
            Cap(lf.transform, "LAP", 11, null, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 10, 0, 50, 0));
            lapText = TB(lf.transform, "1/3", 22, Ink, true, TextAnchor.MiddleRight); Inset(lapText.rectTransform, 30, 0, 10, 0);
            var cr = Sticker(s, Cream, out var cf, 12, 3, 4, -1.5f, "Monedas"); TopAt(cr, 12, 58, 82, 34, true);
            Icn(cf.transform, B("coin"), 5, 4, 24, 24);
            coinText = TB(cf.transform, "0", 20, Ink, true, TextAnchor.MiddleRight); Inset(coinText.rectTransform, 30, 0, 10, 0);
            var tmr = Sticker(s, Cream, out var tf, 12, 3, 4, 1, "Tiempo"); TopAt(tmr, 12, 100, 92, 32, true); timeRoot = tmr.gameObject;
            timeText = TB(tf.transform, "0:00.0", 18, Ink); Inset(timeText.rectTransform, 6, 0, 6, 0);
            // contrarreloj: récord y diferencia con el fantasma
            var br = Sticker(s, SkyB, out var bf, 12, 3, 4, -1.5f, "Record"); TopAt(br, 12, 140, 92, 40, true); bestChip = br.gameObject;
            Cap(bf.transform, "BEST", 9).rectTransform.Let(r => Inset(r, 0, 2, 0, 22));
            bestText = TB(bf.transform, "", 17, Ink); Inset(bestText.rectTransform, 4, 16, 4, 2);
            var dr = Sticker(s, MintB, out deltaBg, 14, 3, 4, -2, "Fantasma"); TopCenter(dr, 110, 130, 52, 60); deltaRoot = dr.gameObject;
            deltaText = TB(deltaBg.transform, "", 26, Ink); Inset(deltaText.rectTransform, 4, 0, 4, 14);
            Cap(deltaBg.transform, "VS YOUR GHOST", 9).rectTransform.Let(r => Inset(r, 0, 32, 0, 2));
            deltaRoot.SetActive(false); bestChip.SetActive(false);
            // objeto: arriba a la izquierda, bajo el puesto (se toca para usarlo)
            var ir = Sticker(s, Cream, out itemBg, 22, 3.5f, 5, 3, "Objeto"); TopAt(ir, 32, 101, 93, 89); itemRect = ir;
            Tape(ir, 34);
            itemIcon = Img(itemBg.transform, Color.white, null, "Icono"); itemIcon.type = Image.Type.Simple; itemIcon.preserveAspect = true; itemIcon.raycastTarget = false;
            TopCenter(itemIcon.rectTransform, 6, 40, 40);
            itemName = TB(itemBg.transform, "NO ITEM", 13, Ink); TopBox(itemName.rectTransform, 2, 2, 47, 16);
            itemHint = Cap(itemBg.transform, "GRAB A BOX", 8); TopBox(itemHint.rectTransform, 2, 2, 64, 12);
            // aviso de rival / regla del modo (centrado, bajo el objeto)
            rivalRoot = Sticker(s, Cream, out var rf, 12, 3, 4, -1.5f, "Rival"); TopCenter(rivalRoot, 206, 240, 32);
            rivalText = TB(rf.transform, "", 12, Ink, false); Inset(rivalText.rectTransform, 10, 0, 10, 0);
            // velocímetro (abajo a la izquierda) con la velocidad segura de la próxima curva
            var sp = Sticker(s, Cream, out var spf, 16, 3, 4, -1.5f, "Velocimetro");
            sp.anchorMin = sp.anchorMax = new Vector2(0, 0); sp.pivot = new Vector2(0.5f, 0.5f); sp.sizeDelta = new Vector2(236 * K, 74 * K); sp.anchoredPosition = new Vector2((12 + 118) * K, (22 + 37) * K);
            Cap(spf.transform, "SPEED", 11, null, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 12, 100, 8, 20));
            var cl = Sticker(spf.transform, Cream, out curveBg, 8, 2.5f, 0, 2, "Curva"); TopAt(cl, 10, 6, 120, 22, true);
            curveLbl = TB(curveBg.transform, "", 11, Ink, false); Inset(curveLbl.rectTransform, 4, 0, 4, 0);
            var barRoot = R("Barra", spf.transform); TopBox(barRoot, 12, 12, 40, 20);
            fillImg = Bar(barRoot, SunC, 0, 10, 3); fillRect = fillImg.rectTransform; Fill((RectTransform)fillImg.transform.parent.parent);
            var safe = Img(barRoot, Ink, null, "Limite"); Soft(safe); safeRect = safe.rectTransform; safe.raycastTarget = false;
            safeRect.anchorMin = new Vector2(0.7f, -0.45f); safeRect.anchorMax = new Vector2(0.7f, 1.45f); safeRect.sizeDelta = new Vector2(6 * K, 0); safeRect.anchoredPosition = Vector2.zero;
            var sl = Cap(safe.transform, "SAFE", 8); var slr = sl.rectTransform; slr.anchorMin = slr.anchorMax = new Vector2(0.5f, 0); slr.sizeDelta = new Vector2(40 * K, 10 * K); slr.anchoredPosition = new Vector2(0, -2 * K);
        }

        // cuenta atrás: disco amarillo con el número, el circuito y cómo se juega
        void BuildCountdown()
        {
            countRoot = R("Cuenta atras", col); Fill(countRoot);
            var d = Sticker(countRoot, SunC, out countDisc, 85, 4, 6, -6, "Disco"); TopCenter(d, 250, 170, 170);
            countText = TB(countDisc.transform, "", 110, Ink); Fill(countText.rectTransform); countText.resizeTextForBestFit = true;
            var ch = Sticker(countRoot, Cream, out var cf, 12, 3, 4, 2, "Circuito"); TopCenter(ch, 446, 250, 30);
            countSub = Cap(cf.transform, "", 12); Inset(countSub.rectTransform, 8, 0, 8, 0);
            hintRoot = R("Ayuda", countRoot); BotBox(hintRoot, 30, 30, 160, 56);
            foreach (var (big, small, x, rot) in new[] { ("HOLD", "ANYWHERE TO GO", -76f, -2f), ("SWIPE ⟷", "TO CHANGE LANE", 76f, 2f) })
            {
                var h = Sticker(hintRoot, Cream, out var hf, 14, 3, 4, rot, "Ayuda"); h.anchorMin = h.anchorMax = new Vector2(0.5f, 0.5f); h.sizeDelta = new Vector2(140 * K, 54 * K); h.anchoredPosition = new Vector2(x * K, 0);
                TB(hf.transform, big, 20, Ink).rectTransform.Let(r => Inset(r, 4, 4, 4, 22));
                Cap(hf.transform, small, 10).rectTransform.Let(r => Inset(r, 4, 32, 4, 4));
            }
            countRoot.gameObject.SetActive(false);
        }
        void SetCount(string t)
        {
            if (!countRoot) return;
            bool on = !string.IsNullOrEmpty(t) && screens.TryGetValue("hud", out var h) && h.activeSelf;
            countRoot.gameObject.SetActive(on);
            if (!on) return;
            countText.text = t; countText.fontSize = Mathf.RoundToInt((t.Length > 1 ? 64 : 110) * K);
            countDisc.color = t == "GO!" ? MintB : SunC;
            hintRoot.gameObject.SetActive(t != "GO!");
            string where = circ != null ? (circ.id.StartsWith("nivel") ? CozyData.Level(levelNum > 0 ? levelNum : 1, out var k, out _).desc.ToUpper() : circ.n.ToUpper()) : "";
            string what = mode == "level" ? $"LEVEL {levelNum:00}" : mode == "elim" ? "SURVIVAL" : mode == "time" ? "TIME TRIAL" : mode == "online" ? "ONLINE 1 VS 1" : mode == "daily" ? "DAILY CHALLENGE" : "RACE";
            countSub.text = $"{where} · {CozyData.Tod(todId).n.ToUpper()} · {what}";
        }

        // ---------- pausa ----------
        void BuildPause()
        {
            pausePop = R("Pausa", col).gameObject; Fill((RectTransform)pausePop.transform);
            var dim = pausePop.AddComponent<Image>(); dim.color = new Color(0.17f, 0.14f, 0.25f, 0.55f);
            var card = Sticker(pausePop.transform, Cream, out var f, 22, 3, 5, -1.5f, "Panel"); TopBox(card, 32, 32, 180, 420);
            Tape(card, 50, -60, -8, -10); Tape(card, 50, 60, 6, -10);
            TB(f.transform, "PAUSED", 44, Ink).rectTransform.Let(r => TopBox(r, 10, 10, 18, 48));
            var sub = Cap(f.transform, "", 11); TopBox(sub.rectTransform, 10, 10, 66, 16); pauseSub = sub;
            var res = StickerButton(f.transform, SunC, ClosePause, out var rf, 18, 3.5f, 5, 1, "Seguir"); TopBox((RectTransform)res.transform, 20, 20, 96, 70);
            TB(rf.transform, "RESUME", 30, Ink);
            var rst = StickerButton(f.transform, Cream, () => { ClosePause(); Replay(); }, out var sf, 16, 3.5f, 4, -1, "Reiniciar"); TopBox((RectTransform)rst.transform, 20, 20, 182, 58);
            TB(sf.transform, "RESTART", 22, Ink);
            var tg = StickerButton(f.transform, MintB, () => { assistOn = !assistOn; RefreshPause(); }, out pauseAssist, 14, 3, 0, 0, "Linea"); TopBox((RectTransform)tg.transform, 20, 20, 258, 58);
            Cap(pauseAssist.transform, "BRAKE LINE", 10).rectTransform.Let(r => Inset(r, 0, 6, 0, 30));
            pauseAssistTxt = TB(pauseAssist.transform, "ON", 18, Ink); Inset(pauseAssistTxt.rectTransform, 0, 24, 0, 4);
            var q = StickerButton(f.transform, Cream, () => { ClosePause(); QuitRace(); }, out var qf, 10, 0, 0, 0, "Salir"); TopBox((RectTransform)q.transform, 60, 60, 336, 40);
            qf.color = new Color(0, 0, 0, 0); q.transform.Find("Borde").GetComponent<Image>().color = new Color(0, 0, 0, 0);
            TB(qf.transform, "<b>QUIT TO HOME</b>", 13, Ink, false);
            pausePop.SetActive(false);
        }
        Text pauseSub, pauseAssistTxt; Image pauseAssist;
        void OpenPause()
        {
            if (mode == "online" || (state != "race" && state != "count")) return;
            Time.timeScale = 0; pausePop.SetActive(true); RefreshPause();
        }
        void RefreshPause()
        {
            int lp = player == null ? 1 : Prog(player) < 0.5f ? 1 : Mathf.Min(laps, Mathf.FloorToInt((Prog(player) - 0.5f) / Track.N) + 1);
            pauseSub.text = $"{(mode == "level" ? "LEVEL " + levelNum.ToString("00") : mode == "elim" ? "SURVIVAL" : mode == "time" ? "TIME TRIAL" : "RACE")} · {(circ != null ? circ.n.Split('·').Last().Trim().ToUpper() : "")} · LAP {lp}/{laps}";
            pauseAssist.color = assistOn ? MintB : Cream; pauseAssistTxt.text = assistOn ? "ON" : "OFF";
        }
        void ClosePause() { Time.timeScale = 1; if (pausePop) pausePop.SetActive(false); }
        void Replay()
        {
            if (mode == "daily") StartDaily(); else if (mode == "level") StartLevel(levelNum);
            else if (mode == "online") { mode = "race"; FindRace(); } else StartRace();
        }
        void QuitRace()
        {
            if (mode == "online") { CloseNet(); mode = "race"; MenuGrid(); OpenOnline(); return; }
            if (mode == "daily" || mode == "level" || mode == "time") { mode = "race"; if (circId.StartsWith("nivel")) BuildCircuit(CozyData.Circuit(PlayerPrefs.GetString("ctCirc", "jardin"))); }
            MenuGrid(); ShowMenu();
        }

        // ---------- resultados (diseño «After the race») ----------
        class ResultView
        {
            public string big, sub, back = "‹ HOME"; public Color disc = SunC;
            public List<(string place, string name, string time, bool me)> rows = new List<(string, string, string, bool)>();
            public List<(string value, string label)> stats = new List<(string, string)>();
            public string chestTier, chestTitle, chestSub, tip, primary = "REPLAY", secondary = "HOME"; public Color primaryBg = SunC;
            public Action onPrimary, onSecondary, onBack; public bool photo;
        }
        Text resBig, resSub, resBack; Image resDisc; RectTransform resRows, resStats, resCard; Text resPrimary, resSecondary, resCardTitle, resCardSub, resTip; Image resCardBg, resCardChest, resPrimaryBg;
        GameObject resCam, resX2; Action resOnPrimary, resOnSecondary, resOnBack;
        void BuildResults()
        {
            var s = MakeScreen("results").transform;
            var bk = StickerButton(s, Cream, () => resOnBack?.Invoke(), out var bkf, 12, 3, 4, -3, "Atras"); TopAt((RectTransform)bk.transform, 14, 18, 100, 34);
            resBack = TB(bkf.transform, "‹ HOME", 14, Ink, false); Inset(resBack.rectTransform, 4, 0, 4, 0);
            var cam = StickerButton(s, Cream, () => photoPop.SetActive(true), out var camf, 14, 3, 4, 3, "Foto"); TopAt((RectTransform)cam.transform, 14, 16, 50, 50, true);
            IcnFill(camf.transform, B("camera"), 8); resCam = cam.gameObject;
            // escarapela con el puesto
            var ros = R("Escarapela", s); TopCenter(ros, 76, 150, 170);
            foreach (var (x, c, rot) in new[] { (-28f, Coral, 14f), (28f, Blue, -14f) })
            {
                var rb = Img(ros, Ink, null, "Cinta"); Soft(rb); var rr = rb.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1); rr.sizeDelta = new Vector2(32 * K, 70 * K); rr.anchoredPosition = new Vector2(x * K, -135 * K); Tilt(rb.transform, rot);
                var rf = Img(rb.transform, c, null, "Color"); Soft(rf); Inset(rf.rectTransform, 3, 0, 3, 3);
            }
            var disc = Sticker(ros, SunC, out resDisc, 65, 3.5f, 5, 0, "Disco"); TopCenter(disc, 0, 130, 130);
            resBig = TB(resDisc.transform, "", 50, Ink); Inset(resBig.rectTransform, 6, 12, 6, 36);
            resSub = Cap(resDisc.transform, "", 11); Inset(resSub.rectTransform, 8, 90, 8, 18);
            // filas de puestos
            resRows = VList(s, 6 * K); TopBox(resRows, 22, 22, 270, 190);
            // ×2 monedas con vídeo (sobre el recuadro de monedas)
            var x2 = StickerButton(s, Blue, DoubleCoins, out var xf, 18, 3, 4, -2, "x2"); TopAt((RectTransform)x2.transform, 90, 466, 46, 37); resX2 = x2.gameObject;
            Icn(xf.transform, B("video"), 4, 7, 17, 17);
            var xt = TB(xf.transform, "×2", 15, Cream, true, TextAnchor.MiddleRight); Inset(xt.rectTransform, 18, 0, 3, 0); xt.gameObject.AddComponent<Outline>().effectColor = Ink;
            // estadísticas
            resStats = R("Estadisticas", s); TopBox(resStats, 22, 22, 487, 56);
            var hl = resStats.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10 * K; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            // cofre o consejo
            resCard = Sticker(s, SkyB, out resCardBg, 16, 3, 4, 1, "Tarjeta"); TopBox(resCard, 22, 22, 580, 84);
            Tape(resCard, 34, 0, -5);
            resCardChest = Icn(resCardBg.transform, null, 8, 2, 72, 72);
            resCardTitle = TB(resCardBg.transform, "", 21, Ink, true, TextAnchor.MiddleLeft); Inset(resCardTitle.rectTransform, 88, 12, 10, 40);
            resCardSub = TB(resCardBg.transform, "", 12, Ink, false, TextAnchor.UpperLeft); Inset(resCardSub.rectTransform, 88, 44, 10, 6);
            resTip = TB(resCardBg.transform, "", 14, Ink, false, TextAnchor.MiddleLeft); Inset(resTip.rectTransform, 14, 6, 14, 6);
            // botones
            var bar = R("Botones", s); BotBox(bar, 20, 20, 60, 84);
            var p1 = StickerButton(bar, SunC, () => resOnPrimary?.Invoke(), out resPrimaryBg, 18, 3.5f, 5, -1.5f, "Principal");
            var p1r = (RectTransform)p1.transform; p1r.anchorMin = Vector2.zero; p1r.anchorMax = new Vector2(0.6f, 1); p1r.offsetMin = Vector2.zero; p1r.offsetMax = new Vector2(-7 * K, 0);
            resPrimary = TB(resPrimaryBg.transform, "", 26, Ink); Inset(resPrimary.rectTransform, 8, 0, 8, 0);
            var p2 = StickerButton(bar, Cream, () => resOnSecondary?.Invoke(), out var p2f, 18, 3.5f, 5, 1.5f, "Secundario");
            var p2r = (RectTransform)p2.transform; p2r.anchorMin = new Vector2(0.6f, 0); p2r.anchorMax = Vector2.one; p2r.offsetMin = new Vector2(7 * K, 0); p2r.offsetMax = Vector2.zero;
            resSecondary = TB(p2f.transform, "", 22, Ink); Inset(resSecondary.rectTransform, 8, 0, 8, 0);
        }
        void ShowResultView(ResultView v)
        {
            x2Coins = x2Pending; x2Pending = 0;
            ShowScreen("results");
            resBack.text = v.back; resOnBack = v.onBack ?? (() => QuitRace());
            resBig.text = v.big; resSub.text = v.sub ?? ""; resDisc.color = v.disc;
            resBig.fontSize = Mathf.RoundToInt((v.big.Length > 4 ? 30 : 50) * K);
            foreach (Transform t in resRows) Destroy(t.gameObject);
            for (int k = 0; k < v.rows.Count && k < 4; k++)
            {
                var (pl, nm, tm, me) = v.rows[k];
                var row = Sticker(resRows, me ? SunC : Cream, out var rf, 12, 3, 0, k % 2 == 0 ? 0.8f : -0.8f, "Fila");
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40 * K;
                TB(rf.transform, pl, 20, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 10, 0, 0, 0));
                TB(rf.transform, nm, 14, Ink, false, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 46, 0, 80, 0));
                TB(rf.transform, tm, 16, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => Inset(r, 0, 0, 10, 0));
            }
            foreach (Transform t in resStats) Destroy(t.gameObject);
            for (int k = 0; k < v.stats.Count; k++)
            {
                var (val, lab) = v.stats[k];
                var tile = Sticker(resStats, Cream, out var tf, 12, 3, 4, k == 0 ? -2 : k == 1 ? 1.5f : -1, "Dato");
                TB(tf.transform, val, 22, Ink).rectTransform.Let(r => Inset(r, 4, 4, 4, 18));
                Cap(tf.transform, lab, 9).rectTransform.Let(r => Inset(r, 4, 38, 4, 4));
            }
            resStats.gameObject.SetActive(v.stats.Count > 0);
            resX2.SetActive(x2Coins > 0 && v.stats.Count > 0);
            bool chest = !string.IsNullOrEmpty(v.chestTier), tip = !string.IsNullOrEmpty(v.tip);
            resCard.gameObject.SetActive(chest || tip);
            resCardChest.gameObject.SetActive(chest); resCardTitle.gameObject.SetActive(chest); resCardSub.gameObject.SetActive(chest); resTip.gameObject.SetActive(!chest && tip);
            if (chest) { resCardChest.sprite = ChestSprite(v.chestTier); resCardTitle.text = v.chestTitle; resCardSub.text = v.chestSub; resCardBg.color = ChestColor(v.chestTier); }
            else { resTip.text = "<b>TIP</b>\n" + v.tip; resCardBg.color = Cream; }
            resPrimary.text = v.primary; resPrimaryBg.color = v.primaryBg; resPrimary.color = v.primaryBg == Coral ? Cream : Ink;
            resSecondary.text = v.secondary; resOnPrimary = v.onPrimary ?? Replay; resOnSecondary = v.onSecondary ?? QuitRace;
            resCam.SetActive(v.photo && photoReady);
            if (lineGo) lineGo.SetActive(false);
        }
        // Resultado sencillo (online, conexión perdida…): filas «nombre\ttiempo» y líneas de texto
        void ShowResults(string big, string msg, List<string> rows, List<string> chest)
        {
            var v = new ResultView { big = big, sub = "", tip = msg + (chest != null && chest.Count > 0 ? "\n" + string.Join("\n", chest) : "") };
            foreach (var r in rows) { var p = r.Split('\t'); v.rows.Add(("", p[0], p.Length > 1 ? p[1] : "", false)); }
            if (mode == "online") { v.back = "‹ ONLINE"; v.primary = "RACE AGAIN"; v.secondary = "DONE"; }
            ShowResultView(v);
        }
        static Color ChestColor(string tier) => tier == "legendary" ? CozyData.Hex("#FFE07A") : tier == "epic" ? CozyData.Hex("#D9C8F7") : tier == "rare" ? CozyData.Hex("#BFE0F7") : CozyData.Hex("#FFF4E0");

        // ×2 monedas viendo un vídeo
        int x2Pending, x2Coins;
        void OfferDouble(int coins) => x2Pending = coins;
        void DoubleCoins()
        {
            if (x2Coins <= 0) return;
            int c = x2Coins; x2Coins = 0; resX2.SetActive(false);
            StartCoroutine(FakeAd(() => { prof.coins += c; SaveProfile(); Toast($"+{c} coins!"); RefreshHome(); }));
        }

        // ---------- foto del podio (ventana con polaroid) ----------
        GameObject photoPop; RawImage photoImg; Text photoLine1, photoLine2; bool photoReady;
        void BuildPhotoPopup()
        {
            photoPop = R("Foto", col).gameObject; Fill((RectTransform)photoPop.transform); KraftBg(photoPop.transform);
            TB(photoPop.transform, "PHOTO", 38, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 14, 44));
            BackButton(photoPop.transform, () => photoPop.SetActive(false));
            var pol = Sticker(photoPop.transform, Cream, out var pf, 3, 3, 6, -3, "Polaroid"); TopBox(pol, 30, 30, 110, 470);
            Tape(pol, 54, -100, -12, -10); Tape(pol, 54, 100, 10, -10);
            var frame = Img(pf.transform, Ink, null, "Marco"); Soft(frame); TopBox(frame.rectTransform, 14, 14, 14, 380);
            photoImg = R("Imagen", frame.transform).gameObject.AddComponent<RawImage>(); Inset(photoImg.rectTransform, 2.5f, 2.5f, 2.5f, 2.5f);
            photoLine1 = TB(pf.transform, "", 22, Ink); BotBox(photoLine1.rectTransform, 14, 14, 34, 30);
            photoLine2 = TB(pf.transform, "", 12, Ink, false); BotBox(photoLine2.rectTransform, 14, 14, 14, 20);
            var done = StickerButton(photoPop.transform, SunC, () => photoPop.SetActive(false), out var df, 18, 3.5f, 5, -1.5f, "Listo"); BotBox((RectTransform)done.transform, 40, 40, 60, 80);
            TB(df.transform, "NICE!", 28, Ink);
            photoPop.SetActive(false);
        }
        void ShowPhoto(Texture t, string l1, string l2) { photoImg.texture = t; photoLine1.text = l1; photoLine2.text = l2; photoReady = true; if (resCam && screens["results"].activeSelf) resCam.SetActive(true); }

        // ---------- apertura de cofre (diseño «Chest opening») ----------
        Text rewardTitle; Image rewardChest; Transform rewardGrid;
        void BuildChestPopup()
        {
            rewardPop = R("Recompensa", col).gameObject; Fill((RectTransform)rewardPop.transform); KraftBg(rewardPop.transform);
            var rays = Img(rewardPop.transform, new Color(1, 0.81f, 0.24f, 0.35f), CozyTex.CircleSprite(), "Brillo"); rays.type = Image.Type.Simple; rays.raycastTarget = false; TopCenter(rays.rectTransform, 120, 420, 420);
            rewardTitle = TB(rewardPop.transform, "", 34, Ink); TopBox(rewardTitle.rectTransform, 20, 20, 80, 44);
            Cap(rewardPop.transform, "HERE'S WHAT'S INSIDE", 11).rectTransform.Let(r => TopBox(r, 20, 20, 124, 16));
            rewardChest = Icn(rewardPop.transform, null, 0, 0, 10, 10); TopCenter(rewardChest.rectTransform, 160, 190, 190); Tilt(rewardChest.transform, -4);
            rewardGrid = R("Botin", rewardPop.transform); TopBox((RectTransform)rewardGrid, 22, 22, 380, 320);
            var g = rewardGrid.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(160 * K, 100 * K); g.spacing = new Vector2(12 * K, 12 * K); g.childAlignment = TextAnchor.UpperCenter; g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 2;
            var ok = StickerButton(rewardPop.transform, SunC, () => { rewardPop.SetActive(false); RefreshHome(); }, out var of, 18, 3.5f, 5, -1, "Recoger"); BotBox((RectTransform)ok.transform, 20, 20, 44, 80);
            TB(of.transform, "COLLECT", 30, Ink);
            rewardPop.SetActive(false);
        }
        // Botín: cada línea «título|subtítulo|color»
        void ShowChestReward(string tier, List<string> loot)
        {
            rewardTitle.text = TierName(tier) + " chest!"; rewardChest.sprite = ChestSprite(tier);
            foreach (Transform t in rewardGrid) Destroy(t.gameObject);
            for (int k = 0; k < loot.Count; k++)
            {
                var p = loot[k].Split('|');
                var c = p.Length > 2 ? CozyData.Hex(p[2]) : Cream;
                var tile = Sticker(rewardGrid, c, out var tf, 16, 3, 4, k % 2 == 0 ? -2 : 1.5f, "Premio");
                TB(tf.transform, p[0], 17, Ink).rectTransform.Let(r => Inset(r, 8, 14, 8, 34));
                Cap(tf.transform, p.Length > 1 ? p[1] : "", 10).rectTransform.Let(r => Inset(r, 8, 66, 8, 10));
            }
            rewardPop.SetActive(true); RefreshHome();
        }
        static string TierName(string tier) => string.IsNullOrEmpty(tier) ? "Common" : char.ToUpper(tier[0]) + tier.Substring(1);

        // ---------- garaje 3D (estantería con toda la colección; orden «garage» de pruebas) ----------
        void BuildGarageUI()
        {
            var s = MakeScreen("garage", false);
            var panel = Sticker(s.transform, Cream, out var pf, 22, 3.5f, 5, 0, "Panel"); BotBox(panel, 12, 12, 16, 270);
            var v = VList(pf.transform, 14, new RectOffset(30, 30, 30, 40)); Fill(v);
            gName = LayoutText(v, "", 46, Ink, 70); gName.font = fontBig;
            LayoutText(v, "Tap a vehicle on the shelf · sticker for the roof", 30, Muted, 50, FontStyle.Normal);
            var gr = R("Pegatinas", v); var grid = gr.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(150, 150); grid.spacing = new Vector2(14, 14); grid.childAlignment = TextAnchor.MiddleCenter;
            gr.gameObject.AddComponent<LayoutElement>().preferredHeight = 330; gStickers = gr;
            Btn(v, "DONE", SunC, Ink, CloseGarage, 120, 46);
        }
        void RefreshGarageUI()
        {
            if (gName == null) return;
            var d = CozyData.Car(choice);
            gName.text = $"{choice} · {d.role}";
            foreach (Transform t in gStickers) Destroy(t.gameObject);
            string cur = StickerOf(choice);
            void Add(string id, string label)
            {
                bool owned = id == null || prof.stickers.Contains(id);
                var img = Img(gStickers, cur == id ? Ink : Color.white, null, "Pegatina"); var b = img.gameObject.AddComponent<Button>();
                if (owned) b.onClick.AddListener(() => { SetSticker(choice, id); BuildGarage(); RefreshGarageUI(); });
                if (id != null && owned) { var ri = R("Img", img.transform).gameObject.AddComponent<RawImage>(); ri.texture = CozyTex.Get("sticker_" + id); Fill(ri.rectTransform, 10, 10, 10, 10); ri.raycastTarget = false; }
                else { var t = T(img.transform, owned ? label : "?", owned ? 28 : 60, owned ? (cur == id ? Cream : Ink) : Muted); Fill(t.rectTransform, 6, 6, 6, 6); }
                if (!owned) img.color = Paper;
            }
            Add(null, "No\nsticker");
            for (int k = 0; k < CozyData.StickerIds.Length; k++) Add(CozyData.StickerIds[k], CozyData.StickerNames[k]);
        }

        // ---------- lista de niveles y taller (pantallas de prueba: «levels», «taller») ----------
        void BuildLevels()
        {
            var s = MakeScreen("levels");
            var (_, c) = Scroll(s.transform);
            LayoutText(c, "LEVELS", 96, Ink, 120).font = fontBig;
            var gr = R("Rejilla", c); var grid = gr.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(150, 130); grid.spacing = new Vector2(16, 16); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
            gr.gameObject.AddComponent<LayoutElement>().preferredHeight = 6 * 146; levelsGrid = gr;
            Btn(c, "BACK", Cream, Ink, ShowMenu, 120, 44);
        }
        void RefreshLevels()
        {
            foreach (Transform t in levelsGrid) Destroy(t.gameObject);
            for (int n = 1; n <= CozyData.LevelCount; n++)
            {
                int nn = n; bool open = n <= prof.level + 1 || allUnlocked, done = n <= prof.level;
                var b = Btn(levelsGrid, n.ToString("00"), done ? MintB : open ? SunC : Paper, open ? Ink : Muted, () => { if (open) StartLevel(nn); else Toast("Beat the level before"); }, 130, 50);
            }
        }
        void BuildTaller()
        {
            var s = MakeScreen("taller");
            var (_, c) = Scroll(s.transform);
            LayoutText(c, "GARAGE", 96, Ink, 120).font = fontBig;
            tallerTitle = LayoutText(c, "", 40, Ink, 70);
            tallerCoins = LayoutText(c, "", 34, Muted, 60, FontStyle.Normal);
            tallerRows = VList(c, 18);
            LayoutText(c, "Each upgrade gives less than the one before. Speed up to +20 %, acceleration up to +33 % and brakes up to +40 %.", 30, Muted, 100, FontStyle.Normal);
            Btn(c, "BACK", Cream, Ink, ShowMenu, 120, 44);
        }
        void RefreshTaller()
        {
            foreach (Transform t in tallerRows) Destroy(t.gameObject);
            tallerTitle.text = $"{choice} · {CozyData.Car(choice).role}"; tallerCoins.text = $"{prof.coins} coins";
            var up = Upgrades(choice);
            for (int k = 0; k < 3; k++)
            {
                int kk = k; var row = Row(tallerRows, 120);
                Label(row, CozyData.UpgradeNames[k], 260);
                var pips = R("Niveles", row); pips.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                for (int p = 0; p < 5; p++) { var pip = Img(pips, p < up[k] ? Blue : Cream); var pr = pip.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0, 0.5f); pr.pivot = new Vector2(0, 0.5f); pr.anchoredPosition = new Vector2(p * 62, 0); pr.sizeDelta = new Vector2(52, 24); pip.gameObject.AddComponent<Outline>().effectColor = Ink; }
                string label = up[k] >= 5 ? "MAX" : $"+ {CozyData.UpgradeCost[up[k]]}";
                var b = Btn(row, label, up[k] >= 5 ? Paper : SunC, Ink, () => { if (BuyUpgrade(choice, kk)) { Toast("Upgraded!"); RefreshTaller(); } else Toast("Not enough coins"); }, 110, 34);
                b.GetComponent<LayoutElement>().preferredWidth = 330; b.GetComponent<LayoutElement>().flexibleWidth = 0;
            }
        }

        // ---------- navegación ----------
        void ShowScreen(string name)
        {
            foreach (var kv in screens) kv.Value.SetActive(kv.Key == name);
            if (uiBg) uiBg.SetActive(name != "hud" && name != "garage");
            if (settingsPop) settingsPop.SetActive(false);
            if (name == "album") RefreshAlbum();
            if (name == "levels") RefreshLevels();
            if (name == "taller") RefreshTaller();
            if (name != "hud") { bubbles.SetActive(false); SetCount(""); }
            if (name != "results" && photoPop) photoPop.SetActive(false);
            if (name == "album" || name == "levels" || name == "taller") { state = "menu"; }
            if (showCam) showCam.enabled = name == "menu" || name == "car" || name == "online" || name == "searching";
            if (name == "hud" && pauseBtn) pauseBtn.SetActive(mode != "online");
        }
        void ShowMenu()
        {
            state = "menu"; ClosePause(); ShowScreen("menu"); if (!showCar) UpdateShowroom(); RefreshHome();
            if (lineGo) lineGo.SetActive(false);
            if (ghost) ghost.SetActive(false);
        }
        // nombres antiguos que aún llaman otras partes del juego
        void RefreshMenuSelections() => RefreshHome();
        void RefreshCards() => RefreshHome();

        // ---------- HUD y avisos ----------
        static string Ordinal(int n) => n == 1 ? "ST" : n == 2 ? "ND" : n == 3 ? "RD" : "TH";
        void UpdateHUD()
        {
            var order = cars.Where(c => !c.isOut).OrderByDescending(Prog).ToList();
            int place = order.IndexOf(player) + 1;
            bool tt = mode == "time";
            posText.text = tt ? $"<size={Mathf.RoundToInt(11 * K)}>TIME</size>\n{Fmt(raceT)}" : $"{place}<size={Mathf.RoundToInt(22 * K)}>{Ordinal(place)}</size><size={Mathf.RoundToInt(20 * K)}><color=#2B234088> /{order.Count}</color></size>";
            posText.fontSize = Mathf.RoundToInt((tt ? 28 : 54) * K); posText.lineSpacing = tt ? 0.8f : 1;
            int lp = Prog(player) < 0.5f ? 1 : Mathf.Min(laps, Mathf.FloorToInt((Prog(player) - 0.5f) / Track.N) + 1);
            lapText.text = $"{lp}/{laps}"; coinText.text = player.coins.ToString(); timeText.text = Fmt(raceT);
            timeRoot.SetActive(!tt);
            bestChip.SetActive(tt && ghostData != null); if (tt && ghostData != null) bestText.text = Fmt(ghostTime);
            var dRoot = deltaRoot;
            if (tt && ghostData != null && state == "race") { float d = GhostDelta(); dRoot.SetActive(true); deltaText.text = (d <= 0 ? "−" : "+") + Mathf.Abs(d).ToString("0.00"); deltaBg.color = d <= 0 ? MintB : Peach; }
            else dRoot.SetActive(false);
            float top = player.vtop * 1.25f;
            fillRect.anchorMax = new Vector2(Mathf.Min(1, player.v / top), 1);
            var nc = NextCurve(player);
            bool warn = false;
            if (nc != null && nc.Value.d < 14)
            {
                safeRect.gameObject.SetActive(true); float sx = Mathf.Min(1, nc.Value.v / top); safeRect.anchorMin = new Vector2(sx, -0.45f); safeRect.anchorMax = new Vector2(sx, 1.45f);
                float ratio = player.v / nc.Value.v;
                warn = player.slip > 0.02f || player.mustBrake || ratio > 1;
                fillImg.color = warn ? Coral : ratio > 0.9f ? SunC : CozyData.Hex("#8FC8F0");
                curveLbl.text = player.slip > 0.02f ? "SLIDING!" : player.draft ? "SLIPSTREAM" : player.mustBrake ? "LET GO!" : nc.Value.d == 0 ? "IN THE CURVE" : $"CURVE IN {Mathf.CeilToInt(nc.Value.d)} M";
            }
            else { safeRect.gameObject.SetActive(false); fillImg.color = SunC; curveLbl.text = player.draft ? "SLIPSTREAM" : "STRAIGHT"; }
            curveBg.color = warn ? Coral : Cream; curveLbl.color = warn ? Cream : Ink;
            bubbles.SetActive(player.bubbleT > 0);
            bool has = player.item != null;
            itemBg.color = has ? SunC : Cream;
            itemIcon.sprite = has ? B("item_" + player.item) : null; itemIcon.color = has ? Color.white : new Color(0, 0, 0, 0);
            itemName.text = has ? CozyData.Items[player.item][1].ToUpper() + (player.item == "turbo3" ? " ×" + player.uses : "") : "NO ITEM";
            itemHint.text = has ? "TAP TO USE" : mode == "online" ? "PURE RACING" : "GRAB A BOX";
            if (flashImg.color.a > 0) { var c = flashImg.color; c.a = Mathf.Max(0, c.a - Time.deltaTime * 1.7f); flashImg.color = c; }
        }
        // Diferencia con el fantasma: tiempo actual menos el que tardó el fantasma en llegar al mismo punto
        float GhostDelta()
        {
            var s = ghostData; if (s == null || s.Length < 6) return 0;
            float pr = Prog(player); int n = s.Length / 3;
            for (int k = 1; k < n; k++)
                if (s[k * 3 + 1] >= pr)
                {
                    float p0 = s[(k - 1) * 3 + 1], p1 = s[k * 3 + 1], f = p1 > p0 ? (pr - p0) / (p1 - p0) : 1;
                    return raceT - Mathf.Lerp(s[(k - 1) * 3], s[k * 3], f);
                }
            return raceT - s[(n - 1) * 3];
        }
        void SetRivalChip(string t) { rivalText.text = t; rivalRoot.gameObject.SetActive(!string.IsNullOrEmpty(t)); }
        void Toast(string t) => ToastC(t, ToastColor(t));
        static Color ToastColor(string t)
        {
            string u = t.ToUpper();
            if (u.Contains("PERFECT") || u.Contains("SAVED") || u.Contains("+")) return CozyData.Hex("#73E0A6");
            if (u.Contains("OUT") || u.Contains("CRASH") || u.Contains("HIT") || u.Contains("OFF THE TRACK") || u.Contains("BLACKOUT") || u.Contains("FALSE")) return CozyData.Hex("#FF6B4A");
            return CozyData.Cream;
        }
        void ToastC(string t, Color c)
        {
            if (!toastText) return;
            toastText.text = t.ToUpper(); toastBg.color = c; toastText.color = c == Coral ? Cream : Ink;
            toastRect.sizeDelta = new Vector2(Mathf.Clamp(t.Length * 12 + 40, 140, 360) * K, 46 * K);
            Tilt(toastRect, UnityEngine.Random.Range(-5f, 4f));
            toastRect.gameObject.SetActive(true); CancelInvoke(nameof(HideToast)); Invoke(nameof(HideToast), 1.0f);
        }
        void HideToast() => toastRect.gameObject.SetActive(false);
        void Flash(Color c) { c.a = 0.85f; flashImg.color = c; }
        void ShowAdOverlay(bool on) => adOverlay.SetActive(on);

        bool OverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos }; var res = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, res);
            return res.Any(r => r.gameObject.GetComponentInParent<Button>() != null);
        }
        bool OverItemButton(Vector2 p) => (itemRect && RectTransformUtility.RectangleContainsScreenPoint(itemRect, p, null)) || (pauseBtn && pauseBtn.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)pauseBtn.transform, p, null));

        // ---------- líneas de velocidad (capa 2D) ----------
        readonly List<(RectTransform r, float a, float rad, float len, float sp)> speedLines = new List<(RectTransform, float, float, float, float)>();
        void UpdateSpeedLines(float dt)
        {
            float k = 0;
            if (player != null && state == "race")
                k = Mathf.Max(0, player.v / player.vtop - 0.8f) * 4 + (player.turbo > 0 || player.autoT > 0 || player.superT > 0 ? 0.8f : 0) + (Track.SurfId(player.i) == "turbo" ? 0.4f : 0);
            int want = k <= 0.02f ? 0 : Mathf.RoundToInt(26 * Mathf.Min(1.5f, k));
            while (speedLines.Count < want)
            {
                var im = Img(speedLayer, Color.white, null, "Linea"); Soft(im); im.raycastTarget = false;
                speedLines.Add((im.rectTransform, UnityEngine.Random.value * Mathf.PI * 2, 0.35f + UnityEngine.Random.value * 0.3f, 0.08f + UnityEngine.Random.value * 0.15f, 1.5f + UnityEngine.Random.value * 2));
            }
            while (speedLines.Count > want) { Destroy(speedLines[speedLines.Count - 1].r.gameObject); speedLines.RemoveAt(speedLines.Count - 1); }
            var size = speedLayer.rect.size; float R2 = size.magnitude / 2; var center = new Vector2(0, size.y * 0.08f);
            Color c = player != null && player.superT > 0 ? Color.HSVToRGB((airT * 1.1f) % 1, 0.6f, 1) : Color.white; c.a = Mathf.Min(0.55f, k * 0.4f);
            for (int i = 0; i < speedLines.Count; i++)
            {
                var L = speedLines[i]; L.rad += dt * L.sp; if (L.rad > 1.1f) { L.rad = 0.35f + UnityEngine.Random.value * 0.2f; L.a = UnityEngine.Random.value * Mathf.PI * 2; }
                speedLines[i] = L;
                var dir = new Vector2(Mathf.Cos(L.a), Mathf.Sin(L.a));
                L.r.anchorMin = L.r.anchorMax = new Vector2(0.5f, 0.5f); L.r.pivot = new Vector2(0, 0.5f);
                L.r.anchoredPosition = center + dir * L.rad * R2; L.r.sizeDelta = new Vector2(L.len * R2, 4);
                L.r.localRotation = Quaternion.Euler(0, 0, L.a * Mathf.Rad2Deg);
                L.r.GetComponent<Image>().color = c;
            }
        }
    }

    static class UiExt
    {
        // pequeña ayuda para configurar algo en línea: x.Let(r => …)
        public static void Let<T>(this T x, Action<T> f) => f(x);
    }
}
