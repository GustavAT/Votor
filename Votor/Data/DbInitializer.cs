using Votor.Areas.Portal.Data;

namespace Votor.Data
{
    public class DbInitializer
    {
        public static void Initialize(VotorContext context)
        {
            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }
}
