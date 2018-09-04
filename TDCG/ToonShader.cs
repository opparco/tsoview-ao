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

        public Shader current_shader = null;

        public ToonShader(Effect effect)
        {
            this.effect = effect;

            handle_ShadeTex_texture = effect.GetParameter(null, "ShadeTex_texture");
            handle_ColorTex_texture = effect.GetParameter(null, "ColorTex_texture");

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
        public void SwitchShader(Shader shader, Dictionary<string, Texture> d3d_texturemap)
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

            AssignTexture(shader.ShadeTexName, handle_ShadeTex_texture, d3d_texturemap);
            AssignTexture(shader.ColorTexName, handle_ColorTex_texture, d3d_texturemap);
        }

        void AssignTexture(string name, EffectHandle handle, Dictionary<string, Texture> d3d_texturemap)
        {
            Texture d3d_tex;
            if (name != null && d3d_texturemap.TryGetValue(name, out d3d_tex))
                effect.SetValue(handle, d3d_tex);
        }
    }
}
