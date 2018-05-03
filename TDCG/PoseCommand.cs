
namespace TDCG
{
    /// ����������܂��B
    public interface ICommand
    {
        /// ���ɖ߂��B
        void Undo();

        /// ��蒼���B
        void Redo();

        /// ���s����B
        bool Execute();
    }

    /// node����
    public class NodeCommand : ICommand
    {
        public NodeCommand(TMONode node)
        {
        }

        /// ���ɖ߂��B
        public void Undo()
        {
        }

        /// ��蒼���B
        public void Redo()
        {
        }

        /// ���s����B
        public bool Execute()
        {
            return false;
        }
    }

    /// pose����
    public class PoseCommand : ICommand
    {
        public PoseCommand(Figure fig)
        {
        }

        /// ���ɖ߂��B
        public void Undo()
        {
        }

        /// ��蒼���B
        public void Redo()
        {
        }

        /// ���s����B
        public bool Execute()
        {
            return false;
        }
    }
}
