using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TDCG
{
    /// ボーンを抽出する
    public class NodeFilter
    {
        //顔以外のボーン名称の短い形式配列
        string[] not_face_nodenames;

        //顔のボーン名称の短い形式配列
        string[] face_nodenames;

        public NodeFilter()
        {
        }

        public void LoadNotFaceNodes(string nodes_path)
        {
            List<string> names = new List<string>();
            using (StreamReader source = new StreamReader(File.OpenRead(nodes_path)))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    names.Add(line);
                }
            }
            not_face_nodenames = new string[names.Count];
            names.CopyTo(not_face_nodenames);
        }

        public void LoadFaceNodes(string nodes_path)
        {
            List<string> names = new List<string>();
            using (StreamReader source = new StreamReader(File.OpenRead(nodes_path)))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    names.Add(line);
                }
            }
            face_nodenames = new string[names.Count];
            names.CopyTo(face_nodenames);
        }

        TMONode[] GetNodes(TMOFile tmo, string[] nodenames)
        {
            TMONode[] nodes = new TMONode[nodenames.Length];
            int idx = 0;
            foreach (string nodename in nodenames)
            {
                TMONode node = tmo.FindNodeByName(nodename); // nullable
                if (node != null)
                    nodes[idx++] = node;
            }
            Array.Resize(ref nodes, idx);
            return nodes;
        }

        //顔以外のボーン配列を得る
        public TMONode[] GetNotFaceNodes(TMOFile tmo)
        {
            return GetNodes(tmo, not_face_nodenames);
        }

        //顔のボーン配列を得る
        public TMONode[] GetFaceNodes(TMOFile tmo)
        {
            return GetNodes(tmo, face_nodenames);
        }
    }
}
