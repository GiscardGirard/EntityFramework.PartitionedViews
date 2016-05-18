using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{

    public class DatabaseAdapter
    {
        private DbContext emptyContext = null;        
        public DatabaseAdapter(string nameOrConnectionStringName)
        {
            emptyContext = new DbContext(nameOrConnectionStringName);
        }
        public virtual void CreateOrAlterPartitionedView(string viewName,IEnumerable<string> memberTableNames)
        {
            var createOrAlter = ObjectExists(viewName) ? "alter" : "create";

            var selects = String.Join(" union all ",
                memberTableNames.Select(tableName => "select * from " + tableName));
            emptyContext.Database.ExecuteSqlCommand(createOrAlter + " view " + viewName + " as " + selects);
        }
        public virtual bool ObjectExists(string objectName)
        {
            var select = String.Format("select name from sysobjects where name='{0}'", objectName);
            var o = emptyContext.Database.SqlQuery<string>(select);
            return o.Any();
        }
        public virtual IEnumerable<string> GetObjectNamesStartingWith(string prefix)
        {
            return emptyContext.Database
               .SqlQuery<string>(String.Format(
               "select name from sysobjects where name like '{0}%'", prefix));
        }
        public virtual void AddConstraintCheckIfEqual(string tableName, string columnName, object value)
        {
            var cmd = String.Format("alter table {0} add check({1}={2})",
                   tableName,columnName, value);
            emptyContext.Database.ExecuteSqlCommand(cmd);
        }
    }
}
