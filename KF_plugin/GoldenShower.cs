using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    //[KSPAddon(KSPAddon.Startup.Flight, false)]
    class GoldenShower : MonoBehaviour
    {
        Rect _windowRect = new Rect(400, 400, 128f, 1f);
        GameObject _coinPrefab;
        const int ShowerCoinCount = 800;

        void Start()
        {
            _coinPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere); // note: comes with sphere collider
            _coinPrefab.transform.localScale = new Vector3(0.04f, 0.01f, 0.04f);

            // KSP/Diffuse apparently needs a texture, can't just set a color like regular Diffuse shader
            var goldTexture = new Texture2D(1, 1);
            goldTexture.SetPixels32(new Color32[] { XKCDColors.GoldenYellow });
            goldTexture.Apply();

            _coinPrefab.renderer.material = new Material(Shader.Find("KSP/Diffuse")) { mainTexture = goldTexture };

            var rb = _coinPrefab.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.angularDrag = 5f;

            _coinPrefab.collider.material = new PhysicMaterial
            {
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounceCombine = PhysicMaterialCombine.Minimum,
                bounciness = 0.45f,
                dynamicFriction = 0.05f,
                staticFriction = 0.25f
            };
            _coinPrefab.SetActive(false);
        }

        void OnGUI()
        {
            _windowRect = KSPUtil.ClampRectToScreen(GUILayout.Window(123, _windowRect, DrawWindow, "Menu"));
        }

        void DrawWindow(int winid)
        {
            GUILayout.BeginVertical();
            //GUILayout.Label(string.Format("Gravity: {0}"));
            GUILayout.Label(string.Format("Accel: {0}", Physics.gravity.magnitude));
            if (GUILayout.Button("Increase monetary wealth?"))
                StartCoroutine(CoinShower());
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        float Random360()
        {
            return UnityEngine.Random.Range(0f, 360f);
        }

        System.Collections.IEnumerator CoinShower()
        {
            print("Let there be wealth!");
            var vessel = FlightGlobals.ActiveVessel;
            float start = Time.realtimeSinceStartup;

            for (int i = 0; i < ShowerCoinCount; ++i)
            {
                var spawn = vessel.GetWorldPos3D() + FlightGlobals.upAxis * 20f;
                var coin = (GameObject)Instantiate(_coinPrefab, spawn + UnityEngine.Random.insideUnitSphere * 2f,
                    Quaternion.Euler(new Vector3(Random360(),
                                                 Random360(),
                                                 Random360())));

                coin.rigidbody.velocity = vessel.rigidbody.velocity; // else if in orbit, coins will miss

                // impart a bit of force to get it spinning
                coin.rigidbody.AddTorque(new Vector3(Random360() * 0.1f, Random360() * 0.1f, Random360() * 0.1f), ForceMode.Impulse);
                coin.SetActive(true);

                // we might need to spawn more than [fps] coins per second if we're to reach ShowerCoinCount in
                // two seconds
                // so delay here if we're ahead of schedule, otherwise continue dumping coins
                while ((Time.realtimeSinceStartup - start) / 2f <= (float)i / ShowerCoinCount)
                    yield return 0;
            }
            print("Wealth complete");
        }
    }
} 
