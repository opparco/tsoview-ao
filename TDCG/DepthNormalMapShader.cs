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

        public Shader current_shader = null;

        public DepthNormalMapShader(Effect effect)
        {
            this.effect = effect;

            handle_ColorTex_texture = effect.GetParameter(null, "ColorTex_texture");
        }

        /// <summary>
        /// dnmap 向けにシェーダ設定を切り替えます。
        /// 色テクスチャのみ切り替えます。
        /// </summary>
        /// <param name="shader">シェーダ設定</param>
        public void SwicthShader(Shader shader, Dictionary<string, Texture> d3d_texturemap)
        {
            if (shader == current_shader)
                return;
            current_shader = shader;

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
