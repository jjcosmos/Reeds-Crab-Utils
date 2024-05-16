using System.Globalization;
using System.IO;
using UnityEngine;

namespace ReedsCrabUtils
{
// --------------------- EDIT REF --------------------- //
// Root::Player::Start - Mod initialized

    public class Mod : UnityEngine.MonoBehaviour
    {
        public static Mod ModInstance;

        public bool ShowDebug;
        public Player PlayerRef;
        public DebugView DebugView = new DebugView();
        
        private Camera mainCamera;
        private Camera dbgCamera;

        public int DbgLayer = 31;

        private RenderTexture _overlayTex;
        private Material _overlayMaterial;

        public AssetBundle CustomBundle;

        private void OnGUI()
        {
            // ------------------------------------- Overlay (first, so that the UI renders over it)
            if (_overlayTex && ShowDebug && _overlayMaterial)
            {
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex, _overlayMaterial);
            }
            
            var hPos = 10;
            
            // ------------------------------------- Mod tag
            if (GUI.Button(new Rect(hPos, 10, 150, 50), "Mod Loaded!"))
            {
                print("Idk do something");
            }
            hPos += 150;
            
            
            // ------------------------------------- Triggers
            var showTriggersText = $"Col Vis: {DebugView.ColliderVisibility}";
            if (ShowDebug && GUI.Button(new Rect(hPos, 10, 150, 50), showTriggersText))
            {
                DebugView.ColliderVisibility = (DebugView.ColVis)((int)(DebugView.ColliderVisibility + 1) % (int)DebugView.ColVis.COUNT);
            }
            hPos += 150;
            
            // ------------------------------------- Names
            var showNamesText = DebugView.ShowNames ? "Names : Visible" : "Names : Hidden";
            if (ShowDebug && GUI.Button(new Rect(hPos, 10, 150, 50), showNamesText))
            {
                DebugView.ShowNames = !DebugView.ShowNames;
            }
            hPos += 150;
            
            if (ShowDebug && DebugView.ShowNames)
            {
                var screenSize = new Vector3(Screen.width, Screen.height);
                foreach (var rnd in DebugView.RenderNameDatas)
                {
                    rnd.Render(mainCamera, screenSize);
                }
            }

            // ------------------------------------- Alpha slider
            if (ShowDebug)
            {
                GUI.Label(new Rect(hPos, 10, 150, 100), "Collision Overlay Alpha");
                var alpha = GUI.HorizontalSlider(new Rect(hPos, 30, 150, 100), DebugView.Alpha, 0.01f, 1f);
                hPos += 160;
                GUI.Label(new Rect(hPos, 20, 30, 100), ((int)(DebugView.Alpha * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                if (!Mathf.Approximately(alpha, DebugView.Alpha))
                {
                    DebugView.Alpha = alpha;
                    //DebugView.SetMaterialColors();
                    _overlayMaterial.SetFloat("_Fade", DebugView.Alpha);
                }
                hPos += 30;
            }
        }

        private void Awake()
        {
            if (ModInstance != null)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                ModInstance = this;
                DontDestroyOnLoad(gameObject);
            }

            // Do this first. Please
            if (CustomBundle == null)
            {
                CustomBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "reedscustom"));
                if (CustomBundle == null)
                {
                    Debug.Log("[ReedsCrabUtils] Failed to load AssetBundle!");
                    return;
                }
            }
            
            DebugView.InitMeshes();
            CreateDbgCam();
        }

        private void CreateDbgCam()
        {
            mainCamera = Camera.main;

            if (mainCamera)
            {
                dbgCamera = GameObject.Instantiate(new GameObject()).AddComponent<Camera>();
                dbgCamera.transform.SetParent(transform);

                // set up culling
                dbgCamera.cullingMask = 1 << DbgLayer;
                mainCamera.cullingMask &= ~(1 << DbgLayer);
                
                dbgCamera.clearFlags = CameraClearFlags.SolidColor;
                dbgCamera.backgroundColor = Color.clear;
                dbgCamera.fieldOfView = mainCamera.fieldOfView;

                // set up rendering to tex for overlay
                dbgCamera.renderingPath = RenderingPath.VertexLit;
                
                if (_overlayTex == null)
                {
                    _overlayTex = new RenderTexture(Screen.width, Screen.height, 16);
                }

                dbgCamera.targetTexture = _overlayTex;
                _overlayMaterial = new Material(CustomBundle.LoadAsset<Shader>("OverlayFade"));
                _overlayMaterial.SetFloat("_Fade", DebugView.Alpha);
                
                Debug.Log($"[ReedsCrabUtils] Created collision overlay cam! Fov: {dbgCamera.fieldOfView} Flags:{dbgCamera.cullingMask}");
            }
            else
            {
                Debug.LogError("[ReedsCrabUtils] Failed to find main camera!");
            }
        }

        private void LateUpdate()
        {
            if (dbgCamera && mainCamera)
            {
                dbgCamera.fieldOfView = mainCamera.fieldOfView;
                dbgCamera.focalLength = mainCamera.focalLength;
                dbgCamera.transform.position = mainCamera.transform.position;
                dbgCamera.transform.rotation = mainCamera.transform.rotation;   
            }
        }

        private void Update()
        {
            PollInput();

            if (!PlayerRef)
            {
                PlayerRef = FindObjectOfType<Player>();
            }

            if (PlayerRef != null && ShowDebug)
            {
                DebugView.PollMeshes(PlayerRef.transform);
            }
        }

        private void PollInput()
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                ShowDebug = !ShowDebug;
            }
        }
    }
}