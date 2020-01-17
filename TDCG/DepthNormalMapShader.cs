using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public class DepthNormalMapShader
    {
        Effect effect;

        EffectHandle handle_ColorTex_texture;
        EffectHandle handle_NormalMap_texture;

        public Func<string, Texture> FetchNormalMap;

        public Shader current_shader = null;

        public DepthNormalMapShader(Effect effect)
        {
            this.effect = effect;

            handle_ColorTex_texture = effect.GetParameter(null, "ColorTex_texture");
            handle_NormalMap_texture = effect.GetParameter(null, "NormalMap_texture");
        }

        /// <summary>
        /// dnmap 向けにシェーダ設定を切り替えます。
        /// 色テクスチャのみ切り替えます。
        /// </summary>
        /// <param name="shader">シェーダ設定</param>
        public void SwitchShader(Shader shader, Func<string, Texture> fetch_d3d_texture)
        {
            if (shader == current_shader)
                return;
            current_shader = shader;

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

        void AssignTexture(string name, EffectHandle handle, Dictionary<string, Texture> d3d_texturemap)
        {
            Texture d3d_tex;
            if (name != null && d3d_texturemap.TryGetValue(name, out d3d_tex))
                effect.SetValue(handle, d3d_tex);
        }
    }
}
