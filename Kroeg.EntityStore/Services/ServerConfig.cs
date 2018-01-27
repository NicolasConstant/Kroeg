using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kroeg.ActivityStreams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Kroeg.EntityStore.Store;
using Kroeg.EntityStore.Models;
using System.Data;
using Dapper;
using System.Data.Common;
using System.Diagnostics;

namespace Kroeg.EntityStore.Services
{
    public class ServerConfig
    {
        private static Dictionary<int, APEntity> _servers = null;

        private readonly DbConnection _database;
        private readonly TripleEntityStore _entityStore;

        private APEntity _currentServer;

        public APEntity CurrentServer { get { return _currentServer; } }

        public ServerConfig(IConfigurationSection kroegSection, TripleEntityStore entityStore, DbConnection database)
        {
            _kroegSection = kroegSection;
            _entityStore = entityStore;
            _database = database;
        }

        public async Task Prepare(Uri request)
        {
            if (_servers == null) await _preloadServers();
            _currentServer = null;
            foreach (var server in _servers.Values)
            {
                var url = server.Data["url"].FirstOrDefault()?.Id;
                if (url == null) continue;

                if ((new Uri(url)).IsBaseOf(request))
                {
                    _currentServer = server;
                    break;
                }
            }

            if (_currentServer == null && _servers.Count == 1)
                _currentServer = _servers.First().Value;

            _entityStore.CurrentServer = _currentServer.DbId;
        }

        internal void Prepare(int dbId)
        {
            Debug.Assert(_servers.ContainsKey(dbId));

            _entityStore.CurrentServer = dbId;
            _currentServer = _servers[dbId];
        }

        public async Task Prime(APEntity entity)
        {
            var idAttr = (await _entityStore.ReverseAttribute(entity.Id, true)).Value;
            var obj = await _database.ExecuteScalarAsync<int>("insert into \"TripleEntities\" (\"IdId\", \"IsOwner\", \"Type\") values (@IdId, null, 'https://puckipedia.com/kroeg/ns#Server') returning \"EntityId\"", new { IdId = idAttr });
            await _database.ExecuteAsync("update \"TripleEntities\" set \"IsOwner\" = @Id where \"EntityId\" = @Id", new { Id = obj });

            _entityStore.CurrentServer = obj;
            entity.DbId = obj;

            await _entityStore.StoreEntity(entity);
        }

        internal async Task ForcePreload() => await _preloadServers();

        internal static void UpdateServer(APEntity server)
        {
            _servers[server.DbId] = server;
        }

        private async Task _preloadServers()
        {
            var servers = await _database.QueryAsync<APTripleEntity>("select * from \"TripleEntities\" where \"Type\" = 'https://puckipedia.com/kroeg/ns#Server'");
            _servers = (await _entityStore.GetEntities(servers.Select(a => a.EntityId).ToList())).ToDictionary(a => a.DbId, a => a);
            _database.Close();
        }

        private readonly IConfigurationSection _kroegSection;
        public string BaseUri => _currentServer.Data["url"].First().Id;
        public string BaseDomain => (new Uri(BaseUri)).Host;
        public string BasePath => (new Uri(BaseUri)).AbsolutePath;
        public string Context => BaseUri + "render/context";

        public string BaseUriForServer(int id) { return _servers.ContainsKey(id) ? _servers[id].Data["url"].First().Id : null; }


        public bool RewriteRequestScheme => _kroegSection["RewriteRequestScheme"] == "True";
        public bool UnflattenRemotely => _kroegSection["UnflattenRemotely"] == "True";

        public static readonly List<Type> ServerToServerHandlers = new List<Type>();
        public static readonly List<Type> ClientToServerHandlers = new List<Type>();
        public static readonly List<IConverterFactory> Converters = new List<IConverterFactory>();
    }
}
