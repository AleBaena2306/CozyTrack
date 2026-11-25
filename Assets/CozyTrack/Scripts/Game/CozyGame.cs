using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyTrack
{
    // Juego completo de Cozy Track. Lo crea la escena Assets/CozyTrack/Scenes/CozyTrack.unity (un objeto con este componente)
    // y monta todo por código: cámara, luz, circuito, coches, interfaz, garaje y podio.
    public partial class CozyGame : MonoBehaviour
    {
        public static CozyGame I;
        CozyLibrary lib;
        Camera cam; Light sun; Transform trackRoot; GameObject ground; Material groundMat;
        Sprites3D fx, air;

        string state = "menu";                 // menu, count, race, end, garage
        readonly List<RaceCar> cars = new List<RaceCar>();
        RaceCar player, rival;
        string choice = "Dart", mode = "race", circId = "jardin", todId = "atardecer", look = "normal";
        CircuitDef circ;
        int laps = 3, perfects, crashes, levelNum; float raceT, countT, shake, menuT, levelSkill = 0.9f;
        bool countGas, assistOn = true, holding, autoTest; float pressedAt = 99;
        Vector3 camPos = new Vector3(0, 8, -8), camLook, camFocus; float camHeading;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60; Application.runInBackground = true;
            lib = CozyLibrary.I;
            if (lib == null || lib.toon == null) { Debug.LogError("[CozyTrack] Falta Resources/CozyLibrary: usa COZY TRACK > Configurar proyecto."); enabled = false; return; }
            LoadProfile();
            choice = PlayerPrefs.GetString("ctCar", "Dart");
            SetupScene();
            BuildUI();
            todId = PlayerPrefs.GetString("ctTod", "atardecer");
            look = PlayerPrefs.GetString("ctLook", "normal");
            if (!Usable(choice)) choice = CozyData.StartCars[0];
            BuildCircuit(CozyData.Circuit(PlayerPrefs.GetString("ctCirc", "jardin")));
            SetLook(look);
            ShowMenu();
        }

        void OnDestroy() { if (lib != null) foreach (var f in lib.edgeFeatures) if (f) f.SetActive(false); }

        // ---------- escena ----------
        void SetupScene()
        {
            var camGo = new GameObject("Camara"); camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 42; cam.nearClipPlane = 0.3f; cam.farClipPlane = 220; cam.clearFlags = CameraClearFlags.SolidColor;
            var sunGo = new GameObject("Sol"); sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 40; RenderSettings.fogEndDistance = 95;
            RenderSettings.sun = sun;
            ground = GameObject.CreatePrimitive(PrimitiveType.Quad); ground.name = "Suelo"; Destroy(ground.GetComponent<Collider>());
            ground.transform.rotation = Quaternion.Euler(90, 0, 0); ground.transform.position = new Vector3(0, -0.21f, 0); ground.transform.localScale = new Vector3(800, 800, 1);
            groundMat = Mat(Color.white, CozyTex.Get("floor_jardin")); groundMat.SetFloat("_Outline", 0);
            groundMat.SetTextureScale("_BaseMap", new Vector2(80, 80));
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            trackRoot = new GameObject("Circuito").transform;
            var unlit = new Material(lib.unlit); unlit.SetTexture("_BaseMap", CozyTex.Get("dot"));
            fx = new Sprites3D("Particulas", 400, unlit);
            var airMat = new Material(lib.unlit); airMat.SetTexture("_BaseMap", CozyTex.Get("dot"));
            air = new Sprites3D("Aire", 240, airMat);
            for (int k = 0; k < 240; k++) air.Add(new Vector3(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(0f, 6f), UnityEngine.Random.Range(-20f, 20f)), Color.white, 0.15f, 1e9f, Vector3.zero, 0);
            for (int k = 0; k < air.list.Count; k++) air.list[k].t = UnityEngine.Random.value * 6.28f;   // fase
            BuildBrakeLine();
            Shader.SetGlobalFloat("_CozyOutlineWidth", 0.045f);
            Shader.SetGlobalColor("_CozyShadowTint", CozyData.Hex("#6f55b0"));
        }

        // ---------- materiales ----------
        public Material Mat(Color c, Texture tex = null, bool transparent = false, bool cullOff = false, Color? emis = null, float outline = 1, float rim = 0)
        {
            var m = new Material(transparent ? lib.toonTransparent : lib.toon);
            m.SetColor("_BaseColor", c);
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Cull", cullOff || transparent ? 0 : 2);
            m.SetColor("_EmissionColor", emis ?? Color.black);
            m.SetFloat("_Outline", outline); m.SetFloat("_Rim", rim);
            return m;
        }
        readonly Dictionary<string, Material> toonCache = new Dictionary<string, Material>();
        Material ToonOf(Material s, bool outline, bool rim)
        {
            if (s == null) return Mat(Color.white);
            string key = s.GetInstanceID() + (outline ? "o" : "") + (rim ? "r" : "");
            if (toonCache.TryGetValue(key, out var m)) return m;
            Color c = s.HasProperty("_BaseColor") ? s.GetColor("_BaseColor") : s.HasProperty("_Color") ? s.color : Color.white;
            Texture t = s.HasProperty("_BaseMap") ? s.GetTexture("_BaseMap") : s.HasProperty("_MainTex") ? s.mainTexture : null;
            m = Mat(c, t, false, false, null, outline ? 1 : 0, rim ? 1 : 0); m.name = s.name;
            toonCache[key] = m; return m;
        }
        public GameObject Spawn(string name, Transform parent, bool outline = false, bool rim = false, Mesh wheel = null)
        {
            var src = lib.Get(name);
            GameObject g;
            if (src == null) { Debug.LogWarning("[CozyTrack] Falta el modelo " + name); g = new GameObject(name); g.transform.SetParent(parent, false); return g; }
            g = Instantiate(src, parent); g.name = name; g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity;
            if (name.StartsWith("Car_")) FixWheels(g, wheel);
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++) ms[k] = ToonOf(ms[k], outline, rim);
                r.sharedMaterials = ms;
            }
            return g;
        }
        // Las ruedas del FBX llegan en ejes de Blender (los empties no se convierten al exportar). Se recolocan colgando
        // del coche, con el eje a lo ancho (derecha del coche) y el mismo diámetro y grosor que traían.
        static void FixWheels(GameObject car, Mesh swap = null)
        {
            var root = car.transform; var keep = (root.position, root.rotation, root.localScale);
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var t in car.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Wheel_")) continue;
                var mf = t.GetComponent<MeshFilter>(); var r = t.GetComponent<Renderer>(); if (!mf || !mf.sharedMesh || !r) continue;
                var wb = r.bounds.size;
                float D = Mathf.Max(wb.x, Mathf.Max(wb.y, wb.z)), W = Mathf.Min(wb.x, Mathf.Min(wb.y, wb.z));
                var center = r.bounds.center;
                if (swap) mf.sharedMesh = swap;   // skin de ruedas: otro diseño con el mismo diámetro y grosor
                var ms = mf.sharedMesh.bounds.size;
                int ax = ms.x <= ms.y && ms.x <= ms.z ? 0 : ms.y <= ms.z ? 1 : 2;       // eje del disco en la malla
                t.SetParent(root, true);
                var axisLocal = ax == 0 ? Vector3.right : ax == 1 ? Vector3.up : Vector3.forward;
                t.localRotation = Quaternion.FromToRotation(axisLocal, Vector3.right);
                var sc = new Vector3(D / Mathf.Max(1e-4f, ms.x), D / Mathf.Max(1e-4f, ms.y), D / Mathf.Max(1e-4f, ms.z));
                sc[ax] = W / Mathf.Max(1e-4f, ms[ax]);
                t.localScale = sc;
                // los huecos de rueda (empties) llegan con los ejes de Blender (Y adelante, Z arriba): se pasan a los de Unity
                var target = new Vector3(-center.x, center.z, -center.y);
                t.position += target - r.bounds.center;
            }
            root.SetPositionAndRotation(keep.Item1, keep.Item2);
        }

        public GameObject Prim(PrimitiveType t, Color c, Vector3 pos, Vector3 scale, Transform parent, float ry = 0, Material mat = null)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localRotation = Quaternion.Euler(0, ry, 0);
            g.GetComponent<Renderer>().sharedMaterial = mat ? mat : Mat(c, null, false, false, null, 0);
            return g;
        }

        // ---------- estilo visual y hora del día ----------
        void SetLook(string l)
        {
            look = l; PlayerPrefs.SetString("ctLook", l);
            Shader.SetGlobalFloat("_CozyToon", l == "normal" ? 0 : 1);
            foreach (var f in lib.edgeFeatures) if (f) f.SetActive(l == "edge");
            if (l == "edge" && lib.edgeFeatures.Length == 0) Toast("Falta el efecto de bordes: COZY TRACK > Configurar proyecto");
            ApplyTod(todId);
            RefreshMenuSelections();
        }

        readonly List<GameObject> lampBulbs = new List<GameObject>();
        void ApplyTod(string id)
        {
            todId = id; var t = CozyData.Tod(id); PlayerPrefs.SetString("ctTod", id);
            cam.backgroundColor = t.bg; RenderSettings.fogColor = t.fog;
            var sky = t.sky * 0.95f; var gnd = look == "normal" ? t.gnd : CozyData.Hex("#6f55b0");
            Shader.SetGlobalColor("_CozySky", sky); Shader.SetGlobalColor("_CozyGround", gnd);
            sun.color = t.sun; sun.intensity = t.sunI; sun.transform.rotation = Quaternion.Euler(t.sunDir);
            groundMat.SetColor("_BaseColor", circ != null && circ.room != "jardin" ? Color.white : t.ground);
            foreach (var b in lampBulbs) if (b) b.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", t.lamps ? CozyData.Hex("#ffd88a") * 1.6f : Color.black);
            air.mat.SetColor("_BaseColor", t.air == "luciernagas" ? CozyData.Hex("#fff38a") : t.air == "hojas" ? CozyData.Hex("#ff9e40") : Color.white);
            air.mat.SetFloat("_DstBlend", t.air == "luciernagas" ? 1 : 10);   // aditivo para las luciérnagas
            RefreshMenuSelections();
        }

        float airT;
        void UpdateAir(float dt)
        {
            airT += dt; var t = CozyData.Tod(todId); var c = camLook;
            float size = t.air == "hojas" ? 0.35f : t.air == "luciernagas" ? 0.22f : 0.12f;
            foreach (var p in air.list)
            {
                var x = p.pos; float ph = p.t;
                if (t.air == "hojas") { x.y -= dt * 0.6f; x.x += Mathf.Sin(airT * 1.5f + ph) * dt * 1.2f + dt * 0.8f; }
                else if (t.air == "luciernagas") { x.x += Mathf.Sin(airT * 0.8f + ph) * dt * 0.6f; x.y = 0.6f + Mathf.Abs(Mathf.Sin(airT * 0.5f + ph)) * 2.5f; x.z += Mathf.Cos(airT * 0.7f + ph) * dt * 0.6f; }
                else { x.y += dt * 0.1f; x.x += Mathf.Sin(airT * 0.4f + ph) * dt * 0.3f; }
                if (x.y < 0) x.y = 6; if (x.y > 6.5f) x.y = 0.2f;
                if (x.x - c.x > 20) x.x -= 40; if (x.x - c.x < -20) x.x += 40; if (x.z - c.z > 20) x.z -= 40; if (x.z - c.z < -20) x.z += 40;
                p.pos = x; p.size = size;
                p.col = new Color(1, 1, 1, t.air == "luciernagas" ? 0.6f + 0.4f * Mathf.Sin(airT * 3 + ph) : 0.85f);
            }
            air.Render(cam, false);
        }

        // ---------- bucle ----------
        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            ReadInput(dt);
            DevCommands(dt);
            OnlineTick(dt);
            if (state == "count")
            {
                countT -= dt;
                bool g = Gas();
                if (g && !countGas) pressedAt = countT;
                countGas = g;
                int n = Mathf.CeilToInt(countT - 0.2f);
                SetCount(n > 0 ? n.ToString() : "¡Ya!");
                if (countT <= 0)
                {
                    state = "race"; SetCount("");
                    if (countGas && pressedAt <= 0.6f) { player.turbo = 0.8f; Toast("¡Salida perfecta!"); }
                    else if (countGas && pressedAt > 1.4f) { player.spin = 0.5f; Toast("Salida en falso"); }
                }
            }
            if (state == "race" || state == "end") RaceTick(dt);
            fx.Tick(dt, cam);
            UpdateAir(dt);
            AnimateSurfaces(dt);
            UpdateObstacles(dt);
            foreach (var c in cars) PlaceCar(c, dt);
            foreach (var b in boxes) if (b.m) b.m.transform.Rotate(0, dt * 90, 0);
            foreach (var co in coins) if (co.m) co.m.transform.Rotate(0, dt * 180, 0);
            UpdateCamera(dt);
            UpdateSpeedLines(dt);
            UpdateChestsUI();
        }

        void UpdateCamera(float dt)
        {
            if (state == "garage")
            {
                cam.transform.position = new Vector3(200, garageView.y + 0.5f, -garageView.z); cam.transform.LookAt(new Vector3(200, garageView.y, 0));
                foreach (var s in garageSlots) s.m.transform.localRotation = Quaternion.Euler(0, s.n == choice ? s.m.transform.localEulerAngles.y + dt * 70 : 180 - 32, 0);
                sun.transform.rotation = Quaternion.Euler(35, 160, 0);
                return;
            }
            if (player != null && state != "menu")
            {
                // La cámara sigue al coche tal y como se ve (también cuando sale volando en un accidente), sin quedarse atrás:
                // solo se suaviza el giro, no la distancia, y mira poco por delante para que el coche quede en el centro.
                var w = Track.World(player.i, player.u, player.o);
                var focus = player.g ? player.g.transform.position : w.pos;
                if (player.crash > 0) focus.y = Mathf.Min(focus.y, w.pos.y + 1.5f);
                camHeading = Mathf.LerpAngle(camHeading, w.h * Mathf.Rad2Deg, 1 - Mathf.Exp(-dt * (player.crash > 0 ? 1.5f : 4)));
                float hr = camHeading * Mathf.Deg2Rad;
                var f = new Vector3(Mathf.Sin(hr), 0, Mathf.Cos(hr));
                camFocus = Vector3.Lerp(camFocus, focus, 1 - Mathf.Exp(-dt * 14));
                if ((camFocus - focus).sqrMagnitude > 64) camFocus = focus;   // salida o recolocación: sin barrido
                var want = new Vector3(camFocus.x, camFocus.y * 0.6f + 10.5f, camFocus.z) - f * 7.5f;
                camPos = want;
                camLook = new Vector3(camFocus.x, camFocus.y * 0.6f, camFocus.z) + f * 3.5f;
                cam.fieldOfView += ((42 + 9 * Mathf.Min(1.3f, player.v / player.vtop)) - cam.fieldOfView) * Mathf.Min(1, dt * 3);
                UpdateBrakeLine();
                if (state != "end") UpdateHUD();
            }
            else
            {
                // escaparate del menú: el coche elegido gira en su plataforma
                menuT += dt;
                bool picking = screens.TryGetValue("car", out var cs) && cs.activeSelf;
                camPos = ShowroomPos + new Vector3(0, 2.4f, -10.5f); camLook = ShowroomPos + new Vector3(0, picking ? -1.7f : -0.6f, 0);
                cam.fieldOfView = 34;
                if (showCar) showCar.transform.rotation = Quaternion.Euler(0, 200 + menuT * 25, 0);
                sun.transform.rotation = Quaternion.Euler(42, 25, 0);
            }
            cam.transform.position = camPos;
            if (shake > 0) { shake = Mathf.Max(0, shake - dt); cam.transform.position += new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, 0) * shake; }
            cam.transform.LookAt(camLook);
            if (state != "garage" && state != "menu") sun.transform.rotation = Quaternion.Euler(CozyData.Tod(todId).sunDir);
        }

        // ---------- entrada: mantener = acelerar, deslizar = carril; teclado ↑/espacio, ←/→, E ----------
        float swipeX = -1; bool wasDown;
        bool Gas() => holding || (Keyboard.current != null && (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.spaceKey.isPressed));
        void ReadInput(float dt)
        {
            bool down = false; Vector2 pos = Vector2.zero;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) { down = true; pos = Touchscreen.current.primaryTouch.position.ReadValue(); }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed) { down = true; pos = Mouse.current.position.ReadValue(); }
            bool started = down && !wasDown; wasDown = down;
            if (state == "garage" && started && !OverUI(pos)) PickGarage(pos);
            if (state == "race" || state == "count")
            {
                if (started) { if (OverItemButton(pos)) { if (state == "race") UseItem(player); swipeX = -1; } else swipeX = pos.x; }
                holding = (down && swipeX >= 0) || autoTest;
                if (down && swipeX >= 0 && Mathf.Abs(pos.x - swipeX) > Screen.width * 0.06f) { Steer(pos.x > swipeX ? 1 : -1); swipeX = pos.x; }
                if (!down) swipeX = -1;
                var kb = Keyboard.current;
                if (kb != null && state == "race")
                {
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Steer(-1);
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Steer(1);
                    if (kb.eKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame) UseItem(player);
                }
            }
            else holding = false;
        }
        void Steer(int d) { var w = Track.World(player.i, player.u, player.o); ChangeLane(player, Mathf.Cos(w.roll) < 0 ? -d : d); }

        // ---------- órdenes de prueba desde Logs/cozy_game_cmd.txt (solo en el Editor) ----------
        float cmdPoll, autoItem;
        void DevCommands(float dt)
        {
#if UNITY_EDITOR
            if (autoTest && state == "race") { autoItem -= dt; if (autoItem <= 0) { autoItem = 2.5f; player.item = "autopilot"; UseItem(player); } }
            cmdPoll -= dt; if (cmdPoll > 0) return; cmdPoll = 0.5f;
            string path = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "cozy_game_cmd.txt");
            if (!System.IO.File.Exists(path)) return;
            string[] lines; try { lines = System.IO.File.ReadAllLines(path); System.IO.File.Delete(path); } catch { return; }
            foreach (var raw in lines)
            {
                var p = raw.Trim().Split(' '); if (p.Length == 0 || p[0] == "") continue;
                try
                {
                    switch (p[0])
                    {
                        case "go": mode = p.Length > 1 ? p[1] : mode; StartRace(); break;
                        case "circuit": BuildCircuit(CozyData.Circuit(p[1])); break;
                        case "look": SetLook(p[1]); break;
                        case "tod": ApplyTod(p[1]); break;
                        case "car": choice = p[1]; MenuGrid(); break;
                        case "auto": autoTest = p.Length < 2 || p[1] != "off"; break;
                        case "give": if (player != null) { player.item = p[1]; player.uses = p[1] == "turbo3" ? 3 : 0; } break;
                        case "garage": OpenGarage(); break;
                        case "album": ShowScreen("album"); break;
                        case "levels": ShowScreen("levels"); break;
                        case "level": StartLevel(int.Parse(p[1])); break;
                        case "taller": ShowScreen("taller"); break;
                        case "daily": StartDaily(); break;
                        case "menu": ShowMenu(); break;
                        case "online": OpenOnline(); break;
                        case "find": FindRace(); break;
                        case "board": OpenBoard(); break;
                        case "account": OpenAccount(); break;
                        case "scroll": menuScroll.verticalNormalizedPosition = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture); break;
                    }
                }
                catch (Exception e) { Debug.LogWarning("[CozyTrack] orden " + raw + ": " + e.Message); }
            }
#endif
        }
    }
}
