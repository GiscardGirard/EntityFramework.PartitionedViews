using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class MemberTable
    {
        public DbContext DbContext { get; set; }
        public string DataRangeKey { get; set; }
        public Type DataType { get; set; }
    }
}
