
namespace TDCG
{
    /// 操作を扱います。
    public interface ICommand
    {
        /// 元に戻す。
        void Undo();

        /// やり直す。
        void Redo();

        /// 実行する。
        bool Execute();
    }

    /// node操作
    public class NodeCommand : ICommand
    {
        public NodeCommand(TMONode node)
        {
        }

        /// 元に戻す。
        public void Undo()
        {
        }

        /// やり直す。
        public void Redo()
        {
        }

        /// 実行する。
        public bool Execute()
        {
            return false;
        }
    }

    /// pose操作
    public class PoseCommand : ICommand
    {
        public PoseCommand(Figure fig)
        {
        }

        /// 元に戻す。
        public void Undo()
        {
        }

        /// やり直す。
        public void Redo()
        {
        }

        /// 実行する。
        public bool Execute()
        {
            return false;
        }
    }
}
