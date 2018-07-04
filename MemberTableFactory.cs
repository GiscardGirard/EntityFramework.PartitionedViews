using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class MemberTableFactory<T>
    {
        public PartitionedViewConfiguration<T> Config { get; private set; }
        public MemberTableFactory(PartitionedViewConfiguration<T> config)
        {
            Config = config;
        }        
        public MemberTable Create(string dataRangeKey)
        {
            var memberTable = new MemberTable();
            memberTable.DataRangeKey = dataRangeKey;
            memberTable.DataType = CreatePartitionTableType(Config.DataType,dataRangeKey);
            memberTable.DbContext = CreateTableContext(memberTable.DataType, dataRangeKey);
            return memberTable;
        }
        private DbContext CreateTableContext(Type type, string dataRangeKey)
        {            
            var contextType = typeof(MemberTableDbContext<>).MakeGenericType(type);
            var context = (DbContext)Activator.CreateInstance(contextType, Config.PrimaryKeyPropertyNames, Config.ConnectionName, dataRangeKey);
            return context;
        }
        private Type CreatePartitionTableType(Type partitionedViewType, string suffix)
        {
            var asm = new AssemblyName(String.Concat(partitionedViewType.Name, "PartitionedViewMemberTables"));
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asm, AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule("MemberTables");
            var typeName = String.Concat(partitionedViewType.Name, suffix);
            var typeBuilder = moduleBuilder.DefineType(typeName);
            typeBuilder.SetParent(partitionedViewType);
            return typeBuilder.CreateType();
        }
    }
}
