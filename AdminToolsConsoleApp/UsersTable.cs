using Common;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminToolsConsoleApp
{
    public class UsersTable
    {
        private readonly CloudTable _users = Storage.GetTable("Users");
        private CloudTable Table => Storage.GetTable("Users");

        public IEnumerable<string> GetAllEmails()
        {
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "User");
            var query = new TableQuery<UserEntity>().Where(filter);

            // RowKey = email po tvojoj šemi
            return Table.ExecuteQuery(query).Select(u => u.RowKey).OrderBy(x => x);
        }
        public UserEntity GetByEmail(string emailLower)
        {
            var res = _users.Execute(TableOperation.Retrieve<UserEntity>("User", emailLower));
            return res.Result as UserEntity;
        }

        public void Upsert(UserEntity user)
        {
            _users.Execute(TableOperation.InsertOrReplace(user));
        }
    }
}
