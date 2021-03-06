using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// <summary>
    /// シェーダ設定の型名
    /// </summary>
    public enum ShaderParameterType
    {
        /// <summary>
        /// わからない
        /// </summary>
        Unknown,
        /// <summary>
        /// string
        /// </summary>
        String,
        /// <summary>
        /// float
        /// </summary>
        Float,
        /// <summary>
        /// float3
        /// </summary>
        Float3,
        /// <summary>
        /// float4
        /// </summary>
        Float4,
        /// <summary>
        /// テクスチャ
        /// </summary>
        Texture
    };

    /// <summary>
    /// シェーダ設定パラメータ
    /// </summary>
    public class ShaderParameter
    {
        internal ShaderParameterType type;
        internal string name;

        string str;
        float f1;
        float f2;
        float f3;
        float f4;
        int dim = 0;

        //Effect.SetValue で指定できる name か
        internal bool assignable = false;

        /// <summary>
        /// パラメータの名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// float値1
        /// </summary>
        public float F1 { get { return f1; } set { f1 = value; } }
        /// <summary>
        /// float値2
        /// </summary>
        public float F2 { get { return f2; } set { f2 = value; } }
        /// <summary>
        /// float値3
        /// </summary>
        public float F3 { get { return f3; } set { f3 = value; } }
        /// <summary>
        /// float値4
        /// </summary>
        public float F4 { get { return f4; } set { f4 = value; } }
        /// <summary>
        /// float次元数
        /// </summary>
        public int Dimension { get { return dim; } }

        /// <summary>
        /// シェーダ設定ファイルの行を解析してシェーダ設定パラメータを生成します。
        /// </summary>
        /// <param name="line">シェーダ設定ファイルの行</param>
        /// <returns>シェーダ設定パラメータ</returns>
        public static ShaderParameter Parse(string line)
        {
            int m = line.IndexOf('='); if (m < 0) throw new ArgumentException();
            string type_name = line.Substring(0,m);
            string value = line.Substring(m+1).Trim();
            m = type_name.IndexOf(' '); if (m < 0) throw new ArgumentException();
            string type = type_name.Substring(0,m);
            string name = type_name.Substring(m+1).Trim();

            return new ShaderParameter(type, name, value);
        }

        /// <summary>
        /// シェーダ設定パラメータを生成します。
        /// </summary>
        public ShaderParameter()
        {
        }

        /// <summary>
        /// シェーダ設定パラメータを生成します。
        /// </summary>
        /// <param name="type_string">型名</param>
        /// <param name="name">名称</param>
        /// <param name="value">値</param>
        public ShaderParameter(string type_string, string name, string value)
        {
            this.name = name;

            switch (type_string)
            {
            case "string":
                type = ShaderParameterType.String;
                SetString(value);
                break;
            case "float":
                type = ShaderParameterType.Float;
                SetFloat(value);
                break;
            case "float3":
                type = ShaderParameterType.Float3;
                SetFloat3(value);
                break;
            case "float4":
                type = ShaderParameterType.Float4;
                SetFloat4(value);
                break;
            case "texture":
                type = ShaderParameterType.Texture;
                SetTexture(value);
                break;
            default:
                type = ShaderParameterType.Unknown;
                break;
            }
        }

        /// 文字列として表現します。
        public override string ToString()
        {
            return GetTypeName() + " " + name + " = " + GetValueString();
        }

        /// 型名を文字列として得ます。
        public string GetTypeName()
        {
            switch (type)
            {
                case ShaderParameterType.String:
                    return "string";
                case ShaderParameterType.Float:
                    return "float";
                case ShaderParameterType.Float3:
                    return "float3";
                case ShaderParameterType.Float4:
                    return "float4";
                case ShaderParameterType.Texture:
                    return "texture";
            }
            return null;
        }

        /// <summary>
        /// 値を文字列として得ます。
        /// </summary>
        public string GetValueString()
        {
            switch (type)
            {
                case ShaderParameterType.String:
                    return "\"" + str + "\"";
                case ShaderParameterType.Float:
                    return string.Format("[{0}]", f1);
                case ShaderParameterType.Float3:
                    return string.Format("[{0}, {1}, {2}]", f1, f2, f3);
                case ShaderParameterType.Float4:
                    return string.Format("[{0}, {1}, {2}, {3}]", f1, f2, f3, f4);
                case ShaderParameterType.Texture:
                    return str;
            }
            return str;
        }

        /// <summary>
        /// 文字列を取得します。
        /// </summary>
        /// <returns>文字列</returns>
        public string GetString()
        {
            return str;
        }

        /// <summary>
        /// 文字列を設定します。
        /// </summary>
        /// <param name="value">文字列表現</param>
        public void SetString(string value)
        {
            str = value.Trim('"', ' ', '\t');
        }

        static Regex re_float_array = new Regex(@"\s*,\s*|\s+");

        /// <summary>
        /// float値の配列を設定します。
        /// </summary>
        /// <param name="value">float配列値の文字列表現</param>
        /// <param name="dim">次元数</param>
        public void SetFloatDim(string value, int dim)
        {
            string[] token = re_float_array.Split(value.Trim('[', ']', ' ', '\t'));
            this.dim = dim;
            if (token.Length > 0)
                f1 = float.Parse(token[0].Trim());
            if (token.Length > 1)
                f2 = float.Parse(token[1].Trim());
            if (token.Length > 2)
                f3 = float.Parse(token[2].Trim());
            if (token.Length > 3)
                f4 = float.Parse(token[3].Trim());
        }

        /// <summary>
        /// float値を取得します。
        /// </summary>
        /// <returns>float値</returns>
        public float GetFloat()
        {
            return f1;
        }
        /// <summary>
        /// float値を設定します。
        /// </summary>
        /// <param name="value">float値の文字列表現</param>
        public void SetFloat(string value)
        {
            try
            {
                SetFloatDim(value, 1);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float): " + value);
            }
        }

        /// <summary>
        /// float3値を取得します。
        /// </summary>
        /// <returns>float3値</returns>
        public Vector3 GetFloat3()
        {
            return new Vector3(f1, f2, f3);
        }
        /// <summary>
        /// float3値を設定します。
        /// </summary>
        /// <param name="value">float3値の文字列表現</param>
        public void SetFloat3(string value)
        {
            try
            {
                SetFloatDim(value, 3);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float3): " + value);
            }
        }

        /// <summary>
        /// float4値を取得します。
        /// </summary>
        /// <returns>float4値</returns>
        public Vector4 GetFloat4()
        {
            return new Vector4(f1, f2, f3, f4);
        }
        /// <summary>
        /// float4値を設定します。
        /// </summary>
        /// <param name="value">float4値の文字列表現</param>
        public void SetFloat4(string value)
        {
            try
            {
                SetFloatDim(value, 4);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float4): " + value);
            }
        }

        /// <summary>
        /// テクスチャ名を取得します。
        /// </summary>
        /// <returns>テクスチャ名</returns>
        public string GetTexture()
        {
            return str;
        }
        /// <summary>
        /// テクスチャ名を設定します。
        /// </summary>
        /// <param name="value">テクスチャ名</param>
        public void SetTexture(string value)
        {
            str = value;
        }
    }

    /// <summary>
    /// シェーダ設定
    /// </summary>
    public class Shader
    {
        /// <summary>
        /// シェーダ設定パラメータの配列
        /// </summary>
        public ShaderParameter[] shader_parameters;

        //internal string     description;     // = "TA ToonShader v0.50"
        //internal string     shader;          // = "TAToonshade_050.cgfx"
        string technique;       // = "ShadowOn"
        //internal Vector4    shadowColor;     // = [0, 0, 0, 1]
        string shade_tex;        // = Ninjya_Ribbon_Toon_Tex
        //internal float      highLight;       // = [0]
        //internal float      colorBlend;      // = [10]
        //internal float      highLightBlend;  // = [10]
        //internal Vector4    penColor;        // = [0.166, 0.166, 0.166, 1]
        //internal float      ambient;         // = [38]
        string color_tex;        // = file24
        //internal float      thickness;       // = [0.018]
        //internal float      shadeBlend;      // = [10]
        //internal float      highLightPower;  // = [100]
        string normal_map = "nmap";
        string environment_map = "emap";

        static Dictionary<string, string> techmap;

        public static void LoadTechMap(string techmap_path)
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(techmap_path)))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                        continue;

                    string[] tokens = line.Split(delim);
                    {
                        Debug.Assert(tokens.Length == 2, "tokens length should be 2");
                        string name = tokens[0];
                        string assumed_name = tokens[1];
                        techmap[name] = assumed_name;
                    }
                }
            }
        }

        static Dictionary<string, bool> namemap;

        public static void LoadNameMap(string namemap_path)
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(namemap_path)))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                        continue;

                    string[] tokens = line.Split(delim);
                    {
                        Debug.Assert(tokens.Length == 2 || tokens.Length == 3, "tokens length should be 2 or 3");
                        string type = tokens[0];
                        string name = tokens[1];
                        namemap[name] = true;
                    }
                }
            }
        }

        static Shader()
        {
            techmap = new Dictionary<string, string>();
            namemap = new Dictionary<string, bool>();
        }

        static string RenameTechnique(string name)
        {
            string assumed_name;
            if (techmap.TryGetValue(name, out assumed_name))
                return assumed_name;
            else
                return name;
        }

        static bool IsAssignableName(string name)
        {
            return namemap.ContainsKey(name);
        }

        public string Technique { get { return technique; } }

        /// <summary>
        /// 陰テクスチャのファイル名
        /// </summary>
        public string ShadeTex { get { return shade_tex; } }
        /// <summary>
        /// 色テクスチャのファイル名
        /// </summary>
        public string ColorTex { get { return color_tex; } }
        /// <summary>
        /// 法線マップのファイル名
        /// </summary>
        public string NormalMap { get { return normal_map; } }
        /// 環境マップのファイル名
        /// </summary>
        public string EnvironmentMap { get { return environment_map; } }

        /// <summary>
        /// シェーダ設定を読み込みます。
        /// </summary>
        /// <param name="lines">テキスト行配列</param>
        public void Load(string[] lines)
        {
            shader_parameters = new ShaderParameter[lines.Length];
            int i = 0;
            foreach (string line in lines)
            {
                ShaderParameter p = ShaderParameter.Parse(line);
                switch(p.name)
                {
                    case "description":
                        break;
                    case "shader":
                        break;
                    case "technique":
                        technique = RenameTechnique(p.GetString());
                        break;
                    case "LightDirX":
                    case "LightDirY":
                    case "LightDirZ":
                    case "LightDirW":
                    case "LightDir":
                        break;
                    case "ShadeTex":
                        shade_tex = p.GetString();
                        break;
                    case "ColorTex":
                        color_tex = p.GetString();
                        break;
                    case "NormalMap":
                        normal_map = p.GetString();
                        break;
                    case "EnvironmentMap":
                        environment_map = p.GetString();
                        break;
                    default:
                        p.assignable = IsAssignableName(p.name);
                        break;
                }
                shader_parameters[i++] = p;
            }
            Array.Resize(ref shader_parameters, i);
        }

        /// <summary>
        /// シェーダ設定を文字列の配列として得ます。
        /// </summary>
        public string[] GetLines()
        {
            string[] lines = new string[shader_parameters.Length];
            int i = 0;
            foreach (ShaderParameter p in shader_parameters)
            {
                lines[i++] = p.ToString();
            }
            Array.Resize(ref lines, i);
            return lines;
        }
    }
}
