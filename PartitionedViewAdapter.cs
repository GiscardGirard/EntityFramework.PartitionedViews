using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class PartitionedViewAdapter<T>:DbContext where T : class
    {
        private List<MemberTable> memberTables;

        public static string EMPTY_PARTITION_SUFFIX = "_Empty";

        public PartitionedViewConfiguration<T> Config { get; set; }
        public DatabaseAdapter Adapter { get; private set; }
        public MemberTableFactory<T> MemberTableFactory { get; private set; }
        public IDbSet<T> View { get; set; }

        internal PartitionedViewAdapter(PartitionedViewConfiguration<T> config, MemberTableFactory<T> memberTableFactory, DatabaseAdapter adapter)
            : base(config.ConnectionName)
        {
            Config = config;
            Adapter = adapter;
            MemberTableFactory = memberTableFactory;
            memberTables = GetDataRangeKeys().Select(MemberTableFactory.Create).ToList();
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            foreach (var tableContext in memberTables.Select(mt => mt.DbContext))
            {
                tableContext.Database.Initialize(true);
            }
            CreateView();
            modelBuilder.Entity<T>().ToTable(ViewName).HasKey(Config.PrimaryKeyExpression);
        }
        private void CreateView()
        {
            var keys = GetDataRangeKeys();
            if (keys.Count() > 1)
                keys = keys.Where(p => p != EMPTY_PARTITION_SUFFIX);
            var memberTableNames = keys.Select(dataRangeKey => PartitionTablePrefix + dataRangeKey);
            Adapter.CreateOrAlterPartitionedView(ViewName, memberTableNames);
        }      
        
        public void AddPartitionFor(T o)
        {
            var dataRangeKey = GetDataRangeKey(o);
            if (!memberTables.Any(mt=>mt.DataRangeKey==dataRangeKey))
            {
                var memberTable = MemberTableFactory.Create(dataRangeKey);
                memberTable.DbContext.Database.Initialize(true);
                AddCheckConstraint(o, dataRangeKey);
                memberTables.Add(memberTable);
                CreateView();
            }
        }
        
        
        public override int SaveChanges()
        {
            var objectsWritten = 0;
            foreach (var o in ChangeTracker.Entries<T>().Where(e=>e.State!=EntityState.Unchanged))
            {
                var dataRangeKey = GetDataRangeKey(o.Entity);
                var memberTable = memberTables.Single(mt => mt.DataRangeKey == dataRangeKey);
                var copy = CloneTo(o.Entity, memberTable.DataType);
                memberTable.DbContext.Entry(copy).State = o.State;
                objectsWritten += memberTable.DbContext.SaveChanges();                
                Copy(memberTable.DbContext.Entry(copy).Entity as T, o.Entity);

                if (o.State == EntityState.Deleted)
                    o.State = EntityState.Detached;
                else
                    o.State = EntityState.Unchanged;
            }
            return objectsWritten;
        }
       
        public Func<T, Type, object> CloneTo = (T from, Type type) =>
        {
            var to = Activator.CreateInstance(type);
            foreach (PropertyInfo sourcePropertyInfo in from.GetType().GetProperties())
            {
                PropertyInfo destPropertyInfo = type.GetProperty(sourcePropertyInfo.Name);
                destPropertyInfo.SetValue(to, sourcePropertyInfo.GetValue(from, null), null);
            }
            return to;
        };
        public Action<T,T> Copy = (T from, T to) =>
        {
            foreach (PropertyInfo sourcePropertyInfo in from.GetType().GetProperties())
            {
                PropertyInfo destPropertyInfo = from.GetType().GetProperty(sourcePropertyInfo.Name);
                destPropertyInfo.SetValue(to, sourcePropertyInfo.GetValue(from, null), null);
            }   
        };
        
        private IEnumerable<string> GetDataRangeKeys()
        {
            return Adapter
                .GetObjectNamesStartingWith(PartitionTablePrefix)
                .Select(p => p.Replace(PartitionTablePrefix, ""))
                .Concat(new[] { EMPTY_PARTITION_SUFFIX }).Distinct();
        }
        private string PartitionTablePrefix
        {
            get { return Config.DataType.Name; }
        }
        private string GetDataRangeKey(T o)
        {
            var dataRangeKey = Config.DataRangeKeyExpression.Compile()(o);
            var dataRangeKeyValues = dataRangeKey.GetType().GetProperties().Select(p => p.GetValue(dataRangeKey).ToString());
            return String.Concat(dataRangeKeyValues);
        }
        private IDictionary<string, object> GetDataRangeKeyAsDictionary(T o)
        {
            var dataRangeKey = Config.DataRangeKeyExpression.Compile()(o);
            return dataRangeKey.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(dataRangeKey));
        }
        private void AddCheckConstraint(T o, string dataRangeKey)
        {
            var dict = GetDataRangeKeyAsDictionary(o);
            foreach (var kv in dict)
            {
                Adapter.AddConstraintCheckIfEqual(PartitionTablePrefix + dataRangeKey, kv.Key, kv.Value);                
            }
        }
        private string ViewName
        {
            get { return String.Concat("View", Config.DataType.Name); }
        }
       
    }
}
