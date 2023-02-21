using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coretronic.Reality.UI
{
    /** \brief CoreParticleGazeCursor is to control cursor animation of CoreGazePointer module. */
    public class CoreParticleGazeCursor : MonoBehaviour
    {
        /** \brief The CoreGazePointer module. */
        public CoreGazePointer gazePointer;

        /** \brief The emission scale. */
        public float emissionScale;

        /** \brief The max speed of emission. */
        public float maxSpeed;
        
        [Header("Particle emission curves")]

        /** 
         * \brief Curve for trailing edge of pointer. 
         * 
         * The scale on the x axis of the curves runs from 0 to maxSpeed.
         */
        [Tooltip("Curve for trailing edge of pointer")]
        public AnimationCurve halfEmission;

        /** 
         * \brief Curve for full perimeter of pointer. 
         * 
         * The scale on the x axis of the curves runs from 0 to maxSpeed.
         */
        [Tooltip("Curve for full perimeter of pointer")]
        public AnimationCurve fullEmission;
        
        /** \brief Enable particle emission curves. */
        [Tooltip("Curve for full perimeter of pointer")]
        public bool particleTrail;

        /** \brief The scale of particle emission curves. */
        public float particleScale = 0.68f;

        Vector3 lastPos;
        ParticleSystem psHalf;
        ParticleSystem psFull;
        MeshRenderer quadRenderer;
        Color particleStartColor;

        // Use this for initialization
        void Start()
        {
            foreach (Transform child in transform)
            {
                if (child.name.Equals("Half"))
                    psHalf = child.GetComponent<ParticleSystem>();
                if (child.name.Equals("Full"))
                    psFull = child.GetComponent<ParticleSystem>();
                if (child.name.Equals("Quad"))
                    quadRenderer = child.GetComponent<MeshRenderer>();
            }
            float scale = transform.lossyScale.x;
            var psHalfMain = psHalf.main;
            psHalfMain.startSize = psHalfMain.startSize.constant * scale;
            psHalfMain.startSpeed = psHalfMain.startSpeed.constant * scale;
            var psFullMain = psFull.main;
            psFullMain.startSize = psFullMain.startSize.constant * scale;
            psFullMain.startSpeed = psFullMain.startSpeed.constant * scale;
            particleStartColor = psFullMain.startColor.color;

            if (!particleTrail)
            {
                GameObject.Destroy(psHalf);
                GameObject.Destroy(psFull);
            }
        }

        // Update is called once per frame
        void Update()
        {
            var delta = gazePointer.positionDelta;

            if (particleTrail)
            {
                // Evaluate these curves to decide the emission rate of the two sources of particles.
                var psHalfEmission = psHalf.emission;
                psHalfEmission.rateOverTime = halfEmission.Evaluate((delta.magnitude / Time.deltaTime) / maxSpeed) * emissionScale;
                var psFullEmission = psFull.emission;
                psFullEmission.rateOverTime = fullEmission.Evaluate((delta.magnitude / Time.deltaTime) / maxSpeed) * emissionScale;
                // Make the particles fade out with visibitly the same way the main ring does
                Color color = particleStartColor;
                color.a = gazePointer.visibilityStrength;
                var psHalfMain = psHalf.main;
                psHalfMain.startColor = color;
                var psFullMain = psFull.main;
                psFullMain.startColor = color;
                psHalfMain.startSize = particleScale * transform.lossyScale.x;
                psFullMain.startSize = particleScale * transform.lossyScale.x;
            }

            // Set the main pointers alpha value to the correct level to achieve the desired level of fade
            quadRenderer.material.SetColor("_TintColor",new Color(1, 1, 1, gazePointer.visibilityStrength));
        }
    }
}