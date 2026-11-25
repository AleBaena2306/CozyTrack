using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CozyTrack
{
    // Estilo B («caja de juguetes») del lienzo de diseño Cozy Track · UI:
    // cartón kraft con rayas, pegatinas troqueladas (borde de tinta grueso + sombra dura desplazada + ligero giro),
    // cinta adhesiva, franja azul de carreras con línea a cuadros, Fredoka para textos grandes y Nunito para los pequeños.
    //
    // Las medidas se escriben en píxeles del diseño (teléfono de 390×844) y se multiplican por K: el lienzo de Unity mide
    // 1920 de alto (CanvasScaler a altura), así que 844 px de diseño = 1920 unidades → K = 2,275. El ancho se adapta:
    // casi todo se ancla a izquierda y derecha con márgenes, como en el diseño.
    public partial class CozyGame
    {
        const float K = 2.275f;
        static readonly Color Kraft = CozyData.Hex("#E7C391"), Blue = CozyData.Hex("#2F7DD1"), Purple = CozyData.Hex("#7B63D1"),
            MintB = CozyData.Hex("#73E0A6"), Paper = CozyData.Hex("#E9DCC4"), SkyB = CozyData.Hex("#BFE0F7"), Peach = CozyData.Hex("#FF9E7F"), Lilac = CozyData.Hex("#C9B8F2"),
            Green = CozyData.Hex("#1F9D57");

        // ---------- recursos ----------
        readonly Dictionary<string, Sprite> bSprites = new Dictionary<string, Sprite>();
        // Iconos del estilo B (Resources/UI/B/*.png, dibujados a partir de los SVG del lienzo)
        Sprite B(string name)
        {
            if (bSprites.TryGetValue(name, out var s)) return s;
            var t = Resources.Load<Texture2D>("UI/B/" + name);
            s = t ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100) : null;
            return bSprites[name] = s;
        }
        Sprite ChestSprite(string tier) => B("chest_" + (string.IsNullOrEmpty(tier) ? "common" : tier));

        Sprite kraftSprite, checkerSprite;
        // Cartón kraft: rayas verticales de 2 px cada 9 px
        Sprite KraftSprite()
        {
            if (kraftSprite) return kraftSprite;
            var t = new Texture2D(9, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var dark = Color.Lerp(Kraft, CozyData.Hex("#785028"), 0.07f);
            for (int x = 0; x < 9; x++) for (int y = 0; y < 4; y++) t.SetPixel(x, y, x < 2 ? dark : Kraft);
            t.Apply();
            return kraftSprite = Sprite.Create(t, new Rect(0, 0, 9, 4), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        }
        // Cuadros de bandera (2×2 píxeles, sin suavizado)
        Sprite CheckerSprite()
        {
            if (checkerSprite) return checkerSprite;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            t.SetPixel(0, 0, Ink); t.SetPixel(1, 1, Ink); t.SetPixel(1, 0, Cream); t.SetPixel(0, 1, Cream); t.Apply();
            return checkerSprite = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        }

        // ---------- colocación en píxeles del diseño ----------
        // franja anclada arriba, con márgenes a izquierda y derecha
        static void TopBox(RectTransform r, float left, float right, float top, float h)
        {
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(left * K, -(top + h) * K); r.offsetMax = new Vector2(-right * K, -top * K);
        }
        // franja anclada abajo
        static void BotBox(RectTransform r, float left, float right, float bottom, float h)
        {
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(left * K, bottom * K); r.offsetMax = new Vector2(-right * K, (bottom + h) * K);
        }
        // caja de tamaño fijo anclada arriba a la izquierda (x desde la izquierda) o a la derecha (x desde la derecha)
        static void TopAt(RectTransform r, float x, float top, float w, float h, bool fromRight = false)
        {
            r.anchorMin = r.anchorMax = new Vector2(fromRight ? 1 : 0, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w * K, h * K);
            r.anchoredPosition = new Vector2((fromRight ? -(x + w / 2) : x + w / 2) * K, -(top + h / 2) * K);
        }
        // caja de tamaño fijo centrada en horizontal
        static void TopCenter(RectTransform r, float top, float w, float h, float dx = 0)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w * K, h * K); r.anchoredPosition = new Vector2(dx * K, -(top + h / 2) * K);
        }
        static void Inset(RectTransform r, float l, float t, float rr, float b) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l * K, b * K); r.offsetMax = new Vector2(-rr * K, -t * K); }
        // giro como en CSS (rotate(Xdeg) en sentido horario)
        static void Tilt(Transform t, float deg) => t.localRotation = Quaternion.Euler(0, 0, -deg);

        // ---------- piezas ----------
        // radio de esquina en px de diseño (el sprite redondeado tiene un borde de 24 px)
        static void Radius(Image i, float r) { i.type = Image.Type.Sliced; i.pixelsPerUnitMultiplier = 24f / Mathf.Max(0.5f, r * K); }

        // Pegatina: sombra dura de tinta desplazada, borde de tinta y cara de color. Devuelve el contenedor; «face» es la cara.
        RectTransform Sticker(Transform p, Color faceColor, out Image face, float radius = 14, float border = 3, float shadow = 4, float rot = 0, string name = "Pegatina")
        {
            var root = R(name, p);
            if (shadow > 0)
            {
                var sh = Img(root, Ink, null, "Sombra"); Fill(sh.rectTransform, shadow * K, -shadow * K, -shadow * K, shadow * K); Radius(sh, radius); sh.raycastTarget = false;
            }
            var bd = Img(root, Ink, null, "Borde"); Fill(bd.rectTransform); Radius(bd, radius);
            face = Img(bd.transform, faceColor, null, "Cara"); Fill(face.rectTransform, border * K, border * K, border * K, border * K); Radius(face, Mathf.Max(1, radius - border)); face.raycastTarget = false;
            if (rot != 0) Tilt(root, rot);
            return root;
        }
        Button StickerButton(Transform p, Color faceColor, System.Action on, out Image face, float radius = 18, float border = 3.5f, float shadow = 5, float rot = 0, string name = "Boton")
        {
            var r = Sticker(p, faceColor, out face, radius, border, shadow, rot, name);
            var bd = r.Find("Borde").GetComponent<Image>();
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = bd; b.transition = Selectable.Transition.None;
            var f = face;
            b.onClick.AddListener(() => { StartCoroutine(Press(r)); on?.Invoke(); });
            return b;
        }
        // pequeño «toque»: la pegatina se hunde un instante
        System.Collections.IEnumerator Press(RectTransform r)
        {
            if (!r) yield break; var s = r.localScale;
            r.localScale = s * 0.94f; yield return new WaitForSecondsRealtime(0.08f); if (r) r.localScale = s;
        }
        // Cinta adhesiva semitransparente pegada arriba
        Image Tape(Transform p, float w = 34, float dx = 0, float rot = -6, float top = -9)
        {
            var t = Img(p, new Color(1, 0.96f, 0.88f, 0.85f), null, "Cinta"); t.sprite = null; t.raycastTarget = false;
            var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w * K, 14 * K); r.anchoredPosition = new Vector2(dx * K, -(top + 7) * K); Tilt(t.transform, rot);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.17f, 0.14f, 0.25f, 0.25f); o.effectDistance = new Vector2(2, -2);
            return t;
        }
        // Texto en px de diseño; big = Fredoka (grande), si no Nunito
        Text TB(Transform p, string s, float px, Color c, bool big = true, TextAnchor a = TextAnchor.MiddleCenter)
        {
            var t = T(p, s, Mathf.RoundToInt(px * K), c, a, FontStyle.Normal);
            t.font = big ? fontBig : fontBlack; t.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(px * K * 0.6f));
            return t;
        }
        // Rótulo pequeño en mayúsculas (Nunito Black)
        Text Cap(Transform p, string s, float px = 11, Color? c = null, TextAnchor a = TextAnchor.MiddleCenter) => TB(p, s, px, c ?? Ink, false, a);
        Image Icn(Transform p, Sprite s, float x, float top, float w, float h, bool fromRight = false)
        {
            var i = Img(p, Color.white, s, "Icono"); i.type = Image.Type.Simple; i.preserveAspect = true; i.raycastTarget = false; TopAt(i.rectTransform, x, top, w, h, fromRight); return i;
        }
        Image IcnFill(Transform p, Sprite s, float pad = 0)
        {
            var i = Img(p, Color.white, s, "Icono"); i.type = Image.Type.Simple; i.preserveAspect = true; i.raycastTarget = false; Inset(i.rectTransform, pad, pad, pad, pad); return i;
        }
        // Fondo de cartón kraft para una pantalla entera
        Image KraftBg(Transform p)
        {
            var bg = Img(p, Color.white, KraftSprite(), "Carton"); bg.type = Image.Type.Tiled; bg.pixelsPerUnitMultiplier = 1f / K; Fill(bg.rectTransform);
            return bg;
        }
        // Línea a cuadros (bandera de meta)
        Image Checker(Transform p, float cell = 11)
        {
            var c = Img(p, Color.white, CheckerSprite(), "Cuadros"); c.type = Image.Type.Tiled; c.pixelsPerUnitMultiplier = 1f / (cell * K); c.raycastTarget = false; return c;
        }
        // Franja azul de carreras (de la propuesta C) con dos líneas crema y una línea a cuadros debajo, girada
        RectTransform Band(Transform p, float top, float h, float rot = -9, bool checker = true)
        {
            var root = R("Franja", p); root.anchorMin = root.anchorMax = new Vector2(0.5f, 1); root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(560 * K, (h + (checker ? 50 : 0)) * K); root.anchoredPosition = new Vector2(0, -(top + (h + (checker ? 50 : 0)) / 2) * K);
            Tilt(root, rot);
            var bd = Img(root, Ink, null, "Borde"); bd.sprite = null; TopBox(bd.rectTransform, 0, 0, 0, h + 7); bd.raycastTarget = false;
            var bl = Img(bd.transform, Blue, null, "Azul"); bl.sprite = null; Inset(bl.rectTransform, 0, 3.5f, 0, 3.5f); bl.raycastTarget = false;
            var l1 = Img(bl.transform, Cream, null, "Linea"); l1.sprite = null; TopBox(l1.rectTransform, 0, 0, 13, 10); l1.raycastTarget = false;
            var l2 = Img(bl.transform, Cream, null, "Linea"); l2.sprite = null; TopBox(l2.rectTransform, 0, 0, h - 33, 10); l2.raycastTarget = false;
            if (checker)
            {
                var cb = Img(root, Ink, null, "Borde cuadros"); cb.sprite = null; TopBox(cb.rectTransform, 0, 0, h + 20, 28); cb.raycastTarget = false;
                var ch = Checker(cb.transform); Inset(ch.rectTransform, 0, 3, 0, 3);
            }
            return root;
        }
        // Barra de progreso con rayas (la de nivel, liga, etc.)
        Image Bar(Transform p, Color fill, float value, float radius = 6, float border = 2.5f)
        {
            var bd = Img(p, Ink, null, "Barra"); Radius(bd, radius); bd.raycastTarget = false;
            var bg = Img(bd.transform, Cream, null, "Fondo"); Inset(bg.rectTransform, border, border, border, border); Radius(bg, radius - border); bg.raycastTarget = false;
            var f = Img(bg.transform, fill, null, "Relleno"); var fr = f.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(Mathf.Clamp01(value), 1); fr.offsetMin = fr.offsetMax = Vector2.zero; Radius(f, radius - border); f.raycastTarget = false;
            return f;
        }
        static void SetBar(Image fill, float v) { if (fill) fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1); }

        // Botón «atrás» cuadrado con flecha (cabeceras)
        Button BackButton(Transform p, System.Action on)
        {
            var b = StickerButton(p, Cream, on, out var f, 14, 3, 3, -3, "Atras"); TopAt((RectTransform)b.transform, 14, 16, 50, 50);
            IcnFill(f.transform, B("back"), 8); return b;
        }
        // Hucha de monedas (píldora con moneda y cantidad)
        Text CoinPill(Transform p, float right, float top, float w = 118, float rot = 2)
        {
            var r = Sticker(p, Cream, out var f, 22, 3, 3, rot, "Monedas"); TopAt(r, right, top, w, 40, true);
            Icn(f.transform, B("coin"), 3, 3, 28, 28);
            var t = TB(f.transform, "0", 18, Ink, true, TextAnchor.MiddleRight); Inset(t.rectTransform, 34, 0, 10, 0); return t;
        }
        // Rótulo-pegatina (texto en una pegatina con tamaño ajustado al ancho dado)
        Text TagSticker(Transform p, string text, Color bg, float x, float top, float w, float h, float px = 13, bool big = false, float rot = 0, bool fromRight = false, Color? ink = null)
        {
            var r = Sticker(p, bg, out var f, 10, 3, 3, rot, "Rotulo"); TopAt(r, x, top, w, h, fromRight);
            var t = TB(f.transform, text, px, ink ?? Ink, big); Inset(t.rectTransform, 6, 0, 6, 0); return t;
        }

        static string N0(int v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
