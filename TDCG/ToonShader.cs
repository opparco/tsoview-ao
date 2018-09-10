using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public class ToonShader
    {
        Effect effect;

        EffectHandle[] techniques;
        Dictionary<string, EffectHandle> techmap;

        EffectHandle handle_ShadeTex_texture;
        EffectHandle handle_ColorTex_texture;
        EffectHandle handle_NormalMap_texture;

        public delegate Texture FetchNormalMapHandler(string name);
        public FetchNormalMapHandler FetchNormalMap;

        public Shader current_shader = null;

        public ToonShader(Effect effect)
        {
            this.effect = effect;

            handle_ShadeTex_texture = effect.GetParameter(null, "ShadeTex_texture");
            handle_ColorTex_texture = effect.GetParameter(null, "ColorTex_texture");
            handle_NormalMap_texture = effect.GetParameter(null, "NormalMap_texture");

            techmap = new Dictionary<string, EffectHandle>();

            int ntech = effect.Description.Techniques;
            techniques = new EffectHandle[ntech];

            //Console.WriteLine("Techniques:");

            for (int i = 0; i < ntech; i++)
            {
                techniques[i] = effect.GetTechnique(i);
                string tech_name = effect.GetTechniqueDescription(techniques[i]).Name;
                techmap[tech_name] = techniques[i];

                //Console.WriteLine(i + " " + tech_name);
            }
        }

        /// <summary>
        /// シェーダ設定を切り替えます。
        /// </summary>
        /// <param name="shader">シェーダ設定</param>
        public void SwitchShader(Shader shader, Func<string, Texture> fetch_d3d_texture)
        {
            if (shader == current_shader)
                return;
            current_shader = shader;

            try
            {
                effect.Technique = techmap[shader.Technique];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Error: shader technique not found. " + shader.Technique);
                return;
            }
            effect.ValidateTechnique(effect.Technique);

            foreach (ShaderParameter p in shader.shader_parameters)
            {
                if (p.system_p)
                    continue;

                switch (p.type)
                {
                case ShaderParameterType.String:
                    effect.SetValue(p.name, p.GetString());
                    break;
                case ShaderParameterType.Float:
                case ShaderParameterType.Float3:
                case ShaderParameterType.Float4:
                    effect.SetValue(p.name, new float[]{ p.F1, p.F2, p.F3, p.F4 });
                    break;
                    /*
                case ShaderParameter.Type.Texture:
                    effect.SetValue(p.name, p.GetTexture());
                    break;
                    */
                }
            }

            {
                Texture d3d_tex = fetch_d3d_texture(shader.ShadeTex);
                if (d3d_tex != null)
                {
                    effect.SetValue(handle_ShadeTex_texture, d3d_tex);
                }
            }

            {
                Texture d3d_tex = fetch_d3d_texture(shader.ColorTex);
                if (d3d_tex != null)
                {
                    effect.SetValue(handle_ColorTex_texture, d3d_tex);
                }
            }
            if (FetchNormalMap != null)
            {
                Texture d3d_tex = FetchNormalMap(shader.NormalMap);
                if (d3d_tex != null)
                {
                    effect.SetValue(handle_NormalMap_texture, d3d_tex);
                }
            }
        }
    }
}
