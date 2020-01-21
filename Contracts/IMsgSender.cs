using System;
using System.Collections.Generic;
using System.Text;

namespace Indusoft.CalendarPlanning.Common.Contracts
{
    public interface IMsgSender
    {
        void Send(string commandName, string dto, SagaMessageType msgType, Guid id, bool error);
    }

    public enum SagaMessageType
    {
        Prepare,
        Commit,
        Rollback
    }

    public interface ISagaCommand
    {
        Guid Id { get; set; }
        string Dto { get; set; }
        void Execute();
        void Rollback();
        void Commit();
        IMsgSender MsgSender { get; set; }
        IServiceProvider serviceProvider { get; set; }
    }
}
