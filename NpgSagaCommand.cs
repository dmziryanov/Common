using Indusoft.CalendarPlanning.Common.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Indusoft.CalendarPlanning.Common
{
    public abstract class NpgSagaCommand : ISagaCommand
    {
        public IServiceProvider serviceProvider { get; set; }
        public string Dto { get; set; }
        public IMsgSender MsgSender { get; set; }
        public Guid Id { get; set; }

        public NpgSagaCommand()
        {

        }

        public abstract void Execute(DbConnection conn, DbTransaction transaction);


        public virtual void Execute()
        {
            try
            {
                using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    using NpgsqlConnection conn = new NpgsqlConnection(conf.GetConnectionString("DefaultConnection"));
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    Execute(conn, tran);
                    NpgsqlCommand command = new NpgsqlCommand($"PREPARE TRANSACTION '{Id}';", conn);
                    command.ExecuteNonQuery();
                    conn.Close();
                    MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Prepare, Id, false);
                }
            }

            catch (Exception ex)
            {
                MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Prepare, Id, false);
            }
        }

        public void Commit()
        {
            try
            {
                using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    NpgsqlConnection conn = new NpgsqlConnection(conf.GetConnectionString("DefaultConnection"));
                    conn.Open();
                    var cmd = new NpgsqlCommand($"COMMIT PREPARED '{Id}';", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Commit, Id, false);
                }
            }
            catch (Exception ex)
            {
                MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Commit, Id, true);
            }
        }

        public void Rollback()
        {
            try
            {
                using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    NpgsqlConnection conn = new NpgsqlConnection(conf.GetConnectionString("DefaultConnection"));
                    conn.Open();
                    var cmd = new NpgsqlCommand($"ROLLBACK PREPARED '{Id}';", conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Rollback, Id, true);
            }
        }
    }
}
