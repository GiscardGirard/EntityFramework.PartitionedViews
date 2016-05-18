using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class PartitionedViewAdapterFactory
    {
        public virtual PartitionedViewAdapter<T> Create<T>(PartitionedViewConfiguration<T> config) where T : class
        {
            return new PartitionedViewAdapter<T>(config, CreateMemberTableFactory(config), CreateDatabaseAdapter(config.ConnectionName));
        }
        public virtual DatabaseAdapter CreateDatabaseAdapter(string nameOrConnectionString)
        {
            return new DatabaseAdapter(nameOrConnectionString);
        }
        public virtual MemberTableFactory<T> CreateMemberTableFactory<T>(PartitionedViewConfiguration<T> config)
        {
            return new MemberTableFactory<T>(config);
        }
    }
}
