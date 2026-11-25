using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CozyTrack
{
    // Interfaz hecha por código con uGUI: columna vertical 9:16, textos centrados con autoajuste de tamaño.
    public partial class CozyGame
    {
        Canvas canvas; RectTransform canvasRect, col; Font font, fontBlack;
        readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>();
        ScrollRect menuScroll;
        Text posText, rivalText, lapText, coinText, timeText, curveLbl, itemIc, itemTxt, countText, toastText, circDesc, dailyText, profText;
        Image fillImg, itemBg, flashImg; RectTransform fillRect, safeRect, itemRect, speedLayer;
        GameObject bubbles, adOverlay, rewardPop; Text rewardText;
        readonly Dictionary<string, Image> selButtons = new Dictionary<string, Image>();
        readonly Dictionary<string, Image> cardImgs = new Dictionary<string, Image>();
        readonly Dictionary<string, Text> cardRoles = new Dictionary<string, Text>();
        readonly Text[] chestTexts = new Text[4]; readonly GameObject[] chestAds = new GameObject[4];
        Text assistTxt, unlockTxt;
        Text resBig, resMsg, resLeft, resRight, chestBox, photoLine1, photoLine2; RawImage photoImg; GameObject photoBox, chestBoxGo;
        Text gName; Transform gStickers;
        Text albumSum; Transform albumGrid, albumStk; Transform levelsGrid; Transform tallerRows; Text tallerTitle, tallerCoins;

        static readonly Color Cream = CozyData.Cream, Ink = CozyData.Ink, Coral = CozyData.Coral, SunC = CozyData.Sun, Muted = new Color(0.43f, 0.41f, 0.5f);

        // ---------- piezas ----------
        RectTransform R(string name, Transform parent) { var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform; }
        static void Fill(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t); }
        Image Img(Transform p, Color c, Sprite s = null, string name = "Fondo")
        {
            var r = R(name, p); var i = r.gameObject.AddComponent<Image>(); i.color = c; i.sprite = s ? s : CozyTex.Round(); i.type = Image.Type.Sliced; return i;
        }
        Text T(Transform p, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, FontStyle st = FontStyle.Bold)
        {
            var r = R("Texto", p); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st; t.raycastTarget = false;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, size / 2); t.resizeTextMaxSize = size;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.supportRichText = true;
            return t;
        }
        Text LayoutText(Transform p, string s, int size, Color c, float h, FontStyle st = FontStyle.Bold)
        {
            var t = T(p, s, size, c, TextAnchor.MiddleCenter, st); var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h; return t;
        }
        Button Btn(Transform p, string label, Color bg, Color fg, Action on, float h = 120, int size = 44, string key = null)
        {
            var img = Img(p, bg, null, "Boton " + label); var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.pressedColor = new Color(0.85f, 0.85f, 0.85f); b.colors = cb;
            b.onClick.AddListener(() => on());
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h; le.flexibleWidth = 1;
            var t = T(img.transform, label, size, fg); Fill(t.rectTransform, 14, 6, 14, 6);
            var sh = img.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0.18f, 0.16f, 0.24f, 0.22f); sh.effectDistance = new Vector2(0, -7);
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
        GameObject MakeScreen(string name, bool backdrop = true)
        {
            var r = R(name, col); Fill(r);
            if (backdrop) { var bg = r.gameObject.AddComponent<Image>(); bg.color = new Color(0.66f, 0.85f, 0.73f, 0.55f); }
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

        // ---------- montaje ----------
        void BuildUI()
        {
            font = Resources.Load<Font>("Fonts/Nunito-ExtraBold"); fontBlack = Resources.Load<Font>("Fonts/Nunito-Black"); if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (!fontBlack) fontBlack = font;
            var cg = new GameObject("Interfaz", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cg.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 1;
            canvasRect = (RectTransform)cg.transform;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            col = R("Columna", cg.transform); col.anchorMin = new Vector2(0.5f, 0); col.anchorMax = new Vector2(0.5f, 1); col.sizeDelta = new Vector2(1080, 0);

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

            BuildHome(); BuildHud(); BuildResults(); BuildGarageUI(); BuildAlbum(); BuildLevels(); BuildTaller(); BuildSettings();

            flashImg = Img(col, new Color(1, 1, 1, 0), null, "Destello"); Fill(flashImg.rectTransform); flashImg.sprite = null; flashImg.raycastTarget = false;
            toastText = T(col, "", 64, Cream); var tr = toastText.rectTransform; tr.anchorMin = new Vector2(0, 0.66f); tr.anchorMax = new Vector2(1, 0.74f); tr.offsetMin = tr.offsetMax = Vector2.zero;
            var ol = toastText.gameObject.AddComponent<Outline>(); ol.effectColor = Ink; ol.effectDistance = new Vector2(4, -4);
            toastText.gameObject.SetActive(false);
            countText = T(col, "", 260, Cream); Fill(countText.rectTransform); var ol2 = countText.gameObject.AddComponent<Outline>(); ol2.effectColor = Ink; ol2.effectDistance = new Vector2(8, -8);

            // ventana de recompensa del cofre y anuncio de prueba
            rewardPop = R("Recompensa", col).gameObject; Fill((RectTransform)rewardPop.transform); rewardPop.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.24f, 0.6f);
            var card = Img(rewardPop.transform, SunC); card.rectTransform.anchorMin = new Vector2(0.08f, 0.33f); card.rectTransform.anchorMax = new Vector2(0.92f, 0.67f); card.rectTransform.offsetMin = card.rectTransform.offsetMax = Vector2.zero;
            rewardText = T(card.transform, "", 46, Ink); Fill(rewardText.rectTransform, 40, 170, 40, 40);
            var ok = Btn(card.transform, "¡Genial!", Coral, Color.white, () => rewardPop.SetActive(false), 120, 46); var okr = (RectTransform)ok.transform; okr.anchorMin = new Vector2(0.25f, 0); okr.anchorMax = new Vector2(0.75f, 0); okr.pivot = new Vector2(0.5f, 0); okr.anchoredPosition = new Vector2(0, 40); okr.sizeDelta = new Vector2(0, 120);
            rewardPop.SetActive(false);
            adOverlay = R("Anuncio", col).gameObject; Fill((RectTransform)adOverlay.transform); adOverlay.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.95f);
            var at = T(adOverlay.transform, "Anuncio de prueba…\n(en la build de publisher aquí va su anuncio con recompensa)", 50, Cream); Fill(at.rectTransform, 60, 0, 60, 0);
            adOverlay.SetActive(false);
        }

        void LateUpdate()
        {
            if (col) col.sizeDelta = new Vector2(Mathf.Min(1080, canvasRect.rect.width), 0);
        }

        void BuildHud()
        {
            var s = MakeScreen("hud", false);
            RectTransform Chip(string name, Vector2 anchor, Vector2 pos, Vector2 size, out Text t, int fs = 40)
            {
                var im = Img(s.transform, new Color(1, 0.97f, 0.93f, 0.92f), null, name); im.raycastTarget = false;
                var r = im.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = size;
                t = T(im.transform, "", fs, Ink); Fill(t.rectTransform, 12, 4, 12, 4); return r;
            }
            Chip("Posicion", new Vector2(0, 1), new Vector2(28, -28), new Vector2(230, 140), out posText, 96);
            Chip("Rival", new Vector2(0, 1), new Vector2(28, -184), new Vector2(560, 70), out rivalText, 34);
            Chip("Vuelta", new Vector2(1, 1), new Vector2(-28, -28), new Vector2(300, 70), out lapText);
            Chip("Monedas", new Vector2(1, 1), new Vector2(-28, -112), new Vector2(300, 70), out coinText);
            Chip("Tiempo", new Vector2(1, 1), new Vector2(-28, -196), new Vector2(300, 70), out timeText);
            var meter = Img(s.transform, new Color(1, 0.97f, 0.93f, 0.92f), null, "Velocimetro"); meter.raycastTarget = false;
            var mr = meter.rectTransform; mr.anchorMin = mr.anchorMax = new Vector2(0, 0); mr.pivot = Vector2.zero; mr.anchoredPosition = new Vector2(28, 40); mr.sizeDelta = new Vector2(700, 130);
            var lbl = T(meter.transform, "Velocidad", 30, Muted, TextAnchor.UpperLeft); Fill(lbl.rectTransform, 22, 60, 22, 12);
            curveLbl = T(meter.transform, "", 30, Muted, TextAnchor.UpperRight); Fill(curveLbl.rectTransform, 22, 60, 22, 12);
            var bar = Img(meter.transform, new Color(0.9f, 0.88f, 0.83f), null, "Barra"); var br = bar.rectTransform; br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0); br.pivot = new Vector2(0.5f, 0); br.offsetMin = new Vector2(22, 22); br.offsetMax = new Vector2(-22, 52);
            fillImg = Img(bar.transform, CozyData.Sky, null, "Relleno"); fillRect = fillImg.rectTransform; fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(0, 1); fillRect.pivot = new Vector2(0, 0.5f); fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var safe = Img(bar.transform, Ink, null, "Limite"); safe.sprite = null; safeRect = safe.rectTransform; safeRect.anchorMin = new Vector2(0, -0.15f); safeRect.anchorMax = new Vector2(0, 1.15f); safeRect.sizeDelta = new Vector2(8, 0);
            itemBg = Img(s.transform, Cream, null, "Objeto"); itemRect = itemBg.rectTransform; itemRect.anchorMin = itemRect.anchorMax = new Vector2(1, 0); itemRect.pivot = new Vector2(1, 0); itemRect.anchoredPosition = new Vector2(-28, 40); itemRect.sizeDelta = new Vector2(210, 210);
            itemIc = T(itemBg.transform, "·", 70, Ink); Fill(itemIc.rectTransform, 0, 70, 0, 20);
            itemTxt = T(itemBg.transform, "Sin objeto", 30, Ink); Fill(itemTxt.rectTransform, 8, 20, 8, 130);
        }

        void BuildResults()
        {
            var s = MakeScreen("results");
            var (_, c) = Scroll(s.transform, 90);
            var card = Img(c, Cream, null, "Resultado"); var cv = card.gameObject.AddComponent<VerticalLayoutGroup>(); cv.padding = new RectOffset(40, 40, 30, 30); cv.spacing = 8; cv.childControlHeight = true; cv.childControlWidth = true; cv.childForceExpandHeight = false;
            resBig = LayoutText(card.transform, "1º", 140, Ink, 160);
            resMsg = LayoutText(card.transform, "", 40, Ink, 110, FontStyle.Normal);
            var tbl = Row(card.transform, 420, 0);
            resLeft = T(tbl, "", 36, Ink, TextAnchor.UpperLeft, FontStyle.Normal); resLeft.resizeTextForBestFit = false;
            resRight = T(tbl, "", 36, Ink, TextAnchor.UpperRight, FontStyle.Normal); resRight.resizeTextForBestFit = false;
            var cb = Img(c, SunC, null, "Cofre"); chestBoxGo = cb.gameObject; cb.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            chestBox = T(cb.transform, "", 36, Ink); Fill(chestBox.rectTransform, 20, 10, 20, 10);
            var pb = Img(c, Cream, null, "Foto"); photoBox = pb.gameObject; pb.gameObject.AddComponent<LayoutElement>().preferredHeight = 820;
            photoImg = R("Imagen", pb.transform).gameObject.AddComponent<RawImage>(); var pr = photoImg.rectTransform; pr.anchorMin = new Vector2(0, 1); pr.anchorMax = new Vector2(1, 1); pr.pivot = new Vector2(0.5f, 1); pr.offsetMin = new Vector2(24, -24 - 660); pr.offsetMax = new Vector2(-24, -24);
            photoLine1 = T(pb.transform, "", 40, Ink); var l1 = photoLine1.rectTransform; l1.anchorMin = new Vector2(0, 0); l1.anchorMax = new Vector2(1, 0); l1.pivot = new Vector2(0.5f, 0); l1.anchoredPosition = new Vector2(0, 60); l1.sizeDelta = new Vector2(-40, 56);
            photoLine2 = T(pb.transform, "", 28, Muted, TextAnchor.MiddleCenter, FontStyle.Normal); var l2 = photoLine2.rectTransform; l2.anchorMin = new Vector2(0, 0); l2.anchorMax = new Vector2(1, 0); l2.pivot = new Vector2(0.5f, 0); l2.anchoredPosition = new Vector2(0, 18); l2.sizeDelta = new Vector2(-40, 40);
            var row = Row(c, 140);
            Btn(row, "Otra vez", Coral, Color.white, () => { if (mode == "daily") StartDaily(); else if (mode == "level") StartLevel(levelNum); else StartRace(); }, 140, 50);
            Btn(row, "Menú", Cream, Ink, () => { if (mode == "daily" || mode == "level") { mode = "race"; if (circId.StartsWith("nivel")) BuildCircuit(CozyData.Circuit(PlayerPrefs.GetString("ctCirc", "jardin"))); } MenuGrid(); ShowMenu(); }, 140, 50);
            if (mode == "level") { }
        }

        void BuildGarageUI()
        {
            var s = MakeScreen("garage", false);
            var panel = Img(s.transform, Cream, null, "Panel"); var pr = panel.rectTransform; pr.anchorMin = new Vector2(0, 0); pr.anchorMax = new Vector2(1, 0); pr.pivot = new Vector2(0.5f, 0); pr.offsetMin = new Vector2(0, -30); pr.offsetMax = new Vector2(0, 640);
            var v = VList(panel.transform, 14, new RectOffset(30, 30, 30, 60)); Fill(v);
            gName = LayoutText(v, "", 46, Ink, 70);
            LayoutText(v, "Toca un vehículo de la estantería · pegatina para el techo", 30, Muted, 50, FontStyle.Normal);
            var gr = R("Pegatinas", v); var grid = gr.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(150, 150); grid.spacing = new Vector2(14, 14); grid.childAlignment = TextAnchor.MiddleCenter;
            gr.gameObject.AddComponent<LayoutElement>().preferredHeight = 330; gStickers = gr;
            Btn(v, "Listo", Coral, Color.white, CloseGarage, 120, 46);
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
                if (!owned) img.color = new Color(0.9f, 0.88f, 0.83f);
            }
            Add(null, "Sin\npegatina");
            for (int k = 0; k < CozyData.StickerIds.Length; k++) Add(CozyData.StickerIds[k], CozyData.StickerNames[k]);
        }

        void BuildAlbum()
        {
            var s = MakeScreen("album");
            var (_, c) = Scroll(s.transform);
            LayoutText(c, "Álbum", 96, Ink, 120);
            albumSum = LayoutText(c, "", 36, Muted, 60, FontStyle.Normal);
            var gr = R("Coleccion", c); var grid = gr.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(318, 170); grid.spacing = new Vector2(12, 12); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            gr.gameObject.AddComponent<LayoutElement>().preferredHeight = 12 * 182; albumGrid = gr;
            LayoutText(c, "Cada vehículo tiene 3 piezas; salen en los cofres al ganar.", 32, Muted, 60, FontStyle.Normal);
            var st = R("Pegatinas", c); var sg = st.gameObject.AddComponent<GridLayoutGroup>(); sg.cellSize = new Vector2(120, 120); sg.spacing = new Vector2(12, 12); sg.childAlignment = TextAnchor.MiddleCenter;
            st.gameObject.AddComponent<LayoutElement>().preferredHeight = 260; albumStk = st;
            Btn(c, "Volver", Cream, Ink, ShowMenu, 120, 44);
        }
        void RefreshAlbum()
        {
            foreach (Transform t in albumGrid) Destroy(t.gameObject);
            foreach (Transform t in albumStk) Destroy(t.gameObject);
            int owned = CozyData.Cars.Count(c => prof.owned.Contains(c.name));
            albumSum.text = $"{owned}/{CozyData.Cars.Length} vehículos · {prof.stickers.Count}/{CozyData.StickerIds.Length} pegatinas · {prof.coins} monedas";
            foreach (var cd in CozyData.Cars)
            {
                bool own = prof.owned.Contains(cd.name); int p = own ? 3 : Pieces(cd.name);
                var cell = Img(albumGrid, own ? Cream : new Color(0.9f, 0.88f, 0.83f), null, cd.name);
                var sw = Img(cell.transform, own ? cd.color : new Color(0.79f, 0.76f, 0.84f)); var swr = sw.rectTransform; swr.anchorMin = swr.anchorMax = new Vector2(0.5f, 1); swr.anchoredPosition = new Vector2(0, -34); swr.sizeDelta = new Vector2(44, 44);
                var n = T(cell.transform, cd.name, 32, own ? Ink : Muted); var nr = n.rectTransform; nr.anchorMin = new Vector2(0, 1); nr.anchorMax = new Vector2(1, 1); nr.pivot = new Vector2(0.5f, 1); nr.anchoredPosition = new Vector2(0, -62); nr.sizeDelta = new Vector2(-16, 40);
                var r = T(cell.transform, cd.role, 22, Muted, TextAnchor.MiddleCenter, FontStyle.Normal); var rr = r.rectTransform; rr.anchorMin = new Vector2(0, 1); rr.anchorMax = new Vector2(1, 1); rr.pivot = new Vector2(0.5f, 1); rr.anchoredPosition = new Vector2(0, -100); rr.sizeDelta = new Vector2(-16, 30);
                for (int k = 0; k < 3; k++) { var pip = Img(cell.transform, k < p ? Coral : new Color(0.79f, 0.76f, 0.84f)); var pr = pip.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0); pr.anchoredPosition = new Vector2((k - 1) * 40, 22); pr.sizeDelta = new Vector2(32, 14); }
            }
            for (int k = 0; k < CozyData.StickerIds.Length; k++)
            {
                bool has = prof.stickers.Contains(CozyData.StickerIds[k]);
                var cell = Img(albumStk, has ? Color.white : new Color(0.9f, 0.88f, 0.83f), null, CozyData.StickerNames[k]);
                if (has) { var ri = R("Img", cell.transform).gameObject.AddComponent<RawImage>(); ri.texture = CozyTex.Get("sticker_" + CozyData.StickerIds[k]); Fill(ri.rectTransform, 8, 8, 8, 8); }
                else { var t = T(cell.transform, "?", 50, Muted); Fill(t.rectTransform); }
            }
        }

        void BuildLevels()
        {
            var s = MakeScreen("levels");
            var (_, c) = Scroll(s.transform);
            LayoutText(c, "Niveles", 96, Ink, 120);
            LayoutText(c, "Gana cada nivel para abrir el siguiente. Las curvas se cierran y los rivales mejoran: el taller ayuda, pero no te hace inmortal.", 32, Muted, 110, FontStyle.Normal);
            var gr = R("Rejilla", c); var grid = gr.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(180, 150); grid.spacing = new Vector2(14, 14); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
            gr.gameObject.AddComponent<LayoutElement>().preferredHeight = 6 * 164; levelsGrid = gr;
            Btn(c, "Volver", Cream, Ink, ShowMenu, 120, 44);
        }
        void RefreshLevels()
        {
            foreach (Transform t in levelsGrid) Destroy(t.gameObject);
            for (int n = 1; n <= CozyData.LevelCount; n++)
            {
                int nn = n; bool open = n <= prof.level + 1, done = n <= prof.level;
                var img = Img(levelsGrid, done ? CozyData.Mint : open ? SunC : new Color(0.9f, 0.88f, 0.83f), null, "Nivel " + n);
                var b = img.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { if (open) StartLevel(nn); else Toast("Gana el nivel anterior"); });
                var t = T(img.transform, n.ToString(), 56, open ? Ink : Muted); Fill(t.rectTransform, 4, 30, 4, 4);
                var w = T(img.transform, done ? "superado" : open ? "jugar" : "bloqueado", 22, Muted, TextAnchor.MiddleCenter, FontStyle.Normal); Fill(w.rectTransform, 4, 6, 4, 100);
            }
        }

        void BuildTaller()
        {
            var s = MakeScreen("taller");
            var (_, c) = Scroll(s.transform);
            LayoutText(c, "Taller", 96, Ink, 120);
            tallerTitle = LayoutText(c, "", 40, Ink, 70);
            tallerCoins = LayoutText(c, "", 34, Muted, 60, FontStyle.Normal);
            tallerRows = VList(c, 18);
            LayoutText(c, "Cada mejora rinde menos que la anterior. Velocidad hasta +20 %, aceleración hasta +33 % y frenos hasta +40 %.", 30, Muted, 100, FontStyle.Normal);
            Btn(c, "Volver", Cream, Ink, ShowMenu, 120, 44);
        }
        void RefreshTaller()
        {
            foreach (Transform t in tallerRows) Destroy(t.gameObject);
            tallerTitle.text = $"{choice} · {CozyData.Car(choice).role}"; tallerCoins.text = $"{prof.coins} monedas";
            var up = Upgrades(choice);
            for (int k = 0; k < 3; k++)
            {
                int kk = k; var row = Row(tallerRows, 120);
                Label(row, CozyData.UpgradeNames[k], 260);
                var pips = R("Niveles", row); pips.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                for (int p = 0; p < 5; p++) { var pip = Img(pips, p < up[k] ? Coral : new Color(0.9f, 0.88f, 0.83f)); var pr = pip.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0, 0.5f); pr.pivot = new Vector2(0, 0.5f); pr.anchoredPosition = new Vector2(p * 62, 0); pr.sizeDelta = new Vector2(52, 24); }
                string label = up[k] >= 5 ? "Al máximo" : $"Mejorar · {CozyData.UpgradeCost[up[k]]}";
                var b = Btn(row, label, up[k] >= 5 ? new Color(0.9f, 0.88f, 0.83f) : SunC, Ink, () => { if (BuyUpgrade(choice, kk)) { Toast("¡Mejorado!"); RefreshTaller(); } else Toast("Te faltan monedas"); }, 110, 34);
                b.GetComponent<LayoutElement>().preferredWidth = 330; b.GetComponent<LayoutElement>().flexibleWidth = 0;
            }
        }

        // ---------- navegación ----------
        void ShowScreen(string name)
        {
            foreach (var kv in screens) kv.Value.SetActive(kv.Key == name);
            if (name == "album") RefreshAlbum();
            if (name == "levels") RefreshLevels();
            if (name == "taller") RefreshTaller();
            if (name != "hud") { bubbles.SetActive(false); SetCount(""); }
            if (name == "album" || name == "levels" || name == "taller") { state = "menu"; }
        }
        void ShowMenu()
        {
            state = "menu"; ShowScreen("menu"); if (!showCar) UpdateShowroom(); RefreshMenuSelections(); RefreshCards();
            if (lineGo) lineGo.SetActive(false);
            if (ghost) ghost.SetActive(false);
        }
        void RefreshMenuSelections()
        {
            if (selButtons.Count == 0) return;
            foreach (var kv in selButtons)
            {
                var p = kv.Key.Split(':'); bool sel = (p[0] == "tod" && p[1] == todId) || (p[0] == "look" && p[1] == look) || (p[0] == "circ" && p[1] == circId) || (p[0] == "mode" && p[1] == mode);
                kv.Value.color = sel ? Ink : Cream; var t = kv.Value.GetComponentInChildren<Text>(); if (t) t.color = sel ? Cream : Ink;
            }
            if (circ != null && circDesc) circDesc.text = circ.desc;
            if (assistTxt) assistTxt.text = (assistOn ? "✓ " : "") + "Línea de frenada";
            if (unlockTxt) unlockTxt.text = (allUnlocked ? "✓ " : "") + "Prototipo: todo desbloqueado";
            if (profText) profText.text = $"{prof.coins} monedas · {prof.owned.Count}/36 vehículos · nivel {prof.level}/{CozyData.LevelCount}";
            var d = DailyDef(); bool done = prof.dailyLast == d.key;
            RefreshHome();
            if (dailyText) dailyText.text = $"<b>Desafío diario{(done ? " · superado" : "")}</b>\n{CozyData.DailyGoals[d.goal]} con {CozyData.Car(d.car).role.ToLower()} en {CozyData.Circuit(d.circ).n} ({CozyData.Tod(d.tod).n.ToLower()})\n<size=26>Racha: {StreakNow()} · cada 7 días, cofre de oro</size>";
        }
        void RefreshCards()
        {
            foreach (var kv in cardImgs)
            {
                bool own = Usable(kv.Key); kv.Value.color = kv.Key == choice ? SunC : own ? Cream : new Color(0.9f, 0.88f, 0.83f);
                cardRoles[kv.Key].text = own ? CozyData.Car(kv.Key).role : $"Bloqueado · {Pieces(kv.Key)}/3 piezas";
            }
            RefreshMenuSelections();
        }

        // ---------- HUD y avisos ----------
        void UpdateHUD()
        {
            var order = cars.Where(c => !c.isOut).OrderByDescending(Prog).ToList();
            posText.text = $"{order.IndexOf(player) + 1}º<size=44>/{order.Count}</size>";
            int lp = Prog(player) < 0.5f ? 1 : Mathf.Min(laps, Mathf.FloorToInt((Prog(player) - 0.5f) / Track.N) + 1);
            lapText.text = $"Vuelta {lp}/{laps}"; coinText.text = $"Monedas {player.coins}"; timeText.text = Fmt(raceT);
            float top = player.vtop * 1.25f;
            fillRect.anchorMax = new Vector2(Mathf.Min(1, player.v / top), 1);
            var nc = NextCurve(player);
            if (nc != null && nc.Value.d < 14)
            {
                safeRect.gameObject.SetActive(true); safeRect.anchorMin = new Vector2(Mathf.Min(1, nc.Value.v / top), -0.15f); safeRect.anchorMax = new Vector2(Mathf.Min(1, nc.Value.v / top), 1.15f);
                float ratio = player.v / nc.Value.v;
                fillImg.color = player.slip > 0.02f || ratio > 1 ? Coral : ratio > 0.9f ? SunC : CozyData.Sky;
                curveLbl.text = player.slip > 0.02f ? "¡Derrapando!" : player.draft ? "Rebufo" : player.mustBrake ? "¡Suelta!" : nc.Value.d == 0 ? "En curva" : $"Curva a {Mathf.CeilToInt(nc.Value.d)} m";
            }
            else { safeRect.gameObject.SetActive(false); fillImg.color = CozyData.Sky; curveLbl.text = ""; }
            bubbles.SetActive(player.bubbleT > 0);
            itemBg.color = player.item != null ? SunC : Cream;
            itemIc.text = player.item != null ? CozyData.Items[player.item][0] + (player.item == "turbo3" ? " ×" + player.uses : "") : "·";
            itemTxt.text = player.item != null ? CozyData.Items[player.item][1] : "Sin objeto";
            if (flashImg.color.a > 0) { var c = flashImg.color; c.a = Mathf.Max(0, c.a - Time.deltaTime * 1.7f); flashImg.color = c; }
        }
        void SetRivalChip(string t) { rivalText.text = t; rivalText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(t)); }
        void SetCount(string t) { if (countText) countText.text = t; }
        float toastHide;
        void Toast(string t)
        {
            if (!toastText) return;
            toastText.text = t; toastText.gameObject.SetActive(true); CancelInvoke(nameof(HideToast)); Invoke(nameof(HideToast), 0.9f);
        }
        void HideToast() => toastText.gameObject.SetActive(false);
        void Flash(Color c) { c.a = 0.85f; flashImg.color = c; }
        void ShowAdOverlay(bool on) => adOverlay.SetActive(on);
        void ShowChestReward(List<string> lines) { rewardText.text = "<b>Cofre</b>\n" + string.Join("\n", lines); rewardPop.SetActive(true); RefreshMenuSelections(); RefreshCards(); }

        void ShowResults(string big, string msg, List<string> rows, List<string> chest)
        {
            ShowScreen("results");
            resBig.text = big; resMsg.text = msg;
            resLeft.text = string.Join("\n", rows.Select(r => r.Split('\t')[0]));
            resRight.text = string.Join("\n", rows.Select(r => r.Split('\t').Length > 1 ? r.Split('\t')[1] : ""));
            chestBoxGo.SetActive(chest != null); if (chest != null) chestBox.text = string.Join("\n", chest);
            photoBox.SetActive(false);
            if (lineGo) lineGo.SetActive(false);
        }
        void ShowPhoto(Texture t, string l1, string l2) { photoImg.texture = t; photoLine1.text = l1; photoLine2.text = l2; photoBox.SetActive(true); }

        bool OverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos }; var res = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, res);
            return res.Any(r => r.gameObject.GetComponentInParent<Button>() != null);
        }
        bool OverItemButton(Vector2 p) => itemRect && RectTransformUtility.RectangleContainsScreenPoint(itemRect, p, null);

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
                var im = Img(speedLayer, Color.white, null, "Linea"); im.sprite = null; im.raycastTarget = false;
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
}
