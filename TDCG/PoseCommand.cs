using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX;

namespace TDCG
{
    struct TMONodeData
    {
        public Vector3  Scaling;
        public Quaternion       Rotation;
        public Vector3  Translation;
    }

    /// node操作
    public class NodeCommand : ICommand
    {
        Figure figure;
        string nodename;
        TMONodeData old_data;
        TMONodeData new_data;

        public NodeCommand(Figure figure, string nodename)
        {
            this.figure = figure;
            this.nodename = nodename;
            TMONode node = figure.Tmo.FindNodeByName(nodename);
            old_data.Scaling    = node.Scaling;
            old_data.Rotation   = node.Rotation;
            old_data.Translation        = node.Translation;
        }

        /// 元に戻す。
        public void Undo()
        {
            TMONode node = figure.Tmo.FindNodeByName(nodename);
            node.Scaling        = old_data.Scaling;
            node.Rotation       = old_data.Rotation;
            node.Translation    = old_data.Translation;
            figure.UpdateBoneMatrices();
        }

        /// やり直す。
        public void Redo()
        {
            TMONode node = figure.Tmo.FindNodeByName(nodename);
            node.Scaling        = new_data.Scaling;
            node.Rotation       = new_data.Rotation;
            node.Translation    = new_data.Translation;
            figure.UpdateBoneMatrices();
        }

        /// 実行する。
        public bool Execute()
        {
            TMONode node = figure.Tmo.FindNodeByName(nodename);
            new_data.Scaling    = node.Scaling;
            new_data.Rotation   = node.Rotation;
            new_data.Translation        = node.Translation;

            return !old_data.Equals(new_data);
        }
    }

    /// pose操作
    public class PoseCommand : ICommand
    {
        Figure figure;
        Dictionary<string, TMONodeData> old_datamap = new Dictionary<string, TMONodeData>();
        Dictionary<string, TMONodeData> new_datamap = new Dictionary<string, TMONodeData>();

        public PoseCommand(Figure figure)
        {
            this.figure = figure;
            foreach (TMONode node in figure.Tmo.nodes)
            {
                TMONodeData old_data;
                old_data.Scaling        = node.Scaling;
                old_data.Rotation       = node.Rotation;
                old_data.Translation    = node.Translation;
                old_datamap[node.Name] = old_data;
            }
        }

        /// 元に戻す。
        public void Undo()
        {
            foreach (var pair in old_datamap)
            {
                TMONode node = figure.Tmo.FindNodeByName(pair.Key);
                node.Scaling    = pair.Value.Scaling;
                node.Rotation   = pair.Value.Rotation;
                node.Translation        = pair.Value.Translation;
            }
            figure.UpdateBoneMatrices();
        }

        /// やり直す。
        public void Redo()
        {
            foreach (var pair in new_datamap)
            {
                TMONode node = figure.Tmo.FindNodeByName(pair.Key);
                node.Scaling    = pair.Value.Scaling;
                node.Rotation   = pair.Value.Rotation;
                node.Translation        = pair.Value.Translation;
            }
            figure.UpdateBoneMatrices();
        }

        /// 実行する。
        public bool Execute()
        {
            bool updated = false;
            foreach (var pair in old_datamap)
            {
                TMONode node = figure.Tmo.FindNodeByName(pair.Key);
                TMONodeData new_data;
                new_data.Scaling        = node.Scaling;
                new_data.Rotation       = node.Rotation;
                new_data.Translation    = node.Translation;
                new_datamap[pair.Key] = new_data;
                updated = updated || !pair.Value.Equals(new_data);
            }

            return updated;
        }
    }
}
