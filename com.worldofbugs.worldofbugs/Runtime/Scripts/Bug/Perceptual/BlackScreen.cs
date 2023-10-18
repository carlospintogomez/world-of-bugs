﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorldOfBugs {
    /// <summary>
    /// The BlackScreen bug fills the screen with the given color (black by default).
    /// </summary>
    public class BlackScreen : Bug {

        // TODO support a list of cameras + colours to fill
        // TODO support constant flickering (i.e. not breaks) or have an explicite non-flickering duration.

        [MinMaxSlider(0.01f, 1f), Tooltip("Number of seconds to fill the screen black.")]
        public Vector2 frameRange = new Vector2(0.05f, 0.1f);

        [MinMaxSlider(0f, 10f), Tooltip("Number of seconds to flicker.")]
        public Vector2 flickerRange = new Vector2(2f, 5f);

        public Color ScreenColor = new Color(0, 0, 0, 1);

        protected FillScreen[] fillScreens;

        protected IEnumerator FlickerToggerCoroutine;
        protected IEnumerator FillScreenTogglerCoroutine;

        void Awake() {
            FlickerToggerCoroutine = FlickerTogger();
            FillScreenTogglerCoroutine = FillScreenToggler();
        }

        public override void OnEnable() {
            // TODO support multiple cameras?
            fillScreens = new FillScreen[2];
            Camera[] cameras = null;
            cameras = CameraExtensions.GetObservationCameras();
            fillScreens[0] = cameras[0].gameObject.AddComponent<FillScreen>();
            fillScreens[0].color = ScreenColor;
            cameras = CameraExtensions.GetBugMaskCamera();
            fillScreens[1] = cameras[0].gameObject.AddComponent<FillScreen>();
            fillScreens[1].color = (Color) bugType;
            //StartCoroutine(FlickerToggerCoroutine);
        }

        public override void OnDisable() {
            //StopCoroutine(FlickerToggerCoroutine);
            StopCoroutine(FillScreenTogglerCoroutine);

            foreach(FillScreen fs in fillScreens) {
                Destroy(fs);
            }
        }

        protected IEnumerator FlickerTogger() {
            bool _enabled = false;

            while(true) {
                _enabled = ! _enabled;

                if(_enabled) {
                    StartCoroutine(FillScreenTogglerCoroutine);
                } else {
                    StopCoroutine(FillScreenTogglerCoroutine);

                    foreach(FillScreen fs in fillScreens) {
                        fs.enabled = false;
                    }
                }

                yield return new WaitForSeconds(UnityEngine.Random.Range(flickerRange.x,
                                                flickerRange.y));
            }
        }

        protected IEnumerator FillScreenToggler() {
            while(true) {
                foreach(FillScreen fs in fillScreens) {
                    fs.enabled = !fs.enabled;
                }

                yield return new WaitForSeconds(UnityEngine.Random.Range(frameRange.x, frameRange.y));
            }
        }

        public class FillScreen : MonoBehaviour {

            [HideInInspector]
            public Color color {
                get {
                    return _color;
                }
                set {
                    _color = value;
                    _solid = new Texture2D(1, 1); // dont need a massive texture, let uv handle that!
                    _solid.SetPixels(new Color[] { value });
                    _solid.Apply();
                }
            }

            protected Color _color;
            protected Texture2D _solid;

            [HideInInspector]
            public RenderTexture renderTexture;

            public void Awake() {
                enabled = false;
            }

            public void OnRenderImage(RenderTexture src, RenderTexture dst) {
                if(enabled) {
                    RenderTexture.active = dst;
                    //Debug.Log($"fill screen {_color} {src} {dst}");
                    Graphics.Blit(_solid, dst);
                }  else {
                    // Graphics.Blit(src, dst);
                }
            }
        }
    }
}
