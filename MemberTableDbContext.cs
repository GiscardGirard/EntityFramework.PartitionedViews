using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class MemberTableDbContext<T>:DbContext where T:class
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.RegisterEntityType(DataType);
            modelBuilder.Types()
                .Where(t=>t == DataType)
                .Configure(c => c.HasKey(PrimaryKeyPropertyNames));
            base.OnModelCreating(modelBuilder);
        }
        public Type DataType
        {
            get { return typeof(T); }
        }
    
        public IEnumerable<string> PrimaryKeyPropertyNames {get; private set;}

        public string PartitionDataRange { get; private set; }

        public DbContext EmptyContext { get; private set; }
       
        public MemberTableDbContext(IEnumerable<string> primaryKeyPropertyNames, string connectionString,string suffix)
            : base(connectionString)
        {           
            PartitionDataRange = suffix;
            PrimaryKeyPropertyNames = primaryKeyPropertyNames;
            Database.SetInitializer<MemberTableDbContext<T>>(new MigrateDatabaseToLatestVersion<MemberTableDbContext<T>, Configuration<T>>(true));
        }
        
           
        
    }
}
