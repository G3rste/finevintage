using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace FineVintage
{
    public class FineVintage : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockEntityClass("WineBarrel", typeof(BlockEntityWineBarrel));

            api.RegisterBlockClass("WineBarrel", typeof(BlockWineBarrel));
        }
    }
}
