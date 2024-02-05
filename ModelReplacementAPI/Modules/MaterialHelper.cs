using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using ModelReplacement.Enemies;

namespace ModelReplacement.Modules
{
    public class MaterialHelper
    {


        private BodyReplacementBase bodyReplacement;
        private bool DontConvertUnsupportedShaders => bodyReplacement.DontConvertUnsupportedShaders;
        public MaterialHelper(BodyReplacementBase bodyreplacement)
        {
            this.bodyReplacement = bodyreplacement;
        }


        /// <summary> Shaders with any of these prefixes won't be automatically converted. </summary>
        private static readonly string[] shaderPrefixWhitelist =
        {
            "HDRP/",
            "GUI/",
            "Sprites/",
            "UI/",
            "Unlit/",
            "Toon",
            "lilToon",
            "Shader Graphs/",
            "Hidden/"
        };

        /// <summary>
        /// Get a replacement material based on the original game material, and the material found on the replacing model.
        /// </summary>
        /// <param name="gameMaterial">The equivalent material on the model being replaced.</param>
        /// <param name="modelMaterial">The material on the replacing model.</param>
        /// <returns>The replacement material created from the <see cref="gameMaterial"/> and the <see cref="modelMaterial"/></returns>
        public virtual Material GetReplacementMaterial(Material gameMaterial, Material modelMaterial)
        {

            if (DontConvertUnsupportedShaders || shaderPrefixWhitelist.Any(prefix => modelMaterial.shader.name.StartsWith(prefix)))
            {
                return modelMaterial;
            }
            else
            {
                ModelReplacementAPI.Instance.Logger.LogInfo($"Creating replacement material for material {modelMaterial.name} / shader {modelMaterial.shader.name}");
                // XXX Ideally this material would be manually destroyed when the replacement model is destroyed.

                Material replacementMat = new Material(gameMaterial);
                replacementMat.color = modelMaterial.color;
                replacementMat.mainTexture = modelMaterial.mainTexture;
                replacementMat.mainTextureOffset = modelMaterial.mainTextureOffset;
                replacementMat.mainTextureScale = modelMaterial.mainTextureScale;

                /*
                if (modelMaterial.HasTexture("_BaseColorMap"))
                {
                    replacementMat.SetTexture("_BaseColorMap", modelMaterial.GetTexture("_BaseColorMap"));
                }
                if (modelMaterial.HasTexture("_SpecularColorMap"))
                {
                    replacementMat.SetTexture("_SpecularColorMap", modelMaterial.GetTexture("_SpecularColorMap"));
                    replacementMat.EnableKeyword("_SPECGLOSSMAP");
                }
                if (modelMaterial.HasFloat("_Smoothness"))
                {
                    replacementMat.SetFloat("_Smoothness", modelMaterial.GetFloat("_Smoothness"));
                }
                if (modelMaterial.HasTexture("_EmissiveColorMap"))
                {
                    replacementMat.SetTexture("_EmissiveColorMap", modelMaterial.GetTexture("_EmissiveColorMap"));
                }
                if (modelMaterial.HasTexture("_BumpMap"))
                {
                    replacementMat.SetTexture("_BumpMap", modelMaterial.GetTexture("_BumpMap"));
                    replacementMat.EnableKeyword("_NORMALMAP");
                }
                if (modelMaterial.HasColor("_EmissiveColor"))
                {
                    replacementMat.SetColor("_EmissiveColor", modelMaterial.GetColor("_EmissiveColor"));
                    replacementMat.EnableKeyword("_EMISSION");
                }
                */
                replacementMat.EnableKeyword("_EMISSION");
                replacementMat.EnableKeyword("_NORMALMAP");
                replacementMat.EnableKeyword("_SPECGLOSSMAP");
                replacementMat.SetFloat("_NormalScale", 0);

                HDMaterial.ValidateMaterial(replacementMat);

                return replacementMat;
            }
        }


    }

    public class MaterialHelperEnemy
    {


        private EnemyReplacementBase bodyReplacement;
        private bool DontConvertUnsupportedShaders => bodyReplacement.DontConvertUnsupportedShaders;
        public MaterialHelperEnemy(EnemyReplacementBase bodyreplacement)
        {
            this.bodyReplacement = bodyreplacement;
        }


        /// <summary> Shaders with any of these prefixes won't be automatically converted. </summary>
        private static readonly string[] shaderPrefixWhitelist =
        {
            "HDRP/",
            "GUI/",
            "Sprites/",
            "UI/",
            "Unlit/",
            "Toon",
            "lilToon",
            "Shader Graphs/",
            "Hidden/"
        };

        /// <summary>
        /// Get a replacement material based on the original game material, and the material found on the replacing model.
        /// </summary>
        /// <param name="gameMaterial">The equivalent material on the model being replaced.</param>
        /// <param name="modelMaterial">The material on the replacing model.</param>
        /// <returns>The replacement material created from the <see cref="gameMaterial"/> and the <see cref="modelMaterial"/></returns>
        public virtual Material GetReplacementMaterial(Material gameMaterial, Material modelMaterial)
        {

            if (DontConvertUnsupportedShaders || shaderPrefixWhitelist.Any(prefix => modelMaterial.shader.name.StartsWith(prefix)))
            {
                return modelMaterial;
            }
            else
            {
                ModelReplacementAPI.Instance.Logger.LogInfo($"Creating replacement material for material {modelMaterial.name} / shader {modelMaterial.shader.name}");
                // XXX Ideally this material would be manually destroyed when the replacement model is destroyed.

                Material replacementMat = new Material(gameMaterial);
                replacementMat.color = modelMaterial.color;
                replacementMat.mainTexture = modelMaterial.mainTexture;
                replacementMat.mainTextureOffset = modelMaterial.mainTextureOffset;
                replacementMat.mainTextureScale = modelMaterial.mainTextureScale;

                /*
                if (modelMaterial.HasTexture("_BaseColorMap"))
                {
                    replacementMat.SetTexture("_BaseColorMap", modelMaterial.GetTexture("_BaseColorMap"));
                }
                if (modelMaterial.HasTexture("_SpecularColorMap"))
                {
                    replacementMat.SetTexture("_SpecularColorMap", modelMaterial.GetTexture("_SpecularColorMap"));
                    replacementMat.EnableKeyword("_SPECGLOSSMAP");
                }
                if (modelMaterial.HasFloat("_Smoothness"))
                {
                    replacementMat.SetFloat("_Smoothness", modelMaterial.GetFloat("_Smoothness"));
                }
                if (modelMaterial.HasTexture("_EmissiveColorMap"))
                {
                    replacementMat.SetTexture("_EmissiveColorMap", modelMaterial.GetTexture("_EmissiveColorMap"));
                }
                if (modelMaterial.HasTexture("_BumpMap"))
                {
                    replacementMat.SetTexture("_BumpMap", modelMaterial.GetTexture("_BumpMap"));
                    replacementMat.EnableKeyword("_NORMALMAP");
                }
                if (modelMaterial.HasColor("_EmissiveColor"))
                {
                    replacementMat.SetColor("_EmissiveColor", modelMaterial.GetColor("_EmissiveColor"));
                    replacementMat.EnableKeyword("_EMISSION");
                }
                */
                replacementMat.EnableKeyword("_EMISSION");
                replacementMat.EnableKeyword("_NORMALMAP");
                replacementMat.EnableKeyword("_SPECGLOSSMAP");
                replacementMat.SetFloat("_NormalScale", 0);

                HDMaterial.ValidateMaterial(replacementMat);

                return replacementMat;
            }
        }


    }
}
