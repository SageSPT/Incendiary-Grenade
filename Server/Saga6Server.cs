using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace IncendiaryGrenadeServer;

[Injectable(TypePriority = OnLoadOrder.Preload + 2)]
public class Saga6Server(
    ISptLogger<Saga6Server> logger,
    CustomItemService customItemService,
    TradersTable traders) : IOnLoad
{
    private const string ItemId      = "6aabad0ff49370bbdc709173";
    private const string RgnId       = "617fd91e5539a84ec44ce155";
    private const string ThrowWeapId = "543be6564bdc2df4348b4568";
    private const string GrenadesHb  = "5b5f7a2386f774093f2ed3c4";
    private const string BundleKey   = "assets/content/weapons/saga6/weapon_grenade_saga6_container.bundle";

    private const string PraporId    = "54cb50c76803fa8b248b4571";
    private const string OfferId     = "6aabad0f138b16b5efab6b2d";
    private const string RoublesId   = "5449016a4bdc2d6f028b456f";

    private const int    PriceRoubles    = 15000;
    private const int    PraporLoyalty   = 2;
    private const int    StockPerRestock = 10;
    private const int    BuyLimit        = 5;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var details = new NewItemFromCloneDetails
        {
            ItemTplToClone = RgnId,
            ParentId = ThrowWeapId,
            NewId = ItemId,
            NewItemName = "weapon_grenade_saga6",
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab { Path = BundleKey, Rcid = "" },

                Strength = 0,
                MaxExplosionDistance = 0,
                ContusionDistance = 0,
                FragmentsCount = 0,
                Contusion = new Vector3(0, 0, 0),
                ArmorDistanceDistanceDamage = new Vector3(0, 0, 0),
                ExplosionEffectType = "Grenade_new",

                CanPlantOnGround = false,

                PlayFuzeSound = true,
            },
            HandbookParentId = GrenadesHb,
            HandbookPriceRoubles = 13900,
            FleaPriceRoubles = 16000,
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "SAGA-6 Incendiary Grenade",
                    ShortName = "SAGA-6",
                    Description =
                        "Swedish incendiary grenade made by Sagiku for area denial. Ignites on impact and floods the " +
                        "area with fire burning at around 1,200°C for roughly 15 seconds, forcing anyone out of cover. " +
                        "Throw it well clear of yourself.",
                },
            },
        };

        CreateItemResult result = customItemService.CreateItemFromClone(details);
        if (!result.Success)
        {
            logger.Error($"[Incendiary Grenade] Could not create the SAGA-6: {string.Join("; ", result.Errors ?? new List<string>())}");
            return Task.CompletedTask;
        }

        AddToPrapor();
        return Task.CompletedTask;
    }

    private void AddToPrapor()
    {
        if (!traders.TryGetValue(PraporId, out Trader? prapor) || prapor?.Assort == null)
        {
            logger.Warning("[Incendiary Grenade] Prapor not found, so the SAGA-6 is not sold.");
            return;
        }

        var offer = new TraderAssort
        {
            Items = new List<Item>
            {
                new Item
                {
                    Id = OfferId,
                    Template = ItemId,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = new Upd
                    {
                        UnlimitedCount = false,
                        StackObjectsCount = StockPerRestock,
                        BuyRestrictionMax = BuyLimit,
                        BuyRestrictionCurrent = 0,
                    },
                },
            },
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>
            {
                [OfferId] = new List<List<BarterScheme>>
                {
                    new List<BarterScheme> { new BarterScheme { Count = PriceRoubles, Template = RoublesId } },
                },
            },
            LoyalLevelItems = new Dictionary<MongoId, int> { [OfferId] = PraporLoyalty },
        };

        prapor.Assort.MergeAssorts(offer);
    }
}
