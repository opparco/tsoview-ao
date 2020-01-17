using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    public class DepthNormalMapRenderer
    {
        Device device;

        public Effect effect_dnclear;
        public Effect effect_dnmap;
        public Effect effect_depth;

        public Texture dmap_texture;
        public Texture nmap_texture;

        public Surface dmap_surface;
        public Surface nmap_surface;

        /// <summary>
        /// effect handle for LocalBoneMats
        /// since v0.90
        /// </summary>
        EffectHandle handle_LocalBoneMats;

        DepthNormalMapShader dnmap_shader = null;

        public DepthNormalMapRenderer(Device device)
        {
            this.device = device;

            techmap = new Dictionary<string, bool>();
            LoadTechMap();
        }

        public void Dispose()
        {
            if (dmap_surface != null)
                dmap_surface.Dispose();
            if (nmap_surface != null)
                nmap_surface.Dispose();

            if (dmap_texture != null)
                dmap_texture.Dispose();
            if (nmap_texture != null)
                nmap_texture.Dispose();
        }

        public void Create(Rectangle device_rect, Format dmap_format, Format nmap_format, NormalMapContainer nmap_container)
        {
            dmap_texture = new Texture(device, device_rect.Width, device_rect.Height, 1, Usage.RenderTarget, dmap_format, Pool.Default);
            dmap_surface = dmap_texture.GetSurfaceLevel(0);

            nmap_texture = new Texture(device, device_rect.Width, device_rect.Height, 1, Usage.RenderTarget, nmap_format, Pool.Default);
            nmap_surface = nmap_texture.GetSurfaceLevel(0);

            effect_depth.SetValue("DepthMap_texture", dmap_texture); // in

            handle_LocalBoneMats = effect_dnmap.GetParameter(null, "LocalBoneMats"); // shared

            dnmap_shader = new DepthNormalMapShader(effect_dnmap);
            dnmap_shader.FetchNormalMap += delegate (string name)
            {
                return nmap_container.GetDirect3DTexture(name);
            };
        }

        static string GetHideTechsPath()
        {
            return @"resources\dnmap-hidetechs.txt";
        }

        Dictionary<string, bool> techmap;

        void LoadTechMap()
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(GetHideTechsPath())))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    string[] tokens = line.Split(delim);
                    string op = tokens[0];
                    if (op == "hide")
                    {
                        Debug.Assert(tokens.Length == 2, "tokens length should be 2");
                        string techname = tokens[1];
                        techmap[techname] = true;
                    }
                }
            }
        }

        bool HiddenTechnique(string technique)
        {
            return techmap.ContainsKey(technique);
        }

        void DrawTSO(Figure fig, TSOFile tso)
        {
            dnmap_shader.current_shader = null;

            foreach (TSOMesh mesh in tso.meshes)
                foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                {
                    Debug.Assert(sub_mesh.spec >= 0 && sub_mesh.spec < tso.sub_scripts.Length, string.Format("mesh.spec out of range: {0}", sub_mesh.spec));
                    Shader shader = tso.sub_scripts[sub_mesh.spec].shader;

                    if (HiddenTechnique(shader.Technique))
                        continue;

                    //device.RenderState.VertexBlend = (VertexBlend)(4 - 1);
                    device.SetStreamSource(0, sub_mesh.vb, 0, 52);

                    dnmap_shader.SwitchShader(shader, tso.GetDirect3dTextureByName);
                    effect_dnmap.SetValue(handle_LocalBoneMats, fig.ClipBoneMatrices(sub_mesh)); // shared

                    int npass = effect_dnmap.Begin(0);
                    for (int ipass = 0; ipass < npass; ipass++)
                    {
                        effect_dnmap.BeginPass(ipass);
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, sub_mesh.vertices.Length - 2);
                        effect_dnmap.EndPass();
                    }
                    effect_dnmap.End();
                }
            dnmap_shader.current_shader = null;
        }

        void AssignWorldViewProjection(ref Matrix world)
        {
            Matrix world_view_matrix = world * device.Transform.View;
            Matrix world_view_projection_matrix = world_view_matrix * device.Transform.Projection;

            effect_dnmap.SetValue("wld", world); // shared
            effect_dnmap.SetValue("wv", world_view_matrix); // shared
            effect_dnmap.SetValue("wvp", world_view_projection_matrix); // shared
        }

        void DrawFigure(Figure fig)
        {
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                AssignWorldViewProjection(ref world);
            }
            foreach (TSOFile tso in fig.TsoList)
            {
                if (tso.Hidden)
                    continue;
                DrawTSO(fig, tso);
            }
        }

        // draw depthmap and normalmap
        // out dmap_surface
        // out nmap_surface
        public void Draw(List<Figure> FigureList)
        {
            foreach (Figure fig in FigureList)
            {
                if (fig.Hidden)
                    continue;
                DrawFigure(fig);
            }
        }
    }
}
