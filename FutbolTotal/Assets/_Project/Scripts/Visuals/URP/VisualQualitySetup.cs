using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FutbolTotal
{
    /// <summary>
    /// Ajustes de imagen para URP, creados al arrancar (sin assets de perfil que puedan perder referencias):
    /// - volumen global con tono de cine (ACES), bloom suave para focos y vallas, ajuste de color y viñeta;
    /// - cámara con posprocesado y suavizado de bordes SMAA de calidad alta;
    /// - sombras más lejanas y con cascadas, MSAA x4 y HDR en el asset de URP activo.
    /// Este script está en un ensamblado que solo se compila si el proyecto usa URP.
    /// Nota: el asset de URP es del proyecto, así que los cambios de sombras y MSAA se quedan guardados.
    /// </summary>
    public class VisualQualitySetup : MonoBehaviour
    {
        [SerializeField] private float bloomIntensity = 0.45f;
        [SerializeField] private float postExposure = 0.15f;
        [SerializeField] private float contrast = 12f;
        [SerializeField] private float saturation = 8f;
        [SerializeField] private float vignette = 0.2f;
        [SerializeField] private float shadowDistance = 110f;

        private VolumeProfile profile;

        private void Awake()
        {
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(0.6f);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(postExposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);

            Vignette vignetteEffect = profile.Add<Vignette>(true);
            vignetteEffect.intensity.Override(vignette);
            vignetteEffect.smoothness.Override(0.4f);

            volume.sharedProfile = profile;

            Camera cam = Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = shadowDistance;
                urp.shadowCascadeCount = 4;
                urp.msaaSampleCount = 4;
                urp.supportsHDR = true;
            }
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }
    }
}
