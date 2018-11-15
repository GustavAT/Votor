using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Votor
{
    public static class Util
    {
        public static Guid? ParseGuid(string source)
        {
            return Guid.TryParse(source, out var guid) ? guid : (Guid?) null;
        }
    }
}
