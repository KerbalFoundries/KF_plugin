/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * HUGE thanks to xEvilReeperx for this water code, along with gerneral coding help along the way!
 * Still works in 1.0.2!
 */

using System;
using System.Linq;
using UnityEngine;



namespace KerbalFoundries
{
    public class ModuleWaterSlider : VesselModule
    {
		readonly GameObject _collider = new GameObject("ModuleWaterSlider.Collider", typeof(BoxCollider), typeof(Rigidbody));
		const float triggerDistance = 25f;
        bool isActive;
        Vessel _vessel;
        public float colliderHeight = -2.5f;

        void Start()
        {
            print("WaterSlider start");
            _vessel = GetComponent<Vessel>();

            float repulsorCount = 0;
            foreach (Part PA in _vessel.parts)
            {
                foreach (KFRepulsor RA in PA.GetComponentsInChildren<KFRepulsor>())
                    repulsorCount++;
                foreach (RepulsorWheel RA in PA.GetComponentsInChildren<RepulsorWheel>())
                    repulsorCount++;
            }
            if (repulsorCount > 0)
                isActive = true;
            
            if (!isActive)
                return;


            var box = _collider.collider as BoxCollider;
			box.size = new Vector3(300f, .5f, 300f); // Probably should encapsulate other colliders in real code
			/*
			The line above reports that a NullReferenceException will occur when "using memver of a null reference."
			Might want to look into this.  I's the "box.size" part that it doesn't like, and gives no suggestions
			on how to fix. - Gaalidas
			 */
            //has always worked fine - assume we don't do that! - Lo-Fi

            var rb = _collider.rigidbody;
            rb.isKinematic = true;

            _collider.SetActive(true);

            var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visible.transform.parent = _collider.transform;
            visible.transform.localScale = box.size;
            visible.renderer.enabled = true; // enable to see collider
            UpdatePosition();
        }

        void UpdatePosition()
        {
            Vector3d oceanNormal = _vessel.mainBody.GetSurfaceNVector(_vessel.latitude, _vessel.longitude);
			Vector3 newPosition = (_vessel.ReferenceTransform.position - oceanNormal * (FlightGlobals.getAltitudeAtPos(_vessel.ReferenceTransform.position) - colliderHeight));
            _collider.rigidbody.position = newPosition;
            _collider.rigidbody.rotation = Quaternion.LookRotation(oceanNormal) * Quaternion.AngleAxis(90f, Vector3.right);
        }

        void FixedUpdate()
        {
            if (Vector3.Distance(_collider.transform.position, _vessel.transform.position) > triggerDistance)
                UpdatePosition();
            colliderHeight = Mathf.Clamp((colliderHeight -= 0.1f), -10, 2.5f);
        }
    }

    public class ModuleCameraShot : VesselModule
    {
        private bool takeHiResShot = false;
        private int resWidth = 6;
        private int resHeight = 6;
        public Color _averageColour = new Color(1,1,1,0.025f);
        int frameCount = 0;
        int threshHold = 1;
        Vessel _vessel;
        GameObject _cameraObject;
        

        void Start()
        {
            _vessel = GetComponent<Vessel>();
            _cameraObject = new GameObject("ColourCam");
            _cameraObject.transform.parent = _vessel.transform;
        }


        public static string ScreenShotName(int width, int height)
        {
            return string.Format("{0}/screen_{1}x{2}_{3}.png",
                                 Application.dataPath,
                                 width, height,
                                 System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        public void TakeHiResShot()
        {
            takeHiResShot = true;
        }

        //[KSPEvent(active=true,guiActive=true,guiName="Take Shot",name="Take Shot")]
        public void Update()
        {
            /*
            takeHiResShot |= Input.GetKeyDown("k");
            if (takeHiResShot && _vessel == FlightGlobals.ActiveVessel)
            {
             * */
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            if(frameCount >= threshHold && _vessel == FlightGlobals.ActiveVessel)
            {
                
                frameCount = 0;
                var _camera = _cameraObject.AddComponent<Camera>();
                _cameraObject.transform.position = _vessel.transform.position;
                _cameraObject.transform.LookAt(_vessel.mainBody.transform.position);
                _cameraObject.transform.Translate(new Vector3(0,0,-10));
                //Debug.LogError("created camera");
                
                RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
                _camera.targetTexture = rt;
                Texture2D groundShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
                _camera.Render();
                //Debug.LogError("rendered something...");
                RenderTexture.active = rt;
                groundShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
                _camera.targetTexture = null;
                RenderTexture.active = null; // JC: added to avoid errors
                Destroy(rt);
                Destroy(_camera);
                //var rb = rt.colorBuffer;

                Color[] texColors = groundShot.GetPixels();
                int total = texColors.Length;
                float divider = total * 1.25f;
                float r = 0;
                float g = 0;
                float b = 0;

                for (int i = 0; i < total; i++)
                {
                    r += texColors[i].r;
                    g += texColors[i].g;
                    b += texColors[i].b;
                }

                _averageColour = new Color(r / divider, g / divider, b / divider, 0.025f);
                //print("fired this frame" + _averageColour);

                //takeHiResShot = false;
                
            }
            frameCount++;
            timer.Stop();
            print(timer.Elapsed);
        }
    }
}

/*
var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visible.transform.parent = _cameraObject.transform;
                visible.transform.localScale = new Vector3(0.1f,0.1f,0.5f);
                visible.renderer.enabled = false;
*/

/*
byte[] bytes = groundShot.EncodeToPNG();
string filename = ScreenShotName(resWidth, resHeight);
Debug.LogError("about to write screenshot");
System.IO.File.WriteAllBytes(filename, bytes);
//KSP.IO.File.WriteAllBytes<RenderTexture>(bytes, filename, _vessel);
Debug.Log(string.Format("Took screenshot to: {0}", filename));
 * */