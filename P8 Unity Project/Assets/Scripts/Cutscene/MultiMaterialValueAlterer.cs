using System;
using UnityEngine;

namespace Cutscene
{
    public class MultiMaterialValueAlterer : MonoBehaviour
    {
        [Header("Hardcoded these to optimize for animation")]
        [SerializeField] private Material[] allMaterials;
        [SerializeField] private bool[] doShadow;
        [SerializeField] private bool[] doSpecular;
        [SerializeField] private bool[] doEmission;
        public bool updateValues = false;
        public float shadowCutoffValue = 1f;
        public float shadowSoftCutoffValue = 1f;
        public Color shadowColor = Color.black;
        public float specularValue = 1f;
        public Color emissionColor = Color.black;


        // Update is called once per frame
        void Update()
        {
            if (!updateValues)
                return;

            for (int i = 0; i < allMaterials.Length; i++)
            {
                Material mat = allMaterials[i];

                if (doShadow[i])
                {
                    mat.SetFloat("_ShadowCutoff", shadowCutoffValue);
                    mat.SetFloat("_ShadowSoftCutoff", shadowSoftCutoffValue);
                    mat.SetColor("_ShadowColor", shadowColor);
                }

                if (doSpecular[i])
                    mat.SetFloat("_SpecularCutoff", specularValue);

                if (doEmission[i])
                    mat.SetColor("_EmissionColor", emissionColor);
            }
        }
    }
}
