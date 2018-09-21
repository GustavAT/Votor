using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Votor.Services
{
    public static class Utility
    {
        /// <summary>
        /// Generates a username from given <see cref="email"/>.
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Username</returns>
        public static string GenerateUserNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;

            var index = email.LastIndexOf('@');

            if (index < 0) return email;

            return email.Substring(0, index);
        }
    }
}
