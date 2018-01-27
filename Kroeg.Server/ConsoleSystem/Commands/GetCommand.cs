using System;
using System.Threading.Tasks;
using Kroeg.EntityStore.Store;
using Kroeg.EntityStore.Services;

namespace Kroeg.Server.ConsoleSystem.Commands
{
    public class GetCommand : IConsoleCommand
    {
        private readonly IEntityStore _entityStore;
        private readonly ServerConfig _serverConfig;

        public GetCommand(IEntityStore entityStore, ServerConfig serverConfig)
        {
            _entityStore = entityStore;
            _serverConfig = serverConfig;
        }

        public async Task Do(string[] args)
        {
            foreach (var item in args)
            {
                Console.WriteLine($"--- {item} ---");
                var data = await _entityStore.GetEntity(item, true);
                if (data == null)
                {
                    Console.WriteLine("not found");
                    continue;
                }

                Console.WriteLine($"--- IsOwner: {data.IsOwner}, LastUpdate: {data.Updated}, Type: {data.Type}, DbId {data.DbId}");
                Console.WriteLine(data.Data.Serialize(_serverConfig.Context, true).ToString());
                Console.WriteLine("--- ---");
            }
        }
    }
}
