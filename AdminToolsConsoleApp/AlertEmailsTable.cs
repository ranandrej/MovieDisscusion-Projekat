using Common;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.AlterEmailEntity;

namespace AdminToolsConsoleApp
{
    public class AlertEmailsTable
    {
        private readonly CloudTable _table = Storage.GetTable("AlertEmails");

        public void Add(string emailLower)
        {
            _table.Execute(TableOperation.InsertOrReplace(new AlertEmailEntity(emailLower)));
        }

        public void Remove(string emailLower)
        {
            var res = _table.Execute(TableOperation.Retrieve<AlertEmailEntity>("Alert", emailLower));
            if (res.Result is AlertEmailEntity e)
                _table.Execute(TableOperation.Delete(e));
        }
    }
}
