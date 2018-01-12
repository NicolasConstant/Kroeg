using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Kroeg.EntityStore.Models;
using System.Data;
using Dapper;
using System.Transactions;
using System.Data.Common;
using Kroeg.EntityStore.Services;

namespace Kroeg.EntityStore
{
    public abstract class BaseTask
    {
        protected EventQueueItem EventQueueItem;

        protected BaseTask(EventQueueItem item)
        {
            EventQueueItem = item;
        }

        public static async Task Go(DbConnection connection, EventQueueItem item, IServiceProvider provider, DbTransaction transaction)
        {
            var serverConfig = provider.GetService<ServerConfig>();
            serverConfig.Prepare(item.Context);

            var type = Type.GetType(item.Action);
            BaseTask resolved = null;

            try
            {
                if (type != null)
                {
                    resolved = (BaseTask)ActivatorUtilities.CreateInstance(provider, type, item);
                    await resolved.Go();
                }

                await connection.ExecuteAsync("DELETE from \"EventQueue\" where \"Id\" = @Id", new { Id = item.Id });

                transaction.Commit();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                // failed
                item.AttemptCount++;
                item.NextAttempt = resolved.NextTry(item.AttemptCount);

                transaction.Rollback();
                await connection.ExecuteAsync("UPDATE \"EventQueue\" set \"AttemptCount\"=@AttemptCount, \"NextAttempt\"=@NextAttempt where \"Id\" = @Id", new { Attemptcount = item.AttemptCount, NextAttempt = item.NextAttempt, Id = item.Id });
            }

            transaction.Dispose();
        }

        public virtual DateTime NextTry(int fails)
        {
            return DateTime.Now.AddMinutes(fails * fails * 3);
        }

        public abstract Task Go();
    }

    public abstract class BaseTask<T, TR> : BaseTask where TR : BaseTask<T, TR>
    {
        protected T Data;

        public static async Task Make(T data, DbConnection connection, APEntity context, DateTime? nextAttempt = null)
        {
            var type = typeof(TR).AssemblyQualifiedName;

            await connection.ExecuteAsync("insert into \"EventQueue\" (\"Action\", \"Added\", \"Data\", \"NextAttempt\", \"Context\", \"AttemptCount\") values (@Action, @Added, @Data, @NextAttempt, @Context, 0)",
                new EventQueueItem
                {
                    Action = type,
                    Added = DateTime.Now,
                    Context = context.DbId,
                    Data = JsonConvert.SerializeObject(data),
                    NextAttempt = nextAttempt ?? DateTime.Now
                });

            await connection.ExecuteAsync("select pg_notify('kroeg', @Val)", new { Val = JsonConvert.SerializeObject(new { Path = BackgroundTaskQueuer.BackgroundTaskPath, Value = "ye" }) });
        }

        protected BaseTask(EventQueueItem item)
            : base(item)
        {
            Data = JsonConvert.DeserializeObject<T>(EventQueueItem.Data);
        }
    }
}
