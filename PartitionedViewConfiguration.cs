using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PartitionedViews
{
    public class PartitionedViewConfiguration<T>
    {
        public Expression<Func<T, Object>> PrimaryKeyExpression { get; set; }
        public Expression<Func<T, Object>> DataRangeKeyExpression { get; set; }
        private IEnumerable<string> GetPropertyNamesFromKeyExpression(Expression<Func<T, Object>> keyProperty)
        {
            var newExpression = keyProperty.Body as NewExpression;

            if (newExpression != null)
            {
                return newExpression.Arguments.Cast<MemberExpression>().Select(e => e.Member.Name);
            }
            var memberExpression = keyProperty.Body as MemberExpression;
            return new string[] { memberExpression.Member.Name };
        }
        public IEnumerable<string> PrimaryKeyPropertyNames
        {
            get { return GetPropertyNamesFromKeyExpression(PrimaryKeyExpression); }
        }
        public IEnumerable<string> DataRangeKeyPropertyNames
        {
            get { return GetPropertyNamesFromKeyExpression(DataRangeKeyExpression); }
        }
        public Type DataType
        {
            get { return typeof(T); }
        }       
        public string ConnectionName
        {
            get { return String.Format("name={0}", typeof(T).Name); }
        }
    }
}
