using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    /// ������Ǘ����܂��B
    public class CommandManager
    {
        /// ���샊�X�g
        public List<ICommand> commands = new List<ICommand>();
        int command_id = 0;

        /// ������������܂��B
        public void ClearCommands()
        {
            commands.Clear();
            command_id = 0;
        }

        /// �ЂƂO�̑���ɂ��ύX�����ɖ߂��邩�B
        public bool CanUndo()
        {
            return (command_id > 0);
        }

        /// �ЂƂO�̑���ɂ��ύX�����ɖ߂��܂��B
        public void Undo()
        {
            if (!CanUndo())
                return;

            command_id--;
            Undo(commands[command_id]);
        }

        /// �w�葀��ɂ��ύX�����ɖ߂��܂��B
        public void Undo(ICommand command)
        {
            command.Undo();
        }

        /// �ЂƂO�̑���ɂ��ύX����蒼���邩�B
        public bool CanRedo()
        {
            return (command_id < commands.Count);
        }

        /// �ЂƂO�̑���ɂ��ύX����蒼���܂��B
        public void Redo()
        {
            if (!CanRedo())
                return;

            Redo(commands[command_id]);
            command_id++;
        }

        /// �w�葀��ɂ��ύX����蒼���܂��B
        public void Redo(ICommand command)
        {
            command.Redo();
        }

        /// �w�葀������s���܂��B
        public void Execute(ICommand command)
        {
            if (command.Execute())
            {
                if (command_id == commands.Count)
                    commands.Add(command);
                else
                    commands[command_id] = command;
                command_id++;
            }
        }
    }
}
