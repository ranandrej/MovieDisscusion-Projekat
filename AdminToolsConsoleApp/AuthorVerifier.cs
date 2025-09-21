using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminToolsConsoleApp
{
    public class AuthorVerifier
    {
        private readonly UsersTable _users = new UsersTable();

        public bool Verify(string email)
        {
            var key = email?.ToLowerInvariant();
            var u = _users.GetByEmail(key);
            if (u == null) return false;

            u.IsAuthorVerified = true;
            _users.Upsert(u);
            return true;
        }
    }
}
