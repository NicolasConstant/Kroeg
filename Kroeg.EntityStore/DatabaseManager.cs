using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace Kroeg.EntityStore
{
    public class DatabaseManager
    {
        private readonly DbConnection _connection;
        private readonly IConfiguration _configuration;

        public DatabaseManager(DbConnection connection, IConfiguration configuration)
        {
            _connection = connection;
            _configuration = configuration;

             _todo = new Dictionary<string, _migration>()
            {
                ["hello, world"] = _addOwnerTable,
                [""] = _createTables
            };
        }

        private class KroegMigrationEntry
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private delegate void _migration();
        private Dictionary<string, _migration> _todo;

        public void EnsureExists()
        {
            _connection.Execute("create table if not exists kroeg_migrations (\"Id\" serial primary key, \"Name\" text)");

            var migrations = _connection.Query<KroegMigrationEntry>("select * from kroeg_migrations order by \"Id\" desc");
            var lastMigration = migrations.FirstOrDefault()?.Name ?? "";
            using (var trans = _connection.BeginTransaction())
            {
                while (_todo.ContainsKey(lastMigration))
                    _todo[lastMigration]();

                trans.Commit();
            }
        }

        private void _createTables()
        {
            Console.WriteLine("[migration] Creating tables... ");

            _connection.Execute(@"create table ""Attributes"" (
                ""AttributeId"" serial primary key,
                ""Uri"" text not null unique
            );");

            _connection.Execute(@"create index on ""Attributes""(""Uri"")");

            _connection.Execute(@"create table ""TripleEntities"" (
                ""EntityId"" serial primary key,
                ""IdId"" int references ""Attributes""(""AttributeId""),
                ""Type"" text,
                ""Updated"" timestamp,
                ""IsOwner"" int references ""TripleEntities"" (""EntityId"")
            )");

            _connection.Execute(@"create index on ""TripleEntities""(""IdId"")");

            _connection.Execute(@"create table ""Triples"" (
                ""TripleId"" serial primary key,
                ""SubjectId"" int not null references ""Attributes""(""AttributeId""),
                ""SubjectEntityId"" int not null references ""TripleEntities""(""EntityId""),
                ""PredicateId"" int not null references ""Attributes""(""AttributeId""),
                ""AttributeId"" int references ""Attributes""(""AttributeId""),
                ""TypeId"" int references ""Attributes""(""AttributeId""),
                ""Object"" text
            );");

            _connection.Execute(@"create index on ""Triples""(""SubjectEntityId"")");

            _connection.Execute(@"create table ""CollectionItems"" (
                ""CollectionItemId"" serial primary key,
                ""CollectionId"" int not null references ""TripleEntities""(""EntityId""),
                ""ElementId"" int not null references ""TripleEntities""(""EntityId""),
                ""IsPublic"" boolean
            );");

            _connection.Execute(@"create index on ""CollectionItems""(""CollectionId"")");
            _connection.Execute(@"create index on ""CollectionItems""(""ElementId"")");

            _connection.Execute(@"create table ""UserActorPermissions"" (
                ""UserActorPermissionId"" serial primary key,
                ""UserId"" text not null,
                ""ActorId"" int not null references ""TripleEntities""(""EntityId""),
                ""IsAdmin"" boolean
            );");

            _connection.Execute(@"create table ""EventQueue"" (
                ""Id"" serial primary key,
                ""Added"" timestamp not null,
                ""NextAttempt"" timestamp not null,
                ""AttemptCount"" int not null,
                ""Action"" text not null,
                ""Data"" text not null
            );");

            _connection.Execute(@"create table ""SalmonKeys"" (
                ""SalmonKeyId"" serial primary key,
                ""EntityId"" int not null references ""TripleEntities""(""EntityId""),
                ""PrivateKey"" text not null
            );");

            _connection.Execute(@"create index on ""SalmonKeys""(""EntityId"")");

            _connection.Execute(@"create table ""WebsubSubscriptions"" (
                ""Id"" serial primary key,
                ""Expiry"" timestamp not null,
                ""Callback"" text not null,
                ""Secret"" text,
                ""UserId"" int not null references ""TripleEntities""(""EntityId"")
            );");

            _connection.Execute(@"create table ""WebSubClients"" (
                ""WebSubClientId"" serial primary key,
                ""ForUserId"" int not null references ""TripleEntities""(""EntityId""),
                ""TargetUserId"" int not null references ""TripleEntities""(""EntityId""),
                ""Topic"" text not null,
                ""Expiry"" timestamp not null,
                ""Secret"" text
            );");

            _connection.Execute(@"create table ""JsonWebKeys"" (
                ""Id"" text not null,
                ""OwnerId"" int not null references ""TripleEntities""(""EntityId""),
                ""SerializedData"" text not null,
                primary key(""Id"", ""OwnerId"")
            );");

            _connection.Execute(@"create table ""Users"" (
                ""Id"" text not null primary key,
                ""Username"" text,
                ""NormalisedUsername"" text,
                ""Email"" text,
                ""PasswordHash"" text
            );");

            _connection.Execute("insert into kroeg_migrations (\"Name\") values ('hello, world')");
            _connection.Execute("insert into kroeg_migrations (\"Name\") values ('kaaskop')");
        }

        private void _addOwnerTable()
        {
            Console.WriteLine("[migration] Adding KaaS support... ");

            int id = _connection.ExecuteScalar<int>("insert into \"Attributes\" (\"Uri\") values (@Uri) on conflict (\"Uri\") do update set \"Uri\" = @Uri returning \"AttributeId\"", new { Uri = "_instance" });
            int entity = _connection.ExecuteScalar<int>("insert into \"TripleEntities\" (\"IdId\", \"Type\", \"Updated\", \"IsOwner\") values (@Id, 'https://puckipedia.com/kroeg/ns#Server', @Updated, true)", new { Id = id, Updated = DateTime.Now });
            _connection.Execute(@"alter table ""TripleEntities"" alter column ""IsOwner"" type int using case when false then null else @Id end, add foreign key (""IsOwner"") references ""TripleEntities"" (""EntityId"")", new { Id = entity });

            _connection.Execute("insert into kroeg_migrations (\"Name\") values ('kaaskop')");
        }
    }
}