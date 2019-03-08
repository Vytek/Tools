﻿using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Playtime_Painter {

    [HelpURL(PlaytimePainter.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterSystemMono
    {

        public static DepthProjectorCamera depthProjectorCamera;

        public static readonly BrushMeshGenerator BrushMeshGenerator = new BrushMeshGenerator();

        public static readonly MeshManager MeshManager = new MeshManager();

        public static readonly TextureDownloadManager DownloadManager = new TextureDownloadManager();
        
        #region Painter Data
        [SerializeField] private PainterDataAndConfig dataHolder;

        [NonSerialized] public bool triedToFindPainterData;

        public static PainterDataAndConfig Data  {
            get  {

                if (!_inst && !Inst)
                    return null;

                if (!_inst.triedToFindPainterData && !_inst.dataHolder) {
                  
                    _inst.dataHolder = Resources.Load<PainterDataAndConfig>("");

                    if (!_inst.dataHolder)
                        _inst.triedToFindPainterData = true;
                }

                return _inst.dataHolder;
            }
        }
        #endregion

        #region Camera Singleton
        private static PainterCamera _inst;

        public static PainterCamera Inst {
            get
            {
                if (_inst) return _inst;

                _inst = null;

                _inst = FindObjectOfType<PainterCamera>();
              
                if (!PainterSystem.applicationIsQuitting) {

                    if (!_inst)
                    {

                       /* var go = new GameObject(PainterDataAndConfig.PainterCameraName);
                        _inst = go.AddComponent<PainterCamera>();
                        PainterSystemManagerPluginBase.RefreshPlugins();
                        */
                        //#if UNITY_EDITOR
                            var go = Resources.Load("prefabs/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                            _inst = Instantiate(go).GetComponent<PainterCamera>();
                            _inst.name = PainterDataAndConfig.PainterCameraName;
                            PainterSystemManagerPluginBase.RefreshPlugins();
                        //#endif

                    }
                }
     
                if (_inst)
                    _inst.gameObject.SetActive(true);

                return _inst;
            }

            private set
            {
                _inst = value;

            }
        }
        #endregion

        public PlaytimePainter focusedPainter;

        public List<ImageMeta> blitJobsActive = new List<ImageMeta>();

        public bool isLinearColorSpace;

        #region Plugins
      
        private ListMetaData _pluginsMeta = new ListMetaData("Plugins", true, true, true, false, icon.Link);

        public IEnumerable<PainterSystemManagerPluginBase> Plugins
        {
            get {

                if (PainterSystemManagerPluginBase.plugins == null)
                    PainterSystemManagerPluginBase.RefreshPlugins();

                return PainterSystemManagerPluginBase.plugins;
            }
        }


        #endregion

        #region Painting Layer
        
        public int LayerFlag => (1 << (Data ? Data.playtimePainterLayer : 30));

        private void UpdateCullingMask() {

            var l = (Data ? Data.playtimePainterLayer : 30);

            var flag = (1 << l);

            if (_mainCamera)
                _mainCamera.cullingMask &= ~flag;

            if (painterCamera)
                painterCamera.cullingMask = flag;

            UnityUtils.RenamingLayer(l, "Playtime Painter's Layer");

            brushRenderer.gameObject.layer = l;

#if UNITY_EDITOR

            var vis = Tools.visibleLayers & flag;
            if (vis>0) {
                Debug.Log("Editor, hiding Layer {0}".F(l));
                Tools.visibleLayers &= ~flag;
            }
#endif

        }
        
        [SerializeField] private Camera _mainCamera;

        public Camera MainCamera {
            get { return _mainCamera; }
            set {
                if (value && painterCamera && value == painterCamera) {
                    "Can't use Painter Camera as Main Camera".showNotificationIn3D_Views();
                    return;
                }

                _mainCamera = value;

                UpdateCullingMask();
            }
        }
        
        [SerializeField] private Camera painterCamera;

        public RenderTexture TargetTexture
        {
            get { return painterCamera.targetTexture; }
            set { painterCamera.targetTexture = value; }
        }
        
        public RenderBrush brushPrefab;
        public const float OrthographicSize = 128; 

        public RenderBrush brushRenderer;
        #endregion

        public Material defaultMaterial;

        private static Vector3 _prevPosPreview;
        private static float _previewAlpha = 1;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("mm", MeshManager)
            .Add_Abstract("pl", PainterSystemManagerPluginBase.plugins, _pluginsMeta);

        public override bool Decode(string tg, string data)
        {
            switch (tg) {
                case "pl": data.Decode_List(out PainterSystemManagerPluginBase.plugins, ref _pluginsMeta, PainterSystemManagerPluginBase.all); break;
                case "mm": MeshManager.Decode(data); break;
                default: return false;
            }

            return true;
        }

        #endregion

        #region Double Buffer Painting

        public const int RenderTextureSize = 2048;
        
        public RenderTexture[] bigRtPair;
        public int bigRtVersion;

        public MeshRenderer secondBufferDebug;

        #endregion

        #region Buffer Scaling
        [NonSerialized] private readonly RenderTexture[] _squareBuffers = new RenderTexture[10];

        public RenderTexture GetSquareBuffer(int width)
        {
            int no = 9;
            switch (width)
            {
                case 8: no = 0; break;
                case 16: no = 1; break;
                case 32: no = 2; break;
                case 64: no = 3; break;
                case 128: no = 4; break;
                case 256: no = 5; break;
                case 512: no = 6; break;
                case 1024: no = 7; break;
                case 2048: no = 8; break;
                case 4096: no = 9; break;
                default: Debug.Log(width + " is not in range "); break;
            }

            if (!_squareBuffers[no])
                _squareBuffers[no] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            return _squareBuffers[no];
        }

        [NonSerialized]
        List<RenderTexture> nonSquareBuffers = new List<RenderTexture>();
        public RenderTexture GetNonSquareBuffer(int width, int height)
        {
            foreach (RenderTexture r in nonSquareBuffers)
                if ((r.width == width) && (r.height == height)) return r;

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            nonSquareBuffers.Add(rt);
            return rt;
        }

        public RenderTexture GetDownscaledBigRt(int width, int height) => Downscale_ToBuffer(bigRtPair[0], width, height);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material mat) => Downscale_ToBuffer(tex, width, height, mat, null);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Shader shade) => Downscale_ToBuffer(tex, width, height, null, shade);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height) => Downscale_ToBuffer(tex, width, height, null, Data.pixPerfectCopy);// brushRendy_bufferCopy);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material, Shader shader)
        {

            if (!tex)
                return null;

            if (!shader) shader = Data.brushBufferCopy;

            bool square = (width == height);
            if ((!square) || (!Mathf.IsPowerOfTwo(width)))
                return Render(tex, GetNonSquareBuffer(width, height), shader);
            else
            {
                int tmpWidth = Mathf.Max(tex.width / 2, width);

                RenderTexture from = material ? Render(tex, GetSquareBuffer(tmpWidth), material) : Render(tex, GetSquareBuffer(tmpWidth), shader);

                while (tmpWidth > width)
                {
                    tmpWidth /= 2;
                    from = material ? Render(from, GetSquareBuffer(tmpWidth), material) : Render(from, GetSquareBuffer(tmpWidth), shader);
                }

                return from;
            }
        }
        #endregion

        #region Buffers MGMT
        public ImageMeta imgMetaUsingRendTex;
        public List<MaterialMeta> materialsUsingTendTex = new List<MaterialMeta>();
        public PlaytimePainter autodisabledBufferTarget;

        public void EmptyBufferTarget()
        {

            if (imgMetaUsingRendTex == null)
                return;

            if (imgMetaUsingRendTex.texture2D) //&& (Application.isPlaying == false))
                imgMetaUsingRendTex.RenderTexture_To_Texture2D();

            imgMetaUsingRendTex.destination = TexTarget.Texture2D;

            foreach (var m in materialsUsingTendTex)
                m.SetTextureOnLastTarget(imgMetaUsingRendTex);

            materialsUsingTendTex.Clear();
            imgMetaUsingRendTex = null;
        }

        public void ChangeBufferTarget(ImageMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (newTarget != imgMetaUsingRendTex)
            {

                if (materialsUsingTendTex.Count > 0)
                    PlaytimePainter.SetOriginalShader();

                if (imgMetaUsingRendTex != null)
                {
                    if (imgMetaUsingRendTex.texture2D)
                        imgMetaUsingRendTex.RenderTexture_To_Texture2D();

                    imgMetaUsingRendTex.destination = TexTarget.Texture2D;

                    foreach (var m in materialsUsingTendTex)
                        m.SetTextureOnLastTarget(imgMetaUsingRendTex);
                }
                materialsUsingTendTex.Clear();
                autodisabledBufferTarget = null;
                imgMetaUsingRendTex = newTarget;
            }

            mat.bufferParameterTarget = parameter;
            mat.painterTarget = painter;
            materialsUsingTendTex.Add(mat);
        }

        public void UpdateBuffersState()
        {

            var cfg = TexMgmtData;

            if (!cfg)
                return;
            
            if (!GotBuffers)
            {
                bigRtPair = new RenderTexture[2];
                bigRtPair[0] = new RenderTexture(RenderTextureSize, RenderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                bigRtPair[1] = new RenderTexture(RenderTextureSize, RenderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                bigRtPair[0].wrapMode = TextureWrapMode.Repeat;
                bigRtPair[1].wrapMode = TextureWrapMode.Repeat;
                bigRtPair[0].name = "Painter Buffer 0 _ " + RenderTextureSize;
                bigRtPair[1].name = "Painter Buffer 1 _ " + RenderTextureSize;

            }

            if (secondBufferDebug)
            {
                secondBufferDebug.sharedMaterial.mainTexture = bigRtPair[1];
                var cmp = secondBufferDebug.GetComponent<PlaytimePainter>();
                if (cmp)
                    cmp.DestroyWhateverComponent();
            }

        }

     

        public static bool GotBuffers => Inst && Inst.bigRtPair != null && _inst.bigRtPair.Length > 0 && _inst.bigRtPair[0];
        #endregion

        #region Brush Shader MGMT

        ShaderProperty.TextureValue decal_HeightProperty =      new ShaderProperty.TextureValue("_VolDecalHeight");
        ShaderProperty.TextureValue decal_OverlayProperty =     new ShaderProperty.TextureValue("_VolDecalOverlay");
        ShaderProperty.VectorValue decal_ParametersProperty =   new ShaderProperty.VectorValue("_DecalParameters");

        public void Shader_UpdateDecal(BrushConfig brush)
        {

            VolumetricDecal vd = Data.decals.TryGet(brush.selectedDecal);

            if (vd != null)
            {
                decal_HeightProperty.GlobalValue = vd.heightMap;
                decal_OverlayProperty.GlobalValue = vd.overlay;
                decal_ParametersProperty.GlobalValue = new Vector4(brush.decalAngle * Mathf.Deg2Rad, (vd.type == VolumetricDecalType.Add) ? 1 : -1,
                        Mathf.Clamp01(brush.speed / 10f), 0);
            }

        }

        public static void Shader_PerFrame_Update(StrokeVector st, bool hidePreview, float size)
        {

            PainterDataAndConfig.BRUSH_POINTED_UV.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            QcMath.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f);

            PainterDataAndConfig.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
            PainterDataAndConfig.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude); //new Vector4(st.posTo.x, st.posTo.y, st.posTo.z, (st.posTo - prevPosPreview).magnitude));
            _prevPosPreview = st.posTo;
        }
        
        ShaderProperty.VectorValue brushColor_Property =        new ShaderProperty.VectorValue("_brushColor");
        ShaderProperty.VectorValue brushMask_Property =         new ShaderProperty.VectorValue("_brushMask");
        ShaderProperty.VectorValue maskDynamics_Property =      new ShaderProperty.VectorValue("_maskDynamics");
        ShaderProperty.VectorValue maskOffset_Property =        new ShaderProperty.VectorValue("_maskOffset");
        ShaderProperty.VectorValue brushForm_Property =         new ShaderProperty.VectorValue("_brushForm");
        ShaderProperty.VectorValue textureSourceParameters = new ShaderProperty.VectorValue("_srcTextureUsage");

        ShaderProperty.TextureValue sourceMask_Property = new ShaderProperty.TextureValue("_SourceMask");
        ShaderProperty.TextureValue sourceTexture_Property = new ShaderProperty.TextureValue("_SourceTexture");
        ShaderProperty.TextureValue transparentLayerUnder_Property = new ShaderProperty.TextureValue("_TransparentLayerUnderlay");

        public void Shader_UpdateBrushConfig(BrushConfig brush = null, float brushAlpha = 1, ImageMeta id = null, PlaytimePainter painter = null)
        {
            if (brush == null)
                brush = GlobalBrush;

            if (!painter)
                painter = PlaytimePainter.selectedInPlaytime;
            
            if (id == null && painter)
                id = painter.ImgMeta;
            
            brush.previewDirty = false;

            if (id == null)
                return;

            float textureWidth = id.width;
            var rendTex = id.TargetIsRenderTexture();

            var brushType = brush.GetBrushType(!rendTex);
            var blitMode = brush.GetBlitMode(!rendTex);

            var is3DBrush = brush.IsA3DBrush(painter);
            var isDecal = rendTex && brushType.IsUsingDecals;

            brushColor_Property.GlobalValue = brush.Color;

            brushMask_Property.GlobalValue = new Vector4(
                BrushExtensions.HasFlag(brush.mask, BrushMask.R) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.G) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.B) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.A) ? 1 : 0);

            float useTransparentLayerBackground = 0;

            if (id.isATransparentLayer)
            {
                var md = painter.MatDta;
                if (md != null && md.usePreviewShader && md.material) {
                    var mt = md.material.mainTexture;
                    transparentLayerUnder_Property.GlobalValue = mt;
                    useTransparentLayerBackground = (mt && (id != mt.GetImgDataIfExists())) ? 1 : 0;
                }
            }


            if (isDecal) Shader_UpdateDecal(brush);

            if (rendTex)
                sourceMask_Property.GlobalValue = brush.useMask ? Data.masks.TryGet(brush.selectedSourceMask) : null;

            maskDynamics_Property.GlobalValue = new Vector4(
                brush.maskTiling,
                rendTex ? brush.hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                ((brush.flipMaskAlpha || brush.useMask) ? 0 : 1) ,
                 (brush.maskFromGreyscale && brush.useMask) ? 1 : 0);

            maskOffset_Property.GlobalValue = brush.maskOffset.ToVector4();
                
            brushForm_Property.GlobalValue = new Vector4(
                brushAlpha, // x - transparency
                brush.Size(is3DBrush), // y - scale for sphere
                brush.Size(is3DBrush) / textureWidth, // z - scale for uv space
                brush.blurAmount); // w - blur amount

            brushType.SetKeyword(id.useTexCoord2);

            UnityUtils.SetShaderKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2, id.useTexCoord2);

            if (blitMode.SupportsTransparentLayer)
                UnityUtils.SetShaderKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);

            blitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (rendTex && blitMode.UsingSourceTexture)
            {
                sourceTexture_Property.GlobalValue = Data.sourceTextures.TryGet(brush.selectedSourceTexture);
                textureSourceParameters.GlobalValue = new Vector4(
                    (float)brush.srcColorUsage, 
                    brush.clampSourceTexture ? 1f : 0f,
                    useTransparentLayerBackground
                    );
            }
        }

        public void Shader_UpdateStrokeSegment(BrushConfig bc, float brushAlpha, ImageMeta id, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (bigRtPair == null)
                UpdateBuffersState();

            var isDoubleBuffer = !id.renderTexture;

            var useSingle = !isDoubleBuffer || bc.IsSingleBufferBrush();

            if (!useSingle && !secondBufferUpdated)
                UpdateBufferTwo();

            if (stroke.firstStroke)
                Shader_UpdateBrushConfig(bc, brushAlpha, id, pntr);

            TargetTexture = id.CurrentRenderTexture();

            if (isDoubleBuffer)
                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = bigRtPair[1];

            Shader shd = null;
            if (pntr)
                foreach (var pl in PainterSystemManagerPluginBase.BrushPlugins) {
                    var bs = useSingle ? pl.GetBrushShaderSingleBuffer(pntr) : pl.GetBrushShaderDoubleBuffer(pntr);
                    if (!bs) continue;
                    shd = bs;
                    break;
                }



            if (!shd)
            {
                var blitMode = bc.GetBlitMode(false);
                shd = useSingle ? blitMode.ShaderForSingleBuffer : blitMode.ShaderForDoubleBuffer;
            }

            brushRenderer.Set(shd);

        }

        #endregion

        #region Blit Textures
        public void Blit(Texture tex, ImageMeta id)
        {
            if (!tex || id == null)
                return;
            brushRenderer.Set(Data.pixPerfectCopy);
            Graphics.Blit(tex, id.CurrentRenderTexture(), brushRenderer.meshRenderer.sharedMaterial);

            AfterRenderBlit(id.CurrentRenderTexture());

        }

        public void Blit(Texture from, RenderTexture to) =>  Blit(from, to, Data.pixPerfectCopy);
        
        public void Blit(Texture from, RenderTexture to, Shader blitShader)
        {

            if (!from)
                return;
            brushRenderer.Set(blitShader);
            Graphics.Blit(from, to, brushRenderer.meshRenderer.sharedMaterial);
            AfterRenderBlit(to);
        }
        #endregion

        #region Render

        ShaderProperty.VectorValue cameraPosition_Property = new ShaderProperty.VectorValue("_RTcamPosition");

        public void Render()
        {

            //if (!secondBufferUpdated)
                //Debug.Log("Second buffer dirty");//  UpdateBufferTwo();

            transform.rotation = Quaternion.identity;
            cameraPosition_Property.GlobalValue = transform.position.ToVector4();

            brushRenderer.gameObject.SetActive(true);
            painterCamera.Render();
            brushRenderer.gameObject.SetActive(false);

            secondBufferUpdated = false;

            if (brushRenderer.deformedBounds)
                brushRenderer.RestoreBounds();

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shade)
        {
            brushRenderer.CopyBuffer(from, to, shade);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to, Material mat)
        {
            brushRenderer.CopyBuffer(from, to, mat);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy);

        public RenderTexture Render(ImageMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy);

        public RenderTexture Render(Texture from, ImageMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy);

        public void Render(Color col, RenderTexture to)
        {
            TargetTexture = to;
            brushRenderer.PrepareColorPaint(col);
            Render();
            AfterRenderBlit(to);
        }

        void AfterRenderBlit(Texture target) {
            if (bigRtPair.Length > 0 && bigRtPair[0] && bigRtPair[0] == target)
                secondBufferUpdated = false;
        }

        public void UpdateBufferTwo() {
            brushRenderer.Set(Data.pixPerfectCopy);
            Graphics.Blit(bigRtPair[0], bigRtPair[1]);
            secondBufferUpdated = true;
            bigRtVersion++;
        }

        public bool secondBufferUpdated;
        public void UpdateBufferSegment()
        {
            if (!Data.disableSecondBufferUpdateDebug)
            {
                brushRenderer.Set(bigRtPair[0]);
                TargetTexture = bigRtPair[1];
                brushRenderer.Set(Data.brushBufferCopy);
                Render();
                secondBufferUpdated = true;
                bigRtVersion++;
            }
        }
        #endregion

        #region Component MGMT
        private void OnEnable() {

            if (!MainCamera)
                MainCamera = Camera.main;

            PainterSystem.applicationIsQuitting = false;

            Inst = this;

            if (!Data)
                dataHolder = Resources.Load("Painter_Data") as PainterDataAndConfig;

            MeshManager.OnEnable();

            if (!painterCamera)
                painterCamera = GetComponent<Camera>();

            UpdateCullingMask();
            
            #if BUILD_WITH_PAINTER
            if (!PainterDataAndConfig.toolEnabled && !Application.isEditor)
                    PainterDataAndConfig.toolEnabled = true;
            #endif

            #if UNITY_EDITOR

            EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            EditorSceneManager.sceneSaving += BeforeSceneSaved;

            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorSceneManager.sceneOpening += OnSceneOpening;

            if (!defaultMaterial)
                defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (!defaultMaterial) Debug.Log("Default Material not found.");

            isLinearColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;

            EditorApplication.update -= CombinedUpdate;
            if (!UnityUtils.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += CombinedUpdate;


            if (!brushPrefab) {
                var go = Resources.Load("prefabs/RenderCameraBrush") as GameObject;
                if (go)
                {
                    brushPrefab = go.GetComponent<RenderBrush>();
                    if (!brushPrefab)
                        Debug.Log("Couldn't find brush prefab.");
                }
                else
                    Debug.LogError("Couldn't load brush Prefab");
            }

           

            #endif

            if (!brushRenderer)
            {
                brushRenderer = GetComponentInChildren<RenderBrush>();
                if (!brushRenderer)
                {
                    brushRenderer = Instantiate(brushPrefab.gameObject).GetComponent<RenderBrush>();
                    brushRenderer.transform.parent = transform;
                }
            }
         

#if BUILD_WITH_PAINTER || UNITY_EDITOR


            transform.position = Vector3.up * 3000;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (!painterCamera)
            {
                painterCamera = GetComponent<Camera>();
                if (!painterCamera)
                    painterCamera = gameObject.AddComponent<Camera>();
            }

            painterCamera.orthographic = true;
            painterCamera.orthographicSize = OrthographicSize;
            painterCamera.clearFlags = CameraClearFlags.Nothing;
            painterCamera.enabled = Application.isPlaying;
            painterCamera.allowHDR = true;
            painterCamera.allowMSAA = false;
            painterCamera.allowDynamicResolution = false;
            painterCamera.depth = 0;
            painterCamera.renderingPath = RenderingPath.Forward;
            painterCamera.nearClipPlane = 0.1f;
            painterCamera.farClipPlane = 1000f;
            painterCamera.rect = Rect.MinMaxRect(0,0,1,1);

#if UNITY_EDITOR
            EditorApplication.update -= CombinedUpdate;
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
                EditorApplication.update += CombinedUpdate;
#endif

            UpdateBuffersState();

#endif

            autodisabledBufferTarget = null;

            PainterSystemManagerPluginBase.RefreshPlugins();

            foreach (var p in PainterSystemManagerPluginBase.plugins)
                p?.Enable();
            
            if (Data)
                Data.ManagedOnEnable();

        }

        private void OnDisable() {
            PainterSystem.applicationIsQuitting = true;
            
            DownloadManager.Dispose();

            BeforeClosing();

            if (PainterSystemManagerPluginBase.plugins!= null)
                foreach (var p in PainterSystemManagerPluginBase.plugins)
                    p?.Disable();
                

            if (Data)
                Data.ManagedOnDisable();

        }

        private void BeforeClosing()
        {
            #if UNITY_EDITOR
            if (PlaytimePainter.previewHolderMaterial)
                PlaytimePainter.previewHolderMaterial.shader = PlaytimePainter.previewHolderOriginalShader;

            if (materialsUsingTendTex.Count > 0)
                autodisabledBufferTarget = materialsUsingTendTex[0].painterTarget;
            EmptyBufferTarget();
            #endif

        }
#if UNITY_EDITOR
        public void OnSceneOpening(string path, OpenSceneMode mode)
        {
            // Debug.Log("On Scene Opening");
        }

        public void BeforeSceneSaved(UnityEngine.SceneManagement.Scene scene, string path)
        {
            //public delegate void SceneSavingCallback(Scene scene, string path);


            BeforeClosing();
            // Debug.Log("Before Scene saved");

        }
        #endif

#if UNITY_EDITOR || BUILD_WITH_PAINTER
        
        public void Update() {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public static GameObject refocusOnThis;
        #if UNITY_EDITOR
        private static int _scipFrames = 3;
        #endif
      

        public void CombinedUpdate() {

            if (!Data)
                return;

            if (!PainterSystem.IsPlaytimeNowDisabled && PlaytimePainter.IsCurrentTool && focusedPainter)
                focusedPainter.ManagedUpdate();

            if (GlobalBrush.previewDirty)
                Shader_UpdateBrushConfig();

            PlaytimePainter uiPainter = null;

            MeshManager.CombinedUpdate();

            if (!Application.isPlaying && depthProjectorCamera)
                depthProjectorCamera.ManagedUpdate();

#if UNITY_2018_1_OR_NEWER
            foreach ( var j in blitJobsActive) 
                if (j.jobHandle.IsCompleted)
                    j.CompleteJob();
#endif

            Data.ManagedUpdate();

           
            var l = PlaytimePainter.PlaybackPainters;

            if (l.Count > 0 && !StrokeVector.pausePlayback)
            {
                if (!l.Last())
                    l.RemoveLast(1);
                else
                    l.Last().PlaybackVectors();
            }

#if UNITY_EDITOR
            if (refocusOnThis) {
                _scipFrames--;
                if (_scipFrames == 0) {
                    UnityUtils.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    _scipFrames = 3;
                }
            }
#endif

            if (Application.isPlaying && Data && Data.disableNonMeshColliderInPlayMode && MainCamera) {
                RaycastHit hit;
                if (Physics.Raycast(MainCamera.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    var c = hit.collider;
                    if (c.GetType() != typeof(MeshCollider) && PlaytimePainter.CanEditWithTag(c.tag)) c.enabled = false;
                }
            }

            if (!uiPainter || !uiPainter.CanPaint()) {

                var p = PlaytimePainter.currentlyPaintedObjectPainter;

                if (p && !Application.isPlaying){
                    if (p.ImgMeta == null)
                        PlaytimePainter.currentlyPaintedObjectPainter = null;
                    else {
                        TexMgmtData.brushConfig.Paint(p.stroke, p);
                        p.ManagedUpdate();
                    }
                }
            }

            var needRefresh = false;
            if (PainterSystemManagerPluginBase.plugins!= null)
                foreach (var pl in PainterSystemManagerPluginBase.plugins)
                    if (pl != null)
                        pl.Update();
                    else needRefresh = true;

            if (needRefresh) {
                Debug.Log("Refreshing plugins");
                PainterSystemManagerPluginBase.RefreshPlugins();
            }

        }

        #endif

        public static void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.PlaybackPainters)
                p.playbackVectors.Clear();

            PlaytimePainter.cody = new StdDecoder(null);
        }

        #endregion

        #region Inspector
        #if PEGI

        public override bool Inspect()
        {
            var changed =  DependenciesInspect(true);

            if (Data)
                Data.Nested_Inspect().nl(ref changed);


            return changed;
        }

        public bool DependenciesInspect(bool showAll = false)
        {
            var changed = false;

            if (showAll)
                "Active Jobs: {0}".F(blitJobsActive.Count).nl();

            #if UNITY_EDITOR
            if (!Data)
            {
                pegi.nl();
                "No data Holder".edit(60, ref dataHolder).nl(ref changed);

                if (icon.Refresh.Click("Try to find it")) {
                    PainterSystem.applicationIsQuitting = false;
                    triedToFindPainterData = false;
                }

                if ("Create".Click().nl()) {
                    
                    PainterSystem.applicationIsQuitting = false;
                    triedToFindPainterData = false;

                    if (!Data) {
                        dataHolder = ScriptableObject.CreateInstance<PainterDataAndConfig>();

                        AssetDatabase.CreateAsset(dataHolder,
                            "Assets/Tools/Playtime_Painter/Resources/Painter_Data.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
            #endif

            if (showAll || bigRtPair.IsNullOrEmpty())
                (bigRtPair.IsNullOrEmpty() ? "No buffers" : "Using HDR buffers " + ((!bigRtPair[0]) ? "uninitialized" : "initialized")).nl();

            if (!painterCamera)
            {
                pegi.nl();
                "no painter camera".writeWarning();
                pegi.nl();
            }

#if BUILD_WITH_PAINTER 
            if (showAll || !MainCamera) {
                pegi.nl();

                var cam = MainCamera;

                if (!cam)
                    icon.Warning.write("No Main Camera found. Playtime Painting will not be possible");

                var cams = new List<Camera>(FindObjectsOfType<Camera>());

                if (painterCamera && cams.Contains(painterCamera))
                    cams.Remove(painterCamera);

                if ("Main Camera".select(60, ref cam, cams).changes(ref changed))
                    MainCamera = cam;
                
                if (icon.Refresh.Click("Try to find camera tagged as Main Camera", ref changed)) {
                    MainCamera = Camera.main;
                    if (!MainCamera)
                        "No camera is tagged as main".showNotificationIn3D_Views();
                }

                pegi.nl();
            }
#endif

            return changed;
        }

        public bool PluginsInspect() {

            var changed = false;

            if (!PainterSystem.IsPlaytimeNowDisabled)
            {

                changed |= _pluginsMeta.edit_List(ref PainterSystemManagerPluginBase.plugins, PainterSystemManagerPluginBase.all);

                if (!_pluginsMeta.Inspecting)
                {

                    if ("Find Plugins".Click())
                        PainterSystemManagerPluginBase.RefreshPlugins();

                    if ("Delete Plugins".Click().nl())
                        PainterSystemManagerPluginBase.plugins = null;

                }
            }
            else _pluginsMeta.Inspecting = false;

            return changed;
        }

        #endif
        #endregion

    }
}