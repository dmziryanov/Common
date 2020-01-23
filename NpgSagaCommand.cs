using Indusoft.CalendarPlanning.Common.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Indusoft.CalendarPlanning.Common
{
    public abstract class SagaCommandBase : ISagaCommand
    {
        public IServiceProvider serviceProvider { get; set; }
        public ISagaDbProvider DbProvider { get; set; }
        public string Dto { get; set; }
        public IMsgSender MsgSender { get; set; }
        public Guid Id { get; set; }

        public SagaCommandBase()
        {
         
        }

        public abstract void Execute(DbConnection conn, DbTransaction transaction);


        public void Execute()
        {
            try
            {
                using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
                {
                    DbProvider = serviceProvider.GetService<ISagaDbProvider>();
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    DbProvider.Execute(conf.GetConnectionString("DefaultConnection"), Id, Execute);
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
                    DbProvider = serviceProvider.GetService<ISagaDbProvider>();
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    DbProvider.Commit(conf.GetConnectionString("DefaultConnection"), Id);
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
                    DbProvider = serviceProvider.GetService<ISagaDbProvider>();
                    var conf = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                    DbProvider.Commit(conf.GetConnectionString("DefaultConnection"), Id);
                    MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Commit, Id, false);
                }
            }
            catch (Exception ex)
            {
                MsgSender.Send(this.GetType().Name, Dto, SagaMessageType.Rollback, Id, true);
            }
        }
    }

    public class NpgSagaDbProvider : ISagaDbProvider
    {

        public virtual void Execute(string ConnectionString, Guid Id, Action<DbConnection, DbTransaction> bl)
        {
            using NpgsqlConnection conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            bl(conn, tran);
            NpgsqlCommand command = new NpgsqlCommand($"PREPARE TRANSACTION '{Id}';", conn);
            command.ExecuteNonQuery();
            conn.Close();
        }

        public void Commit(string ConnectionString, Guid Id)
        {
            using NpgsqlConnection conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var cmd = new NpgsqlCommand($"COMMIT PREPARED '{Id}';", conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Rollback(string ConnectionString, Guid Id)
        {
            using NpgsqlConnection conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var cmd = new NpgsqlCommand($"ROLLBACK PREPARED '{Id}';", conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public DbContextOptionsBuilder<T> GetOptions<T>(DbConnection conn) where T : DbContext
        {
            DbContextOptionsBuilder<T> optsContextOptions = new DbContextOptionsBuilder<T>();
            optsContextOptions.UseNpgsql(conn);
            return optsContextOptions;
        }
    }

    public interface ISagaDbProvider
    {
        void Execute(string ConnectionString, Guid Id, Action<DbConnection, DbTransaction> Excecute);
        void Rollback(string ConnectionString, Guid Id);
        void Commit(string ConnectionString, Guid Id);
        DbContextOptionsBuilder<T> GetOptions<T>(DbConnection ConnectionString) where T : DbContext;
    }
}
